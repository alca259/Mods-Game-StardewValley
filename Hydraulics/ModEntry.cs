using Alca259.Common;
using Hydraulics.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
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
            Monitor.Log(Helper.Translation.Get("log.configReloaded"), LogLevel.Debug);
        }

        if (_config.TogglePipeEditModeKey.JustPressed())
        {
            _pipeEditMode = !_pipeEditMode;
            _isDraggingPipe = false;
            Monitor.Log(Helper.Translation.Get("log.pipeEditMode", new { state = _pipeEditMode ? "ON" : "OFF" }), LogLevel.Debug);
        }

        if (!_pipeEditMode && e.Pressed.Contains(SButton.MouseRight))
        {
            TryRecalculateByPumpClick();
        }

        if (e.Pressed.Contains(SButton.MouseLeft) && Game1.player.CurrentTool is StardewValley.Tools.Hoe)
        {
            Vector2 tilledTile = Helper.Input.GetCursorPosition().Tile;
            if (_network.ContainsPipe(tilledTile))
                RequestIrrigationSyncForPipe(tilledTile);
        }

        if (Game1.player.ActiveObject is not null)
            return;

        if (_pipeEditMode && CanEditAtCurrentLocation())
        {
            if (!CanPlayerEditPipesNow())
            {
                _isDraggingPipe = false;
                return;
            }

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
    }

    private void OnCursorMoved(object? sender, CursorMovedEventArgs e)
    {
        if (!_config.EnableMod || !_pipeEditMode || !_isDraggingPipe)
            return;

        if (!Context.IsWorldReady || !CanEditAtCurrentLocation())
            return;

        if (!CanPlayerEditPipesNow())
        {
            _isDraggingPipe = false;
            return;
        }

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

        _pendingIrrigationSyncTicks--;

        if (_pendingIrrigationSyncTicks == 0)
            RecalculateAndApplyIrrigation();
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
            DrawPipePlacementOverlay(e.SpriteBatch);
            DrawSubnetworkFlowInfo(e.SpriteBatch);
        }
    }

    private bool CanEditAtCurrentLocation()
    {
        return HydraulicWorldRules.IsMainlandFarm(Game1.currentLocation);
    }

    private static bool CanPlayerEditPipesNow()
    {
        if (!Context.IsPlayerFree)
            return false;

        if (Game1.activeClickableMenu is not null)
            return false;

        if (Game1.dialogueUp)
            return false;

        return true;
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

        RequestIrrigationSyncForPump(tile);
    }

    private void RequestIrrigationSync(int ticks = 4)
    {
        if (ticks < 1)
            ticks = 1;

        if (_pendingIrrigationSyncTicks < ticks)
            _pendingIrrigationSyncTicks = ticks;
    }

    private void RequestIrrigationSyncForPipe(Vector2 tile, int ticks = 4)
    {
        if (!_network.RecalculateSubnetworkAtTile(Game1.getFarm(), tile, _config.RequireEnergyForPumps, _config.WaterCostPerTile))
            return;

        RecalculateAndApplyIrrigation();
        RequestIrrigationSync(ticks);
    }

    private void RequestIrrigationSyncForPump(Vector2 tile, int ticks = 4)
    {
        if (!_network.RecalculateSubnetworkAtPumpTile(Game1.getFarm(), tile, _config.RequireEnergyForPumps, _config.WaterCostPerTile))
            return;

        RecalculateAndApplyIrrigation();
        RequestIrrigationSync(ticks);
    }

    private void DrawHydraulics(SpriteBatch spriteBatch)
    {
        foreach (HydraulicPipe pipe in _network.Pipes.Values)
        {
            pipe.Draw(spriteBatch, _network.Pumps, GetUnpoweredPipeColor(), GetPoweredPipeColor());
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
                        animationLength: 8,
                        flipped: false,
                        animationInterval: 60f));
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
                .Where(item => item is not null && item.QualifiedItemId == HydraulicConstants.CopperOreId)
                .Sum(item => item?.Stack ?? 0);

            if (playerOreCount < _config.PipeBuildCopperOreCost)
                return false;
        }

        if (_config.PipeBuildGoldCost > 0)
            Game1.player.Money -= _config.PipeBuildGoldCost;

        if (_config.PipeBuildCopperOreCost > 0)
            RemoveItemFromInventory(HydraulicConstants.CopperOreId, _config.PipeBuildCopperOreCost);

        return true;
    }

    private void ApplyPipeDestroyRefund()
    {
        if (_config.PipeDestroyGoldRefund > 0)
            Game1.player.Money += _config.PipeDestroyGoldRefund;

        if (_config.PipeDestroyCopperOreRefund > 0)
            Game1.player.addItemToInventoryBool(ItemRegistry.Create(HydraulicConstants.CopperOreId, _config.PipeDestroyCopperOreRefund));
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

    private static Color GetPoweredPipeColor() => new(66, 158, 255, 64);

    private static Color GetUnpoweredPipeColor() => new(255, 220, 0, 64);

    private static Color GetOverlayPlaceableColor() => new(60, 200, 80, 220);

    private static Color GetOverlayBlockedColor() => new(220, 40, 40, 220);

    private static Color GetOverlayNetworkTextColor() => Color.White;

    private void DrawSubnetworkFlowInfo(SpriteBatch spriteBatch)
    {
        foreach (HydraulicSubnetworkStatus status in _network.SubnetworkStatuses)
        {
            Vector2 screen = Game1.GlobalToLocal(Game1.viewport, status.LabelPumpTile * Game1.tileSize);
            string networkLabel = $"#{status.Id.ToString()[..8]}";
            string flowLabel = $"{status.ConsumptionFlow:0.##}/{status.MaxFlow:0.##}";

            Utility.drawTextWithShadow(
                spriteBatch,
                networkLabel,
                Game1.smallFont,
                new Vector2(screen.X, screen.Y - (Game1.tileSize / 2f) - 24f),
                GetOverlayNetworkTextColor());

            Utility.drawTextWithShadow(
                spriteBatch,
                flowLabel,
                Game1.smallFont,
                new Vector2(screen.X, screen.Y - (Game1.tileSize / 2f) - 2f),
                GetOverlayNetworkTextColor());
        }
    }

    private void DrawPipePlacementOverlay(SpriteBatch spriteBatch)
    {
        if (!_pipeEditMode || !CanEditAtCurrentLocation())
            return;

        Vector2 tile = Helper.Input.GetCursorPosition().Tile;
        GameLocation location = Game1.currentLocation;
        bool canPlace = HydraulicWorldRules.CanPlacePipeOnTile(location, tile)
            && !_network.ContainsPump(tile)
            && !_network.ContainsPipe(tile);
        Color color = canPlace
            ? GetOverlayPlaceableColor()
            : GetOverlayBlockedColor();

        Vector2 screen = Game1.GlobalToLocal(Game1.viewport, tile * Game1.tileSize);
        int x = (int)screen.X;
        int y = (int)screen.Y;
        int size = Game1.tileSize;
        int border = Math.Max(1, Game1.pixelZoom / 2);

        spriteBatch.Draw(
            Game1.staminaRect,
            new Rectangle(x, y, size, border),
            color);

        spriteBatch.Draw(
            Game1.staminaRect,
            new Rectangle(x, y + size - border, size, border),
            color);

        spriteBatch.Draw(
            Game1.staminaRect,
            new Rectangle(x, y, border, size),
            color);

        spriteBatch.Draw(
            Game1.staminaRect,
            new Rectangle(x + size - border, y, border, size),
            color);
    }
    #endregion
}
