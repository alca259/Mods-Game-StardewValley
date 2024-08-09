using Alca259.Common;
using RegenMod.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using SFarmer = StardewValley.Farmer;

namespace RegenMod;

/// <summary>The main entry point.</summary>
public partial class ModEntry : Mod
{
    #region Fields
    /// <summary>The mod configuration.</summary>
    private ModConfig _config;
    /// <summary>The health regen carried over from the previous tick.</summary>
    private readonly PerScreen<float> _health = new();
    /// <summary>The stamina regen carried over from the previous tick.</summary>
    private readonly PerScreen<float> _stamina = new();
    /// <summary>The time in milliseconds since the player last moved or used a tool.</summary>
    private readonly PerScreen<double> _timeSinceLastMoved = new();
    #endregion

    #region Properties
    private float ElapsedSeconds => (float)(Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds / 1000);
    #endregion

    #region Override entry point
    /// <summary>The mod entry point, called after the mod is first loaded.</summary>
    /// <param name="helper">Provides methods for interacting with the mod directory, such as read/writing a config file or custom JSON files.</param>
    public override void Entry(IModHelper helper)
    {
        CommonHelper.RemoveObsoleteFiles(this, "RegenMod.pdb");

        _config = helper.ReadConfig<ModConfig>();

        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Input.ButtonsChanged += OnButtonChanged;
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
    }
    #endregion

    #region Event handlers
    /// <inheritdoc cref="IInputEvents.ButtonsChanged"/>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event arguments.</param>
    private void OnButtonChanged(object sender, ButtonsChangedEventArgs e)
    {
        if (_config.ReloadKey.JustPressed())
        {
            _config = Helper.ReadConfig<ModConfig>();
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
        _timeSinceLastMoved.Value += Game1.currentGameTime.ElapsedGameTime.TotalMilliseconds;
        if (player.timerSinceLastMovement == 0)
        {
            _timeSinceLastMoved.Value = 0;
        }

        if (player.UsingTool)
        {
            _timeSinceLastMoved.Value = 0;
        }

        // health regen
        HealthRegenTick(player);

        // stamina regen
        StaminaRegenTick(player);
    }

    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
        SetupGenericModMenu();
    }
    #endregion

    #region Methods
    private void StaminaRegenTick(SFarmer player)
    {
        if (_config.RegenStaminaConstant)
        {
            _stamina.Value += _config.RegenStaminaConstantAmountPerSecond * ElapsedSeconds;
        }

        if (_config.RegenStaminaStill && _timeSinceLastMoved.Value > _config.RegenStaminaStillTimeRequiredMS)
        {
            _stamina.Value += _config.RegenStaminaStillAmountPerSecond * ElapsedSeconds;
        }

        if (player.Stamina + _stamina.Value >= player.MaxStamina)
        {
            player.Stamina = player.MaxStamina;
            _stamina.Value = 0;
        }
        else if (_stamina.Value >= 1)
        {
            player.Stamina += 1;
            _stamina.Value -= 1;
        }
        else if (_stamina.Value <= -1)
        {
            player.Stamina -= 1;
            _stamina.Value += 1;
        }
    }

    private void HealthRegenTick(SFarmer player)
    {
        if (_config.RegenHealthConstant)
        {
            _health.Value += _config.RegenHealthConstantAmountPerSecond * ElapsedSeconds;
        }

        if (_config.RegenHealthStill && _timeSinceLastMoved.Value > _config.RegenHealthStillTimeRequiredMS)
        {
            _health.Value += _config.RegenHealthStillAmountPerSecond * ElapsedSeconds;
        }

        if (player.health + _health.Value >= player.maxHealth)
        {
            player.health = player.maxHealth;
            _health.Value = 0;
        }
        else if (_health.Value >= 1)
        {
            player.health += 1;
            _health.Value -= 1;
        }
        else if (_health.Value <= -1)
        {
            player.health -= 1;
            _health.Value += 1;
        }
    }
    #endregion
}