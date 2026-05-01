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
    private readonly Dictionary<Vector2, Guid> _pipeToSubnetworkId = new();
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

    public bool ContainsPipe(Vector2 tile)
    {
        return _pipes.ContainsKey(tile);
    }

    public bool ContainsPump(Vector2 tile)
    {
        return _pumps.Any(p => p.Tile == tile);
    }

    public bool TryAddPipe(Vector2 tile)
    {
        if (_pipes.ContainsKey(tile) || ContainsPump(tile))
            return false;

        _pipes.Add(tile, new HydraulicPipe(tile));
        RefreshConnectionsAround(tile);
        return true;
    }

    public bool TryRemovePipe(Vector2 tile)
    {
        if (!_pipes.Remove(tile))
            return false;

        RefreshConnectionsAround(tile);
        return true;
    }

    public bool TryAddPump(Vector2 tile, WaterPumpTier tier)
    {
        if (_pumps.Any(p => p.Tile == tile) || ContainsPipe(tile))
            return false;

        _pumps.Add(new WaterPumpMachine(tile, tier));
        return true;
    }

    public bool TryRemovePump(Vector2 tile)
    {
        int index = _pumps.FindIndex(p => p.Tile == tile);
        if (index < 0)
            return false;

        _pumps.RemoveAt(index);
        return true;
    }

    public void ClearPumps()
    {
        _pumps.Clear();
    }

    public WaterPumpMachine? FindAdjacentPump(Vector2 tile)
    {
        return _pumps.FirstOrDefault(p => p.IsTileAdjacent(tile));
    }

    public void RefreshConnectionsAround(Vector2 centerTile)
    {
        foreach (Vector2 tile in HydraulicWorldRules.EnumerateCardinalPlusCenter(centerTile))
        {
            if (_pipes.TryGetValue(tile, out HydraulicPipe? pipe))
                pipe.RefreshConnections(_pipes);
        }
    }

    public void RecalculateWater(GameLocation location, bool requireEnergy, float waterDemandPerTile)
    {
        ArgumentNullException.ThrowIfNull(location);

        foreach (HydraulicPipe pipe in _pipes.Values)
            pipe.HasWater = false;

        RebuildSubnetworks();

        foreach (SubnetworkInfo network in _subnetworks.Values)
        {
            network.MaxFlow = 0f;
            network.ConsumptionFlow = 0f;
        }

        if (waterDemandPerTile <= 0f)
            waterDemandPerTile = 0.25f;

        Dictionary<Vector2, WaterPumpMachine> activePumpsByTile = new();
        foreach (WaterPumpMachine pump in _pumps)
        {
            if (pump.RefreshPowerState(location, requireEnergy))
                activePumpsByTile[pump.Tile] = pump;
        }

        if (activePumpsByTile.Count == 0 || _pipes.Count == 0)
            return;

        foreach (SubnetworkInfo network in _subnetworks.Values)
        {
            HashSet<Vector2> activePumpTiles = new();
            foreach (Vector2 pumpTile in network.PumpTiles)
            {
                if (activePumpsByTile.ContainsKey(pumpTile))
                    activePumpTiles.Add(pumpTile);
            }

            if (activePumpTiles.Count == 0)
                continue;

            float componentCapacity = activePumpTiles.Sum(tile => activePumpsByTile[tile].WaterOutput);
            network.MaxFlow = componentCapacity;

            int irrigableTileCount = (int)Math.Floor(componentCapacity / waterDemandPerTile);
            if (irrigableTileCount <= 0)
                continue;

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

            int consumedTileCount = 0;

            while (waterQueue.Count > 0)
            {
                Vector2 tile = waterQueue.Dequeue();
                if (!_pipes.TryGetValue(tile, out HydraulicPipe? pipe))
                    continue;

                bool isIrrigableTile = location.terrainFeatures.TryGetValue(tile, out TerrainFeature? feature)
                    && feature is HoeDirt;

                if (isIrrigableTile && consumedTileCount >= irrigableTileCount)
                    continue;

                if (!pipe.HasWater)
                {
                    pipe.HasWater = true;
                }

                if (isIrrigableTile)
                {
                    consumedTileCount++;
                }

                foreach (Vector2 adjacent in HydraulicWorldRules.EnumerateCardinalNeighbors(tile))
                {
                    if (!network.PipeTiles.Contains(adjacent) || !waterVisited.Add(adjacent))
                        continue;

                    waterQueue.Enqueue(adjacent);
                }
            }

            network.ConsumptionFlow = consumedTileCount * waterDemandPerTile;
        }
    }

    public Guid? TryGetSubnetworkIdByPipe(Vector2 tile)
    {
        if (_pipeToSubnetworkId.TryGetValue(tile, out Guid id))
            return id;

        return null;
    }

    public Guid? TryGetSubnetworkIdByPump(Vector2 tile)
    {
        foreach (SubnetworkInfo network in _subnetworks.Values)
        {
            if (network.PumpTiles.Contains(tile))
                return network.Id;
        }

        return null;
    }

    public bool RecalculateSubnetworkAtTile(GameLocation location, Vector2 tile, bool requireEnergy, float waterDemandPerTile)
    {
        ArgumentNullException.ThrowIfNull(location);

        Guid? targetId = TryGetSubnetworkIdByPipe(tile);
        if (targetId is null)
            return false;

        RecalculateWater(location, requireEnergy, waterDemandPerTile);
        return true;
    }

    public bool RecalculateSubnetworkAtPumpTile(GameLocation location, Vector2 tile, bool requireEnergy, float waterDemandPerTile)
    {
        ArgumentNullException.ThrowIfNull(location);

        Guid? targetId = TryGetSubnetworkIdByPump(tile);
        if (targetId is null)
            return false;

        RecalculateWater(location, requireEnergy, waterDemandPerTile);
        return true;
    }

    private void RebuildSubnetworks(IReadOnlyDictionary<Guid, HashSet<Vector2>>? preferredById = null)
    {
        Dictionary<Guid, HashSet<Vector2>> previousById = preferredById is null
            ? _subnetworks.ToDictionary(
            kvp => kvp.Key,
            kvp => new HashSet<Vector2>(kvp.Value.PipeTiles))
            : preferredById.ToDictionary(kvp => kvp.Key, kvp => new HashSet<Vector2>(kvp.Value));

        _subnetworks.Clear();
        _pipeToSubnetworkId.Clear();

        HashSet<Vector2> visited = new();

        foreach (Vector2 startTile in _pipes.Keys)
        {
            if (!visited.Add(startTile))
                continue;

            Queue<Vector2> queue = new();
            HashSet<Vector2> componentTiles = new();
            HashSet<Vector2> componentPumps = new();

            queue.Enqueue(startTile);

            while (queue.Count > 0)
            {
                Vector2 current = queue.Dequeue();
                componentTiles.Add(current);

                foreach (Vector2 adjacent in HydraulicWorldRules.EnumerateCardinalNeighbors(current))
                {
                    if (_pipes.ContainsKey(adjacent) && visited.Add(adjacent))
                        queue.Enqueue(adjacent);

                    if (ContainsPump(adjacent))
                        componentPumps.Add(adjacent);
                }
            }

            if (componentPumps.Count == 0)
                continue;

            Guid id = ResolveSubnetworkId(componentTiles, previousById);
            SubnetworkInfo info = new()
            {
                Id = id,
                LabelPumpTile = componentPumps.OrderBy(p => p.Y).ThenBy(p => p.X).First(),
            };

            foreach (Vector2 pipeTile in componentTiles)
            {
                info.PipeTiles.Add(pipeTile);
                _pipeToSubnetworkId[pipeTile] = id;
            }

            foreach (Vector2 pumpTile in componentPumps)
                info.PumpTiles.Add(pumpTile);

            _subnetworks[id] = info;
        }
    }

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
                    .Select(tile => _pumps.FirstOrDefault(p => p.Tile == tile))
                    .Where(p => p is not null)
                    .Select(p => new PumpSaveData((int)p!.Tile.X, (int)p.Tile.Y, p.Tier))
                    .ToList(),
            }).ToList(),
        };
    }

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
