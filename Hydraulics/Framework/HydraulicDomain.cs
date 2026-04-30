using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Hydraulics.Framework;

internal enum PumpPowerMode
{
    None = 0,
    SolarPanel = 1,
    DebugBypass = 2,
}

internal sealed class WaterPumpMachine
{
    public Vector2 Tile { get; }

    public PumpPowerMode PowerMode { get; private set; }

    public WaterPumpMachine(Vector2 tile)
    {
        Tile = tile;
    }

    public bool RefreshPowerState(GameLocation location, bool requireEnergy)
    {
        ArgumentNullException.ThrowIfNull(location);

        if (!HydraulicWorldRules.IsMainlandFarm(location))
        {
            PowerMode = PumpPowerMode.None;
            return false;
        }

        bool hasSource = IsAdjacentToWaterOrWell(location);

        if (!hasSource)
        {
            PowerMode = PumpPowerMode.None;
            return false;
        }

        if (!requireEnergy)
        {
            PowerMode = PumpPowerMode.DebugBypass;
            return true;
        }

        bool hasSolar = IsAdjacentToSolarPanel(location);

        if (hasSolar)
        {
            PowerMode = PumpPowerMode.SolarPanel;
            return true;
        }

        PowerMode = PumpPowerMode.None;
        return false;
    }

    public bool IsTileAdjacent(Vector2 tile)
    {
        return IsCardinalNeighbor(Tile, tile);
    }

    private bool IsAdjacentToWaterOrWell(GameLocation location)
    {
        foreach (Vector2 neighbor in HydraulicWorldRules.EnumerateCardinalNeighbors(Tile))
        {
            if (location.isWaterTile((int)neighbor.X, (int)neighbor.Y))
                return true;

            var building = location.getBuildingAt(neighbor);
            if (building is not null && building.buildingType.Value == "Well")
                return true;
        }

        return false;
    }

    private bool IsAdjacentToSolarPanel(GameLocation location)
    {
        foreach (Vector2 neighbor in HydraulicWorldRules.EnumerateCardinalNeighbors(Tile))
        {
            if (!location.Objects.TryGetValue(neighbor, out StardewValley.Object? obj) || obj is null)
                continue;

            if (string.Equals(obj.QualifiedItemId, "(BC)SolarPanel", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsCardinalNeighbor(Vector2 a, Vector2 b)
    {
        int dx = (int)Math.Abs(a.X - b.X);
        int dy = (int)Math.Abs(a.Y - b.Y);
        return dx + dy == 1;
    }
}

internal sealed class HydraulicPipe
{
    private const int CellPixels = 16;
    private const int CorePixels = 4;

    public Vector2 Tile { get; }

    public bool HasWater { get; set; }

    public byte ConnectionMask { get; private set; }

    public HydraulicPipe(Vector2 tile)
    {
        Tile = tile;
    }

    public void RefreshConnections(IReadOnlyDictionary<Vector2, HydraulicPipe> allPipes)
    {
        ArgumentNullException.ThrowIfNull(allPipes);

        byte mask = 0;
        if (allPipes.ContainsKey(new Vector2(Tile.X, Tile.Y - 1))) mask |= 1; // N
        if (allPipes.ContainsKey(new Vector2(Tile.X, Tile.Y + 1))) mask |= 2; // S
        if (allPipes.ContainsKey(new Vector2(Tile.X + 1, Tile.Y))) mask |= 4; // E
        if (allPipes.ContainsKey(new Vector2(Tile.X - 1, Tile.Y))) mask |= 8; // W

        ConnectionMask = mask;
    }

    public void Draw(SpriteBatch spriteBatch, IReadOnlyList<WaterPumpMachine> pumps)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        ArgumentNullException.ThrowIfNull(pumps);

        int centerStart = (CellPixels - CorePixels) / 2;
        int centerEnd = centerStart + CorePixels;
        int zoom = Game1.pixelZoom;
        Color color = HasWater ? new Color(66, 158, 255) : new Color(255, 220, 0);

        byte renderMask = ConnectionMask;
        if (pumps.Any(p => p.Tile == new Vector2(Tile.X, Tile.Y - 1))) renderMask |= 1;
        if (pumps.Any(p => p.Tile == new Vector2(Tile.X, Tile.Y + 1))) renderMask |= 2;
        if (pumps.Any(p => p.Tile == new Vector2(Tile.X + 1, Tile.Y))) renderMask |= 4;
        if (pumps.Any(p => p.Tile == new Vector2(Tile.X - 1, Tile.Y))) renderMask |= 8;

        Vector2 screen = Game1.GlobalToLocal(Game1.viewport, Tile * Game1.tileSize);
        float layerDepth = ((Tile.Y + 1f) * Game1.tileSize) / 10000f;

        DrawSegment(spriteBatch, screen, centerStart, centerStart, CorePixels, CorePixels, zoom, color, layerDepth);

        if ((renderMask & 1) != 0)
            DrawSegment(spriteBatch, screen, centerStart, 0, CorePixels, centerStart, zoom, color, layerDepth);

        if ((renderMask & 2) != 0)
            DrawSegment(spriteBatch, screen, centerStart, centerEnd, CorePixels, CellPixels - centerEnd, zoom, color, layerDepth);

        if ((renderMask & 4) != 0)
            DrawSegment(spriteBatch, screen, centerEnd, centerStart, CellPixels - centerEnd, CorePixels, zoom, color, layerDepth);

        if ((renderMask & 8) != 0)
            DrawSegment(spriteBatch, screen, 0, centerStart, centerStart, CorePixels, zoom, color, layerDepth);
    }

    private static void DrawSegment(
        SpriteBatch spriteBatch,
        Vector2 screen,
        int x,
        int y,
        int width,
        int height,
        int zoom,
        Color color,
        float layerDepth)
    {
        if (width <= 0 || height <= 0)
            return;

        spriteBatch.Draw(
            Game1.staminaRect,
            new Rectangle(
                (int)screen.X + (x * zoom),
                (int)screen.Y + (y * zoom),
                width * zoom,
                height * zoom),
            null,
            color,
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            layerDepth);
    }
}

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

    public bool TryAddPump(Vector2 tile)
    {
        if (_pumps.Any(p => p.Tile == tile) || ContainsPipe(tile))
            return false;

        _pumps.Add(new WaterPumpMachine(tile));
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

    public void RecalculateWater(GameLocation location, bool requireEnergy)
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

        while (queue.Count > 0)
        {
            Vector2 tile = queue.Dequeue();
            if (!_pipes.TryGetValue(tile, out HydraulicPipe? pipe))
                continue;

            pipe.HasWater = true;

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
            Pumps = _pumps.Select(p => new PumpSaveData((int)p.Tile.X, (int)p.Tile.Y)).ToList(),
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
            network.TryAddPump(new Vector2(pump.X, pump.Y));
        }

        return network;
    }
}

internal static class HydraulicWorldRules
{
    private const int WaterSearchDistance = 2;

    public static bool IsMainlandFarm(GameLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        return location is Farm && string.Equals(location.NameOrUniqueName, "Farm", StringComparison.Ordinal);
    }

    public static IEnumerable<Vector2> EnumerateCardinalNeighbors(Vector2 tile)
    {
        yield return new Vector2(tile.X, tile.Y - 1);
        yield return new Vector2(tile.X, tile.Y + 1);
        yield return new Vector2(tile.X + 1, tile.Y);
        yield return new Vector2(tile.X - 1, tile.Y);
    }

    public static IEnumerable<Vector2> EnumerateCardinalPlusCenter(Vector2 center)
    {
        yield return center;
        foreach (Vector2 adjacent in EnumerateCardinalNeighbors(center))
            yield return adjacent;
    }

    public static bool CanPlacePipeOnTile(GameLocation location, Vector2 tile)
    {
        ArgumentNullException.ThrowIfNull(location);

        if (!IsMainlandFarm(location))
            return false;

        if (location.Objects.ContainsKey(tile))
            return false;

        if (!location.isTilePlaceable(tile))
            return false;

        return true;
    }

    public static bool CanPlacePumpOnTile(GameLocation location, Vector2 tile)
    {
        ArgumentNullException.ThrowIfNull(location);

        if (!CanPlacePipeOnTile(location, tile))
            return false;

        foreach (Vector2 nearby in EnumerateCardinalWithinDistance(tile, WaterSearchDistance))
        {
            if (location.isWaterTile((int)nearby.X, (int)nearby.Y))
                return true;

            var building = location.getBuildingAt(nearby);
            if (building is not null && building.buildingType.Value == "Well")
                return true;
        }

        return false;
    }

    private static IEnumerable<Vector2> EnumerateCardinalWithinDistance(Vector2 center, int maxDistance)
    {
        for (int distance = 1; distance <= maxDistance; distance++)
        {
            yield return new Vector2(center.X, center.Y - distance);
            yield return new Vector2(center.X, center.Y + distance);
            yield return new Vector2(center.X + distance, center.Y);
            yield return new Vector2(center.X - distance, center.Y);
        }
    }
}

internal sealed class HydraulicSaveData
{
    public List<TileSaveData> Pipes { get; set; } = new();

    public List<PumpSaveData> Pumps { get; set; } = new();
}

internal readonly record struct TileSaveData(int X, int Y);

internal readonly record struct PumpSaveData(int X, int Y);
