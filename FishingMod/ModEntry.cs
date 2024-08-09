using Alca259.Common;
using FishingMod.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Tools;
using System;

namespace FishingMod;

/// <summary>The main entry point.</summary>
public partial class ModEntry : Mod
{
    /*********
    ** Properties
    *********/
    /// <summary>The mod configuration.</summary>
    private ModConfig Config;

    /// <summary>The current fishing bobber bar.</summary>
    private readonly PerScreen<SBobberBar> Bobber = new();

    /// <summary>Whether the player is in the fishing minigame.</summary>
    private readonly PerScreen<bool> BeganFishingGame = new();

    /// <summary>The number of ticks since the player opened the fishing minigame.</summary>
    private readonly PerScreen<int> UpdateIndex = new();


    /*********
    ** Public methods
    *********/
    /// <summary>The mod entry point, called after the mod is first loaded.</summary>
    /// <param name="helper">Provides simplified APIs for writing mods.</param>
    public override void Entry(IModHelper helper)
    {
        CommonHelper.RemoveObsoleteFiles(this, "FishingMod.pdb");

        Config = helper.ReadConfig<ModConfig>();

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Display.MenuChanged += OnMenuChanged;
        helper.Events.Input.ButtonsChanged += OnButtonsChanged;
    }


    /*********
    ** Private methods
    *********/
    /// <summary>Raised after a game menu is opened, closed, or replaced.</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnMenuChanged(object sender, MenuChangedEventArgs e)
    {
        Bobber.Value = e.NewMenu is BobberBar menu
            ? new SBobberBar(menu, Helper.Reflection)
            : null;
    }

    /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        // apply infinite bait/tackle
        if (e.IsOneSecond && (Config.InfiniteBait || Config.InfiniteTackle) && Game1.player.CurrentTool is FishingRod rod)
        {
            if (Config.InfiniteBait && rod.attachments?.Length > 0 && rod.attachments[0] != null)
                rod.attachments[0].Stack = rod.attachments[0].maximumStackSize();

            // If the player has the iridium band, the tackle will be in the second slot
            if (Config.InfiniteTackle && rod.attachments?.Length > 1 && rod.attachments[1] != null)
                rod.attachments[1].uses.Value = 0;

            // If the player has the double iridium band, can have a second tackle in the third slot
            if (Config.InfiniteTackle && rod.attachments?.Length > 2 && rod.attachments[2] != null)
                rod.attachments[2].uses.Value = 0;
        }

        // apply fishing minigame changes
        if (Game1.activeClickableMenu is BobberBar && Bobber.Value != null)
        {
            SBobberBar bobber = Bobber.Value;

            //Begin fishing game
            if (!BeganFishingGame.Value && UpdateIndex.Value > 15)
            {
                //Do these things once per fishing minigame, 1/4 second after it updates
                bobber.Difficulty *= Config.FishDifficultyMultiplier;
                bobber.Difficulty += Config.FishDifficultyAdditive;

                if (Config.AlwaysFindTreasure)
                    bobber.Treasure = true;

                if (Config.InstantCatchFish)
                {
                    if (bobber.Treasure)
                        bobber.TreasureCaught = true;
                    bobber.DistanceFromCatching += 100;
                }

                if (Config.InstantCatchTreasure && (bobber.Treasure || Config.AlwaysFindTreasure))
                    bobber.TreasureCaught = true;

                if (Config.EasierFishing)
                {
                    bobber.Difficulty = Math.Max(15, Math.Max(bobber.Difficulty, 60));
                    bobber.MotionType = 2;
                }

                BeganFishingGame.Value = true;
            }

            if (UpdateIndex.Value < 20)
                UpdateIndex.Value++;

            if (Config.AlwaysPerfect)
                bobber.Perfect = true;

            if (!bobber.BobberInBar)
                bobber.DistanceFromCatching += Config.LossAdditive;
        }
        else
        {
            //End fishing game
            BeganFishingGame.Value = false;
            UpdateIndex.Value = 0;
        }
    }

    /// <inheritdoc cref="IInputEvents.ButtonsChanged"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
    {
        if (Config.ReloadKey.JustPressed())
        {
            Config = Helper.ReadConfig<ModConfig>();
            Monitor.Log("Config reloaded", LogLevel.Info);
        }
    }

    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
        SetupGenericModMenu();
    }
}
