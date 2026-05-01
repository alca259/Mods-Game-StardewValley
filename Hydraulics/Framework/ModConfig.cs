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
    }
}
