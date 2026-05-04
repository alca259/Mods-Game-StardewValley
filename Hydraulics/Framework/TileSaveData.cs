namespace Hydraulics.Framework;

/// <summary>Información de una casilla que se guarda para mantener el estado de las tuberías entre sesiones de juego.</summary>
/// <param name="X">Coordenada X de la casilla.</param>
/// <param name="Y">Coordenada Y de la casilla.</param>
internal readonly record struct TileSaveData(int X, int Y);
