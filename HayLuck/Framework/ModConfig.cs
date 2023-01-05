using StardewModdingAPI.Utilities;
using StardewModdingAPI;
using System;

namespace HayLuck.Framework
{
    internal sealed class ModConfig
    {
        /// <summary>Minimum level of hay spread on the farm</summary>
        public int MinIterations { get; set; } = 3;

        /// <summary>Maximum level of hay spread on the farm</summary>
        public int MaxIterations { get; set; } = 5;

        /// <summary>The keys which reload the mod config.</summary>
        public KeybindList ReloadKey { get; set; } = new(SButton.F5);

        public void EnsureArguments()
        {
            MinIterations = Math.Abs(MinIterations);
            MaxIterations = Math.Abs(MaxIterations);
            MaxIterations = MaxIterations < MinIterations ? MinIterations + 1 : MaxIterations;
        }
    }
}
