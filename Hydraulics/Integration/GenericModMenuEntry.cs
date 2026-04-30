using Alca259.Common;

namespace Hydraulics;

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

        configMenu.AddKeybindList(
            mod: ModManifest,
            name: () => "Toggle Pipe Edit Mode",
            getValue: () => _config.TogglePipeEditModeKey,
            setValue: value => _config.TogglePipeEditModeKey = value
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Require Energy For Pumps",
            getValue: () => _config.RequireEnergyForPumps,
            setValue: value => _config.RequireEnergyForPumps = value
        );
    }
}
