using Alca259.Common;

namespace HealthBars;

public partial class ModEntry
{
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
            },
            save: () => Helper.WriteConfig(_config)
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Display health bar when not damaged",
            tooltip: () => "Whether to show a health bar for monsters at full health.",
            getValue: () => _config.DisplayHealthWhenNotDamaged,
            setValue: value => _config.DisplayHealthWhenNotDamaged = value
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Display max health number",
            tooltip: () => "Whether to show the maximum health number.",
            getValue: () => _config.DisplayMaxHealthNumber,
            setValue: value => _config.DisplayMaxHealthNumber = value
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Display current health number",
            tooltip: () => "Whether to show the current health number.",
            getValue: () => _config.DisplayCurrentHealthNumber,
            setValue: value => _config.DisplayCurrentHealthNumber = value
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Display text border",
            tooltip: () => "Whether to draw a border around text so it's more visible on some backgrounds.",
            getValue: () => _config.DisplayTextBorder,
            setValue: value => _config.DisplayTextBorder = value
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Bar width",
            tooltip: () => "The health bar width in pixels.",
            getValue: () => _config.BarWidth,
            setValue: value => _config.BarWidth = value,
            min: 50,
            max: 500
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Bar height",
            tooltip: () => "The health bar height in pixels.",
            getValue: () => _config.BarHeight,
            setValue: value => _config.BarHeight = value,
            min: 5,
            max: 50
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Bar border width",
            tooltip: () => "The health bar's vertical border width in pixels.",
            getValue: () => _config.BarBorderWidth,
            setValue: value => _config.BarBorderWidth = value,
            min: 1,
            max: 4
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Bar border height",
            tooltip: () => "The health bar's horizontal border width in pixels.",
            getValue: () => _config.BarBorderHeight,
            setValue: value => _config.BarBorderHeight = value,
            min: 1,
            max: 4
        );

    }
}
