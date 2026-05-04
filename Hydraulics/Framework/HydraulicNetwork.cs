using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace Hydraulics.Framework;

internal sealed class HydraulicNetwork
{
    private sealed class SubnetworkInfo
    {
        public Guid Id { get; set; }

        public HashSet<Vector2> PipeTiles { get; } = new();

        public HashSet<Vector2> PumpTiles { get; } = new();

        public float MaxFlow { get; set; }

        public float ConsumptionFlow { get; set; }

        public Vector2 LabelPumpTile { get; set; }
    }

    private readonly Dictionary<Vector2, HydraulicPipe> _pipes = new();
    private readonly List<WaterPumpMachine> _pumps = new();
    private readonly Dictionary<Vector2, WaterPumpMachine> _pumpByTile = new();
    private readonly Dictionary<Vector2, Guid> _pipeToSubnetworkId = new();
    private readonly Dictionary<Vector2, Guid> _pumpToSubnetworkId = new();
    private readonly Dictionary<Guid, SubnetworkInfo> _subnetworks = new();

    public IReadOnlyDictionary<Vector2, HydraulicPipe> Pipes => _pipes;

    public IReadOnlyList<WaterPumpMachine> Pumps => _pumps;

    public IReadOnlyList<HydraulicSubnetworkStatus> SubnetworkStatuses
    {
        get
        {
            return _subnetworks.Values
                .OrderBy(n => n.Id)
                .Select(n => new HydraulicSubnetworkStatus(n.Id, n.LabelPumpTile, n.MaxFlow, n.ConsumptionFlow))
                .ToList();
        }
    }

    /// <summary>Comprueba si existe una tubería en la casilla indicada.</summary>
    public bool ContainsPipe(Vector2 tile)
    {
        return _pipes.ContainsKey(tile);
    }

    /// <summary>Comprueba si existe una bomba en la casilla indicada.</summary>
    public bool ContainsPump(Vector2 tile)
    {
        return _pumpByTile.ContainsKey(tile);
    }

    /// <summary>Intenta añadir una tubería en la casilla especificada.</summary>
    public bool TryAddPipe(Vector2 tile)
    {
        if (_pipes.ContainsKey(tile) || ContainsPump(tile))
            return false;

        _pipes.Add(tile, new HydraulicPipe(tile));
        RefreshConnectionsAround(tile);
        return true;
    }

    /// <summary>Intenta eliminar la tubería de la casilla indicada.</summary>
    public bool TryRemovePipe(Vector2 tile)
    {
        if (!_pipes.Remove(tile))
            return false;

        RefreshConnectionsAround(tile);
        return true;
    }

    /// <summary>Intenta añadir una bomba del nivel indicado en la casilla especificada.</summary>
    public bool TryAddPump(Vector2 tile, WaterPumpTier tier)
    {
        if (_pumpByTile.ContainsKey(tile) || ContainsPipe(tile))
            return false;

        WaterPumpMachine pump = new(tile, tier);
        _pumps.Add(pump);
        _pumpByTile.Add(tile, pump);
        return true;
    }

    /// <summary>Intenta eliminar la bomba de la casilla indicada.</summary>
    public bool TryRemovePump(Vector2 tile)
    {
        if (!_pumpByTile.TryGetValue(tile, out WaterPumpMachine? pump))
            return false;

        _pumpByTile.Remove(tile);
        _pumpToSubnetworkId.Remove(tile);
        _pumps.Remove(pump);
        return true;
    }

    /// <summary>Elimina todas las bombas registradas en la red.</summary>
    public void ClearPumps()
    {
        _pumps.Clear();
        _pumpByTile.Clear();
        _pumpToSubnetworkId.Clear();
    }

    /// <summary>Busca una bomba adyacente en los cuatro lados de la casilla indicada.</summary>
    public WaterPumpMachine? FindAdjacentPump(Vector2 tile)
    {
        foreach (Vector2 adjacent in HydraulicWorldRules.EnumerateCardinalNeighbors(tile))
        {
            if (_pumpByTile.TryGetValue(adjacent, out WaterPumpMachine? pump))
                return pump;
        }

        return null;
    }

    /// <summary>Actualiza las conexiones de tuberías alrededor de la casilla central indicada.</summary>
    public void RefreshConnectionsAround(Vector2 centerTile)
    {
        foreach (Vector2 tile in HydraulicWorldRules.EnumerateCardinalPlusCenter(centerTile))
        {
            if (_pipes.TryGetValue(tile, out HydraulicPipe? pipe))
                pipe.RefreshConnections(_pipes);
        }
    }

    /// <summary>Recalcula el estado hídrico de toda la red según la configuración actual.</summary>
    public void RecalculateWater(GameLocation location, ModConfig config)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(config);

        foreach (HydraulicPipe pipe in _pipes.Values)
            pipe.HasWater = false;

        RebuildSubnetworks();

        ResetSubnetworkMetrics();

        float waterDemandPerTile = config.WaterCostPerTile;
        if (waterDemandPerTile <= 0f)
            waterDemandPerTile = 0.25f;

        Dictionary<Vector2, WaterPumpMachine> activePumpsByTile = GetActivePumpsByTile(location, config);

        if (activePumpsByTile.Count == 0 || _pipes.Count == 0)
            return;

        foreach (SubnetworkInfo network in _subnetworks.Values)
        {
            RecalculateSubnetworkWater(network, location, config, waterDemandPerTile, activePumpsByTile);
        }
    }

    /// <summary>Obtiene el identificador de subred asociado a una tubería, si está disponible.</summary>
    public Guid? TryGetSubnetworkIdByPipe(Vector2 tile)
    {
        if (_pipeToSubnetworkId.TryGetValue(tile, out Guid id))
            return id;

        return null;
    }

    /// <summary>Obtiene el identificador de subred asociado a una bomba, si está disponible.</summary>
    public Guid? TryGetSubnetworkIdByPump(Vector2 tile)
    {
        if (_pumpToSubnetworkId.TryGetValue(tile, out Guid id))
            return id;

        return null;
    }

    /// <summary>Recalcula la subred asociada a la casilla de tubería indicada.</summary>
    public bool RecalculateSubnetworkAtTile(GameLocation location, Vector2 tile, ModConfig config)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(config);

        RebuildSubnetworks();

        Guid? targetId = TryGetSubnetworkIdByPipe(tile);
        if (targetId is null)
            return false;

        return RecalculateSingleSubnetwork(location, targetId.Value, config);
    }

    /// <summary>Recalcula una subred concreta a partir de su identificador.</summary>
    public bool RecalculateSubnetworkById(GameLocation location, Guid subnetworkId, ModConfig config)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(config);

        RebuildSubnetworks();
        return RecalculateSingleSubnetwork(location, subnetworkId, config);
    }

    /// <summary>Devuelve las casillas de tubería que pertenecen a la subred especificada.</summary>
    public IReadOnlyCollection<Vector2> GetSubnetworkPipeTiles(Guid subnetworkId)
    {
        if (_subnetworks.TryGetValue(subnetworkId, out SubnetworkInfo? network))
            return network.PipeTiles.ToArray();

        return Array.Empty<Vector2>();
    }

    /// <summary>Recalcula la subred asociada a la casilla de bomba indicada.</summary>
    public bool RecalculateSubnetworkAtPumpTile(GameLocation location, Vector2 tile, ModConfig config)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(config);

        RebuildSubnetworks();

        Guid? targetId = TryGetSubnetworkIdByPump(tile);
        if (targetId is null)
            return false;

        return RecalculateSingleSubnetwork(location, targetId.Value, config);
    }

    /// <summary>Reconstruye el mapa de subredes a partir del estado actual de tuberías y bombas.</summary>
    private void RebuildSubnetworks(IReadOnlyDictionary<Guid, HashSet<Vector2>>? preferredById = null)
    {
        Dictionary<Guid, HashSet<Vector2>> previousById = preferredById is null
            ? _subnetworks.ToDictionary(
            kvp => kvp.Key,
            kvp => new HashSet<Vector2>(kvp.Value.PipeTiles))
            : preferredById.ToDictionary(kvp => kvp.Key, kvp => new HashSet<Vector2>(kvp.Value));

        _subnetworks.Clear();
        _pipeToSubnetworkId.Clear();
        _pumpToSubnetworkId.Clear();

        HashSet<Vector2> visitedPipes = new();
        HashSet<Vector2> visitedPumps = new();

        foreach (Vector2 startTile in _pipes.Keys)
        {
            if (!visitedPipes.Add(startTile))
                continue;

            Queue<(Vector2 Tile, bool IsPump)> queue = new();
            HashSet<Vector2> componentTiles = new();
            HashSet<Vector2> componentPumps = new();

            queue.Enqueue((startTile, false));

            while (queue.Count > 0)
            {
                (Vector2 current, bool isPump) = queue.Dequeue();

                if (isPump)
                {
                    componentPumps.Add(current);
                }
                else
                {
                    componentTiles.Add(current);
                }

                foreach (Vector2 adjacent in HydraulicWorldRules.EnumerateCardinalNeighbors(current))
                {
                    if (_pipes.ContainsKey(adjacent) && visitedPipes.Add(adjacent))
                        queue.Enqueue((adjacent, false));

                    if (_pumpByTile.ContainsKey(adjacent) && visitedPumps.Add(adjacent))
                        queue.Enqueue((adjacent, true));
                }
            }

            Guid id = ResolveSubnetworkId(componentTiles, previousById);
            Vector2 labelTile = componentPumps.Count > 0
                ? componentPumps.OrderBy(p => p.Y).ThenBy(p => p.X).First()
                : componentTiles.OrderBy(p => p.Y).ThenBy(p => p.X).First();

            SubnetworkInfo info = new()
            {
                Id = id,
                LabelPumpTile = labelTile,
            };

            foreach (Vector2 pipeTile in componentTiles)
            {
                info.PipeTiles.Add(pipeTile);
                _pipeToSubnetworkId[pipeTile] = id;
            }

            foreach (Vector2 pumpTile in componentPumps)
            {
                info.PumpTiles.Add(pumpTile);
                _pumpToSubnetworkId[pumpTile] = id;
            }

            _subnetworks[id] = info;
        }
    }

    /// <summary>Obtiene las bombas activas por casilla según la ubicación y la configuración.</summary>
    private Dictionary<Vector2, WaterPumpMachine> GetActivePumpsByTile(GameLocation location, ModConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        Dictionary<Vector2, WaterPumpMachine> activePumpsByTile = new();

        foreach (WaterPumpMachine pump in _pumpByTile.Values)
        {
            if (pump.RefreshPowerState(location, config.RequireEnergyForPumps))
                activePumpsByTile[pump.Tile] = pump;
        }

        return activePumpsByTile;
    }

    /// <summary>Reinicia las métricas de caudal y consumo de todas las subredes.</summary>
    private void ResetSubnetworkMetrics()
    {
        foreach (SubnetworkInfo network in _subnetworks.Values)
        {
            network.MaxFlow = 0f;
            network.ConsumptionFlow = 0f;
        }
    }

    /// <summary>Recalcula el agua de una subred concreta y actualiza su estado.</summary>
    private bool RecalculateSingleSubnetwork(GameLocation location, Guid subnetworkId, ModConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        float waterDemandPerTile = config.WaterCostPerTile;
        if (waterDemandPerTile <= 0f)
            waterDemandPerTile = 0.25f;

        if (!_subnetworks.TryGetValue(subnetworkId, out SubnetworkInfo? targetNetwork))
            return false;

        foreach (Vector2 tile in targetNetwork.PipeTiles)
        {
            if (_pipes.TryGetValue(tile, out HydraulicPipe? pipe))
                pipe.HasWater = false;
        }

        targetNetwork.MaxFlow = 0f;
        targetNetwork.ConsumptionFlow = 0f;

        Dictionary<Vector2, WaterPumpMachine> activePumpsByTile = GetActivePumpsByTile(location, config);
        if (activePumpsByTile.Count == 0)
            return true;

        RecalculateSubnetworkWater(targetNetwork, location, config, waterDemandPerTile, activePumpsByTile);
        return true;
    }

    /// <summary>Propaga el agua por una subred y calcula su consumo efectivo.</summary>
    private void RecalculateSubnetworkWater(
        SubnetworkInfo network,
        GameLocation location,
        ModConfig config,
        float waterDemandPerTile,
        IReadOnlyDictionary<Vector2, WaterPumpMachine> activePumpsByTile)
    {
        HashSet<Vector2> activePumpTiles = new();
        foreach (Vector2 pumpTile in network.PumpTiles)
        {
            if (activePumpsByTile.ContainsKey(pumpTile))
                activePumpTiles.Add(pumpTile);
        }

        if (activePumpTiles.Count == 0)
            return;

        float componentCapacity = activePumpTiles.Sum(tile => activePumpsByTile[tile].GetWaterOutput(config));
        network.MaxFlow = componentCapacity;

        int maxConsumableTilledTiles = (int)Math.Floor(componentCapacity / waterDemandPerTile);
        if (maxConsumableTilledTiles <= 0)
            return;

        Queue<Vector2> waterQueue = new();
        HashSet<Vector2> waterVisited = new();

        foreach (Vector2 pumpTile in activePumpTiles)
        {
            foreach (Vector2 adjacent in HydraulicWorldRules.EnumerateCardinalNeighbors(pumpTile))
            {
                if (!network.PipeTiles.Contains(adjacent) || !waterVisited.Add(adjacent))
                    continue;

                waterQueue.Enqueue(adjacent);
            }
        }

        int consumedTilledTiles = 0;

        while (waterQueue.Count > 0)
        {
            Vector2 tile = waterQueue.Dequeue();
            if (!_pipes.TryGetValue(tile, out HydraulicPipe? pipe))
                continue;

            bool isIrrigableTile = location.terrainFeatures.TryGetValue(tile, out TerrainFeature? feature)
                && feature is HoeDirt;

            if (isIrrigableTile && consumedTilledTiles >= maxConsumableTilledTiles)
                continue;

            if (!pipe.HasWater)
                pipe.HasWater = true;

            if (isIrrigableTile)
                consumedTilledTiles++;

            foreach (Vector2 adjacent in HydraulicWorldRules.EnumerateCardinalNeighbors(tile))
            {
                if (!network.PipeTiles.Contains(adjacent) || !waterVisited.Add(adjacent))
                    continue;

                waterQueue.Enqueue(adjacent);
            }
        }

        network.ConsumptionFlow = consumedTilledTiles * waterDemandPerTile;
    }

    /// <summary>Resuelve el identificador de subred reutilizando el de mayor solapamiento previo.</summary>
    private static Guid ResolveSubnetworkId(HashSet<Vector2> componentTiles, IReadOnlyDictionary<Guid, HashSet<Vector2>> previousById)
    {
        Guid matchedId = Guid.Empty;
        int bestOverlap = 0;

        foreach ((Guid id, HashSet<Vector2> previousTiles) in previousById)
        {
            int overlap = previousTiles.Count(componentTiles.Contains);
            if (overlap <= bestOverlap)
                continue;

            matchedId = id;
            bestOverlap = overlap;
        }

        if (matchedId != Guid.Empty)
            return matchedId;

        return Guid.NewGuid();
    }

    /// <summary>Serializa la red hidráulica al formato de guardado del mod.</summary>
    public HydraulicSaveData ToSaveData()
    {
        RebuildSubnetworks();

        return new HydraulicSaveData
        {
            Networks = _subnetworks.Values.Select(network => new HydraulicSubnetworkSaveData
            {
                Id = network.Id,
                Pipes = network.PipeTiles.Select(tile => new TileSaveData((int)tile.X, (int)tile.Y)).ToList(),
                Pumps = network.PumpTiles
                    .Select(tile => _pumpByTile.GetValueOrDefault(tile))
                    .Where(p => p is not null)
                    .Select(p => new PumpSaveData((int)p!.Tile.X, (int)p.Tile.Y, p.Tier))
                    .ToList(),
            }).ToList(),
        };
    }

    /// <summary>Reconstruye una red hidráulica a partir de datos de guardado.</summary>
    public static HydraulicNetwork FromSaveData(HydraulicSaveData? data)
    {
        HydraulicNetwork network = new();
        if (data is null)
            return network;

        Dictionary<Guid, HashSet<Vector2>> preferredById = new();

        foreach (HydraulicSubnetworkSaveData subnetwork in data.Networks)
        {
            HashSet<Vector2> networkPipeTiles = new();

            foreach (TileSaveData pipe in subnetwork.Pipes)
            {
                network.TryAddPipe(new Vector2(pipe.X, pipe.Y));
                networkPipeTiles.Add(new Vector2(pipe.X, pipe.Y));
            }

            foreach (PumpSaveData pump in subnetwork.Pumps)
                network.TryAddPump(new Vector2(pump.X, pump.Y), pump.Tier);

            preferredById[subnetwork.Id] = networkPipeTiles;
        }

        network.RebuildSubnetworks(preferredById);

        return network;
    }
}
