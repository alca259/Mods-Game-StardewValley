namespace Hydraulics.Framework;

internal sealed class HydraulicSubnetworkSaveData
{
    public Guid Id { get; set; }

    public List<TileSaveData> Pipes { get; set; } = new();

    public List<PumpSaveData> Pumps { get; set; } = new();
}
