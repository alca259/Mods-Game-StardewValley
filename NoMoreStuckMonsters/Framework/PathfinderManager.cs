using Microsoft.Xna.Framework;
using NoMoreStuckMonsters.Pathfinding;
using StardewValley;
using StardewValley.Monsters;

namespace NoMoreStuckMonsters.Framework;

/// <summary>
/// Gestiona el estado de pathfinding de cada monstruo activo.
/// Aplica throttling (recálculo cada N frames) para el cálculo A*.
/// </summary>
public class PathfinderManager
{
    #region Campos
    /// <summary>Distancia en píxeles para cada paso de colisión.</summary>
    private const float CollisionStepPixels = 16f;

    /// <summary>Margen para mantener el eje de avance previo y evitar oscilación de dirección.</summary>
    private const float AxisHysteresisPixels = 6f;

    /// <summary>Ticks entre limpiezas incrementales de estados huérfanos.</summary>
    private const int CleanupIntervalTicks = 60;

    /// <summary>La key es el GetHashCode() del monstruo, que es único por instancia en la sesión.</summary>
    private readonly Dictionary<int, MonsterPathState> _states = new();

    /// <summary>Contador interno para espaciar la limpieza incremental.</summary>
    private int _cleanupTickCounter;
    #endregion

    #region API pública
    /// <summary>
    /// Intenta mover <paramref name="monster"/> hacia <paramref name="targetPixel"/>
    /// usando la estrategia indicada por <paramref name="cfg"/>.
    /// Devuelve true si el manager tomó el control del movimiento.
    /// </summary>
    /// <param name="monster">Monstruo a mover.</param>
    /// <param name="fromPixel">Posición actual del monstruo en píxeles (centro de hitbox).</param>
    /// <param name="targetPixel">Posición objetivo en píxeles (centro de hitbox del jugador).</param>
    /// <param name="location">Localización actual del monstruo.</param>
    /// <param name="cfg">Configuración activa de la zona.</param>
    /// <returns>True si el manager procesó el monstruo en este tick.</returns>
    public bool TryMoveMonster(
        Monster monster,
        Vector2 fromPixel,
        Vector2 targetPixel,
        GameLocation location,
        ZoneConfig cfg)
    {
        int id = monster.GetHashCode();
        if (!_states.TryGetValue(id, out var state))
        {
            state = new MonsterPathState();
            _states[id] = state;
        }

        // Detección de atasco
        state.FramesSinceCalc++;
        bool moved = Vector2.Distance(fromPixel, state.LastPosition) >= 2f;
        state.StuckFrames = moved ? 0 : state.StuckFrames + 1;
        state.LastPosition = fromPixel;

        bool needsRecalc =
            state.Path == null
            || state.Path.Count == 0
            || state.FramesSinceCalc >= cfg.RecalcInterval
            || state.StuckFrames >= cfg.StuckThreshold;

        // Recálculo de ruta
        if (needsRecalc)
        {
            state.FramesSinceCalc = 0;
            state.StuckFrames = 0;

            Rectangle bounds = monster.GetBoundingBox();
            var path = AStarPathfinder.FindPath(
                fromPixel,
                targetPixel,
                location,
                cfg.MaxAStarNodes,
                bounds.Width,
                bounds.Height);

            if (path != null && path.Count > 0)
                state.Path = path;
            else
                state.Path = null;
        }

        if (state.Path?.Count > 0)
            return FollowAStarPath(monster, state);

        return true;
    }

    /// <summary>Limpia el cache de estados. Llama al cambiar de mapa o al cargar partida.</summary>
    public void ClearCache()
    {
        _states.Clear();
        _cleanupTickCounter = 0;
    }

    /// <summary>
    /// Elimina estados huérfanos de monstruos que ya no existen en la localización actual.
    /// La limpieza se ejecuta de forma incremental para reducir coste por frame.
    /// </summary>
    /// <param name="location">Localización de referencia para obtener monstruos activos.</param>
    public void CleanupOrphanStates(GameLocation location)
    {
        ArgumentNullException.ThrowIfNull(location);

        if (_states.Count == 0)
            return;

        _cleanupTickCounter++;
        if (_cleanupTickCounter < CleanupIntervalTicks)
            return;

        _cleanupTickCounter = 0;

        HashSet<int> activeMonsterIds = new();
        foreach (var character in location.characters)
        {
            if (character is Monster monster)
                activeMonsterIds.Add(monster.GetHashCode());
        }

        List<int> orphanIds = new();
        foreach (int id in _states.Keys)
        {
            if (!activeMonsterIds.Contains(id))
                orphanIds.Add(id);
        }

        foreach (int id in orphanIds)
            RemoveMonster(id);
    }

    /// <summary>
    /// Intenta devolver la ruta A* actual en coordenadas de tile para depuración visual.
    /// </summary>
    /// <param name="monster">Monstruo a consultar.</param>
    /// <param name="pathTiles">Salida con la ruta en tiles si existe.</param>
    /// <returns>True si hay ruta A* activa para ese monstruo.</returns>
    public bool TryGetDebugPathTiles(Monster monster, out List<Point> pathTiles)
    {
        pathTiles = new List<Point>();

        int id = monster.GetHashCode();
        if (!_states.TryGetValue(id, out var state))
            return false;

        if (state.Path == null || state.Path.Count == 0)
            return false;

        foreach (var tile in state.Path)
            pathTiles.Add(new Point((int)tile.X, (int)tile.Y));

        return pathTiles.Count > 0;
    }
    #endregion

    #region Seguimiento de ruta
    /// <summary>Avanza al monstruo un paso a lo largo de la ruta A* calculada.</summary>
    /// <param name="monster">Monstruo a mover.</param>
    /// <param name="state">Estado de navegación del monstruo.</param>
    /// <returns>True si el método procesó el tick de movimiento.</returns>
    private static bool FollowAStarPath(Monster monster, MonsterPathState state)
    {
        // El path contiene coordenadas de tile; las convertimos a píxeles
        if (state.Path == null || state.Path.Count == 0)
            return false;

        Vector2 monsterCenter = monster.GetBoundingBox().Center.ToVector2();
        Vector2 nextTileCoord = state.Path[0];
        Vector2 nextPixel = nextTileCoord * Game1.tileSize
                                + new Vector2(Game1.tileSize / 2f); // centro del tile

        float speed = monster.speed;
        float dist = Vector2.Distance(monsterCenter, nextPixel);

        // Nodo alcanzado: eliminarlo y apuntar al siguiente en el próximo tick
        if (dist <= speed + 2f)
            state.Path.RemoveAt(0);

        if (state.Path.Count == 0)
            return true; // destino alcanzado, próximo tick recalcula

        nextTileCoord = state.Path[0];
        nextPixel = nextTileCoord * Game1.tileSize + new Vector2(Game1.tileSize / 2f);

        // Mover hacia el siguiente waypoint
        monsterCenter = monster.GetBoundingBox().Center.ToVector2();
        Vector2 toNext = nextPixel - monsterCenter;
        if (toNext.LengthSquared() <= float.Epsilon)
            return true;

        // Movimiento cardinal prioritario para evitar cortes diagonales contra obstáculos.
        Vector2 primaryMove = BuildPrimaryCardinalMovement(toNext, speed, state.LastDirection);
        if (TryMoveWithCollision(monster, primaryMove))
        {
            int direction = VectorToDirection(Vector2.Normalize(primaryMove));
            monster.faceDirection(direction);
            AnimateWalking(monster, direction);
            state.LastDirection = direction;
            return true;
        }

        // Si el eje principal está bloqueado, intentamos el eje secundario antes de recalcular.
        Vector2 secondaryMove = BuildSecondaryCardinalMovement(toNext, speed);
        if (secondaryMove != Vector2.Zero && TryMoveWithCollision(monster, secondaryMove))
        {
            int direction = VectorToDirection(Vector2.Normalize(secondaryMove));
            monster.faceDirection(direction);
            AnimateWalking(monster, direction);
            state.LastDirection = direction;
            return true;
        }

        // Si ambos ejes fallan, invalidamos la ruta para forzar un nuevo cálculo.
        state.Path = null;

        return true;
    }
    #endregion

    #region Utilidades de movimiento
    /// <summary>Construye el desplazamiento cardinal principal hacia el siguiente waypoint.</summary>
    /// <param name="toNext">Vector hacia el siguiente waypoint.</param>
    /// <param name="speed">Velocidad a aplicar en el tick actual.</param>
    /// <returns>Vector de movimiento en eje X o Y.</returns>
    private static Vector2 BuildPrimaryCardinalMovement(Vector2 toNext, float speed, int lastDirection)
    {
        float absX = Math.Abs(toNext.X);
        float absY = Math.Abs(toNext.Y);
        bool keepHorizontal = (lastDirection == 1 || lastDirection == 3) && absY > absX && (absY - absX) <= AxisHysteresisPixels;
        bool keepVertical = (lastDirection == 0 || lastDirection == 2) && absX > absY && (absX - absY) <= AxisHysteresisPixels;

        if (absX >= absY || keepHorizontal)
            return new Vector2(Math.Sign(toNext.X), 0f) * speed;

        if (keepVertical)
            return new Vector2(0f, Math.Sign(toNext.Y)) * speed;

        return new Vector2(0f, Math.Sign(toNext.Y)) * speed;
    }

    /// <summary>Construye un desplazamiento cardinal alternativo en el eje secundario.</summary>
    /// <param name="toNext">Vector hacia el siguiente waypoint.</param>
    /// <param name="speed">Velocidad a aplicar en el tick actual.</param>
    /// <returns>Vector de movimiento alternativo o <see cref="Vector2.Zero"/> si no aplica.</returns>
    private static Vector2 BuildSecondaryCardinalMovement(Vector2 toNext, float speed)
    {
        if (Math.Abs(toNext.X) >= Math.Abs(toNext.Y))
        {
            if (Math.Abs(toNext.Y) <= float.Epsilon)
                return Vector2.Zero;

            return new Vector2(0f, Math.Sign(toNext.Y)) * speed;
        }

        if (Math.Abs(toNext.X) <= float.Epsilon)
            return Vector2.Zero;

        return new Vector2(Math.Sign(toNext.X), 0f) * speed;
    }

    /// <summary>
    /// Intenta mover al monstruo en subpasos para reducir clipping,
    /// validando colisión en cada subpaso.
    /// </summary>
    /// <param name="monster">Monstruo a desplazar.</param>
    /// <param name="movement">Desplazamiento total deseado para el tick.</param>
    /// <returns>True si logró desplazarse al menos un subpaso.</returns>
    private static bool TryMoveWithCollision(Monster monster, Vector2 movement)
    {
        float distance = movement.Length();
        if (distance <= 0f)
            return false;

        int steps = Math.Max(1, (int)Math.Ceiling(distance / CollisionStepPixels));
        Vector2 step = movement / steps;
        bool moved = false;

        for (int i = 0; i < steps; i++)
        {
            Vector2 nextPos = monster.Position + step;
            if (IsPositionBlocked(monster, nextPos))
                break;

            monster.Position = nextPos;
            moved = true;
        }

        return moved;
    }

    /// <summary>Comprueba si la posición candidata del monstruo colisiona con el entorno.</summary>
    /// <param name="monster">Monstruo a evaluar.</param>
    /// <param name="nextPosition">Posición candidata en píxeles.</param>
    /// <returns>True si la posición está bloqueada.</returns>
    private static bool IsPositionBlocked(Monster monster, Vector2 nextPosition)
    {
        GameLocation location = monster.currentLocation;
        Rectangle nextBounds = monster.GetBoundingBox();
        int deltaX = (int)Math.Round(nextPosition.X - monster.Position.X);
        int deltaY = (int)Math.Round(nextPosition.Y - monster.Position.Y);
        nextBounds.Offset(deltaX, deltaY);

        int leftTile = nextBounds.Left / Game1.tileSize;
        int rightTile = (nextBounds.Right - 1) / Game1.tileSize;
        int topTile = nextBounds.Top / Game1.tileSize;
        int bottomTile = (nextBounds.Bottom - 1) / Game1.tileSize;

        for (int x = leftTile; x <= rightTile; x++)
        {
            for (int y = topTile; y <= bottomTile; y++)
            {
                Vector2 tileVec = new(x, y);
                if (x < 0 || y < 0 ||
                    x >= location.Map.Layers[0].LayerWidth ||
                    y >= location.Map.Layers[0].LayerHeight)
                    return true;

                if (location.Objects.ContainsKey(tileVec))
                    return true;

                var buildingsLayer = location.Map.GetLayer("Buildings");
                if (buildingsLayer?.Tiles[x, y] != null)
                    return true;

                if (!location.isTilePassable(tileVec))
                    return true;
            }
        }

        return false;
    }

    /// <summary>Convierte un vector de dirección en una dirección de sprite (0-3).</summary>
    /// <param name="dir">Vector de dirección normalizado.</param>
    /// <returns>Dirección de sprite: 0=arriba, 1=derecha, 2=abajo, 3=izquierda.</returns>
    private static int VectorToDirection(Vector2 dir)
    {
        if (Math.Abs(dir.X) >= Math.Abs(dir.Y))
            return dir.X > 0 ? 1 : 3;
        return dir.Y > 0 ? 2 : 0;
    }

    /// <summary>
    /// Avanza manualmente un frame de la animación de caminar del monstruo.
    /// Se usa cuando el movimiento se aplica reasignando Position.
    /// </summary>
    /// <param name="monster">Monstruo a animar.</param>
    /// <param name="direction">Dirección de sprite: 0=arriba, 1=derecha, 2=abajo, 3=izquierda.</param>
    private static void AnimateWalking(Monster monster, int direction)
    {
        // Slimes (incluyendo variantes grandes) usan animación cíclica propia.
        // Forzar AnimateUp/Down/Left/Right puede pisar su "respiración".
        if (monster is GreenSlime || monster is BigSlime)
            return;

        switch (direction)
        {
            case 0:
                monster.Sprite.AnimateUp(Game1.currentGameTime, 0, string.Empty);
                break;
            case 1:
                monster.Sprite.AnimateRight(Game1.currentGameTime, 0, string.Empty);
                break;
            case 2:
                monster.Sprite.AnimateDown(Game1.currentGameTime, 0, string.Empty);
                break;
            case 3:
                monster.Sprite.AnimateLeft(Game1.currentGameTime, 0, string.Empty);
                break;
        }
    }
    #endregion

    #region Gestión de estado
    /// <summary>Elimina el estado de un monstruo concreto (ej. al morir).</summary>
    private void RemoveMonster(int hashCode) => _states.Remove(hashCode);
    #endregion

    #region Tipos anidados
    /// <summary>Estado por monstruo</summary>
    private class MonsterPathState
    {
        public List<Vector2>? Path = null;
        public int FramesSinceCalc = 0;
        public int StuckFrames = 0;
        public Vector2 LastPosition = Vector2.Zero;
        public int LastDirection = -1;
    }
    #endregion
}
