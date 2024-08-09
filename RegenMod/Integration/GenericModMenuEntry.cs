using Alca259.Common;

namespace RegenMod;

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

        configMenu.AddSectionTitle(
            mod: ModManifest,
            text: () => "Stamina Regeneration"
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Constantly regenerate stamina",
            tooltip: () => "Whether to constantly regenerate stamina.",
            getValue: () => _config.RegenStaminaConstant,
            setValue: value => _config.RegenStaminaConstant = value
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Stamina regen amount per second",
            tooltip: () => "The amount of stamina to constantly regenerate per second.",
            getValue: () => _config.RegenStaminaConstantAmountPerSecond,
            setValue: value => _config.RegenStaminaConstantAmountPerSecond = value,
            min: 0.1f,
            max: 10f
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Regenerate stamina while standing still",
            tooltip: () => "Whether to regenerate stamina while standing still.",
            getValue: () => _config.RegenStaminaStill,
            setValue: value => _config.RegenStaminaStill = value
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Stamina regen amount per second while standing still",
            tooltip: () => "The amount of stamina to regenerate per second while standing still.",
            getValue: () => _config.RegenStaminaStillAmountPerSecond,
            setValue: value => _config.RegenStaminaStillAmountPerSecond = value,
            min: 0.1f,
            max: 10f
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Time required to stand still to regenerate stamina",
            tooltip: () => "The amount of time the player must stand still to regenerate stamina.",
            getValue: () => _config.RegenStaminaStillTimeRequiredMS,
            setValue: value => _config.RegenStaminaStillTimeRequiredMS = value,
            min: 0,
            max: 5000
        );

        configMenu.AddSectionTitle(
            mod: ModManifest,
            text: () => "Health Regeneration"
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Constantly regenerate health",
            tooltip: () => "Whether to constantly regenerate health.",
            getValue: () => _config.RegenHealthConstant,
            setValue: value => _config.RegenHealthConstant = value
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Health regen amount per second",
            tooltip: () => "The amount of health to constantly regenerate per second.",
            getValue: () => _config.RegenHealthConstantAmountPerSecond,
            setValue: value => _config.RegenHealthConstantAmountPerSecond = value,
            min: 0.1f,
            max: 10f
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Regenerate health while standing still",
            tooltip: () => "Whether to regenerate health while standing still.",
            getValue: () => _config.RegenHealthStill,
            setValue: value => _config.RegenHealthStill = value
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Health regen amount per second while standing still",
            tooltip: () => "The amount of health to regenerate per second while standing still.",
            getValue: () => _config.RegenHealthStillAmountPerSecond,
            setValue: value => _config.RegenHealthStillAmountPerSecond = value,
            min: 0.1f,
            max: 10f
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Time required to stand still to regenerate health",
            tooltip: () => "The amount of time the player must stand still to regenerate health.",
            getValue: () => _config.RegenHealthStillTimeRequiredMS,
            setValue: value => _config.RegenHealthStillTimeRequiredMS = value,
            min: 0,
            max: 5000
        );
    }
}
