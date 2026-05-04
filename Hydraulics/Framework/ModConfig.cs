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

    /// <summary>Valida y normaliza todos los argumentos de configuración.</summary>
    public void EnsureArguments()
    {
        EnsurePipeCostAndRefundArguments();
        EnsureRefundNotGreaterThanCost();
        EnsureWaterCostArguments();
        EnsurePumpOutputArguments();
    }

    /// <summary>Valida los límites de costes y reembolsos de tuberías.</summary>
    private void EnsurePipeCostAndRefundArguments()
    {
        if (PipeBuildGoldCost < 0)
            PipeBuildGoldCost = 0;
        else if (PipeBuildGoldCost > 100)
            PipeBuildGoldCost = 100;

        if (PipeBuildCopperOreCost < 0)
            PipeBuildCopperOreCost = 0;
        else if (PipeBuildCopperOreCost > 10)
            PipeBuildCopperOreCost = 10;

        if (PipeDestroyGoldRefund < 0)
            PipeDestroyGoldRefund = 0;
        else if (PipeDestroyGoldRefund > 100)
            PipeDestroyGoldRefund = 100;

        if (PipeDestroyCopperOreRefund < 0)
            PipeDestroyCopperOreRefund = 0;
        else if (PipeDestroyCopperOreRefund > 10)
            PipeDestroyCopperOreRefund = 10;
    }

    /// <summary>Garantiza que ningún reembolso supere su coste de construcción.</summary>
    private void EnsureRefundNotGreaterThanCost()
    {
        if (PipeDestroyGoldRefund > PipeBuildGoldCost)
            PipeDestroyGoldRefund = PipeBuildGoldCost;

        if (PipeDestroyCopperOreRefund > PipeBuildCopperOreCost)
            PipeDestroyCopperOreRefund = PipeBuildCopperOreCost;
    }

    /// <summary>Valida los límites del coste de agua por casilla.</summary>
    private void EnsureWaterCostArguments()
    {
        if (WaterCostPerTile < 0.00f || float.IsNegative(WaterCostPerTile))
            WaterCostPerTile = 0.25f;
        else if (WaterCostPerTile < 0.01f)
            WaterCostPerTile = 0.01f;
        else if (WaterCostPerTile > 1.00f || float.IsPositiveInfinity(WaterCostPerTile))
            WaterCostPerTile = 1.00f;
    }

    /// <summary>Valida los límites de caudal para cada nivel de bomba.</summary>
    private void EnsurePumpOutputArguments()
    {
        if (BronzePumpWaterOutput <= 0f || float.IsNaN(BronzePumpWaterOutput) || float.IsInfinity(BronzePumpWaterOutput))
            BronzePumpWaterOutput = 10f;
        else if (BronzePumpWaterOutput > 20f)
            BronzePumpWaterOutput = 20f;

        if (SteelPumpWaterOutput <= 0f || float.IsNaN(SteelPumpWaterOutput) || float.IsInfinity(SteelPumpWaterOutput))
            SteelPumpWaterOutput = 25f;
        else if (SteelPumpWaterOutput > 50f)
            SteelPumpWaterOutput = 50f;

        if (GoldPumpWaterOutput <= 0f || float.IsNaN(GoldPumpWaterOutput) || float.IsInfinity(GoldPumpWaterOutput))
            GoldPumpWaterOutput = 80f;
        else if (GoldPumpWaterOutput > 160f)
            GoldPumpWaterOutput = 160f;

        if (IridiumPumpWaterOutput <= 0f || float.IsNaN(IridiumPumpWaterOutput) || float.IsInfinity(IridiumPumpWaterOutput))
            IridiumPumpWaterOutput = 200f;
        else if (IridiumPumpWaterOutput > 400f)
            IridiumPumpWaterOutput = 400f;
    }
}
