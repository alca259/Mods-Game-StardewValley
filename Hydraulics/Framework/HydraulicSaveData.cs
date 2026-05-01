namespace Hydraulics.Framework;

internal sealed class HydraulicSaveData
{
    public List<TileSaveData> Pipes { get; set; } = new();

    public List<PumpSaveData> Pumps { get; set; } = new();
}
