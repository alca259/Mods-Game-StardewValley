namespace Hydraulics.Framework;

/// <summary>Datos de guardado de una bomba hidráulica</summary>
/// <param name="X">La coordenada X de la bomba en el mapa</param>
/// <param name="Y">La coordenada Y de la bomba en el mapa</param>
/// <param name="Tier">El nivel de la bomba</param>
internal readonly record struct PumpSaveData(int X, int Y, WaterPumpTier Tier);
