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

    /// <summary>Activa el mod en minas estándar y cueva calavera.</summary>
    public bool Mines { get; set; } = true;

    /// <summary>Activa el mod en Volcano Dungeon (Isla jengibre).</summary>
    public bool Volcano { get; set; } = true;

    /// <summary>Activa el mod en la granja principal e interiores de granja.</summary>
    public bool Farm { get; set; } = false;

    /// <summary>
    /// Activa el mod en cualquier localización que no sea esté ya controlada por otras opciones (como minas, granja, isla jengibre).
    /// <para>Por ejemplo permite calcular rutas en el bosque secreto, u en otros lugares de otros mods.</para>
    /// </summary>
    public bool Wilderness { get; set; } = false;

    /// <summary>Parámetros globales de pathfinding compartidos por todas las zonas activas.</summary>
    public PathFindingConfiguration Pathfinding { get; set; } = new PathFindingConfiguration
    {
        RecalcInterval = 45,
        MaxAStarNodes = 400,
        StuckThreshold = 20
    };

    /// <summary>Valida y normaliza todos los argumentos de configuración.</summary>
    public void EnsureArguments()
    {
        Pathfinding = Pathfinding.Normalize();
    }
}
