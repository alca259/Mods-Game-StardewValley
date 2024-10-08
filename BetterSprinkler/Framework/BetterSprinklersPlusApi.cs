﻿using BetterSprinklersPlus.Framework.Helpers;
using Microsoft.Xna.Framework;

namespace BetterSprinklersPlus.Framework;

/// <summary>The API which provides access to Better Sprinklers for other mods.</summary>
public class BetterSprinklersPlusApi : IBetterSprinklersApi
{
    /// <summary>Get the maximum sprinkler coverage supported by this mod (in tiles wide or high).</summary>
    public int GetMaxGridSize()
    {
        return BetterSprinklersPlusConfig.Active?.MaxGridSize ?? BetterSprinklersPlusConfig.ScarecrowGridSize;
    }

    /// <summary>Get the relative tile coverage by supported sprinkler ID.</summary>
    public IDictionary<int, Vector2[]> GetSprinklerCoverage()
    {
        // build tile grids
        IDictionary<int, Vector2[]> coverage = new Dictionary<int, Vector2[]>();
        foreach (KeyValuePair<int, int[,]> shape in BetterSprinklersPlusConfig.Active.SprinklerShapes)
            coverage[shape.Key] = GridHelper.GetCoveredTiles(Vector2.Zero, shape.Value).ToArray();
        return coverage;
    }
}
