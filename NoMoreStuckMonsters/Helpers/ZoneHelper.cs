using Microsoft.Xna.Framework;
using NoMoreStuckMonsters.Framework;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;

namespace NoMoreStuckMonsters.Helpers;

/// <summary>Detecta la zona activa y filtra los monstruos que no deben ser afectados por el mod.</summary>
public static class ZoneHelper
{
    // Tipos de monstruo que vuelan o flotan y no tienen colisión con el terreno.
    // Puede que me haya dejado alguno, pero a estos no les afecta el pathfinding porque no se atascan con el terreno.
    private static readonly HashSet<Type> _flyingTypes = new()
    {
        // Murciélago
        typeof(Bat),
        // Mosca
        typeof(Fly),
        // Calamar bebe
        typeof(SquidKid),
        // Serpiente
        typeof(Serpent),
        // Fantasma
        typeof(Ghost)
    };

    // Tipos de monstruo que no vuelan pero tienen comportamientos o colisiones especiales que los hacen incompatibles con el pathfinding.
    private static readonly HashSet<Type> _otherExcludedTypes = new()
    {
        // Los dinosaurios no tienen obstaculos de terreno y atacan a distancia, dejo su IA nativa.
        typeof(DinoMonster),
        // El Cavadorín se mueve bajo tierra con lógica propia, no debe forzarse con este pathfinding.
        typeof(Duggy),
    };

    // Tipos que requieren que el update/movimiento nativo siga activo para conservar animaciones/estados internos.
    private static readonly HashSet<Type> _needsNativeAnimation = new()
    {
        typeof(RockCrab)
    };

    /// <summary>
    /// Devuelve la ZoneConfig activa para la localización dada,
    /// o null si la zona no está habilitada en la configuración.
    /// </summary>
    /// <param name="location">Localización actual.</param>
    /// <param name="cfg">Configuración global del mod.</param>
    /// <returns>Configuración de zona activa o null si no aplica.</returns>
    public static PathFindingConfiguration? GetActiveConfig(GameLocation location, ModConfig cfg)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(cfg);

        // Zonas conocidas por tipo (más robusto que nombres de mapa)
        if (location is MineShaft)
            return cfg.Mines ? cfg.Pathfinding : null;

        if (location is VolcanoDungeon)
            return cfg.Volcano ? cfg.Pathfinding : null;

        if (location is Farm || location is FarmHouse)
            return cfg.Farm ? cfg.Pathfinding : null;

        // Fallback general para otras localizaciones (incluye mods de contenido).
        return cfg.Wilderness ? cfg.Pathfinding : null;
    }

    /// <summary>Devuelve true si el monstruo debe ser excluido del pathfinding.</summary>
    /// <param name="monster">Monstruo a evaluar.</param>
    /// <param name="player">Jugador objetivo potencial.</param>
    /// <returns>True si el monstruo no debe procesarse por el mod.</returns>
    public static bool ShouldSkipMonster(Monster monster, Farmer? player)
    {
        // Flag nativo de Stardew para cualquier enemigo aéreo
        if (monster.isGlider.Value) return true;

        // Lista explícita para tipos voladores que no siempre usan isGlider correctamente
        if (_flyingTypes.Contains(monster.GetType())) return true;

        // Otros tipos de monstruos no voladores que quedan excluidos por su comportamiento o colisiones únicas
        if (_otherExcludedTypes.Contains(monster.GetType())) return true;

        // Sin jugador objetivo: no hay aggro
        if (player == null) return true;

        // Fuera del radio de detección nativo: no nos ha visto
        float detectionTiles = monster.moveTowardPlayerThreshold.Value;
        float distanceTiles = Vector2.Distance(monster.Position, player.Position) / Game1.tileSize;
        if (distanceTiles > detectionTiles) return true;

        // Slimes domésticos (pueden socializar): no atacan
        if (monster is GreenSlime slime && slime.CanSocialize) return true;

        return false;
    }

    /// <summary>Indica si el monstruo necesita conservar su ciclo nativo de animación.</summary>
    /// <param name="monster">Monstruo a evaluar.</param>
    /// <returns>True si requiere mantener speed nativo durante UpdateTicking.</returns>
    public static bool NeedsNativeAnimation(Monster monster)
    {
        ArgumentNullException.ThrowIfNull(monster);
        return _needsNativeAnimation.Contains(monster.GetType());
    }
}
