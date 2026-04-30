using Alca259.Common;
using Hydraulics.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Hydraulics;

public partial class ModEntry : Mod
{
    #region Fields
    private const string SaveDataKey = "hydraulic-network";
    private const string PumpBigCraftableId = "Alca259.Hydraulics_WaterPump";

    private ModConfig _config = null!;
    private HydraulicNetwork _network = new();
    private bool _pipeEditMode;
    private bool _isDraggingPipe;
    private int _pendingIrrigationSyncTicks;
    #endregion

    #region Override entry point
    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        CommonHelper.RemoveObsoleteFiles(this, "Hydraulics.pdb");
        _config = helper.ReadConfig<ModConfig>();
        _config.EnsureArguments();

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.Saving += OnSaving;
        helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Input.ButtonsChanged += OnButtonsChanged;
        helper.Events.Input.CursorMoved += OnCursorMoved;
        helper.Events.World.ObjectListChanged += OnObjectListChanged;
        helper.Events.Display.RenderedWorld += OnRenderedWorld;
    }
    #endregion

    #region Event Handlers
    private void OnDayStarted(object? sender, DayStartedEventArgs e)
    {
        if (!_config.EnableMod)
            return;

        if (!Context.IsWorldReady)
            return;

        if (Game1.isRaining)
            return;

        RecalculateAndApplyIrrigation();
    }

    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!_config.EnableMod || !Context.IsWorldReady)
            return;

        if (_config.ReloadKey.JustPressed())
        {
            _config = Helper.ReadConfig<ModConfig>();
            _config.EnsureArguments();
            Monitor.Log("Config reloaded", LogLevel.Info);
        }

        if (_config.TogglePipeEditModeKey.JustPressed())
        {
            _pipeEditMode = !_pipeEditMode;
            _isDraggingPipe = false;
            Monitor.Log($"Pipe edit mode: {(_pipeEditMode ? "ON" : "OFF")}", LogLevel.Info);
        }

        if (!_pipeEditMode && e.Pressed.Contains(SButton.MouseRight))
        {
            TryRecalculateByPumpClick();
        }

        if (e.Pressed.Contains(SButton.MouseLeft) && Game1.player.CurrentTool is StardewValley.Tools.Hoe)
        {
            RequestIrrigationSync();
        }

        if (!_pipeEditMode || !CanEditAtCurrentLocation())
            return;

        if (e.Pressed.Contains(SButton.MouseLeft))
        {
            _isDraggingPipe = true;
            TryAddPipeAtCursor();
        }

        if (e.Released.Contains(SButton.MouseLeft))
        {
            _isDraggingPipe = false;
        }

        if (e.Pressed.Contains(SButton.MouseRight))
        {
            TryRemovePipeAtCursor();
        }
    }

    private void OnCursorMoved(object? sender, CursorMovedEventArgs e)
    {
        if (!_config.EnableMod || !_pipeEditMode || !_isDraggingPipe)
            return;

        if (!Context.IsWorldReady || !CanEditAtCurrentLocation())
            return;

        TryAddPipeAtCursor();
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        SetupGenericModMenu();
    }

    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!_config.EnableMod || !Context.IsWorldReady)
            return;

        if (_pendingIrrigationSyncTicks <= 0)
            return;

        RecalculateAndApplyIrrigation();
        _pendingIrrigationSyncTicks--;
    }

    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _network = HydraulicNetwork.FromSaveData(Helper.Data.ReadSaveData<HydraulicSaveData>(SaveDataKey));
        SyncPumpsFromWorld();
        RecalculateAndApplyIrrigation();
    }

    private void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e)
    {
        if (!_config.EnableMod || !Context.IsWorldReady)
            return;

        if (!HydraulicWorldRules.IsMainlandFarm(e.Location))
            return;

        bool changed = false;

        foreach ((Vector2 tile, StardewValley.Object obj) in e.Added)
        {
            if (!IsHydraulicPumpObject(obj))
                continue;

            if (_network.ContainsPipe(tile))
            {
                _network.TryRemovePipe(tile);
                changed = true;
            }

            changed |= _network.TryAddPump(tile);
        }

        foreach ((Vector2 tile, StardewValley.Object? obj) in e.Removed)
        {
            if (obj is null || !IsHydraulicPumpObject(obj))
                continue;

            changed |= _network.TryRemovePump(tile);
        }

        if (changed)
        {
            RecalculateAndApplyIrrigation();
            RequestIrrigationSync();
        }
    }

    private void OnSaving(object? sender, SavingEventArgs e)
    {
        Helper.Data.WriteSaveData(SaveDataKey, _network.ToSaveData());
    }

    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        _network = new HydraulicNetwork();
        _pipeEditMode = false;
        _isDraggingPipe = false;
        _pendingIrrigationSyncTicks = 0;
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!_config.EnableMod || !Context.IsWorldReady)
            return;

        if (_pipeEditMode)
        {
            DrawHydraulics(e.SpriteBatch);
            DrawPumpPlacementOverlay(e.SpriteBatch);
        }
    }

    private bool CanEditAtCurrentLocation()
    {
        return HydraulicWorldRules.IsMainlandFarm(Game1.currentLocation);
    }

    private void TryAddPipeAtCursor()
    {
        Vector2 tile = Helper.Input.GetCursorPosition().Tile;
        GameLocation location = Game1.currentLocation;

        if (!HydraulicWorldRules.CanPlacePipeOnTile(location, tile))
            return;

        if (_network.ContainsPump(tile))
            return;

        if (!_network.TryAddPipe(tile))
            return;

        RecalculateAndApplyIrrigation();
        RequestIrrigationSync();
    }

    private void TryRemovePipeAtCursor()
    {
        Vector2 tile = Helper.Input.GetCursorPosition().Tile;
        if (_network.TryRemovePipe(tile))
        {
            RecalculateAndApplyIrrigation();
            RequestIrrigationSync();
        }
    }

    private void TryRecalculateByPumpClick()
    {
        Vector2 tile = Helper.Input.GetCursorPosition().Tile;
        if (!_network.ContainsPump(tile))
            return;

        RecalculateAndApplyIrrigation();
        RequestIrrigationSync();
    }

    private void RequestIrrigationSync(int ticks = 60)
    {
        if (ticks < 1)
            ticks = 1;

        if (_pendingIrrigationSyncTicks < ticks)
            _pendingIrrigationSyncTicks = ticks;
    }

    private void DrawHydraulics(SpriteBatch spriteBatch)
    {
        foreach (HydraulicPipe pipe in _network.Pipes.Values)
        {
            pipe.Draw(spriteBatch, _network.Pumps);
        }

        DrawPumps(spriteBatch);
    }

    private void DrawPumps(SpriteBatch spriteBatch)
    {
        foreach (WaterPumpMachine pump in _network.Pumps)
        {
            Color color = pump.PowerMode switch
            {
                PumpPowerMode.SolarPanel => new Color(255, 170, 40),
                PumpPowerMode.DebugBypass => new Color(80, 230, 120),
                _ => new Color(120, 120, 120),
            };

            Vector2 screen = Game1.GlobalToLocal(Game1.viewport, pump.Tile * Game1.tileSize);
            int inset = Game1.pixelZoom * 2;

            spriteBatch.Draw(
                Game1.staminaRect,
                new Rectangle(
                    (int)screen.X + inset,
                    (int)screen.Y + inset,
                    Game1.tileSize - (inset * 2),
                    Game1.tileSize - (inset * 2)),
                color);
        }
    }

    private void RecalculateAndApplyIrrigation()
    {
        Farm farm = Game1.getFarm();
        _network.RecalculateWater(farm, _config.RequireEnergyForPumps);

        foreach (HydraulicPipe pipe in _network.Pipes.Values)
        {
            if (!pipe.HasWater)
                continue;

            if (!farm.terrainFeatures.TryGetValue(pipe.Tile, out TerrainFeature? feature))
                continue;

            if (feature is HoeDirt dirt)
            {
                dirt.state.Value = HoeDirt.watered;
            }
        }
    }

    private void SyncPumpsFromWorld()
    {
        Farm farm = Game1.getFarm();

        foreach ((Vector2 tile, StardewValley.Object obj) in farm.Objects.Pairs)
        {
            if (!IsHydraulicPumpObject(obj))
                continue;

            _network.TryAddPump(tile);
        }
    }

    private static bool IsHydraulicPumpObject(StardewValley.Object obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return obj.bigCraftable.Value
            && string.Equals(obj.QualifiedItemId, $"(BC){PumpBigCraftableId}", StringComparison.Ordinal);
    }

    private void DrawPumpPlacementOverlay(SpriteBatch spriteBatch)
    {
        if (!_pipeEditMode || !CanEditAtCurrentLocation())
            return;

        Vector2 tile = Helper.Input.GetCursorPosition().Tile;
        GameLocation location = Game1.currentLocation;
        bool canPlace = HydraulicWorldRules.CanPlacePipeOnTile(location, tile) && !_network.ContainsPump(tile);
        Color color = canPlace
            ? new Color(40, 160, 255, 90)
            : new Color(220, 40, 40, 90);

        Vector2 screen = Game1.GlobalToLocal(Game1.viewport, tile * Game1.tileSize);
        spriteBatch.Draw(
            Game1.staminaRect,
            new Rectangle((int)screen.X, (int)screen.Y, Game1.tileSize, Game1.tileSize),
            color);
    }

    #endregion
}
