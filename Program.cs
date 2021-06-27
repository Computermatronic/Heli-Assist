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
    partial class Program : MyGridProgram
    {
        IMyShipController controller;
        GyroController gyroController;
        ThrusterController thrustController;

        //Runtime Variables
        TimeSpan timeSinceLastUpdate;
        bool updateFinished = false;
        bool isFirstUpdate = true;

        //State Variables;
        string mode;

        bool enableLateralDampening;
        bool enablePrecisionAim;

        //Config Variables
        string blockGroupName = "Heli Assist";

        string start_mode = "flight";
        bool rememberLastMode = true;

        float maxFlightPitch = 30.0f;
        float maxFlightRoll = 30.0f;

        float maxLandingPitch = 10.0f;
        float maxLandingRoll = 10.0f;

        float precisionAimFactor = 16.0f;
        float mouseSpeed = 0.5f;

        //Cache Variables
        List<IMyShipController> controllerCache;
        List<IMyGyro> gyroCache;
        List<IMyThrust> thrustCache;

        string configCache;

        static Program program;

        public Program()
        {
            controllerCache = new List<IMyShipController>();
            gyroCache = new List<IMyGyro>();
            thrustCache = new List<IMyThrust>();

            timeSinceLastUpdate = TimeSpan.FromSeconds(0);
            Runtime.UpdateFrequency = UpdateFrequency.Update1;

            try { this.Update(); }
            catch (Exception e) { Echo("Error: " + e.Message); }

            program = this;
        }

        public void Save()
        {
            Storage = mode;
        }

        public void Main(string argument, UpdateType updateSource)
        {
            timeSinceLastUpdate += Runtime.TimeSinceLastRun;

            if (isFirstUpdate || !updateFinished || timeSinceLastUpdate > TimeSpan.FromSeconds(10))
            {
                try { this.Update(); }
                catch (Exception e) { Echo("Error: " + e.Message); }
                timeSinceLastUpdate = TimeSpan.FromSeconds(0);
                return;
            }

            Echo("Script running, next update: " + (10 - (uint)timeSinceLastUpdate.TotalSeconds).ToString());
            Echo("Current Mode: " + mode);
            Echo("Precision Aim: " + (enablePrecisionAim ? "enabled" : "disabled"));
            Echo("Lateral Dampening: " + (enableLateralDampening ? "enabled" : "disabled"));

            if (IsValidMode(argument))
                SwitchToMode(argument);
            else if (argument == "toggle_manual") SwitchToMode(mode == "manual" ? "flight" : "manual");
            else if (argument == "toggle_landing") SwitchToMode(mode == "landing" ? "flight" : "landing");
            else if (argument == "toggle_shutdown") SwitchToMode(mode == "shutdown" ? "flight" : "shutdown");
            else if (argument == "toggle_standby") SwitchToMode(mode == "standby" ? "flight" : "standby");
            else if (argument == "toggle_precision") enablePrecisionAim = !enablePrecisionAim;
            else if (argument == "toggle_lateral_dampening") enableLateralDampening = !enableLateralDampening;
            else if (argument == "update")
            {
                updateFinished = false;
                return;
            }

            var wasd = controller.MoveIndicator;
            var mouse = new Vector3(controller.RotationIndicator, controller.RollIndicator * 9);
            var dampeningRotation = gyroController.CalculatePitchRollToAchiveVelocity(Vector3.Zero);
            var autoStop = controller.DampenersOverride;

            if (enablePrecisionAim) mouse *= 1 / precisionAimFactor;
            else mouse *= mouseSpeed;

            switch (mode)
            {
                case "flight":
                    {
                        var pitch = wasd.Z * maxFlightPitch * degToRad;
                        var roll = wasd.X * maxFlightRoll * degToRad;
                        dampeningRotation = Vector2.Min(dampeningRotation, new Vector2(maxFlightRoll, maxFlightPitch) * degToRad);

                        if (autoStop && IsZero(roll)) roll = MinAbs(dampeningRotation.X, maxFlightRoll * degToRad);
                        if (autoStop && IsZero(pitch)) pitch = MinAbs(dampeningRotation.Y, maxFlightPitch * degToRad);

                        gyroController.SetAngularVelocity(gyroController.CalculateVelocityToAlign(pitch, roll) + mouse);
                        thrustController.SetYAxisThrust(wasd.Y != 0 ? 0 : thrustController.CalculateThrustToHover());
                        break;
                    }
                case "landing":
                    {
                        var pitch = wasd.Z * maxFlightPitch * degToRad;
                        var roll = wasd.X * maxFlightRoll * degToRad;
                        dampeningRotation = Vector2.Min(dampeningRotation, new Vector2(maxLandingRoll, maxLandingPitch) * degToRad);

                        if (autoStop && IsZero(roll)) roll = Math.Min(Math.Max(dampeningRotation.X, -maxLandingRoll), maxLandingRoll);
                        if (autoStop && IsZero(pitch)) pitch = Math.Min(Math.Max(dampeningRotation.Y, -maxLandingPitch), maxLandingPitch);

                        gyroController.SetAngularVelocity(gyroController.CalculateVelocityToAlign(pitch, roll) + mouse);
                        thrustController.SetYAxisThrust(wasd.Y != 0 ? 0 : thrustController.CalculateThrustToHover());
                        break;
                    }
                case "manual":
                    gyroController.SetAngularVelocity(mouse); thrustController.SetYAxisThrust(wasd.Y != 0 ? 0 : thrustController.CalculateThrustToHover());
                    break;
                case "shutdown":
                    break;
                case "standby":
                    break;
            }
        }

        void SwitchToMode(string mode)
        {
            if (!IsValidMode(mode)) return;
            switch (mode)
            {
                case "flight":
                    gyroController.SetEnabled(true);
                    thrustController.SetEnabled(true);
                    gyroController.SetOverride(true);
                    break;
                case "landing":
                    gyroController.SetEnabled(true);
                    thrustController.SetEnabled(true);
                    gyroController.SetOverride(true);
                    controller.DampenersOverride = true;
                    break;
                case "manual":
                    gyroController.SetEnabled(true);
                    thrustController.SetEnabled(true);
                    gyroController.SetOverride(true);
                    break;
                case "shutdown":
                    gyroController.SetEnabled(false);
                    thrustController.SetEnabled(false);
                    break;
                case "standby":
                    gyroController.SetEnabled(true);
                    thrustController.SetEnabled(true);
                    gyroController.SetOverride(false);
                    thrustController.SetYAxisThrust(0);
                    break;
            }
            this.mode = mode;
            enablePrecisionAim = false;
            enableLateralDampening = false;
        }

        bool IsValidMode(string mode)
        {
            return mode == "flight" || mode == "landing" || mode == "manual" || mode == "shutdown" || mode == "standby";
        }

        public void Update()
        {
            if (isFirstUpdate || configCache != Me.CustomData)
            {
                configCache = Me.CustomData;
                MyIni configIni = new MyIni();
                MyIniParseResult parseResult;

                if (!configIni.TryParse(configCache, out parseResult))
                    throw new Exception("Failed To Read Config: " + parseResult.Error + " on line" + parseResult.LineNo.ToString());

                blockGroupName = configIni.Get("main", "block_group_name").ToString(blockGroupName);

                start_mode = configIni.Get("main", "start_mode").ToString(start_mode);
                rememberLastMode = configIni.Get("main", "remember_mode").ToBoolean(rememberLastMode);

                maxFlightPitch = (float)configIni.Get("main", "max_pitch").ToDouble(maxFlightPitch);
                maxFlightRoll = (float)configIni.Get("main", "max_roll").ToDouble(maxFlightRoll);

                maxLandingPitch = (float)configIni.Get("main", "max_landing_pitch").ToDouble(maxLandingPitch);
                maxLandingRoll = (float)configIni.Get("main", "max_landing_roll").ToDouble(maxLandingRoll);

                precisionAimFactor = (float)configIni.Get("main", "precision").ToDouble(precisionAimFactor);
            }

            var blockGroup = GridTerminalSystem.GetBlockGroupWithName(blockGroupName);
            if (blockGroup == null) throw new Exception("Could not find block group with name '" + blockGroupName + "'");

            controllerCache.Clear();
            blockGroup.GetBlocksOfType<IMyShipController>(controllerCache);
            if (!controllerCache.Any()) throw new Exception("Ship must have at least one ship controller");
            foreach (var controller in controllerCache)
            {
                if (controller.IsMainCockpit) this.controller = controller;
            }
            if (this.controller == null) this.controller = controllerCache.First();

            gyroCache.Clear();
            blockGroup.GetBlocksOfType<IMyGyro>(gyroCache);
            if (!gyroCache.Any()) throw new Exception("Ship must have atleast one gyroscope");

            thrustCache.Clear();
            blockGroup.GetBlocksOfType<IMyThrust>(thrustCache);
            if (!thrustCache.Any()) throw new Exception("Ship must have atleast one thruster");

            if (thrustController == null) thrustController = new ThrusterController(controller, thrustCache);
            else thrustController.Update(controller, thrustCache);

            if (gyroController == null) gyroController = new GyroController(controller, gyroCache);
            else gyroController.Update(controller, gyroCache);

            if (isFirstUpdate && rememberLastMode && IsValidMode(Storage)) SwitchToMode(Storage);
            else if (isFirstUpdate) SwitchToMode(start_mode);

            isFirstUpdate = false;
            updateFinished = true;
        }
    }
}