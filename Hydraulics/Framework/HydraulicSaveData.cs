namespace Hydraulics.Framework;

/// <summary>Información guardada sobre las redes hidráulicas, que se serializa para mantener el estado entre sesiones de juego.</summary>
internal sealed class HydraulicSaveData
{
    /// <summary>Subredes hidráulicas presentes en el juego, cada una con sus propias tuberías y bombas.</summary>
    public List<HydraulicSubnetworkSaveData> Networks { get; set; } = new();
}
