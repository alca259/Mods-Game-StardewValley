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
    private const string BronzePumpBigCraftableId = "Alca259.Hydraulics_BronzeWaterPump";
    private const string SteelPumpBigCraftableId = "Alca259.Hydraulics_SteelWaterPump";
    private const string GoldPumpBigCraftableId = "Alca259.Hydraulics_GoldWaterPump";
    private const string IridiumPumpBigCraftableId = "Alca259.Hydraulics_IridiumWaterPump";

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
            Monitor.Log(Helper.Translation.Get("log.configReloaded"), LogLevel.Info);
        }

        if (_config.TogglePipeEditModeKey.JustPressed())
        {
            _pipeEditMode = !_pipeEditMode;
            _isDraggingPipe = false;
            Monitor.Log(Helper.Translation.Get("log.pipeEditMode", new { state = _pipeEditMode ? "ON" : "OFF" }), LogLevel.Info);
        }

        if (_config.ToggleGridOverlayKey.JustPressed())
        {
            _config.ShowGridOverlay = !_config.ShowGridOverlay;
            Monitor.Log(Helper.Translation.Get("log.gridOverlay", new { state = _config.ShowGridOverlay ? "ON" : "OFF" }), LogLevel.Info);
        }

        if (!_pipeEditMode && e.Pressed.Contains(SButton.MouseRight))
        {
            TryRecalculateByPumpClick();
        }

        if (e.Pressed.Contains(SButton.MouseLeft) && Game1.player.CurrentTool is StardewValley.Tools.Hoe)
        {
            RequestIrrigationSync();
        }

        if (Game1.player.ActiveObject is not null)
            return;

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
            if (!TryGetPumpTier(obj, out WaterPumpTier tier))
                continue;

            if (_network.ContainsPipe(tile))
            {
                _network.TryRemovePipe(tile);
                changed = true;
            }

            changed |= _network.TryAddPump(tile, tier);
        }

        foreach ((Vector2 tile, StardewValley.Object? obj) in e.Removed)
        {
            if (obj is null || !TryGetPumpTier(obj, out _))
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

            if (_config.ShowGridOverlay)
            {
                DrawPumpPlacementOverlay(e.SpriteBatch);
            }
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

        if (!TryApplyPipeBuildCost())
        {
            _network.TryRemovePipe(tile);
            return;
        }

        RecalculateAndApplyIrrigation();
        RequestIrrigationSync();
    }

    private void TryRemovePipeAtCursor()
    {
        Vector2 tile = Helper.Input.GetCursorPosition().Tile;
        if (_network.TryRemovePipe(tile))
        {
            ApplyPipeDestroyRefund();
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
            pipe.Draw(spriteBatch, _network.Pumps, GetUnpoweredPipeColor(), GetPoweredPipeColor());
        }

        DrawPumps(spriteBatch);
    }

    private void DrawPumps(SpriteBatch spriteBatch)
    {
        foreach (WaterPumpMachine pump in _network.Pumps)
        {
            Color color = pump.PowerMode switch
            {
                PumpPowerMode.SolarPanel => GetPoweredObjectColor(),
                PumpPowerMode.DebugBypass => GetPoweredObjectColor(),
                _ => GetUnpoweredObjectColor(),
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
        _network.RecalculateWater(farm, _config.RequireEnergyForPumps, _config.WaterCostPerTile);

        foreach (HydraulicPipe pipe in _network.Pipes.Values)
        {
            if (!pipe.HasWater)
                continue;

            if (!farm.terrainFeatures.TryGetValue(pipe.Tile, out TerrainFeature? feature))
                continue;

            if (feature is HoeDirt dirt)
            {
                dirt.state.Value = HoeDirt.watered;

                if (_config.PlaySprinklerAnimation)
                {
                    farm.temporarySprites.Add(new TemporaryAnimatedSprite(
                        rowInAnimationTexture: 13,
                        position: pipe.Tile * Game1.tileSize,
                        color: Color.White,
                        animationLength: 4,
                        flipped: false,
                        animationInterval: 30f));
                }
            }
        }
    }

    private void SyncPumpsFromWorld()
    {
        Farm farm = Game1.getFarm();
        _network.ClearPumps();

        foreach ((Vector2 tile, StardewValley.Object obj) in farm.Objects.Pairs)
        {
            if (!TryGetPumpTier(obj, out WaterPumpTier tier))
                continue;

            _network.TryAddPump(tile, tier);
        }
    }

    private bool TryApplyPipeBuildCost()
    {
        if (_config.PipeBuildGoldCost > 0 && Game1.player.Money < _config.PipeBuildGoldCost)
            return false;

        if (_config.PipeBuildCopperOreCost > 0)
        {
            int playerOreCount = Game1.player.Items
                .Where(item => item is not null && item.QualifiedItemId == "(O)378")
                .Sum(item => item?.Stack ?? 0);

            if (playerOreCount < _config.PipeBuildCopperOreCost)
                return false;
        }

        if (_config.PipeBuildGoldCost > 0)
            Game1.player.Money -= _config.PipeBuildGoldCost;

        if (_config.PipeBuildCopperOreCost > 0)
            RemoveItemFromInventory("(O)378", _config.PipeBuildCopperOreCost);

        return true;
    }

    private void ApplyPipeDestroyRefund()
    {
        if (_config.PipeDestroyGoldRefund > 0)
            Game1.player.Money += _config.PipeDestroyGoldRefund;

        if (_config.PipeDestroyCopperOreRefund > 0)
            Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)378", _config.PipeDestroyCopperOreRefund));
    }

    private static void RemoveItemFromInventory(string qualifiedItemId, int amount)
    {
        if (amount <= 0)
            return;

        int remaining = amount;
        IList<Item?> items = Game1.player.Items;

        for (int i = 0; i < items.Count && remaining > 0; i++)
        {
            Item? item = items[i];
            if (item is null || !string.Equals(item.QualifiedItemId, qualifiedItemId, StringComparison.Ordinal))
                continue;

            int take = Math.Min(item.Stack, remaining);
            item.Stack -= take;
            remaining -= take;

            if (item.Stack <= 0)
                items[i] = null;
        }
    }

    private static bool TryGetPumpTier(StardewValley.Object obj, out WaterPumpTier tier)
    {
        ArgumentNullException.ThrowIfNull(obj);

        tier = WaterPumpTier.Bronze;
        if (!obj.bigCraftable.Value)
            return false;

        if (string.Equals(obj.QualifiedItemId, $"(BC){BronzePumpBigCraftableId}", StringComparison.Ordinal))
        {
            tier = WaterPumpTier.Bronze;
            return true;
        }

        if (string.Equals(obj.QualifiedItemId, $"(BC){SteelPumpBigCraftableId}", StringComparison.Ordinal))
        {
            tier = WaterPumpTier.Steel;
            return true;
        }

        if (string.Equals(obj.QualifiedItemId, $"(BC){GoldPumpBigCraftableId}", StringComparison.Ordinal))
        {
            tier = WaterPumpTier.Gold;
            return true;
        }

        if (string.Equals(obj.QualifiedItemId, $"(BC){IridiumPumpBigCraftableId}", StringComparison.Ordinal))
        {
            tier = WaterPumpTier.Iridium;
            return true;
        }

        return false;
    }

    private Color GetPoweredPipeColor() => new(66, 158, 255);

    private Color GetUnpoweredPipeColor() => new(255, 220, 0);

    private Color GetPoweredObjectColor() => new(80, 230, 120);

    private Color GetUnpoweredObjectColor() => new(120, 120, 120);

    private Color GetOverlayPlaceableColor() => new(40, 160, 255, 90);

    private Color GetOverlayBlockedColor() => new(220, 40, 40, 90);

    private Color GetOverlayPoweredGridColor() => new(66, 158, 255, 40);

    private Color GetOverlayUnpoweredGridColor() => new(255, 220, 0, 35);

    private Color GetOverlayPoweredObjectColor() => new(80, 230, 120, 70);

    private Color GetOverlayUnpoweredObjectColor() => new(120, 120, 120, 70);

    private void DrawPumpPlacementOverlay(SpriteBatch spriteBatch)
    {
        if (!_pipeEditMode || !CanEditAtCurrentLocation())
            return;

        DrawGridStateOverlay(spriteBatch);

        Vector2 tile = Helper.Input.GetCursorPosition().Tile;
        GameLocation location = Game1.currentLocation;
        bool canPlace = HydraulicWorldRules.CanPlacePipeOnTile(location, tile) && !_network.ContainsPump(tile);
        Color color = canPlace
            ? GetOverlayPlaceableColor()
            : GetOverlayBlockedColor();

        Vector2 screen = Game1.GlobalToLocal(Game1.viewport, tile * Game1.tileSize);
        spriteBatch.Draw(
            Game1.staminaRect,
            new Rectangle((int)screen.X, (int)screen.Y, Game1.tileSize, Game1.tileSize),
            color);
    }

    private void DrawGridStateOverlay(SpriteBatch spriteBatch)
    {
        foreach (HydraulicPipe pipe in _network.Pipes.Values)
        {
            Color gridColor = pipe.HasWater ? GetOverlayPoweredGridColor() : GetOverlayUnpoweredGridColor();
            Vector2 pipeScreen = Game1.GlobalToLocal(Game1.viewport, pipe.Tile * Game1.tileSize);

            spriteBatch.Draw(
                Game1.staminaRect,
                new Rectangle((int)pipeScreen.X, (int)pipeScreen.Y, Game1.tileSize, Game1.tileSize),
                gridColor);

            if (_config.ShowWateredTileIndicator && pipe.HasWater)
            {
                int iconSize = Game1.pixelZoom * 2;
                spriteBatch.Draw(
                    Game1.staminaRect,
                    new Rectangle((int)pipeScreen.X + Game1.pixelZoom, (int)pipeScreen.Y + Game1.pixelZoom, iconSize, iconSize),
                    new Color(66, 158, 255, 190));
            }
        }

        foreach (WaterPumpMachine pump in _network.Pumps)
        {
            bool powered = pump.PowerMode is PumpPowerMode.SolarPanel or PumpPowerMode.DebugBypass;
            Color objectColor = powered ? GetOverlayPoweredObjectColor() : GetOverlayUnpoweredObjectColor();
            Vector2 screen = Game1.GlobalToLocal(Game1.viewport, pump.Tile * Game1.tileSize);
            int inset = Game1.pixelZoom;

            spriteBatch.Draw(
                Game1.staminaRect,
                new Rectangle(
                    (int)screen.X + inset,
                    (int)screen.Y + inset,
                    Game1.tileSize - (inset * 2),
                    Game1.tileSize - (inset * 2)),
                objectColor);
        }
    }

    #endregion
}
