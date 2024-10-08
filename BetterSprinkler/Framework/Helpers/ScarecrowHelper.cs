using Microsoft.Xna.Framework;

namespace BetterSprinklersPlus.Framework.Helpers;

public static class ScarecrowHelper
{
    public static bool IsScarecrow(this StardewValley.Object obj)
    {
        return obj.bigCraftable.Value && obj.Name.Contains("arecrow");
    }

    public static int[,] GetScarecrowGrid()
    {
        const int maxGridSize = BetterSprinklersPlusConfig.ScarecrowGridSize;
        var grid = new int[maxGridSize, maxGridSize];
        const int scarecrowCenterValue = maxGridSize / 2;
        var scarecrowCenter = new Vector2(scarecrowCenterValue, scarecrowCenterValue);
        for (var x = 0; x < maxGridSize; x++)
        {
            for (var y = 0; y < maxGridSize; y++)
            {
                var vector = new Vector2(x, y);
                grid[x, y] = Vector2.Distance(vector, scarecrowCenter) < 9f ? 1 : 0;
            }
        }

        return grid;
    }
}
