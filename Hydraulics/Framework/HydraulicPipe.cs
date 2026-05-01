using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace Hydraulics.Framework;

internal sealed class HydraulicPipe
{
    private const int CellPixels = 16;
    private const int CorePixels = 4;

    public Vector2 Tile { get; }

    public bool HasWater { get; set; }

    public byte ConnectionMask { get; private set; }

    public HydraulicPipe(Vector2 tile)
    {
        Tile = tile;
    }

    public void RefreshConnections(IReadOnlyDictionary<Vector2, HydraulicPipe> allPipes)
    {
        ArgumentNullException.ThrowIfNull(allPipes);

        byte mask = 0;
        if (allPipes.ContainsKey(new Vector2(Tile.X, Tile.Y - 1))) mask |= 1; // N
        if (allPipes.ContainsKey(new Vector2(Tile.X, Tile.Y + 1))) mask |= 2; // S
        if (allPipes.ContainsKey(new Vector2(Tile.X + 1, Tile.Y))) mask |= 4; // E
        if (allPipes.ContainsKey(new Vector2(Tile.X - 1, Tile.Y))) mask |= 8; // W

        ConnectionMask = mask;
    }

    public void Draw(SpriteBatch spriteBatch, IReadOnlyList<WaterPumpMachine> pumps, Color unpoweredColor, Color poweredColor)
    {
        ArgumentNullException.ThrowIfNull(spriteBatch);
        ArgumentNullException.ThrowIfNull(pumps);

        int centerStart = (CellPixels - CorePixels) / 2;
        int centerEnd = centerStart + CorePixels;
        int zoom = Game1.pixelZoom;
        Color color = HasWater ? poweredColor : unpoweredColor;

        byte renderMask = ConnectionMask;
        if (pumps.Any(p => p.Tile == new Vector2(Tile.X, Tile.Y - 1))) renderMask |= 1;
        if (pumps.Any(p => p.Tile == new Vector2(Tile.X, Tile.Y + 1))) renderMask |= 2;
        if (pumps.Any(p => p.Tile == new Vector2(Tile.X + 1, Tile.Y))) renderMask |= 4;
        if (pumps.Any(p => p.Tile == new Vector2(Tile.X - 1, Tile.Y))) renderMask |= 8;

        Vector2 screen = Game1.GlobalToLocal(Game1.viewport, Tile * Game1.tileSize);
        float layerDepth = ((Tile.Y + 1f) * Game1.tileSize) / 10000f;

        DrawSegment(spriteBatch, screen, centerStart, centerStart, CorePixels, CorePixels, zoom, color, layerDepth);

        if ((renderMask & 1) != 0)
            DrawSegment(spriteBatch, screen, centerStart, 0, CorePixels, centerStart, zoom, color, layerDepth);

        if ((renderMask & 2) != 0)
            DrawSegment(spriteBatch, screen, centerStart, centerEnd, CorePixels, CellPixels - centerEnd, zoom, color, layerDepth);

        if ((renderMask & 4) != 0)
            DrawSegment(spriteBatch, screen, centerEnd, centerStart, CellPixels - centerEnd, CorePixels, zoom, color, layerDepth);

        if ((renderMask & 8) != 0)
            DrawSegment(spriteBatch, screen, 0, centerStart, centerStart, CorePixels, zoom, color, layerDepth);
    }

    private static void DrawSegment(
        SpriteBatch spriteBatch,
        Vector2 screen,
        int x,
        int y,
        int width,
        int height,
        int zoom,
        Color color,
        float layerDepth)
    {
        if (width <= 0 || height <= 0)
            return;

        spriteBatch.Draw(
            Game1.staminaRect,
            new Rectangle(
                (int)screen.X + (x * zoom),
                (int)screen.Y + (y * zoom),
                width * zoom,
                height * zoom),
            null,
            color,
            0f,
            Vector2.Zero,
            SpriteEffects.None,
            layerDepth);
    }
}
