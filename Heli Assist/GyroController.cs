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
        //The GyroController module is based on Flight Assist's GyroController and HoverModule, sharing code in places.
        public class GyroController
        {
            const float gyroVelocity = 2.0f;
            const float dampeningFactor = 0.5f / 9.87f;

            private IMyShipController controller;
            private List<IMyGyro> gyroscopes;

            public GyroController(IMyShipController controller, List<IMyGyro> gyroscopes)
            {
                this.controller = controller;
                this.gyroscopes = new List<IMyGyro>(gyroscopes);
            }

            public void Update(IMyShipController controller, List<IMyGyro> gyroscopes)
            {
                SetController(controller);
                AddGyroscopes(gyroscopes);
            }

            public void AddGyroscopes(List<IMyGyro> gyroscopes)
            {
                this.gyroscopes.AddList(gyroscopes);
                this.gyroscopes = this.gyroscopes.Distinct().ToList();
            }

            public void SetController(IMyShipController controller)
            {
                this.controller = controller;
            }

            public void SetEnabled(bool setEnabled)
            {
                foreach (var gyroscope in gyroscopes)
                {
                    gyroscope.Enabled = setEnabled;
                }
            }

            public void SetOverride(bool setOverride)
            {
                foreach (var gyroscope in gyroscopes)
                {
                    gyroscope.GyroOverride = setOverride;
                }
            }

            public Vector2 CalculatePitchRollToAchiveVelocity(Vector3 targetVelocity)
            {
                Vector3 diffrence = Vector3.Normalize(controller.GetShipVelocities().LinearVelocity - targetVelocity);
                Vector3 gravity = -Vector3.Normalize(controller.GetNaturalGravity());
                float velocity = (float)controller.GetShipSpeed();

                float pitch = NotNaN(Vector3.Dot(diffrence, Vector3.Cross(gravity, controller.WorldMatrix.Right)) * velocity) * gyroVelocity * dampeningFactor;
                float roll = NotNaN(Vector3.Dot(diffrence, Vector3.Cross(gravity, controller.WorldMatrix.Forward)) * velocity) * gyroVelocity * dampeningFactor;

                pitch = MinAbs(pitch, 180.0f * degToRad);
                roll = MinAbs(roll, 180.0f * degToRad);

                return new Vector2(roll, pitch);
            }

            public Vector3 CalculateVelocityToAlign(float offsetPitch = 0.0f, float offsetRoll = 0.0f)
            {
                Matrix shipOrientation = controller.WorldMatrix.GetOrientation();
                Matrix controllerOrientation; controller.Orientation.GetMatrix(out controllerOrientation);

                Vector3 target = Vector3.Transform(controllerOrientation.Down, Quaternion.CreateFromAxisAngle(controllerOrientation.Left,
                    offsetPitch) * Quaternion.CreateFromAxisAngle(controllerOrientation.Backward, offsetRoll));
                Vector3 gravity = Vector3.Transform(-Vector3.Normalize(controller.GetNaturalGravity()), Matrix.Transpose(controller.CubeGrid.WorldMatrix.GetOrientation()));

                return Vector3.Cross(target, gravity);
            }

            public void SetAngularVelocity(Vector3 velocity)
            {
                foreach (var gyroscope in gyroscopes)
                {
                    Matrix localOrientation; gyroscope.Orientation.GetMatrix(out localOrientation);
                    Vector3 localVelocity = Vector3.Transform(velocity, Matrix.Transpose(localOrientation));
                    gyroscope.Pitch = (float)localVelocity.X;
                    gyroscope.Roll = (float)localVelocity.Z;
                    gyroscope.Yaw = (float)localVelocity.Y;
                }
            }
        }
    }
}