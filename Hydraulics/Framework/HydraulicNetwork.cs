using Microsoft.Xna.Framework;
using StardewValley;

namespace Hydraulics.Framework;

internal sealed class HydraulicNetwork
{
    private readonly Dictionary<Vector2, HydraulicPipe> _pipes = new();
    private readonly List<WaterPumpMachine> _pumps = new();

    public IReadOnlyDictionary<Vector2, HydraulicPipe> Pipes => _pipes;

    public IReadOnlyList<WaterPumpMachine> Pumps => _pumps;

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

        Queue<Vector2> queue = new();
        HashSet<Vector2> visited = new();

        foreach (WaterPumpMachine pump in _pumps)
        {
            if (!pump.RefreshPowerState(location, requireEnergy))
                continue;

            foreach (Vector2 adjacent in HydraulicWorldRules.EnumerateCardinalNeighbors(pump.Tile))
            {
                if (!_pipes.ContainsKey(adjacent) || !visited.Add(adjacent))
                    continue;

                queue.Enqueue(adjacent);
            }
        }

        if (waterDemandPerTile <= 0f)
            waterDemandPerTile = 0.25f;

        float totalCapacity = _pumps
            .Where(p => p.PowerMode is PumpPowerMode.SolarPanel or PumpPowerMode.DebugBypass)
            .Sum(p => p.WaterOutput);

        int irrigableTileCount = (int)Math.Floor(totalCapacity / waterDemandPerTile);
        if (irrigableTileCount <= 0)
            return;

        int wateredCount = 0;

        while (queue.Count > 0)
        {
            Vector2 tile = queue.Dequeue();
            if (!_pipes.TryGetValue(tile, out HydraulicPipe? pipe))
                continue;

            pipe.HasWater = true;

            wateredCount++;
            if (wateredCount >= irrigableTileCount)
                break;

            foreach (Vector2 adjacent in HydraulicWorldRules.EnumerateCardinalNeighbors(tile))
            {
                if (!_pipes.ContainsKey(adjacent) || !visited.Add(adjacent))
                    continue;

                queue.Enqueue(adjacent);
            }
        }
    }

    public HydraulicSaveData ToSaveData()
    {
        return new HydraulicSaveData
        {
            Pipes = _pipes.Keys.Select(tile => new TileSaveData((int)tile.X, (int)tile.Y)).ToList(),
            Pumps = _pumps.Select(p => new PumpSaveData((int)p.Tile.X, (int)p.Tile.Y, p.Tier)).ToList(),
        };
    }

    public static HydraulicNetwork FromSaveData(HydraulicSaveData? data)
    {
        HydraulicNetwork network = new();
        if (data is null)
            return network;

        foreach (TileSaveData pipe in data.Pipes)
            network.TryAddPipe(new Vector2(pipe.X, pipe.Y));

        foreach (PumpSaveData pump in data.Pumps)
        {
            network.TryAddPump(new Vector2(pump.X, pump.Y), pump.Tier);
        }

        return network;
    }
}
