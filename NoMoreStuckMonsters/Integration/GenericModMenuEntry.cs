using Alca259.Common;

namespace NoMoreStuckMonsters;

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

    }
}
