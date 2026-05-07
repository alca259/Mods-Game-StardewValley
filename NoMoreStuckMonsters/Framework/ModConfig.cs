using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace NoMoreStuckMonsters.Framework;

/// <summary>Configuración del mod</summary>
public sealed class ModConfig
{
    /// <summary>Indica si el mod está habilitado</summary>
    public bool EnableMod { get; set; } = true;

    /// <summary>Indica si se dibuja la ruta A* de depuración en pantalla.</summary>
    public bool ShowDebugPath { get; set; } = false;

    /// <summary>La tecla para recargar la configuración del mod sin reiniciar el juego.</summary>
    public KeybindList ReloadKey { get; set; } = new(SButton.F5);

    /// <summary>Activa el mod en minas estándar y Skull Cavern.</summary>
    public bool Mines { get; set; } = true;

    /// <summary>Activa el mod en Volcano Dungeon (Isla Ginger).</summary>
    public bool Volcano { get; set; } = true;

    /// <summary>Activa el mod en la granja principal e interiores de granja.</summary>
    public bool Farm { get; set; } = false;

    /// <summary>
    /// Activa el mod en cualquier localización que no sea Mines, Volcano o Farm.
    /// Útil para mods de contenido con mapas nuevos.
    /// </summary>
    public bool Wilderness { get; set; } = false;

    /// <summary>Parámetros globales de pathfinding compartidos por todas las zonas activas.</summary>
    public ZoneConfig Pathfinding { get; set; } = new ZoneConfig
    {
        RecalcInterval = 45,
        MaxAStarNodes = 400,
        StuckThreshold = 20
    };

    /// <summary>Valida y normaliza todos los argumentos de configuración.</summary>
    public void EnsureArguments()
    {
        Pathfinding = NormalizeZone(Pathfinding);
    }

    private static ZoneConfig NormalizeZone(ZoneConfig? zone)
    {
        zone ??= new ZoneConfig();

        if (zone.RecalcInterval < 1)
            zone.RecalcInterval = 1;
        else if (zone.RecalcInterval > 240)
            zone.RecalcInterval = 240;

        if (zone.MaxAStarNodes < 50)
            zone.MaxAStarNodes = 50;
        else if (zone.MaxAStarNodes > 1000)
            zone.MaxAStarNodes = 1000;

        if (zone.StuckThreshold < 1)
            zone.StuckThreshold = 1;
        else if (zone.StuckThreshold > 120)
            zone.StuckThreshold = 120;

        return zone;
    }
}

/// <summary>Parámetros de comportamiento para una zona concreta.</summary>
public sealed class ZoneConfig
{
    /// <summary>Frames entre recálculos de ruta. 60 frames aprox 1 segundo.</summary>
    public int RecalcInterval { get; set; }

    /// <summary>Límite de nodos explorados por A*. Protege los FPS.</summary>
    public int MaxAStarNodes { get; set; }

    /// <summary>Frames sin avanzar antes de considerar al monstruo atascado y recalcular.</summary>
    public int StuckThreshold { get; set; }
}
