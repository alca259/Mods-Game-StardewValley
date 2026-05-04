using Microsoft.Xna.Framework;

namespace Hydraulics.Framework;

/// <summary>Representa el estado actual de una subred hidráulica</summary>
/// <param name="Id">Identificador único de la subred hidráulica</param>
/// <param name="LabelPumpTile">La posición de la bomba que se usará para mostrar la etiqueta de la subred</param>
/// <param name="MaxFlow">El caudal máximo que puede transportar la subred, determinado por las bombas conectadas</param>
/// <param name="ConsumptionFlow">El caudal actual que consume la subred, determinado por las tuberías conectadas a casillas regables</param>
internal readonly record struct HydraulicSubnetworkStatus(
    Guid Id,
    Vector2 LabelPumpTile,
    float MaxFlow,
    float ConsumptionFlow);
