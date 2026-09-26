using Microsoft.Xna.Framework;
using StardewValley;

namespace NoMoreStuckMonsters.Pathfinding;

/// <summary>
/// Implementación ligera de A* que opera sobre la rejilla de tiles del mapa actual.
/// Consulta múltiples capas de obstáculos para determinar si un tile es transitable.
/// </summary>
public static class AStarPathfinder
{
    /// <summary>
    /// Límite de radio para buscar un tile alternativo caminable cuando el inicio o destino están bloqueados.
    /// Evita búsquedas infinitas en mapas muy congestionados. Ajustar conforme a necesidades, pero 8 tiles (128px) suele ser suficiente
    /// para encontrar un punto de inicio/fin válido en la mayoría de los casos.
    /// </summary>
    private const int maxRadius = 8;

    /// <summary>
    /// Calcula una ruta desde <paramref name="fromPixel"/> hasta <paramref name="toPixel"/>
    /// evitando tiles bloqueados. Devuelve null si no existe ruta dentro del límite de nodos.
    /// </summary>
    /// <param name="fromPixel">Posición de origen en píxeles.</param>
    /// <param name="toPixel">Posición de destino en píxeles.</param>
    /// <param name="location">Mapa actual.</param>
    /// <param name="maxNodes">Nodos máximos a explorar. Protege los FPS en mapas grandes.</param>
    /// <param name="entityWidth">Ancho del bounding box de la entidad en píxeles.</param>
    /// <param name="entityHeight">Alto del bounding box de la entidad en píxeles.</param>
    /// <param name="clumpTiles">Set de tiles bloqueados por resource clumps, precalculado externamente.</param>
    /// <returns>Lista de coordenadas de tile (no píxeles) que forman la ruta, o null.</returns>
    public static List<Vector2>? FindPath(
        Vector2 fromPixel,
        Vector2 toPixel,
        GameLocation location,
        int maxNodes = 400,
        int entityWidth = 64,
        int entityHeight = 64,
        HashSet<Point>? clumpTiles = null)
    {
        if (entityWidth <= 0)
            throw new ArgumentOutOfRangeException(nameof(entityWidth));

        if (entityHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(entityHeight));

        int widthTiles = Math.Max(1, (int)Math.Ceiling(entityWidth / (float)Game1.tileSize));
        int heightTiles = Math.Max(1, (int)Math.Ceiling(entityHeight / (float)Game1.tileSize));

        // Obtenemos la capa Buildings UNA SOLA VEZ para toda la búsqueda.
        // GetLayer hace una búsqueda por string; llamarla dentro de los bucles de tiles
        // supone cientos de lookups redundantes por recálculo.
        var buildingsLayer = location.Map.GetLayer("Buildings");

        // Convertir posiciones de centro (píxeles) a coordenadas de tile
        Point start = PixelToTile(fromPixel);
        Point goal = PixelToTile(toPixel);

        if (IsTileBlocked(
            tile: start,
            location: location,
            widthTiles: widthTiles,
            heightTiles: heightTiles,
            clumpTiles: clumpTiles,
            buildingsLayer: buildingsLayer))
        {
            var nearestStart = FindNearestWalkableTile(
                blockedTile: start,
                towardTile: goal,
                location: location,
                widthTiles: widthTiles,
                heightTiles: heightTiles,
                clumpTiles: clumpTiles,
                buildingsLayer: buildingsLayer);

            if (nearestStart == null)
                return null;

            start = nearestStart.Value;
        }

        if (IsTileBlocked(
            tile: goal,
            location: location,
            widthTiles: widthTiles,
            heightTiles: heightTiles,
            clumpTiles: clumpTiles,
            buildingsLayer: buildingsLayer))
        {
            var nearestGoal = FindNearestWalkableTile(
                blockedTile: goal,
                towardTile: start,
                location: location,
                widthTiles: widthTiles,
                heightTiles: heightTiles,
                clumpTiles: clumpTiles,
                buildingsLayer: buildingsLayer);

            if (nearestGoal == null)
                return null;

            goal = nearestGoal.Value;
        }

        if (start == goal) return new List<Vector2>();

        // Obtener dimensiones del mapa para validar límites
        int mapWidth = location.Map.Layers[0].LayerWidth;
        int mapHeight = location.Map.Layers[0].LayerHeight;

        var openSet = new PriorityQueue<Point, float>();
        var closedSet = new HashSet<Point>();
        var cameFrom = new Dictionary<Point, Point>();
        var gScore = new Dictionary<Point, float> { [start] = 0f };
        int explored = 0;

        openSet.Enqueue(start, Heuristic(start, goal));

        // Vecinos cardinales inline con stackalloc para evitar asignaciones en el heap.
        // GetNeighbors con array asignaba ~32 bytes por nodo explorado (400 arrays/recálculo).
        Span<Point> neighbors = stackalloc Point[4];

        while (openSet.Count > 0 && explored < maxNodes)
        {
            var current = openSet.Dequeue();

            if (closedSet.Contains(current))
                continue;

            closedSet.Add(current);
            explored++;

            if (current == goal)
                return ReconstructPath(cameFrom, current);

            int neighborCount = 0;
            if (current.X + 1 < mapWidth)
            {
                neighbors[neighborCount++] = new Point(current.X + 1, current.Y);
            }

            if (current.X - 1 >= 0)
            {
                neighbors[neighborCount++] = new Point(current.X - 1, current.Y);
            }

            if (current.Y + 1 < mapHeight)
            {
                neighbors[neighborCount++] = new Point(current.X, current.Y + 1);
            }

            if (current.Y - 1 >= 0)
            {
                neighbors[neighborCount++] = new Point(current.X, current.Y - 1);
            }

            for (int ni = 0; ni < neighborCount; ni++)
            {
                var neighbor = neighbors[ni];

                if (closedSet.Contains(neighbor))
                    continue;

                if (IsTileBlocked(
                    tile: neighbor,
                    location: location,
                    widthTiles: widthTiles,
                    heightTiles: heightTiles,
                    clumpTiles: clumpTiles,
                    buildingsLayer: buildingsLayer))
                {
                    continue;
                }

                float tentativeG = gScore[current] + 1f;

                if (!gScore.TryGetValue(neighbor, out float existingG) || tentativeG < existingG)
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;

                    float f = tentativeG + Heuristic(neighbor, goal);
                    openSet.Enqueue(neighbor, f);
                }
            }
        }

        // No se encontró ruta dentro del límite de nodos
        return null;
    }

    #region Detección de obstáculos
    /// <summary>
    /// Comprueba obstáculos y transitabilidad para toda la huella de la entidad en tiles.
    /// Recibe la capa Buildings ya resuelta para evitar llamadas repetidas a GetLayer.
    /// </summary>
    /// <param name="tile">Tile ancla (esquina superior izquierda de la huella).</param>
    /// <param name="location">Localización actual.</param>
    /// <param name="widthTiles">Ancho de huella en tiles.</param>
    /// <param name="heightTiles">Alto de huella en tiles.</param>
    /// <param name="clumpTiles">Set de tiles de resource clumps precalculado.</param>
    /// <param name="buildingsLayer">Capa Buildings ya resuelta por el llamador.</param>
    /// <returns>True si cualquier tile de la huella está bloqueado.</returns>
    private static bool IsTileBlocked(
        Point tile,
        GameLocation location,
        int widthTiles,
        int heightTiles,
        HashSet<Point>? clumpTiles,
        xTile.Layers.Layer? buildingsLayer)
    {
        int maxX = tile.X + widthTiles - 1;
        int maxY = tile.Y + heightTiles - 1;

        if (tile.X < 0 || tile.Y < 0 ||
            maxX >= location.Map.Layers[0].LayerWidth ||
            maxY >= location.Map.Layers[0].LayerHeight)
            return true;

        for (int x = tile.X; x <= maxX; x++)
        {
            for (int y = tile.Y; y <= maxY; y++)
            {
                var tileVec = new Vector2(x, y);

                // 1. Objetos pequeños: piedras sueltas, barriles, cofres, artesanía
                if (location.Objects.ContainsKey(tileVec))
                    return true;

                // 2. Capa "Buildings" del mapa TMX: muros, rocas del mapa, estructuras fijas
                if (buildingsLayer?.Tiles[x, y] != null)
                    return true;

                // 3. Validación final de transitabilidad del tile
                if (!location.isTilePassable(tileVec))
                    return true;

                // 4. Resource clumps (rocas/troncos grandes) precalculados externamente
                if (clumpTiles != null && clumpTiles.Contains(new Point(x, y)))
                    return true;
            }
        }

        return false;
    }
    #endregion

    #region Utilidades
    /// <summary>Heurística Manhattan, adecuada para movimiento en 4 direcciones.</summary>
    private static float Heuristic(Point a, Point b)
        => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);

    /// <summary>Busca el tile caminable más cercano a uno bloqueado para usarlo como fallback de inicio/objetivo.</summary>
    /// <param name="blockedTile">Tile bloqueado a sustituir.</param>
    /// <param name="towardTile">Tile de referencia para priorizar candidatos.</param>
    /// <param name="location">Localización actual.</param>
    /// <param name="widthTiles">Ancho de huella en tiles.</param>
    /// <param name="heightTiles">Alto de huella en tiles.</param>
    /// <param name="clumpTiles">Set de tiles de resource clumps precalculado.</param>
    /// <param name="buildingsLayer">Capa Buildings ya resuelta por el llamador.</param>
    /// <returns>Tile alternativo caminable o null si no encuentra candidato.</returns>
    private static Point? FindNearestWalkableTile(
        Point blockedTile,
        Point towardTile,
        GameLocation location,
        int widthTiles,
        int heightTiles,
        HashSet<Point>? clumpTiles,
        xTile.Layers.Layer? buildingsLayer)
    {
        Point? best = null;
        float bestScore = float.MaxValue;

        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                int absDx = Math.Abs(dx);
                int dy = radius - absDx;

                TryCandidate(
                    candidate: new Point(blockedTile.X + dx, blockedTile.Y + dy),
                    blockedTile: blockedTile,
                    towardTile: towardTile,
                    location: location,
                    widthTiles: widthTiles,
                    heightTiles: heightTiles,
                    clumpTiles: clumpTiles,
                    buildingsLayer: buildingsLayer,
                    best: ref best,
                    bestScore: ref bestScore);

                if (dy != 0)
                {
                    TryCandidate(
                        candidate: new Point(blockedTile.X + dx, blockedTile.Y - dy),
                        blockedTile: blockedTile,
                        towardTile: towardTile,
                        location: location,
                        widthTiles: widthTiles,
                        heightTiles: heightTiles,
                        clumpTiles: clumpTiles,
                        buildingsLayer: buildingsLayer,
                        best: ref best,
                        bestScore: ref bestScore);
                }
            }
        }

        return best;
    }

    /// <summary>Evalúa un candidato de fallback y actualiza el mejor score encontrado.</summary>
    /// <param name="candidate">Tile candidato.</param>
    /// <param name="blockedTile">Tile bloqueado original.</param>
    /// <param name="towardTile">Tile hacia el que conviene aproximar.</param>
    /// <param name="location">Localización actual.</param>
    /// <param name="widthTiles">Ancho de huella en tiles.</param>
    /// <param name="heightTiles">Alto de huella en tiles.</param>
    /// <param name="clumpTiles">Set de tiles de resource clumps precalculado.</param>
    /// <param name="buildingsLayer">Capa Buildings ya resuelta por el método previo.</param>
    /// <param name="best">Mejor candidato encontrado hasta el momento.</param>
    /// <param name="bestScore">Puntuación del mejor candidato.</param>
    private static void TryCandidate(
        Point candidate,
        Point blockedTile,
        Point towardTile,
        GameLocation location,
        int widthTiles,
        int heightTiles,
        HashSet<Point>? clumpTiles,
        xTile.Layers.Layer? buildingsLayer,
        ref Point? best,
        ref float bestScore)
    {
        if (IsTileBlocked(candidate, location, widthTiles, heightTiles, clumpTiles, buildingsLayer))
            return;

        float distanceFromBlocked = Heuristic(candidate, blockedTile);
        float towardScore = Heuristic(candidate, towardTile);
        float score = (distanceFromBlocked * 100f) + towardScore;
        if (score >= bestScore)
            return;

        best = candidate;
        bestScore = score;
    }

    /// <summary>Reconstruye la ruta final desde el nodo objetivo hasta el inicio.</summary>
    /// <param name="cameFrom">Mapa de predecesores generado por A*.</param>
    /// <param name="current">Nodo objetivo alcanzado.</param>
    /// <returns>Ruta ordenada desde origen a destino.</returns>
    private static List<Vector2> ReconstructPath(Dictionary<Point, Point> cameFrom, Point current)
    {
        var path = new List<Vector2>();
        while (cameFrom.ContainsKey(current))
        {
            path.Add(new Vector2(current.X, current.Y));
            current = cameFrom[current];
        }
        path.Reverse();
        return path;
    }

    /// <summary>Convierte una posición en píxeles a coordenadas de tile.</summary>
    /// <param name="pixel">Posición en píxeles.</param>
    /// <returns>Coordenada de tile correspondiente.</returns>
    private static Point PixelToTile(Vector2 pixel)
        => new((int)(pixel.X / Game1.tileSize), (int)(pixel.Y / Game1.tileSize));
    #endregion
}
