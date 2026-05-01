using Microsoft.Xna.Framework;
using StardewValley;

namespace Hydraulics.Framework;

internal sealed class WaterPumpMachine
{
    public Vector2 Tile { get; }

    public WaterPumpTier Tier { get; }

    public PumpPowerMode PowerMode { get; private set; }

    public WaterPumpMachine(Vector2 tile, WaterPumpTier tier)
    {
        Tile = tile;
        Tier = tier;
    }

    public float WaterOutput => Tier switch
    {
        WaterPumpTier.Bronze => 10f,
        WaterPumpTier.Steel => 25f,
        WaterPumpTier.Gold => 80f,
        WaterPumpTier.Iridium => 200f,
        _ => 0f,
    };

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

        int requiredPanels = Tier == WaterPumpTier.Iridium ? 2 : 1;
        int panelCount = CountAdjacentSolarPanels(location);

        if (panelCount >= requiredPanels)
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
            if (building is not null && building.buildingType.Value == HydraulicConstants.BuildingWellId)
                return true;
        }

        return false;
    }

    private int CountAdjacentSolarPanels(GameLocation location)
    {
        int count = 0;
        foreach (Vector2 neighbor in HydraulicWorldRules.EnumerateCardinalNeighbors(Tile))
        {
            if (!location.Objects.TryGetValue(neighbor, out StardewValley.Object? obj) || obj is null)
                continue;

            if (string.Equals(obj.QualifiedItemId, HydraulicConstants.SolarPanelId, StringComparison.OrdinalIgnoreCase))
                count++;
        }

        return count;
    }

    private static bool IsCardinalNeighbor(Vector2 a, Vector2 b)
    {
        int dx = (int)Math.Abs(a.X - b.X);
        int dy = (int)Math.Abs(a.Y - b.Y);
        return dx + dy == 1;
    }
}
