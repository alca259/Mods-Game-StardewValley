namespace Hydraulics.Framework;

/// <summary>Los niveles de bomba de agua disponibles, que determinan la cantidad de agua que pueden extraer por segundo.</summary>
internal enum WaterPumpTier
{
    /// <summary>Nivel de bomba de agua más básico, que no requiere paneles solares ni pila para funcionar.</summary>
    /// <remarks>Está balanceado para usarse en el early game (1-2 estación), con un caudal de agua bajo.</remarks>
    Bronze = 0,
    /// <summary>Nivel de bomba de agua intermedio, que requiere pila de energía para funcionar, pero no paneles solares.</summary>
    /// <remarks>Está balanceado para usarse en el mid game (3-4 estación), con un caudal de agua moderado.</remarks>
    Steel = 1,
    /// <summary>Nivel de bomba de agua avanzado, que requiere tanto pila de energía como un panel solar para funcionar.</summary>
    /// <remarks>Está balanceado para usarse en el late game (año 2), con un caudal de agua alto.</remarks>
    Gold = 2,
    /// <summary>Nivel de bomba de agua definitivo, que requiere tanto pila de energía como dos paneles solares para funcionar.</summary>
    /// <remarks>Está balanceado para usarse en el end game (año 2-3+), con un caudal de agua muy alto.</remarks>
    Iridium = 3,
}
