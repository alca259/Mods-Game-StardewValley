namespace Hydraulics.Framework;

/// <summary>Información guardada sobre una subred hidráulica, que se serializa para mantener el estado entre sesiones de juego.</summary>
internal sealed class HydraulicSubnetworkSaveData
{
    /// <summary>Identificador único de la subred hidráulica.</summary>
    public Guid Id { get; set; }
    
    /// <summary>Lista de tuberías pertenecientes a la subred.</summary>
    public List<TileSaveData> Pipes { get; set; } = new();

    /// <summary>Lista de bombas pertenecientes a la subred.</summary>
    public List<PumpSaveData> Pumps { get; set; } = new();
}
