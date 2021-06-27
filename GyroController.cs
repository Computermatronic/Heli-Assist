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
            const float dampeningFactor = 25.0f;

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
                float proportionalModifier = (float)Math.Pow(Math.Abs(diffrence.Length()), 2);

                float pitch = NotNaN(Vector3.Dot(diffrence, Vector3.Cross(gravity, controller.WorldMatrix.Right)) * velocity) * proportionalModifier / dampeningFactor;
                float roll = NotNaN(Vector3.Dot(diffrence, Vector3.Cross(gravity, controller.WorldMatrix.Forward)) * velocity) * proportionalModifier / dampeningFactor;

                pitch = MinAbs(pitch, 90.0f * degToRad);
                roll = MinAbs(roll, 90.0f * degToRad);

                return new Vector2(roll, pitch);
            }

            public Vector3 CalculateVelocityToAlign(float offsetPitch = 0.0f, float offsetRoll = 0.0f)
            {
                var gravity = -Vector3D.Normalize(controller.GetNaturalGravity());

                var pitch = Vector3.Dot(controller.WorldMatrix.Forward, gravity) - offsetPitch;
                var roll = Vector3.Dot(controller.WorldMatrix.Right, gravity) + offsetRoll;

                return new Vector3(pitch, 0, roll);
            }

            public void SetAngularVelocity(Vector3 velocity)
            {
                var cockpitLocalVelocity = Vector3.TransformNormal(velocity, controller.WorldMatrix);
                foreach (var gyro in gyroscopes)
                {
                    var gyroLocalVelocity = Vector3.TransformNormal(cockpitLocalVelocity, Matrix.Transpose(gyro.WorldMatrix));

                    gyro.Pitch = gyroLocalVelocity.X;
                    gyro.Yaw = gyroLocalVelocity.Y;
                    gyro.Roll = gyroLocalVelocity.Z;
                }
            }
        }
    }
}