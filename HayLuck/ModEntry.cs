using Alca259.Common;
using HayLuck.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System;

namespace HayLuck
{
    public partial class ModEntry : Mod
    {
        private ModConfig _config;

        public override void Entry(IModHelper helper)
        {
            CommonHelper.RemoveObsoleteFiles(this, "HayLuck.pdb");
            _config = helper.ReadConfig<ModConfig>();
            _config.EnsureArguments();

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.DayStarted += OnDayStarted;
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            if (!_config.EnableMod)
                return;

            if (!Context.IsWorldReady)
                return;

            DateTime now = DateTime.Now;
            Random random = new(Convert.ToInt32(now.Hour.ToString() + now.Minute + now.Second + now.Millisecond));
            int minValue = _config.MinIterations;
            int maxValue = _config.MaxIterations;
            string currentSeason = Game1.currentSeason;
            string logSeasonName;

            switch (currentSeason)
            {
                case "spring":
                    logSeasonName = "Primavera";
                    minValue *= 2;
                    maxValue *= 2;
                    break;
                case "summer":
                    logSeasonName = "Verano";
                    break;
                case "fall":
                    logSeasonName = "Otoño";
                    minValue /= 2;
                    maxValue /= 2;
                    break;
                case "winter":
                    logSeasonName = "Invierno";
                    break;
                default:
                    logSeasonName = Game1.currentSeason;
                    break;
            }

            int num = random.Next(minValue, maxValue);
            Game1.getFarm().growWeedGrass(num);
            Monitor.Log(string.Format("[{0}] Un nuevo día ha amanecido.", logSeasonName), LogLevel.Info);
            Monitor.Log(string.Format("Incrementado el nivel de hierba en {0}: {1}.", Game1.getFarm().Name, num), LogLevel.Info);
        }

        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (!_config.ReloadKey.JustPressed()) return;

            _config = Helper.ReadConfig<ModConfig>();
            _config.EnsureArguments();
            Monitor.Log("Config reloaded", LogLevel.Info);
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            SetupGenericModMenu();
        }
    }
}
