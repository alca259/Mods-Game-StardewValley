namespace Hydraulics.Framework;

/// <summary>Modos de energía para las bombas de agua</summary>
internal enum PumpPowerMode
{
    /// <summary>La bomba no tiene energía o no tiene agua, y no funcionará.</summary>
    None = 0,
    /// <summary>La bomba está alimentada por un panel solar.</summary>
    SolarPanel = 1,
    /// <summary>El mod está configurado para no requerir energía.</summary>
    DebugBypass = 2,
}
