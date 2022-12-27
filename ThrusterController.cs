using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class ThrusterController
        {
            private IMyShipController controller;
            private List<IMyThrust> allThrusters;
            private List<IMyThrust> upThrusters, downThrusters, leftThrusters, rightThrusters, forwardThrusters, backwardThrusters;

            public ThrusterController(IMyShipController controller, List<IMyThrust> thrusters)
            {
                upThrusters = new List<IMyThrust>();
                downThrusters = new List<IMyThrust>();
                leftThrusters = new List<IMyThrust>();
                rightThrusters = new List<IMyThrust>();
                forwardThrusters = new List<IMyThrust>();
                backwardThrusters = new List<IMyThrust>();

                Update(controller, thrusters);
            }

            public void Update(IMyShipController controller, List<IMyThrust> thrusters)
            {
                this.controller = controller;
                this.allThrusters = thrusters.Distinct().ToList();

                foreach (var thruster in thrusters)
                {
                    if (thruster.GridThrustDirection.Z < 0) forwardThrusters.Add(thruster);
                    if (thruster.GridThrustDirection.Z > 0) backwardThrusters.Add(thruster);
                    if (thruster.GridThrustDirection.Y < 0) upThrusters.Add(thruster);
                    if (thruster.GridThrustDirection.Y > 0) downThrusters.Add(thruster);
                    if (thruster.GridThrustDirection.X < 0) leftThrusters.Add(thruster);
                    if (thruster.GridThrustDirection.X > 0) rightThrusters.Add(thruster);

                    thruster.ThrustOverride = 0;
                }

                forwardThrusters = forwardThrusters.Distinct().ToList();
                backwardThrusters = backwardThrusters.Distinct().ToList();
                upThrusters = upThrusters.Distinct().ToList();
                downThrusters = downThrusters.Distinct().ToList();
                leftThrusters = leftThrusters.Distinct().ToList();
                rightThrusters = rightThrusters.Distinct().ToList();
            }

            public void SetEnabled(bool enabled)
            {
                foreach (var thruster in allThrusters)
                {
                    thruster.Enabled = enabled;
                }
            }

            public float SetZAxisThrust(float thrust)
            {
                return setAxisThrust(thrust, ref forwardThrusters, ref backwardThrusters);
            }

            public float SetYAxisThrust(float thrust)
            {
                return setAxisThrust(thrust, ref upThrusters, ref downThrusters);
            }

            public float SetXAxisThrust(float thrust)
            {
                return setAxisThrust(thrust, ref leftThrusters, ref rightThrusters);
            }

            public float CalculateMaxEffectiveForwardThrust()
            {
                return calculateMaxAxisThrust(ref forwardThrusters);
            }

            public float CalculateMaxEffectiveBackwardThrust()
            {
                return calculateMaxAxisThrust(ref backwardThrusters);
            }

            public float CalculateMaxEffectiveLeftThrust()
            {
                return calculateMaxAxisThrust(ref leftThrusters);
            }

            public float CalculateMaxEffectiveRightThrust()
            {
                return calculateMaxAxisThrust(ref rightThrusters);
            }

            public float CalculateMaxEffectiveUpThrust()
            {
                return calculateMaxAxisThrust(ref upThrusters);
            }

            public float CalculateMaxEffectiveDownThrust()
            {
                return calculateMaxAxisThrust(ref downThrusters);
            }

            public float CalculateThrustToHover()
            {
                var gravityDir = controller.GetNaturalGravity();
                var weight = controller.CalculateShipMass().PhysicalMass * gravityDir.Length();
                var velocity = controller.GetShipVelocities().LinearVelocity;

                gravityDir.Normalize();
                var gravityMatrix = Matrix.Invert(Matrix.CreateFromDir(gravityDir));
                velocity = Vector3D.Transform(velocity, gravityMatrix);


                if (Vector3.Transform(controller.WorldMatrix.GetOrientation().Down, gravityMatrix).Z < 0)
                    return (float)(weight + weight * -velocity.Z);
                else
                    return -(float)(weight + weight * -velocity.Z);
            }

            private float calculateMaxAxisThrust(ref List<IMyThrust> thrusters)
            {
                float thrust = 0;
                foreach (var thruster in thrusters)
                {
                    thrust += thruster.MaxEffectiveThrust;
                }
                return thrust;
            }

            private float calculateEffectiveThustRatio(IMyThrust thruster)
            {
                return thruster.MaxThrust / thruster.MaxEffectiveThrust;
            }

            private float setAxisThrust(float thrust, ref List<IMyThrust> thrustersPos, ref List<IMyThrust> thrustersNeg)
            {
                List<IMyThrust> thrusters, backThrusters;

                if (thrust >= 0)
                {
                    thrusters = thrustersPos;
                    backThrusters = thrustersNeg;
                }
                else
                {
                    thrusters = thrustersNeg;
                    backThrusters = thrustersPos;
                }

                thrust = Math.Abs(thrust);

                foreach (var thruster in backThrusters)
                {
                    thruster.ThrustOverride = 0.0f;
                }

                foreach (var thruster in thrusters)
                {
                    //TODO: replace with smart thruster thrust allocation code.
                    var localThrust = (thrust / thrusters.Count) * calculateEffectiveThustRatio(thruster);
                    thruster.ThrustOverride = (float.IsNaN(localThrust) || float.IsInfinity(localThrust)) ? 0 : localThrust;
                }
                return 0.0f;
            }
        }
    }
}
