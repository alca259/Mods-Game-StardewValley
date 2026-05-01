using Alca259.Common;

namespace Hydraulics;

public partial class ModEntry
{
    private string T(string key)
    {
        return Helper.Translation.Get(key);
    }

    private void SetupGenericModMenu()
    {
        // get Generic Mod Config Menu's API (if it's installed)
        var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (configMenu is null)
            return;

        configMenu.Register(
            mod: ModManifest,
            reset: () =>
            {
                _config = new Framework.ModConfig();
                _config.EnsureArguments();
            },
            save: () => Helper.WriteConfig(_config)
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => T("mod.enabled"),
            getValue: () => _config.EnableMod,
            setValue: value => _config.EnableMod = value
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => T("overlay.show"),
            getValue: () => _config.ShowGridOverlay,
            setValue: value => _config.ShowGridOverlay = value
        );

        configMenu.AddKeybindList(
            mod: ModManifest,
            name: () => T("hotkey.overlay"),
            getValue: () => _config.ToggleGridOverlayKey,
            setValue: value => _config.ToggleGridOverlayKey = value
        );

        configMenu.AddKeybindList(
            mod: ModManifest,
            name: () => T("hotkey.pipeEdit"),
            getValue: () => _config.TogglePipeEditModeKey,
            setValue: value => _config.TogglePipeEditModeKey = value
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => T("energy.require"),
            getValue: () => _config.RequireEnergyForPumps,
            setValue: value => _config.RequireEnergyForPumps = value
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("pipe.cost.gold"),
            getValue: () => _config.PipeBuildGoldCost,
            setValue: value => _config.PipeBuildGoldCost = value,
            min: 0,
            max: 1000,
            interval: 10
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("pipe.cost.copper"),
            getValue: () => _config.PipeBuildCopperOreCost,
            setValue: value => _config.PipeBuildCopperOreCost = value,
            min: 0,
            max: 50,
            interval: 1
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("pipe.refund.gold"),
            getValue: () => _config.PipeDestroyGoldRefund,
            setValue: value => _config.PipeDestroyGoldRefund = value,
            min: 0,
            max: 1000,
            interval: 10
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("pipe.refund.copper"),
            getValue: () => _config.PipeDestroyCopperOreRefund,
            setValue: value => _config.PipeDestroyCopperOreRefund = value,
            min: 0,
            max: 50,
            interval: 1
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("water.cost.perTile"),
            getValue: () => (int)(_config.WaterCostPerTile * 100f),
            setValue: value => _config.WaterCostPerTile = value / 100f,
            min: 1,
            max: 1000,
            interval: 1
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => T("water.indicator.show"),
            getValue: () => _config.ShowWateredTileIndicator,
            setValue: value => _config.ShowWateredTileIndicator = value
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => T("water.sprinklerAnimation"),
            getValue: () => _config.PlaySprinklerAnimation,
            setValue: value => _config.PlaySprinklerAnimation = value
        );
    }
}
