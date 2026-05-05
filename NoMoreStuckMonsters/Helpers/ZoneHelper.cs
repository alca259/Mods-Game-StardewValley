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

    /// <summary>Devuelve el tipo de zona para una GameLocation concreta.</summary>
    /// <param name="location">Localización a evaluar.</param>
    /// <returns>Tipo de zona mapeado para la localización.</returns>
    public static ZoneType GetZoneType(GameLocation location)
    {
        if (location == null) return ZoneType.None;

        // MineShaft cubre todas las plantas de minas estándar y Skull Cavern
        if (location is MineShaft) return ZoneType.Mines;

        // VolcanoDungeon (Isla Ginger) tiene enemigos terrestres y suelo sólido
        if (location is VolcanoDungeon) return ZoneType.Mines;

        // Granja principal y construcciones interiores de la granja
        if (location is Farm || location is FarmHouse)
            return ZoneType.Farm;

        return ZoneType.None;
    }

    /// <summary>
    /// Devuelve la ZoneConfig activa para la localización dada,
    /// o null si la zona no está habilitada en la configuración.
    /// </summary>
    /// <param name="location">Localización actual.</param>
    /// <param name="cfg">Configuración global del mod.</param>
    /// <returns>Configuración de zona activa o null si no aplica.</returns>
    public static ZoneConfig? GetActiveConfig(GameLocation location, ModConfig cfg)
    {
        return GetZoneType(location) switch
        {
            ZoneType.Mines when cfg.Mines.Enabled => cfg.Mines,
            ZoneType.Farm when cfg.Farm.Enabled => cfg.Farm,
            _ => null
        };
    }

    /// <summary>Devuelve true si el monstruo debe ser excluido del pathfinding.</summary>
    /// <param name="monster">Monstruo a evaluar.</param>
    /// <param name="player">Jugador objetivo potencial.</param>
    /// <returns>True si el monstruo no debe procesarse por el mod.</returns>
    public static bool ShouldSkipMonster(Monster monster, Farmer? player)
    {
        // Flag nativo de Stardew para cualquier enemigo aéreo
        if (monster.isGlider.Value) return true;

        // Lista explícita para tipos que no siempre usan isGlider correctamente
        if (_flyingTypes.Contains(monster.GetType())) return true;

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
}
