using Microsoft.Xna.Framework;

namespace Hydraulics.Framework;

internal readonly record struct HydraulicSubnetworkStatus(
    Guid Id,
    Vector2 LabelPumpTile,
    float MaxFlow,
    float ConsumptionFlow);
