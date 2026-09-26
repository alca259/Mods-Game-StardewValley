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
            text: () => T("config.zones.section")
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => T("config.zone.mines.name"),
            tooltip: () => T("config.zone.mines.tooltip"),
            getValue: () => _config.Mines,
            setValue: value => _config.Mines = value
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => T("config.zone.volcano.name"),
            tooltip: () => T("config.zone.volcano.tooltip"),
            getValue: () => _config.Volcano,
            setValue: value => _config.Volcano = value
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => T("config.zone.farm.name"),
            tooltip: () => T("config.zone.farm.tooltip"),
            getValue: () => _config.Farm,
            setValue: value => _config.Farm = value
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => T("config.zone.wilderness.name"),
            tooltip: () => T("config.zone.wilderness.tooltip"),
            getValue: () => _config.Wilderness,
            setValue: value => _config.Wilderness = value
        );

        configMenu.AddSectionTitle(
            mod: ModManifest,
            text: () => T("config.pathfinding.section")
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("config.zone.recalcInterval.name"),
            tooltip: () => T("config.zone.recalcInterval.tooltip"),
            getValue: () => _config.Pathfinding.RecalcInterval,
            setValue: value => _config.Pathfinding.RecalcInterval = value,
            min: 1,
            max: 240,
            interval: 1
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("config.zone.maxAStarNodes.name"),
            tooltip: () => T("config.zone.maxAStarNodes.tooltip"),
            getValue: () => _config.Pathfinding.MaxAStarNodes,
            setValue: value => _config.Pathfinding.MaxAStarNodes = value,
            min: 50,
            max: 1000,
            interval: 10
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => T("config.zone.stuckThreshold.name"),
            tooltip: () => T("config.zone.stuckThreshold.tooltip"),
            getValue: () => _config.Pathfinding.StuckThreshold,
            setValue: value => _config.Pathfinding.StuckThreshold = value,
            min: 1,
            max: 120,
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
