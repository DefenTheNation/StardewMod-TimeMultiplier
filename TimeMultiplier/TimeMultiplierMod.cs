using System;
using System.IO;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace TimeMultiplier
{
    public class TimeMultiplierMod : Mod
    {
        // 7 seconds is hardcoded into StardewValley.exe, class Game1, method UpdateGameClock
        public const int IntervalMax = 7000;

        public int LastTimeInterval { get; set; }
        public TimeMultiplierConfig Config { get; set; }

        public override void Entry(IModHelper helper)
        {
            Config = new TimeMultiplierConfig();
            
            LastTimeInterval = 0;

            SaveEvents.AfterLoad += (sender, e) =>
            {
                LastTimeInterval = 0;

                string configLocation = Path.Combine("data", Constants.SaveFolderName + ".json");
                Config = helper.ReadJsonFile<TimeMultiplierConfig>(configLocation) ?? new TimeMultiplierConfig();
            };

            SaveEvents.AfterReturnToTitle += (sender, e) =>
            {
                LastTimeInterval = 0;

                string configLocation = Path.Combine("data", Constants.SaveFolderName + ".json");
                helper.WriteJsonFile<TimeMultiplierConfig>(configLocation, Config);

                Config = new TimeMultiplierConfig();
            };

            helper.ConsoleCommands.Add("time_multiplier_change", "Updates time multiplier on the fly", (string command, string[] args) =>
            {
                float multiplierArg;

                if (args.Length != 1)
                {
                    Monitor.Log("Usage: time_multiplier_change 1.00 ", LogLevel.Error);
                    return;
                }
                else if(!float.TryParse(args[0], out multiplierArg))
                {
                    Monitor.Log("Error: '" + args[0] + "' is not a valid decimal. Usage: time_multiplier_change 1.00 ", LogLevel.Error);
                    return;
                }

                Config.TimeMultiplier = multiplierArg;

                string configLocation = Path.Combine("data", Constants.SaveFolderName + ".json");
                helper.WriteJsonFile<TimeMultiplierConfig>(configLocation, Config);

                Monitor.Log("Time now multiplied by " + multiplierArg, LogLevel.Info);
            });

            helper.ConsoleCommands.Add("time_multiplier_toggle", "Updates time multiplier on the fly", (string command, string[] args) =>
            {
                LastTimeInterval = 0;
                Config.Enabled = !Config.Enabled;

                string configLocation = Path.Combine("data", Constants.SaveFolderName + ".json");
                helper.WriteJsonFile<TimeMultiplierConfig>(configLocation, Config);

                Monitor.Log("Time multiplier enabled: " + Config.Enabled, LogLevel.Info);
            });

            GameEvents.UpdateTick += (sender, e) =>
            {
                if (!Context.IsWorldReady || !Context.IsMainPlayer || !Config.Enabled) return;

                int delta;
                if (Game1.gameTimeInterval < LastTimeInterval)
                {
                    delta = Game1.gameTimeInterval;
                    LastTimeInterval = 0;
                }
                else
                {
                    delta = Game1.gameTimeInterval - LastTimeInterval;
                }

                Game1.gameTimeInterval = LastTimeInterval + (int)(delta * Config.TimeMultiplier);
                LastTimeInterval = Game1.gameTimeInterval;
            };

            Monitor.Log("Initialized");
        }

        private bool timeMultiplierToggled(bool isEnabled)
        {
            if(!Context.IsMainPlayer)
            {
                Monitor.Log("Warning: only the host can manipulate the game clock.", LogLevel.Warn);
                return false;
            }
            else if (isEnabled)
            {
                GameEvents.UpdateTick += updateTick_ModifyClock;
            }
            else
            {
                GameEvents.UpdateTick -= updateTick_ModifyClock;
            }

            return true;
        }

        private void updateTick_ModifyClock(object sender, EventArgs e)
        {
            if (!Context.IsWorldReady) return;

            int delta;
            if (Game1.gameTimeInterval < LastTimeInterval)
            {
                delta = Game1.gameTimeInterval;
                LastTimeInterval = 0;
            }
            else
            {
                delta = Game1.gameTimeInterval - LastTimeInterval;
            }

            Game1.gameTimeInterval = LastTimeInterval + (int)(delta * Config.TimeMultiplier);
            LastTimeInterval = Game1.gameTimeInterval;
        }
    }
}
