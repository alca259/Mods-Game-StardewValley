using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace Hydraulics.Framework;

internal sealed class ModConfig
{
    public bool EnableMod { get; set; } = true;

    /// <summary>The keys which reload the mod config.</summary>
    public KeybindList ReloadKey { get; set; } = new(SButton.F5);

    /// <summary>The keys which toggle pipe edit mode.</summary>
    public KeybindList TogglePipeEditModeKey { get; set; } = new(SButton.F6);

    /// <summary>Whether pumps require energy (solar panel or lever) to move water.</summary>
    public bool RequireEnergyForPumps { get; set; } = true;

    /// <summary>Ensure all arguments are valid.</summary>
    public void EnsureArguments()
    {
    }
}
