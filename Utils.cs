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

        public static bool IsEqual(float value1, float value2, float epsilon = 0.0001f)
        {
            return Math.Abs(NotNaN(value1 - value2)) <= epsilon;
        }

        public static float NotNaN(float value)
        {
            return float.IsNaN(value) ? 0 : value;
        }

        public static float MinAbs(float value1, float value2)
        {
            return Math.Min(Math.Abs(value1), Math.Abs(value2)) * (value1 < 0 ? -1 : 1);
        }
        class ConfigSection
        {
            Dictionary<string, string> config;
            string name;

            public ConfigSection(string name)
            {
                this.config = new Dictionary<string, string>();
                this.name = name;
            }

            public void Read(string text)
            {
                config.Clear();

                var ini = new MyIni();
                MyIniParseResult parseResult;
                if (!ini.TryParse(text, out parseResult))
                    throw new Exception("Failed To Read Config: " + parseResult.Error + " on line" + parseResult.LineNo.ToString());

                var keys = new List<MyIniKey>();
                ini.GetKeys(name, keys);
                foreach (var key in keys)
                {
                    config.Add(key.Name, ini.Get(key).ToString());
                }
            }

            public string write()
            {
                MyIni ini = new MyIni();
                ini.AddSection(name);

                foreach (var kv in config) { ini.Set(name, kv.Key, kv.Value); }

                return ini.ToString();
            }

            public T Get<T>(string key, T value)
            {
                if (!config.ContainsKey(key))
                {
                    config.Add(key, value.ToString());
                    return value;
                }
                string result; config.TryGetValue(key, out result);
                return (T)Convert.ChangeType(result, typeof(T));
            }
        }
    }
}
