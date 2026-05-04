using Microsoft.Xna.Framework;
using StardewValley;

namespace Hydraulics.Framework;

internal sealed class WaterPumpMachine
{
    public Vector2 Tile { get; }

    public WaterPumpTier Tier { get; }

    public PumpPowerMode PowerMode { get; private set; }

    /// <summary>Crea una bomba de agua en la casilla y nivel indicados.</summary>
    public WaterPumpMachine(Vector2 tile, WaterPumpTier tier)
    {
        Tile = tile;
        Tier = tier;
    }

    /// <summary>Obtiene el caudal de la bomba según su nivel y la configuración activa.</summary>
    public float GetWaterOutput(ModConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return Tier switch
        {
            WaterPumpTier.Bronze => config.BronzePumpWaterOutput,
            WaterPumpTier.Steel => config.SteelPumpWaterOutput,
            WaterPumpTier.Gold => config.GoldPumpWaterOutput,
            WaterPumpTier.Iridium => config.IridiumPumpWaterOutput,
            _ => 0f,
        };
    }

    /// <summary>Actualiza el estado de energía de la bomba para la ubicación actual.</summary>
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

        int requiredPanels = Tier switch
        {
            WaterPumpTier.Bronze => 0,
            WaterPumpTier.Steel => 0,
            WaterPumpTier.Gold => 1,
            WaterPumpTier.Iridium => 2,
            _ => 0, // impossible
        };
        int panelCount = CountAdjacentSolarPanels(location);

        if (panelCount >= requiredPanels)
        {
            PowerMode = PumpPowerMode.SolarPanel;
            return true;
        }

        PowerMode = PumpPowerMode.None;
        return false;
    }

    /// <summary>Indica si una casilla es adyacente cardinal a la bomba.</summary>
    public bool IsTileAdjacent(Vector2 tile)
    {
        return IsCardinalNeighbor(Tile, tile);
    }

    /// <summary>Comprueba si la bomba está junto a agua o a un pozo.</summary>
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

    /// <summary>Cuenta los paneles solares adyacentes a la bomba.</summary>
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

    /// <summary>Comprueba si dos casillas son vecinas cardinales.</summary>
    private static bool IsCardinalNeighbor(Vector2 a, Vector2 b)
    {
        int dx = (int)Math.Abs(a.X - b.X);
        int dy = (int)Math.Abs(a.Y - b.Y);
        return dx + dy == 1;
    }
}
