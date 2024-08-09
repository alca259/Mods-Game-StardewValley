using Alca259.Common;

namespace FishingMod;

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
                _config.EnsureArguments();
            },
            save: () => Helper.WriteConfig(_config)
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Mod Enabled?",
            getValue: () => _config.EnableMod,
            setValue: value => _config.EnableMod = value
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Min iterations",
            getValue: () => _config.MinIterations,
            setValue: value => _config.MinIterations = value,
            min: 0,
            max: 10);

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Max iterations",
            getValue: () => _config.MaxIterations,
            setValue: value => _config.MaxIterations = value,
            min: 1,
            max: 20);
    }
}
