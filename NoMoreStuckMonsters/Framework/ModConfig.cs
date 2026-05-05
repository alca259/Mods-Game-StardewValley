using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace NoMoreStuckMonsters.Framework;

/// <summary>Configuración del mod</summary>
internal sealed class ModConfig
{
    /// <summary>Indica si el mod está habilitado</summary>
    public bool EnableMod { get; set; } = true;

    /// <summary>La tecla para recargar la configuración del mod sin reiniciar el juego.</summary>
    public KeybindList ReloadKey { get; set; } = new(SButton.F5);

    /// <summary>Valida y normaliza todos los argumentos de configuración.</summary>
    public void EnsureArguments()
    {
    }

}
