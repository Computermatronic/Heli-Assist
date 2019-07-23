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
    partial class Program
    {
        const float degToRad = (float)Math.PI / 180;
        const float radToDeg = 180.0f / (float)Math.PI;

        public static bool IsZero(float value, float epsilon = 0.0001f)
        {
            return Math.Abs(NotNaN(value)) <= epsilon;
        }

        public static float NotNaN(float value)
        {
            return float.IsNaN(value) ? 0 : value;
        }
    }
}
