using Alca259.Common;

namespace Hydraulics;

public partial class ModEntry
{
    /// <summary>Obtiene una traducción del mod a partir de su clave.</summary>
    private string T(string key)
    {
        return Helper.Translation.Get(key);
    }

    /// <summary>Configura las opciones del mod en Generic Mod Config Menu.</summary>
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
            max: 100,
            interval: 5
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("pipe.cost.copper"),
            getValue: () => _config.PipeBuildCopperOreCost,
            setValue: value => _config.PipeBuildCopperOreCost = value,
            min: 0,
            max: 10,
            interval: 1
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("pipe.refund.gold"),
            getValue: () => _config.PipeDestroyGoldRefund,
            setValue: value => _config.PipeDestroyGoldRefund = value,
            min: 0,
            max: 100,
            interval: 5
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("pipe.refund.copper"),
            getValue: () => _config.PipeDestroyCopperOreRefund,
            setValue: value => _config.PipeDestroyCopperOreRefund = value,
            min: 0,
            max: 10,
            interval: 1
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("water.cost.perTile"),
            getValue: () => _config.WaterCostPerTile * 100f,
            setValue: value => _config.WaterCostPerTile = value / 100f,
            min: 1,
            max: 100,
            interval: 1
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("pump.output.bronze"),
            getValue: () => _config.BronzePumpWaterOutput,
            setValue: value => _config.BronzePumpWaterOutput = value,
            min: 1f,
            max: 20f,
            interval: 1f
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("pump.output.steel"),
            getValue: () => _config.SteelPumpWaterOutput,
            setValue: value => _config.SteelPumpWaterOutput = value,
            min: 1f,
            max: 50f,
            interval: 1f
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("pump.output.gold"),
            getValue: () => _config.GoldPumpWaterOutput,
            setValue: value => _config.GoldPumpWaterOutput = value,
            min: 1f,
            max: 160f,
            interval: 1f
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("pump.output.iridium"),
            getValue: () => _config.IridiumPumpWaterOutput,
            setValue: value => _config.IridiumPumpWaterOutput = value,
            min: 1f,
            max: 400f,
            interval: 1f
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => T("water.sprinklerAnimation"),
            getValue: () => _config.PlaySprinklerAnimation,
            setValue: value => _config.PlaySprinklerAnimation = value
        );
    }
}
