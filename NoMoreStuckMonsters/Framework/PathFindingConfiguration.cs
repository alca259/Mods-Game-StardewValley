namespace NoMoreStuckMonsters.Framework;

/// <summary>Parámetros de comportamiento para una zona concreta.</summary>
public sealed class PathFindingConfiguration
{
    /// <summary>Frames entre recálculos de ruta. 60 frames aprox 1 segundo.</summary>
    public int RecalcInterval { get; set; }

    /// <summary>Límite de nodos explorados por A*. Protege los FPS.</summary>
    public int MaxAStarNodes { get; set; }

    /// <summary>Frames sin avanzar antes de considerar al monstruo atascado y recalcular.</summary>
    public int StuckThreshold { get; set; }

    /// <summary>Normaliza los valores de la configuración para que estén dentro de rangos razonables.</summary>
    public PathFindingConfiguration Normalize()
    {
        if (RecalcInterval < 1)
            RecalcInterval = 1;
        else if (RecalcInterval > 240)
            RecalcInterval = 240;

        if (MaxAStarNodes < 50)
            MaxAStarNodes = 50;
        else if (MaxAStarNodes > 1000)
            MaxAStarNodes = 1000;

        if (StuckThreshold < 1)
            StuckThreshold = 1;
        else if (StuckThreshold > 120)
            StuckThreshold = 120;

        return this;
    }
}
