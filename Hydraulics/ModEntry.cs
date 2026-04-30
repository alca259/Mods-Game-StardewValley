using Alca259.Common;
using Hydraulics.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace Hydraulics;

public partial class ModEntry : Mod
{
    #region Fields
    private ModConfig _config;
    #endregion

    #region Override entry point
    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        CommonHelper.RemoveObsoleteFiles(this, "Hydraulics.pdb");
        _config = helper.ReadConfig<ModConfig>();
        _config.EnsureArguments();

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.Input.ButtonsChanged += OnButtonsChanged;
    }
    #endregion

    #region Event Handlers
    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!_config.EnableMod)
            return;

        if (!Context.IsWorldReady)
            return;

        DateTime now = DateTime.Now;
        Random random = new(Convert.ToInt32(now.Hour.ToString() + now.Minute + now.Second + now.Millisecond));

    }

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!_config.ReloadKey.JustPressed()) return;

        _config = Helper.ReadConfig<ModConfig>();
        _config.EnsureArguments();
        Monitor.Log("Config reloaded", LogLevel.Info);
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        SetupGenericModMenu();
    }
    #endregion
}
