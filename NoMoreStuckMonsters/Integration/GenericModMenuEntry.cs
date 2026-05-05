using Alca259.Common;

namespace NoMoreStuckMonsters;

public partial class ModEntry
{
    /// <summary>Obtiene una traducción del mod a partir de su clave.</summary>
    /// <param name="key">Clave de traducción.</param>
    /// <returns>Texto traducido para la clave indicada.</returns>
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
            name: () => T("config.reloadKey.name"),
            tooltip: () => T("config.reloadKey.tooltip"),
            getValue: () => _config.ReloadKey,
            setValue: value => _config.ReloadKey = value
        );

        configMenu.AddSectionTitle(
            mod: ModManifest,
            text: () => T("config.mines.section")
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => T("config.zone.enabled.name"),
            tooltip: () => T("config.zone.enabled.tooltip"),
            getValue: () => _config.Mines.Enabled,
            setValue: value => _config.Mines.Enabled = value
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("config.zone.recalcInterval.name"),
            tooltip: () => T("config.zone.recalcInterval.tooltip"),
            getValue: () => _config.Mines.RecalcInterval,
            setValue: value => _config.Mines.RecalcInterval = value,
            min: 1,
            max: 240,
            interval: 1
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("config.zone.maxAStarNodes.name"),
            tooltip: () => T("config.zone.maxAStarNodes.tooltip"),
            getValue: () => _config.Mines.MaxAStarNodes,
            setValue: value => _config.Mines.MaxAStarNodes = value,
            min: 50,
            max: 2000,
            interval: 10
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("config.zone.stuckThreshold.name"),
            tooltip: () => T("config.zone.stuckThreshold.tooltip"),
            getValue: () => _config.Mines.StuckThreshold,
            setValue: value => _config.Mines.StuckThreshold = value,
            min: 1,
            max: 240,
            interval: 1
        );

        configMenu.AddSectionTitle(
            mod: ModManifest,
            text: () => T("config.farm.section")
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => T("config.zone.enabled.name"),
            tooltip: () => T("config.zone.enabled.tooltip"),
            getValue: () => _config.Farm.Enabled,
            setValue: value => _config.Farm.Enabled = value
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("config.zone.recalcInterval.name"),
            tooltip: () => T("config.zone.recalcInterval.tooltip"),
            getValue: () => _config.Farm.RecalcInterval,
            setValue: value => _config.Farm.RecalcInterval = value,
            min: 1,
            max: 240,
            interval: 1
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("config.zone.maxAStarNodes.name"),
            tooltip: () => T("config.zone.maxAStarNodes.tooltip"),
            getValue: () => _config.Farm.MaxAStarNodes,
            setValue: value => _config.Farm.MaxAStarNodes = value,
            min: 50,
            max: 2000,
            interval: 10
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("config.zone.stuckThreshold.name"),
            tooltip: () => T("config.zone.stuckThreshold.tooltip"),
            getValue: () => _config.Farm.StuckThreshold,
            setValue: value => _config.Farm.StuckThreshold = value,
            min: 1,
            max: 240,
            interval: 1
        );

        configMenu.AddSectionTitle(
            mod: ModManifest,
            text: () => T("config.debug.section")
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => T("config.showDebugPath.name"),
            tooltip: () => T("config.showDebugPath.tooltip"),
            getValue: () => _config.ShowDebugPath,
            setValue: value => _config.ShowDebugPath = value
        );

    }
}
