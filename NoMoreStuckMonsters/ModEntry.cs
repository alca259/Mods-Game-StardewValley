using Alca259.Common;
using Microsoft.Xna.Framework;
using NoMoreStuckMonsters.Framework;
using NoMoreStuckMonsters.Helpers;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Monsters;

namespace NoMoreStuckMonsters;

/// <summary>
/// Punto de entrada del mod.
/// Coordina el ciclo de pathfinding mediante eventos nativos de SMAPI,
/// restaura velocidades al finalizar cada tick y mantiene limpio el estado interno.
/// <para>Prefiero no usar Harmony a menos que sea absolutamente necesario.</para>
///
/// Estrategia de movimiento:
/// <para>- UpdateTicking: ponemos speed = 0 en todos los monstruos elegibles
/// ANTES de que el juego ejecute su IA nativa, evitando el doble movimiento.</para>
/// <para>- UpdateTicked:  restauramos la velocidad real y aplicamos nuestro pathfinding
/// DESPUÉS de que el juego haya procesado su tick.</para>
/// <para>Cuando el juego está en pausa o con menú abierto, el mod evita cálculo adicional
/// y garantiza la restauración de velocidades guardadas.</para>
/// </summary>
public partial class ModEntry : Mod
{
    #region Fields
    private ModConfig _config = null!;
    private static PathfinderManager _pathfinderManager = default!;
    private readonly Dictionary<int, int> _savedSpeeds = new();
    #endregion

    #region Override entry point
    /// <inheritdoc/>
    public override void Entry(IModHelper helper)
    {
        CommonHelper.RemoveObsoleteFiles(this, "NoMoreStuckMonsters.pdb");
        _config = helper.ReadConfig<ModConfig>();
        _config.EnsureArguments();
        _pathfinderManager = new PathfinderManager();

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.UpdateTicking += OnUpdateTicking;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Input.ButtonsChanged += OnButtonsChanged;
        helper.Events.Player.Warped += OnWarped;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.Display.RenderedWorld += OnRenderedWorld;

        Monitor.Log($"Mod cargado: {ModManifest.Name} v{ModManifest.Version}");
    }
    #endregion

    #region Event Handlers
    /// <summary>Inicializa integraciones del mod tras cargar el juego.</summary>
    /// <param name="sender">Origen del evento.</param>
    /// <param name="e">Argumentos del evento <see cref="GameLaunchedEventArgs"/>.</param>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        SetupGenericModMenu();
    }

    /// <summary>
    /// Recarga la configuración desde disco cuando el usuario pulsa la tecla configurada.
    /// </summary>
    /// <param name="sender">Origen del evento.</param>
    /// <param name="e">Argumentos del evento <see cref="ButtonsChangedEventArgs"/>.</param>
    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!_config.ReloadKey.JustPressed())
            return;

        _config = Helper.ReadConfig<ModConfig>();
        _config.EnsureArguments();
        Monitor.Log(T("log.configReloaded"), LogLevel.Debug);
    }

    /// <summary>
    /// Se ejecuta ANTES del tick del juego.
    /// Suprimimos la velocidad de los monstruos elegibles para que la IA nativa
    /// no los mueva durante este tick.
    /// </summary>
    /// <param name="sender">Origen del evento.</param>
    /// <param name="e">Argumentos del evento <see cref="UpdateTickingEventArgs"/>.</param>
    private void OnUpdateTicking(object? sender, UpdateTickingEventArgs e)
    {
        if (!_config.EnableMod || !Context.IsWorldReady) return;
        if (!Context.IsPlayerFree || Game1.activeClickableMenu is not null) return;

        var location = Game1.currentLocation;
        if (ZoneHelper.GetActiveConfig(location, _config) == null) return;

        _savedSpeeds.Clear();

        var farmer = location.farmers.FirstOrDefault();
        foreach (var character in location.characters)
        {
            if (character is not Monster monster) continue;
            if (ZoneHelper.ShouldSkipMonster(monster, farmer)) continue;

            int id = monster.GetHashCode();
            _savedSpeeds[id] = monster.speed;

            // Monstruos con animación/estado interno sensible mantienen su ciclo nativo.
            if (!ZoneHelper.NeedsNativeAnimation(monster))
            {
                // Cancelamos el desplazamiento nativo en este frame.
                monster.speed = 0;
            }
        }
    }

    /// <summary>
    /// Se ejecuta DESPUÉS del tick del juego.
    /// Restauramos velocidades y aplicamos nuestro pathfinding.
    /// </summary>
    /// <param name="sender">Origen del evento.</param>
    /// <param name="e">Argumentos del evento <see cref="UpdateTickedEventArgs"/>.</param>
    private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
    {
        if (!_config.EnableMod || !Context.IsWorldReady) return;
        if (!Context.IsPlayerFree || Game1.activeClickableMenu is not null)
        {
            RestoreSavedSpeeds(Game1.currentLocation);
            return;
        }

        var location = Game1.currentLocation;
        var cfg = ZoneHelper.GetActiveConfig(location, _config);
        if (cfg == null) return;

        _pathfinderManager.CleanupOrphanStates(location);

        // Necesitamos al menos un jugador como objetivo
        var player = location.farmers?.FirstOrDefault();
        if (player == null) return;

        foreach (var character in location.characters)
        {
            if (character is not Monster monster) continue;

            int id = monster.GetHashCode();
            if (!_savedSpeeds.TryGetValue(id, out int realSpeed)) continue;

            // Reponemos velocidad original antes de aplicar movimiento personalizado.
            monster.speed = realSpeed;

            _pathfinderManager.TryMoveMonster(
                monster,
                monster.GetBoundingBox().Center.ToVector2(),
                player.GetBoundingBox().Center.ToVector2(),
                location,
                cfg);
        }

        _savedSpeeds.Clear();
    }

    /// <summary>Limpia el estado temporal al cambiar de localización.</summary>
    /// <param name="sender">Origen del evento.</param>
    /// <param name="e">Argumentos del evento <see cref="WarpedEventArgs"/>.</param>
    private void OnWarped(object? sender, WarpedEventArgs e)
    {
        RestoreSavedSpeeds(e.OldLocation);
        _pathfinderManager.ClearCache();
        _savedSpeeds.Clear();
    }

    /// <summary>Limpia el estado temporal al cargar una partida guardada.</summary>
    /// <param name="sender">Origen del evento.</param>
    /// <param name="e">Argumentos del evento <see cref="SaveLoadedEventArgs"/>.</param>
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        RestoreSavedSpeeds(Game1.currentLocation);
        _pathfinderManager.ClearCache();
        _savedSpeeds.Clear();
    }

    /// <summary>Dibuja el overlay de depuración de rutas A* para los monstruos procesados.</summary>
    /// <param name="sender">Origen del evento.</param>
    /// <param name="e">Argumentos del evento <see cref="RenderedWorldEventArgs"/>.</param>
    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!_config.EnableMod || !Context.IsWorldReady || !_config.ShowDebugPath) return;

        var location = Game1.currentLocation;
        var cfg = ZoneHelper.GetActiveConfig(location, _config);
        if (cfg == null) return;

        var player = location.farmers?.FirstOrDefault();
        if (player == null) return;

        foreach (var character in location.characters)
        {
            if (character is not Monster monster) continue;
            if (ZoneHelper.ShouldSkipMonster(monster, player)) continue;

            if (_pathfinderManager.TryGetDebugPathTiles(monster, out var astarTiles))
            {
                for (int i = 0; i < astarTiles.Count; i++)
                {
                    var tile = astarTiles[i];
                    Color fill = i == 0 ? Color.Orange * 0.28f : Color.Cyan * 0.22f;
                    Color border = i == 0 ? Color.OrangeRed : Color.Cyan;
                    DrawWorldTile(e, tile, fill, border);
                }
            }
        }
    }
    #endregion

    #region Utilidades
    /// <summary>
    /// Restaura las velocidades guardadas de monstruos en una localización.
    /// Se usa como medida de seguridad al pausar o cambiar de mapa.
    /// </summary>
    /// <param name="location">Localización cuyos monstruos deben recuperar su velocidad.</param>
    private void RestoreSavedSpeeds(GameLocation? location)
    {
        if (location == null || _savedSpeeds.Count == 0)
            return;

        foreach (var character in location.characters)
        {
            if (character is not Monster monster)
                continue;

            int id = monster.GetHashCode();
            if (_savedSpeeds.TryGetValue(id, out int realSpeed))
                monster.speed = realSpeed;
        }

        _savedSpeeds.Clear();
    }

    /// <summary>Dibuja una casilla del mundo convertida a coordenadas de pantalla.</summary>
    /// <param name="e">Argumentos de render del mundo.</param>
    /// <param name="tile">Coordenadas de tile en el mundo.</param>
    /// <param name="fill">Color de relleno.</param>
    /// <param name="border">Color de borde.</param>
    private static void DrawWorldTile(RenderedWorldEventArgs e, Point tile, Color fill, Color border)
    {
        var worldBounds = new Rectangle(tile.X * Game1.tileSize, tile.Y * Game1.tileSize, Game1.tileSize, Game1.tileSize);
        DrawWorldRectangle(e, worldBounds, fill, border);
    }

    /// <summary>Dibuja un rectángulo del mundo convertido a coordenadas de pantalla.</summary>
    /// <param name="e">Argumentos de render del mundo.</param>
    /// <param name="worldBounds">Rectángulo en coordenadas de mundo.</param>
    /// <param name="fill">Color de relleno.</param>
    /// <param name="border">Color de borde.</param>
    private static void DrawWorldRectangle(RenderedWorldEventArgs e, Rectangle worldBounds, Color fill, Color border)
    {
        Vector2 local = Game1.GlobalToLocal(Game1.viewport, new Vector2(worldBounds.Left, worldBounds.Top));
        var screenBounds = new Rectangle((int)local.X, (int)local.Y, worldBounds.Width, worldBounds.Height);

        e.SpriteBatch.Draw(Game1.staminaRect, screenBounds, fill);
        e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(screenBounds.Left, screenBounds.Top, screenBounds.Width, 2), border);
        e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(screenBounds.Left, screenBounds.Bottom - 2, screenBounds.Width, 2), border);
        e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(screenBounds.Left, screenBounds.Top, 2, screenBounds.Height), border);
        e.SpriteBatch.Draw(Game1.staminaRect, new Rectangle(screenBounds.Right - 2, screenBounds.Top, 2, screenBounds.Height), border);
    }
    #endregion
}
