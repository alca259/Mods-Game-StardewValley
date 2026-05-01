using Microsoft.Xna.Framework;
using StardewValley;

namespace Hydraulics.Framework;

internal static class HydraulicWorldRules
{
    private const int WaterSearchDistance = 2;

    public static bool IsMainlandFarm(GameLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);
        return location.IsFarm;
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

        if (tile.X < 0 || tile.Y < 0 || tile.X >= location.Map.Layers[0].LayerWidth || tile.Y >= location.Map.Layers[0].LayerHeight)
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
            if (building is not null && building.buildingType.Value == HydraulicConstants.BuildingWellId)
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
