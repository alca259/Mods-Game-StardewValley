using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace Hydraulics.Framework;

internal sealed class ModConfig
{
    public bool EnableMod { get; set; } = true;

    /// <summary>The keys which reload the mod config.</summary>
    public KeybindList ReloadKey { get; set; } = new(SButton.F5);

    /// <summary>Ensure all arguments are valid.</summary>
    public void EnsureArguments()
    {
    }
}
