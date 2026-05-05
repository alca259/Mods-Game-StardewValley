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

    /// <summary>Configuración para las minas (MineShaft y Skull Cavern). Activa por defecto.</summary>
    public ZoneConfig Mines { get; set; } = new ZoneConfig
    {
        Enabled = true,
        RecalcInterval = 45,
        MaxAStarNodes = 400,
        StuckThreshold = 20
    };

    /// <summary>Configuración para la granja. Desactivada por defecto.</summary>
    public ZoneConfig Farm { get; set; } = new ZoneConfig
    {
        Enabled = false,
        RecalcInterval = 60,
        MaxAStarNodes = 200,
        StuckThreshold = 30
    };

    /// <summary>Valida y normaliza todos los argumentos de configuración.</summary>
    public void EnsureArguments()
    {
        Mines = NormalizeZone(Mines, enabledByDefault: true);
        Farm = NormalizeZone(Farm, enabledByDefault: false);
    }

    private static ZoneConfig NormalizeZone(ZoneConfig? zone, bool enabledByDefault)
    {
        zone ??= new ZoneConfig
        {
            Enabled = enabledByDefault
        };

        if (zone.RecalcInterval < 1)
            zone.RecalcInterval = 1;

        if (zone.MaxAStarNodes < 50)
            zone.MaxAStarNodes = 50;

        if (zone.StuckThreshold < 1)
            zone.StuckThreshold = 1;

        return zone;
    }
}

/// <summary>Parámetros de comportamiento para una zona concreta.</summary>
public sealed class ZoneConfig
{
    /// <summary>Activa o desactiva el mod en esta zona.</summary>
    public bool Enabled { get; set; }

    /// <summary>Frames entre recálculos de ruta. 60 frames aprox 1 segundo.</summary>
    public int RecalcInterval { get; set; }

    /// <summary>Límite de nodos explorados por A*. Protege los FPS.</summary>
    public int MaxAStarNodes { get; set; }

    /// <summary>Frames sin avanzar antes de considerar al monstruo atascado y recalcular.</summary>
    public int StuckThreshold { get; set; }
}
