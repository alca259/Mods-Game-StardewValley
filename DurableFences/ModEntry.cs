using Alca259.Common;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace DurableFences;

/// <summary>The main entry point.</summary>
public class ModEntry : Mod
{
    /*********
    ** Public methods
    *********/
    /// <summary>The mod entry point, called after the mod is first loaded.</summary>
    /// <param name="helper">Provides simplified APIs for writing mods.</param>
    public override void Entry(IModHelper helper)
    {
        CommonHelper.RemoveObsoleteFiles(this, "DurableFences.pdb");
        helper.Events.GameLoop.DayStarted += OnDayStarted;
    }

    private void OnDayStarted(object sender, DayStartedEventArgs e)
    {
        if (!Context.IsWorldReady) // ignore if not in-game
            return;

        if (!Game1.IsMasterGame) // only run on host
            return;

        foreach (GameLocation location in Game1.locations)
        {
            foreach (Object obj in location.Objects.Values)
            {
                if (obj is Fence fence)
                    fence.health.Value = fence.maxHealth.Value;
            }
        }
    }
}
