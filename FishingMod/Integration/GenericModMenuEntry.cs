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
            },
            save: () => Helper.WriteConfig(_config)
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Always perfect catch",
            tooltip: () => "Whether the game should consider every catch to be perfectly executed, even if it wasn't.",
            getValue: () => _config.AlwaysPerfect,
            setValue: value => _config.AlwaysPerfect = value
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Always find treasure",
            tooltip: () => "Whether to always find treasure.",
            getValue: () => _config.AlwaysFindTreasure,
            setValue: value => _config.AlwaysFindTreasure = value
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Instant catch fish",
            tooltip: () => "Whether to catch fish instantly.",
            getValue: () => _config.InstantCatchFish,
            setValue: value => _config.InstantCatchFish = value
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Instant catch treasure",
            tooltip: () => "Whether to catch treasure instantly.",
            getValue: () => _config.InstantCatchTreasure,
            setValue: value => _config.InstantCatchTreasure = value
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Easier fishing",
            tooltip: () => "Whether to significantly lower the max fish difficulty.",
            getValue: () => _config.EasierFishing,
            setValue: value => _config.EasierFishing = value
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Fish difficulty multiplier",
            tooltip: () => "A multiplier applied to the fish difficulty. This can a number between 0 and 1 to lower difficulty, or more than 1 to increase it.",
            getValue: () => _config.FishDifficultyMultiplier,
            setValue: value => _config.FishDifficultyMultiplier = value,
            min: 0,
            max: 10
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Fish difficulty additive",
            tooltip: () => "A value added to the fish difficulty. This can be less than 0 to decrease difficulty, or more than 0 to increase it.",
            getValue: () => _config.FishDifficultyAdditive,
            setValue: value => _config.FishDifficultyAdditive = value
        );

        configMenu.AddNumberOption(
            mod: ModManifest,
            name: () => "Loss additive",
            tooltip: () => "A value added to the initial fishing completion. For example, a value of 1 will instantly catch the fish.",
            getValue: () => _config.LossAdditive,
            setValue: value => _config.LossAdditive = value
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Infinite tackle",
            tooltip: () => "Whether fishing tackles last forever.",
            getValue: () => _config.InfiniteTackle,
            setValue: value => _config.InfiniteTackle = value
        );

        configMenu.AddBoolOption(
            mod: ModManifest,
            name: () => "Infinite bait",
            tooltip: () => "Whether fishing bait lasts forever.",
            getValue: () => _config.InfiniteBait,
            setValue: value => _config.InfiniteBait = value
        );
    }
}
