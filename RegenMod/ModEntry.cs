using Alca259.Common;
using RegenMod.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using SFarmer = StardewValley.Farmer;

namespace RegenMod;

/// <summary>The main entry point.</summary>
public class ModEntry : Mod
{
    /*********
    ** Properties
    *********/
    /// <summary>The mod configuration.</summary>
    private ModConfig Config;

    /// <summary>The health regen carried over from the previous tick.</summary>
    private readonly PerScreen<float> Health = new();

    /// <summary>The stamina regen carried over from the previous tick.</summary>
    private readonly PerScreen<float> Stamina = new();

    /// <summary>The time in milliseconds since the player last moved or used a tool.</summary>
    private readonly PerScreen<double> TimeSinceLastMoved = new();

    private float ElapsedSeconds => (float)(Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds / 1000);


    /*********
    ** Public methods
    *********/
    /// <summary>The mod entry point, called after the mod is first loaded.</summary>
    /// <param name="helper">Provides methods for interacting with the mod directory, such as read/writing a config file or custom JSON files.</param>
    public override void Entry(IModHelper helper)
    {
        CommonHelper.RemoveObsoleteFiles(this, "RegenMod.pdb");

        Config = helper.ReadConfig<ModConfig>();

        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Input.ButtonsChanged += OnButtonChanged;
    }


    /*********
    ** Private methods
    *********/
    /// <inheritdoc cref="IInputEvents.ButtonsChanged"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnButtonChanged(object sender, ButtonsChangedEventArgs e)
    {
        if (Config.ReloadKey.JustPressed())
        {
            Config = Helper.ReadConfig<ModConfig>();
            Monitor.Log("Config reloaded", LogLevel.Info);
        }
    }

    /// <summary>Raised after the game state is updated (≈60 times per second).</summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.paused || Game1.activeClickableMenu != null)
            return;

        SFarmer player = Game1.player;

        //detect movement or tool use
        TimeSinceLastMoved.Value += Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
        if (player.timerSinceLastMovement == 0)
            TimeSinceLastMoved.Value = 0;
        if (player.UsingTool)
            TimeSinceLastMoved.Value = 0;

        // health regen
        if (Config.RegenHealthConstant)
            Health.Value += Config.RegenHealthConstantAmountPerSecond * ElapsedSeconds;
        if (Config.RegenHealthStill)
        {
            if (TimeSinceLastMoved.Value > Config.RegenHealthStillTimeRequiredMS)
                Health.Value += Config.RegenHealthStillAmountPerSecond * ElapsedSeconds;
        }
        if (player.health + Health.Value >= player.maxHealth)
        {
            player.health = player.maxHealth;
            Health.Value = 0;
        }
        else if (Health.Value >= 1)
        {
            player.health += 1;
            Health.Value -= 1;
        }
        else if (Health.Value <= -1)
        {
            player.health -= 1;
            Health.Value += 1;
        }

        // stamina regen
        if (Config.RegenStaminaConstant)
            Stamina.Value += Config.RegenStaminaConstantAmountPerSecond * ElapsedSeconds;
        if (Config.RegenStaminaStill)
        {
            if (TimeSinceLastMoved.Value > Config.RegenStaminaStillTimeRequiredMS)
                Stamina.Value += Config.RegenStaminaStillAmountPerSecond * ElapsedSeconds;
        }
        if (player.Stamina + Stamina.Value >= player.MaxStamina)
        {
            player.Stamina = player.MaxStamina;
            Stamina.Value = 0;
        }
        else if (Stamina.Value >= 1)
        {
            player.Stamina += 1;
            Stamina.Value -= 1;
        }
        else if (Stamina.Value <= -1)
        {
            player.Stamina -= 1;
            Stamina.Value += 1;
        }
    }
}