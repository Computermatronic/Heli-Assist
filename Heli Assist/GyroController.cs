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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        //The GyroController module is based on Flight Assist's GyroController and HoverModule, sharing code in places.
        public class GyroController
        {
            const double minGyroRpmScale = 0.001 / 9.87;
            const double gyroVelocityScale = 0.2 / 9.87;


            private readonly List<IMyGyro> gyros;
            private readonly IMyShipController controller;


            public GyroController(IMyShipController controller, List<IMyGyro> gyros)
            {
                this.gyros = gyros;
                this.controller = controller;

                foreach (var gyro in gyros)
                {
                    gyro.Pitch = 0f;
                    gyro.Roll = 0f;
                    gyro.Yaw = 0f;
                    gyro.GyroPower = 1f;
                }
            }

            public void SetEnabled(bool enabled)
            {
                foreach (var gyro in gyros)
                {
                    gyro.Enabled = enabled;
                }
            }

            public void SetGyroOverride(bool gyroOverride)
            {
                foreach (var gyro in gyros)
                {
                    gyro.GyroOverride = gyroOverride;
                }
            }

            public Vector2 CalculatePitchRollToStop(float maxPitch, float maxRoll)
            {
                Vector2 localVelocity;

                Vector3 worldVelocity = Vector3.Normalize(controller.GetShipVelocities().LinearVelocity);
                Vector3 gravity = -Vector3D.Normalize(controller.GetNaturalGravity());

                localVelocity.Y = Vector3.Dot(worldVelocity, Vector3.Cross(gravity, controller.WorldMatrix.Right)) * (float)controller.GetShipSpeed();
                localVelocity.X = Vector3.Dot(worldVelocity, Vector3.Cross(gravity, controller.WorldMatrix.Forward)) * (float)controller.GetShipSpeed();

                localVelocity.X = double.IsNaN(localVelocity.X) ? 0 : localVelocity.X;
                localVelocity.Y = double.IsNaN(localVelocity.Y) ? 0 : localVelocity.Y;

                localVelocity *= (float)gyroVelocityScale * 4;

                localVelocity.X = Math.Abs(localVelocity.X) < maxRoll ? localVelocity.X : localVelocity.X > 0 ? maxRoll : -maxRoll;
                localVelocity.Y = Math.Abs(localVelocity.Y) < maxPitch ? localVelocity.Y : localVelocity.Y > 0 ? maxPitch : -maxPitch;

                return localVelocity;
            }

            public void SetPitchRoll(float pitch, float roll, Vector3 velocity)
            {
                Matrix matrix; controller.Orientation.GetMatrix(out matrix);
                Vector3 reference = Vector3D.Transform(matrix.Down, Quaternion.CreateFromAxisAngle(matrix.Left, pitch) * Quaternion.CreateFromAxisAngle(matrix.Backward, roll));
                Vector3 target = controller.GetNaturalGravity();

                foreach (var gyro in gyros)
                {
                    gyro.Orientation.GetMatrix(out matrix);
                    matrix = Matrix.Transpose(matrix);

                    var localReference = Vector3D.Transform(reference, (MatrixD)matrix);
                    var localVelocity = Vector3D.Transform(velocity, (MatrixD)matrix);
                    var localTarget = Vector3D.Transform(target, MatrixD.Transpose(gyro.WorldMatrix.GetOrientation()));

                    var axis = Vector3D.Cross(localReference, localTarget);
                    var angle = axis.Length();

                    angle = Math.Atan2(angle, Math.Sqrt(Math.Max(0.0, 1.0 - angle * angle)));
                    if (Vector3D.Dot(localReference, localTarget) < 0) angle = Math.PI;

                    axis.Normalize();
                    axis *= Math.Max(minGyroRpmScale, gyro.GetMaximum<float>("Roll") * (angle / Math.PI) * gyroVelocityScale);

                    gyro.Pitch = (float)(-axis.X + localVelocity.X);
                    gyro.Roll = (float)(-axis.Z + localVelocity.Z);
                    gyro.Yaw = (float)(-axis.Y + localVelocity.Y);
                }
            }

            public void SetVelocity(Vector3D velocity)
            {
                Matrix matrix;

                foreach (var gyro in gyros)
                {
                    gyro.Orientation.GetMatrix(out matrix);
                    matrix = Matrix.Transpose(matrix);
                    var localVelocity = Vector3D.Transform(velocity, (MatrixD)matrix);

                    gyro.Pitch = (float)localVelocity.X;
                    gyro.Roll = (float)localVelocity.Z;
                    gyro.Yaw = (float)localVelocity.Y;
                }
            }
        }
    }
}