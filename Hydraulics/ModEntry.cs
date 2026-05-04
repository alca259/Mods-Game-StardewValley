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
    private const string IrrigationRequestMessageType = "irrigation-request";
    private const string BronzePumpBigCraftableId = "Alca259.Hydraulics_BronzeWaterPump";
    private const string SteelPumpBigCraftableId = "Alca259.Hydraulics_SteelWaterPump";
    private const string GoldPumpBigCraftableId = "Alca259.Hydraulics_GoldWaterPump";
    private const string IridiumPumpBigCraftableId = "Alca259.Hydraulics_IridiumWaterPump";

    private ModConfig _config = null!;
    private HydraulicNetwork _network = new();
    private bool _pipeEditMode;
    private bool _isDraggingPipe;
    private bool _isRemovingPipe;
    private Point? _lastDraggedPipeTile;
    private bool _lastDragWasRemoving;
    private int _pendingIrrigationSyncTicks;

    private enum IrrigationRequestKind
    {
        PipeTile,
        PumpTile,
    }

    private readonly record struct IrrigationRequestData(IrrigationRequestKind Kind, int TileX, int TileY);
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
        helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        helper.Events.Display.RenderedWorld += OnRenderedWorld;
    }
    #endregion

    #region Event Handlers
    /// <summary>Recalcula el riego al comenzar el día si procede.</summary>
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

    /// <summary>Gestiona teclas y clics para edición y sincronización de riego.</summary>
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
            _isRemovingPipe = false;
            _lastDraggedPipeTile = null;
            Monitor.Log(Helper.Translation.Get("log.pipeEditMode", new { state = _pipeEditMode ? "ON" : "OFF" }), LogLevel.Debug);
        }

        if (!_pipeEditMode && e.Pressed.Contains(SButton.MouseRight))
        {
            TryRecalculateByPumpClick();
        }

        if (!_pipeEditMode && e.Pressed.Contains(SButton.MouseLeft) && Game1.player.CurrentTool is StardewValley.Tools.Hoe)
        {
            Vector2 tilledTile = Helper.Input.GetCursorPosition().Tile;
            if (_network.ContainsPipe(tilledTile))
                RequestIrrigationSyncForPipe(tilledTile);
        }

        if (_pipeEditMode && CanEditAtCurrentLocation())
        {
            if (e.Pressed.Contains(SButton.MouseLeft))
                Helper.Input.Suppress(SButton.MouseLeft);

            if (e.Pressed.Contains(SButton.MouseRight))
                Helper.Input.Suppress(SButton.MouseRight);

            if (!CanPlayerEditPipesNow())
            {
                _isDraggingPipe = false;
                _isRemovingPipe = false;
                _lastDraggedPipeTile = null;
                return;
            }

            if (e.Pressed.Contains(SButton.MouseLeft))
            {
                _isDraggingPipe = true;
                _isRemovingPipe = false;
                _lastDraggedPipeTile = null;
                TryAddPipeAtCursor();
            }

            if (e.Released.Contains(SButton.MouseLeft) && !_isRemovingPipe)
            {
                _isDraggingPipe = false;
                _lastDraggedPipeTile = null;
            }

            if (e.Pressed.Contains(SButton.MouseRight))
            {
                _isDraggingPipe = true;
                _isRemovingPipe = true;
                _lastDraggedPipeTile = null;
                TryRemovePipeAtCursor();
            }

            if (e.Released.Contains(SButton.MouseRight) && _isRemovingPipe)
            {
                _isDraggingPipe = false;
                _isRemovingPipe = false;
                _lastDraggedPipeTile = null;
            }
        }
    }

    /// <summary>Procesa mensajes de sincronización de riego en multijugador.</summary>
    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (!_config.EnableMod || !Context.IsWorldReady)
            return;

        if (!CanMutateNetwork())
            return;

        if (!string.Equals(e.FromModID, ModManifest.UniqueID, StringComparison.Ordinal))
            return;

        if (!string.Equals(e.Type, IrrigationRequestMessageType, StringComparison.Ordinal))
            return;

        IrrigationRequestData request = e.ReadAs<IrrigationRequestData>();
        Vector2 tile = new(request.TileX, request.TileY);

        switch (request.Kind)
        {
            case IrrigationRequestKind.PipeTile:
                RequestIrrigationSyncForPipe(tile);
                break;

            case IrrigationRequestKind.PumpTile:
                RequestIrrigationSyncForPump(tile);
                break;
        }
    }

    /// <summary>Gestiona el arrastre para colocar o quitar tuberías.</summary>
    private void OnCursorMoved(object? sender, CursorMovedEventArgs e)
    {
        if (!_config.EnableMod || !_pipeEditMode || !_isDraggingPipe)
            return;

        if (!Context.IsWorldReady || !CanEditAtCurrentLocation())
            return;

        if (!CanPlayerEditPipesNow())
        {
            _isDraggingPipe = false;
            _isRemovingPipe = false;
            _lastDraggedPipeTile = null;
            return;
        }

        Vector2 dragTile = Helper.Input.GetCursorPosition().Tile;
        Point tilePoint = new((int)dragTile.X, (int)dragTile.Y);
        if (_lastDraggedPipeTile is Point lastTile
            && lastTile == tilePoint
            && _lastDragWasRemoving == _isRemovingPipe)
        {
            return;
        }

        _lastDraggedPipeTile = tilePoint;
        _lastDragWasRemoving = _isRemovingPipe;

        if (_isRemovingPipe)
            TryRemovePipeAtCursor();
        else
            TryAddPipeAtCursor();
    }

    /// <summary>Inicializa integraciones del mod tras cargar el juego.</summary>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        SetupGenericModMenu();
    }

    /// <summary>Ejecuta recálculos pendientes de riego por ticks.</summary>
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

    /// <summary>Carga la red guardada y sincroniza bombas del mundo.</summary>
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        _network = HydraulicNetwork.FromSaveData(Helper.Data.ReadSaveData<HydraulicSaveData>(SaveDataKey));
        SyncPumpsFromWorld();
        RecalculateAndApplyIrrigation();
    }

    /// <summary>Reacciona a cambios de objetos para mantener la red actualizada.</summary>
    private void OnObjectListChanged(object? sender, ObjectListChangedEventArgs e)
    {
        if (!_config.EnableMod || !Context.IsWorldReady)
            return;

        if (!CanMutateNetwork())
            return;

        if (!HydraulicWorldRules.IsMainlandFarm(e.Location))
            return;

        bool changed = false;
        HashSet<Vector2> pumpsToRefresh = new();

        foreach ((Vector2 tile, StardewValley.Object obj) in e.Added)
        {
            if (IsSolarPanel(obj))
                AddAdjacentPumpTiles(tile, pumpsToRefresh);

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
            if (obj is not null && IsSolarPanel(obj))
                AddAdjacentPumpTiles(tile, pumpsToRefresh);

            if (obj is null || !TryGetPumpTier(obj, out _))
                continue;

            changed |= _network.TryRemovePump(tile);
        }

        foreach (Vector2 pumpTile in pumpsToRefresh)
        {
            RequestIrrigationSyncForPump(pumpTile, 1);
        }

        if (changed)
        {
            RecalculateAndApplyIrrigation();
            RequestIrrigationSync();
        }
    }

    /// <summary>Guarda el estado actual de la red hidráulica.</summary>
    private void OnSaving(object? sender, SavingEventArgs e)
    {
        Helper.Data.WriteSaveData(SaveDataKey, _network.ToSaveData());
    }

    /// <summary>Reinicia estado temporal del mod al volver al título.</summary>
    private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e)
    {
        _network = new HydraulicNetwork();
        _pipeEditMode = false;
        _isDraggingPipe = false;
        _isRemovingPipe = false;
        _lastDraggedPipeTile = null;
        _pendingIrrigationSyncTicks = 0;
    }

    /// <summary>Dibuja overlays del sistema hidráulico durante el render del mundo.</summary>
    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!_config.EnableMod || !Context.IsWorldReady)
            return;

        DrawPumpStatusOverlay(e.SpriteBatch);

        if (_pipeEditMode)
        {
            DrawHydraulics(e.SpriteBatch);
            DrawPipePlacementOverlay(e.SpriteBatch);
            DrawSubnetworkFlowInfo(e.SpriteBatch);
        }
    }

    /// <summary>Comprueba si se puede editar en la ubicación actual.</summary>
    private bool CanEditAtCurrentLocation()
    {
        return HydraulicWorldRules.IsMainlandFarm(Game1.currentLocation);
    }

    /// <summary>Indica si el jugador actual puede mutar la red.</summary>
    private static bool CanMutateNetwork()
    {
        return Context.IsMainPlayer;
    }

    /// <summary>Comprueba si el jugador está en estado válido para editar tuberías.</summary>
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

    /// <summary>Intenta colocar una tubería en la casilla bajo el cursor.</summary>
    private void TryAddPipeAtCursor()
    {
        if (!CanMutateNetwork())
            return;

        Vector2 tile = Helper.Input.GetCursorPosition().Tile;
        GameLocation location = Game1.currentLocation;
        Guid? previousSubnetworkId = _network.TryGetSubnetworkIdByPipe(tile);

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

        RequestIrrigationSyncForPipeEdit(tile, previousSubnetworkId);
        RequestIrrigationSync();
    }

    /// <summary>Intenta quitar una tubería en la casilla bajo el cursor.</summary>
    private void TryRemovePipeAtCursor()
    {
        if (!CanMutateNetwork())
            return;

        Vector2 tile = Helper.Input.GetCursorPosition().Tile;
        Guid? targetSubnetworkId = _network.TryGetSubnetworkIdByPipe(tile);

        if (_network.TryRemovePipe(tile))
        {
            ApplyPipeDestroyRefund();
            if (targetSubnetworkId is Guid id)
                RecalculateAndApplySubnetworkIrrigation(id);

            RequestIrrigationSync();
        }
    }

    /// <summary>Solicita recálculo al hacer clic derecho sobre una bomba.</summary>
    private void TryRecalculateByPumpClick()
    {
        if (!CanMutateNetwork())
            return;

        Vector2 tile = Helper.Input.GetCursorPosition().Tile;
        if (!_network.ContainsPump(tile))
            return;

        RequestIrrigationSyncForPump(tile);
    }

    /// <summary>Programa una sincronización de riego tras un número de ticks.</summary>
    private void RequestIrrigationSync(int ticks = 4)
    {
        if (ticks < 1)
            ticks = 1;

        if (_pendingIrrigationSyncTicks < ticks)
            _pendingIrrigationSyncTicks = ticks;
    }

    /// <summary>Solicita recálculo y sincronización para la subred de una tubería.</summary>
    private void RequestIrrigationSyncForPipe(Vector2 tile, int ticks = 60)
    {
        if (!CanMutateNetwork())
        {
            SendIrrigationRequest(IrrigationRequestKind.PipeTile, tile);
            return;
        }

        if (!_network.RecalculateSubnetworkAtTile(Game1.getFarm(), tile, _config))
            return;

        Guid? subnetworkId = _network.TryGetSubnetworkIdByPipe(tile);
        if (subnetworkId is not Guid id)
            return;

        RecalculateAndApplySubnetworkIrrigation(id);
        RequestIrrigationSync(ticks);
    }

    /// <summary>Solicita recálculo y sincronización para la subred de una bomba.</summary>
    private void RequestIrrigationSyncForPump(Vector2 tile, int ticks = 4)
    {
        if (!CanMutateNetwork())
        {
            SendIrrigationRequest(IrrigationRequestKind.PumpTile, tile);
            return;
        }

        if (!_network.RecalculateSubnetworkAtPumpTile(Game1.getFarm(), tile, _config))
            return;

        Guid? subnetworkId = _network.TryGetSubnetworkIdByPump(tile);
        if (subnetworkId is not Guid id)
            return;

        RecalculateAndApplySubnetworkIrrigation(id);
        RequestIrrigationSync(ticks);
    }

    /// <summary>Envía una petición de riego al anfitrión en multijugador.</summary>
    private void SendIrrigationRequest(IrrigationRequestKind kind, Vector2 tile)
    {
        if (!Context.IsMultiplayer || Context.IsMainPlayer)
            return;

        Helper.Multiplayer.SendMessage(
            new IrrigationRequestData(kind, (int)tile.X, (int)tile.Y),
            IrrigationRequestMessageType,
            new[] { ModManifest.UniqueID },
            new[] { Game1.MasterPlayer.UniqueMultiplayerID });
    }

    /// <summary>Recalcula y aplica riego para subredes afectadas por edición de tuberías.</summary>
    private void RequestIrrigationSyncForPipeEdit(Vector2 tile, Guid? previousSubnetworkId, int ticks = 4)
    {
        if (!CanMutateNetwork())
            return;

        Guid? currentSubnetworkId = _network.TryGetSubnetworkIdByPipe(tile);
        if (previousSubnetworkId is Guid previousId)
            _network.RecalculateSubnetworkById(Game1.getFarm(), previousId, _config);

        if (currentSubnetworkId is Guid currentId)
            _network.RecalculateSubnetworkById(Game1.getFarm(), currentId, _config);

        if (previousSubnetworkId is Guid previousToApply)
            RecalculateAndApplySubnetworkIrrigation(previousToApply);

        if (currentSubnetworkId is Guid currentToApply && currentSubnetworkId != previousSubnetworkId)
            RecalculateAndApplySubnetworkIrrigation(currentToApply);

        RequestIrrigationSync(ticks);
    }

    /// <summary>Dibuja todas las tuberías de la red en pantalla.</summary>
    private void DrawHydraulics(SpriteBatch spriteBatch)
    {
        foreach (HydraulicPipe pipe in _network.Pipes.Values)
        {
            pipe.Draw(spriteBatch, _network.Pumps, GetUnpoweredPipeColor(), GetPoweredPipeColor());
        }
    }

    /// <summary>Recalcula y aplica el riego de toda la red.</summary>
    private void RecalculateAndApplyIrrigation()
    {
        Farm farm = Game1.getFarm();
        HashSet<Vector2> previouslyWateredPipeTiles = _network.Pipes.Values
            .Where(pipe => pipe.HasWater)
            .Select(pipe => pipe.Tile)
            .ToHashSet();

        _network.RecalculateWater(farm, _config);

        if (!CanMutateNetwork())
            return;

        foreach (HydraulicPipe pipe in _network.Pipes.Values)
        {
            if (!pipe.HasWater)
                continue;

            if (!farm.terrainFeatures.TryGetValue(pipe.Tile, out TerrainFeature? feature))
                continue;

            if (feature is HoeDirt dirt)
            {
                dirt.state.Value = HoeDirt.watered;

                if (_config.PlaySprinklerAnimation && !previouslyWateredPipeTiles.Contains(pipe.Tile))
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

    /// <summary>Recalcula y aplica el riego de una subred concreta.</summary>
    private void RecalculateAndApplySubnetworkIrrigation(Guid subnetworkId)
    {
        Farm farm = Game1.getFarm();
        HashSet<Vector2> subnetworkTiles = _network.GetSubnetworkPipeTiles(subnetworkId).ToHashSet();
        if (subnetworkTiles.Count == 0)
            return;

        HashSet<Vector2> previouslyWateredPipeTiles = _network.Pipes.Values
            .Where(pipe => pipe.HasWater && subnetworkTiles.Contains(pipe.Tile))
            .Select(pipe => pipe.Tile)
            .ToHashSet();

        if (!_network.RecalculateSubnetworkById(farm, subnetworkId, _config))
            return;

        if (!CanMutateNetwork())
            return;

        foreach (Vector2 tile in subnetworkTiles)
        {
            if (!_network.Pipes.TryGetValue(tile, out HydraulicPipe? pipe) || !pipe.HasWater)
                continue;

            if (!farm.terrainFeatures.TryGetValue(pipe.Tile, out TerrainFeature? feature))
                continue;

            if (feature is HoeDirt dirt)
            {
                dirt.state.Value = HoeDirt.watered;

                if (_config.PlaySprinklerAnimation && !previouslyWateredPipeTiles.Contains(pipe.Tile))
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

    /// <summary>Sincroniza las bombas de la red con los objetos del mapa.</summary>
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

    /// <summary>Intenta aplicar el coste de construcción de una tubería.</summary>
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

    /// <summary>Comprueba si un objeto es un panel solar válido.</summary>
    private static bool IsSolarPanel(StardewValley.Object obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return obj.bigCraftable.Value
            && string.Equals(obj.QualifiedItemId, HydraulicConstants.SolarPanelId, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Añade bombas adyacentes de una casilla al conjunto de refresco.</summary>
    private void AddAdjacentPumpTiles(Vector2 tile, ISet<Vector2> pumpTiles)
    {
        ArgumentNullException.ThrowIfNull(pumpTiles);

        foreach (Vector2 adjacentTile in HydraulicWorldRules.EnumerateCardinalNeighbors(tile))
        {
            if (_network.ContainsPump(adjacentTile))
                pumpTiles.Add(adjacentTile);
        }
    }

    /// <summary>Aplica el reembolso configurado al destruir una tubería.</summary>
    private void ApplyPipeDestroyRefund()
    {
        if (_config.PipeDestroyGoldRefund > 0)
            Game1.player.Money += _config.PipeDestroyGoldRefund;

        if (_config.PipeDestroyCopperOreRefund > 0)
            Game1.player.addItemToInventoryBool(ItemRegistry.Create(HydraulicConstants.CopperOreId, _config.PipeDestroyCopperOreRefund));
    }

    /// <summary>Retira una cantidad de un objeto del inventario del jugador.</summary>
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

    /// <summary>Intenta obtener el nivel de bomba a partir de un objeto colocable.</summary>
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

    private static Color GetOverlayPumpUnpoweredColor() => new(220, 50, 50, 220);

    private static Color GetOverlayPumpPoweredIdleColor() => new(255, 220, 90, 220);

    private static Color GetOverlayPumpProducingColor() => new(70, 200, 90, 220);

    /// <summary>Dibuja un indicador de estado sobre cada bomba.</summary>
    private void DrawPumpStatusOverlay(SpriteBatch spriteBatch)
    {
        if (!HydraulicWorldRules.IsMainlandFarm(Game1.currentLocation))
            return;

        Dictionary<Guid, HydraulicSubnetworkStatus> statusesById = _network.SubnetworkStatuses
            .ToDictionary(status => status.Id);

        int markerSize = Math.Max(4, Game1.pixelZoom + 1);

        foreach (WaterPumpMachine pump in _network.Pumps)
        {
            bool isPowered = pump.PowerMode != PumpPowerMode.None;
            bool isProducing = false;

            if (isPowered
                && _network.TryGetSubnetworkIdByPump(pump.Tile) is Guid subnetworkId
                && statusesById.TryGetValue(subnetworkId, out HydraulicSubnetworkStatus status))
            {
                isProducing = status.MaxFlow > 0f && status.ConsumptionFlow > 0f;
            }

            Color indicatorColor = !isPowered
                ? GetOverlayPumpUnpoweredColor()
                : (isProducing ? GetOverlayPumpProducingColor() : GetOverlayPumpPoweredIdleColor());

            Vector2 screen = Game1.GlobalToLocal(Game1.viewport, pump.Tile * Game1.tileSize);
            int x = (int)screen.X + Game1.tileSize - markerSize - 8;
            int y = (int)screen.Y - Game1.tileSize + 20;

            spriteBatch.Draw(
                Game1.staminaRect,
                new Rectangle(x - 1, y - 1, markerSize + 2, markerSize + 2),
                new Color(0, 0, 0, 150));

            spriteBatch.Draw(
                Game1.staminaRect,
                new Rectangle(x, y, markerSize, markerSize),
                indicatorColor);
        }
    }

    /// <summary>Dibuja información de caudal por subred sobre el mundo.</summary>
    private void DrawSubnetworkFlowInfo(SpriteBatch spriteBatch)
    {
        List<Rectangle> occupiedBounds = new();

        foreach (HydraulicSubnetworkStatus status in _network.SubnetworkStatuses)
        {
            Vector2 screen = Game1.GlobalToLocal(Game1.viewport, status.LabelPumpTile * Game1.tileSize);
            string networkLabel = string.Empty;
            string flowLabel = $"{status.ConsumptionFlow:0.##}/{status.MaxFlow:0.##}";

            Vector2 idSize = Vector2.Zero;
#if DEBUG
            networkLabel = $"#{status.Id.ToString()[..8]}";
            idSize = Game1.smallFont.MeasureString(networkLabel);
#endif
            Vector2 flowSize = Game1.smallFont.MeasureString(flowLabel);

            float centerX = screen.X + (Game1.tileSize / 2f);
            float flowX = centerX - (flowSize.X / 2f);
            float flowY = screen.Y - (Game1.tileSize / 2f) - 2f;
            float idX = centerX - (idSize.X / 2f);
            float idY = flowY - idSize.Y - 2f;

            Rectangle idBounds = new((int)idX, (int)idY, (int)Math.Ceiling(idSize.X), (int)Math.Ceiling(idSize.Y));
            Rectangle flowBounds = new((int)flowX, (int)flowY, (int)Math.Ceiling(flowSize.X), (int)Math.Ceiling(flowSize.Y));

            while (occupiedBounds.Any(r => r.Intersects(idBounds) || r.Intersects(flowBounds)))
            {
                float offset = idSize.Y + flowSize.Y + 4f;
                idY -= offset;
                flowY -= offset;

                idBounds = new Rectangle((int)idX, (int)idY, (int)Math.Ceiling(idSize.X), (int)Math.Ceiling(idSize.Y));
                flowBounds = new Rectangle((int)flowX, (int)flowY, (int)Math.Ceiling(flowSize.X), (int)Math.Ceiling(flowSize.Y));
            }

#if DEBUG
            Utility.drawTextWithShadow(
                spriteBatch,
                networkLabel,
                Game1.smallFont,
                new Vector2(idX, idY),
                GetOverlayNetworkTextColor());
#endif

            Utility.drawTextWithShadow(
                spriteBatch,
                flowLabel,
                Game1.smallFont,
                new Vector2(flowX, flowY),
                GetOverlayNetworkTextColor());

            occupiedBounds.Add(idBounds);
            occupiedBounds.Add(flowBounds);
        }
    }

    /// <summary>Dibuja un marco de colocación para tuberías en la casilla apuntada.</summary>
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
