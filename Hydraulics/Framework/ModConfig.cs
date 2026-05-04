using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace Hydraulics.Framework;

/// <summary>Configuración del mod</summary>
internal sealed class ModConfig
{
    /// <summary>Indica si el mod está habilitado</summary>
    public bool EnableMod { get; set; } = true;

    /// <summary>La tecla para recargar la configuración del mod sin reiniciar el juego.</summary>
    public KeybindList ReloadKey { get; set; } = new(SButton.F5);

    /// <summary>La tecla para alternar el modo de edición de tuberías, que permite colocar o quitar tuberías.</summary>
    public KeybindList TogglePipeEditModeKey { get; set; } = new(SButton.End);

    /// <summary>Whether pumps require energy (solar panel) to move water.</summary>
    public bool RequireEnergyForPumps { get; set; } = true;

    /// <summary>Establece el coste en oro para construir una tubería.</summary>
    public int PipeBuildGoldCost { get; set; } = 100;

    /// <summary>Establece el coste en mineral de cobre para construir una tubería.</summary>
    public int PipeBuildCopperOreCost { get; set; } = 2;

    /// <summary>Establece el reembolso en oro al destruir una tubería.</summary>
    public int PipeDestroyGoldRefund { get; set; } = 50;

    /// <summary>Establece el reembolso en mineral de cobre al destruir una tubería.</summary>
    public int PipeDestroyCopperOreRefund { get; set; } = 1;

    /// <summary>Establece el coste de agua por cada casilla regada por cada tubería.</summary>
    public float WaterCostPerTile { get; set; } = 0.25f;

    /// <summary>Establece el caudal del nivel bronce de la bomba.</summary>
    public float BronzePumpWaterOutput { get; set; } = 10f;

    /// <summary>Establece el caudal del nivel acero de la bomba.</summary>
    public float SteelPumpWaterOutput { get; set; } = 25f;

    /// <summary>Establece el caudal del nivel oro de la bomba.</summary>
    public float GoldPumpWaterOutput { get; set; } = 80f;

    /// <summary>Establece el caudal del nivel iridio de la bomba.</summary>
    public float IridiumPumpWaterOutput { get; set; } = 200f;

    /// <summary>Indica si se reproduce una animación de aspersor al regar las casillas.</summary>
    public bool PlaySprinklerAnimation { get; set; } = true;

    /// <summary>Ensure all arguments are valid.</summary>
    public void EnsureArguments()
    {
        if (PipeBuildGoldCost < 0)
            PipeBuildGoldCost = 0;
        else if (PipeBuildGoldCost > 1000)
            PipeBuildGoldCost = 1000;

        if (PipeBuildCopperOreCost < 0)
            PipeBuildCopperOreCost = 0;
        else if (PipeBuildCopperOreCost > 50)
            PipeBuildCopperOreCost = 50;

        if (PipeDestroyGoldRefund < 0)
            PipeDestroyGoldRefund = 0;
        else if (PipeDestroyGoldRefund > 1000)
            PipeDestroyGoldRefund = 1000;

        if (PipeDestroyCopperOreRefund < 0)
            PipeDestroyCopperOreRefund = 0;
        else if (PipeDestroyCopperOreRefund > 50)
            PipeDestroyCopperOreRefund = 50;

        if (WaterCostPerTile < 0.00f || float.IsNegative(WaterCostPerTile))
            WaterCostPerTile = 0.25f;
        else if (WaterCostPerTile > 10.00f || float.IsPositiveInfinity(WaterCostPerTile))
            WaterCostPerTile = 10.00f;

        if (BronzePumpWaterOutput <= 0f || float.IsNaN(BronzePumpWaterOutput) || float.IsInfinity(BronzePumpWaterOutput))
            BronzePumpWaterOutput = 10f;
        else if (BronzePumpWaterOutput > 1000f)
            BronzePumpWaterOutput = 1000f;

        if (SteelPumpWaterOutput <= 0f || float.IsNaN(SteelPumpWaterOutput) || float.IsInfinity(SteelPumpWaterOutput))
            SteelPumpWaterOutput = 25f;
        else if (SteelPumpWaterOutput > 1000f)
            SteelPumpWaterOutput = 1000f;

        if (GoldPumpWaterOutput <= 0f || float.IsNaN(GoldPumpWaterOutput) || float.IsInfinity(GoldPumpWaterOutput))
            GoldPumpWaterOutput = 80f;
        else if (GoldPumpWaterOutput > 1000f)
            GoldPumpWaterOutput = 1000f;

        if (IridiumPumpWaterOutput <= 0f || float.IsNaN(IridiumPumpWaterOutput) || float.IsInfinity(IridiumPumpWaterOutput))
            IridiumPumpWaterOutput = 200f;
        else if (IridiumPumpWaterOutput > 1000f)
            IridiumPumpWaterOutput = 1000f;
    }
}
