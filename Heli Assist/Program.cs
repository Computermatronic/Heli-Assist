using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        const string defaultConfigString = "[main]\nblock_group_name=Heli Assist\nmax_pitch=45\nmax_roll=45\nmax_landing_pitch=10\nmax_landing_roll=10\nprecision=16\nstart_mode=flight\nremember_mode=true";

        IMyShipController controller;

        GyroController gyroController;
        ThrusterController thrusterController;

        TimeSpan timeSinceLastUpdate;
        bool updateSuceeded = false;
        bool isFirstRun = true;
        string configString;

        List<IMyShipController> controllers;
        List<IMyGyro> gyros;
        List<IMyThrust> thrusters;

        bool precisionEnabled = false;
        bool lateralDampeners = false;

        //Config Options
        string blockGroupName = "Heli Assist";

        float maxFlightPitch = 45.0f;
        float maxFlightRoll = 45.0f;

        float maxLandingPitch = 10.0f;
        float maxLandingRoll = 10.0f;

        float precisionMultiplier = 16.0f;

        string mode = "flight";
        bool rememberMode = true;


        public Program()
        {
            this.controllers = new List<IMyShipController>();
            this.gyros = new List<IMyGyro>();
            this.thrusters = new List<IMyThrust>();

            timeSinceLastUpdate = TimeSpan.FromSeconds(0);
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
        }

        string Update()
        {
            updateSuceeded = false;
            try
            {
                string configString = Me.CustomData;
                if (configString.Length == 0) Me.CustomData = configString = defaultConfigString;

                if (configString != this.configString)
                {
                    this.configString = configString;
                    MyIni configIni = new MyIni();
                    MyIniParseResult parseResult;

                    if (!configIni.TryParse(configString, out parseResult)) throw new Exception("Failed To Read Config: " + parseResult.Error + " on line" + parseResult.LineNo.ToString());

                    maxFlightPitch = (float)configIni.Get("main", "max_pitch").ToDouble(maxFlightPitch);
                    maxFlightRoll = (float)configIni.Get("main", "max_roll").ToDouble(maxFlightRoll);

                    maxLandingPitch = (float)configIni.Get("main", "max_landing_pitch").ToDouble(maxLandingPitch);
                    maxLandingRoll = (float)configIni.Get("main", "max_landing_roll").ToDouble(maxLandingRoll);

                    precisionMultiplier = (float)configIni.Get("main", "precision").ToDouble(precisionMultiplier);

                    blockGroupName = configIni.Get("main", "block_group_name").ToString(blockGroupName);
                    rememberMode = configIni.Get("main", "remember_mode").ToBoolean(rememberMode);

                    if (isFirstRun)
                    {
                        mode = configIni.Get("main", "start_mode").ToString(mode);
                        if (!isValidMode(mode))
                            throw new Exception("'" + mode + "is not a valid value for start_mode");
                    }
                }

                if (isFirstRun && Storage.Length > 0 && rememberMode) mode = Storage;

                var blockGroup = GridTerminalSystem.GetBlockGroupWithName(blockGroupName);
                if (blockGroup == null) throw new Exception("Could not find block group with name '" + blockGroupName + "'");

                controllers.Clear();
                blockGroup.GetBlocksOfType<IMyShipController>(controllers);
                if (!controllers.Any()) throw new Exception("Ship must have atleast one ship controller");
                foreach (var controller in controllers)
                {
                    if (controller.IsMainCockpit) this.controller = controller;
                }
                if (this.controller == null) this.controller = controllers.First();

                gyros.Clear();
                blockGroup.GetBlocksOfType<IMyGyro>(gyros);
                if (!gyros.Any()) throw new Exception("Ship must have atleast one gyroscope");
                gyroController = new GyroController(controller, gyros);

                thrusters.Clear();
                blockGroup.GetBlocksOfType<IMyThrust>(thrusters);
                if (!thrusters.Any()) throw new Exception("Ship must have atleast one thruster");
                thrusterController = new ThrusterController(controller, thrusters);

                if (isFirstRun) SwitchToMode(mode);

                updateSuceeded = true;
                isFirstRun = false;
            }
            catch (Exception e)
            {
                return "Error: " + e.Message;
            }
            return "";
        }

        public void Save()
        {
            Storage = mode;
        }



        public void Main(string argument, UpdateType updateSource)
        {
            timeSinceLastUpdate += Runtime.TimeSinceLastRun;

            if (isFirstRun || !updateSuceeded || timeSinceLastUpdate > TimeSpan.FromSeconds(10))
            {
                Echo(Update());
                timeSinceLastUpdate = TimeSpan.FromSeconds(0);
                return;
            }

            Echo("Script running, next update: " + (10 - (uint)timeSinceLastUpdate.TotalSeconds).ToString());
            Echo("Current Mode: " + mode);
            Echo("Precision Aim: " + (precisionEnabled ? "enabled" : "disabled"));
            Echo("Lateral Dampening: " + (lateralDampeners ? "enabled" : "disabled"));

            var wasd = controller.MoveIndicator;
            var mouse = new Vector3D(controller.RotationIndicator, controller.RollIndicator * 9);

            if (precisionEnabled) mouse *= 1 / precisionMultiplier;

            if (isValidMode(argument))
                SwitchToMode(argument);
            else if (argument == "toggle_manual") SwitchToMode(mode == "manual" ? "flight" : "manual");
            else if (argument == "toggle_landing") SwitchToMode(mode == "landing" ? "flight" : "landing");
            else if (argument == "toggle_shutdown") SwitchToMode(mode == "shutdown" ? "flight" : "shutdown");
            else if (argument == "toggle_standby") SwitchToMode(mode == "standby" ? "flight" : "standby");
            else if (argument == "toggle_precision") precisionEnabled = !precisionEnabled;
            else if (argument == "toggle_lateral_dampening") lateralDampeners = !lateralDampeners;
            else if (argument == "update")
            {
                updateSuceeded = false;
                return;
            }

            switch (mode)
            {
                case "flight":
                    {
                        var pitch = wasd.Z * maxFlightPitch * degToRad;
                        var roll = wasd.X * maxFlightRoll * degToRad;

                        var pitchRollToStop = gyroController.CalculatePitchRollToStop(maxFlightPitch * degToRad, maxFlightRoll * degToRad);
                        var autoStop = controller.DampenersOverride;

                        if (IsZero(pitch) && autoStop && !lateralDampeners) pitch = (float)pitchRollToStop.Y;
                        if (IsZero(roll) && autoStop) roll = (float)pitchRollToStop.X;

                        gyroController.SetPitchRoll(pitch, roll, mouse);
                        thrusterController.SetYAxisThrust(IsZero(wasd.Y) ? thrusterController.CalculateThrustToHover() : 0);
                        break;
                    }
                case "landing":
                    {
                        var pitch = wasd.Z * maxLandingPitch * degToRad;
                        var roll = wasd.X * maxLandingRoll * degToRad;

                        var pitchRollToStop = gyroController.CalculatePitchRollToStop(maxLandingPitch * degToRad, maxLandingRoll * degToRad);
                        var autoStop = controller.DampenersOverride;

                        if (IsZero(pitch) && autoStop && !lateralDampeners) pitch = (float)pitchRollToStop.Y;
                        if (IsZero(roll) && autoStop) roll = (float)pitchRollToStop.X;

                        gyroController.SetPitchRoll(pitch, roll, mouse);
                        thrusterController.SetYAxisThrust(IsZero(wasd.Y) ? thrusterController.CalculateThrustToHover() : 0);
                        break;
                    }
                case "manual":
                    thrusterController.SetYAxisThrust(IsZero(wasd.Y) ? thrusterController.CalculateThrustToHover() : 0);
                    gyroController.SetVelocity(mouse);
                    break;
                case "shutdown":
                    break;
                case "standby":
                    break;
            }
        }

        void SwitchToMode(string mode)
        {
            switch (mode)
            {
                case "flight":
                    gyroController.SetEnabled(true);
                    thrusterController.SetEnabled(true);
                    gyroController.SetGyroOverride(true);
                    break;
                case "landing":
                    gyroController.SetEnabled(true);
                    thrusterController.SetEnabled(true);
                    gyroController.SetGyroOverride(true);
                    controller.DampenersOverride = true;
                    break;
                case "manual":
                    gyroController.SetEnabled(true);
                    thrusterController.SetEnabled(true);
                    gyroController.SetGyroOverride(true);
                    break;
                case "shutdown":
                    gyroController.SetEnabled(false);
                    thrusterController.SetEnabled(false);
                    break;
                case "standby":
                    gyroController.SetEnabled(true);
                    thrusterController.SetEnabled(true);
                    gyroController.SetGyroOverride(false);
                    thrusterController.SetYAxisThrust(0);
                    break;
            }
            if (isValidMode(mode)) this.mode = mode;
            precisionEnabled = false;
            lateralDampeners = false;
        }

        bool isValidMode(string mode)
        {
            return mode == "flight" || mode == "landing" || mode == "manual" || mode == "shutdown" || mode == "standby";
        }
    }
}