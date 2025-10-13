// Runtime/LegacySoModel/CharacterAnchor.cs
/// <summary>
/// Posición del personaje durante el diálogo (usado por el editor y el runtime).
/// </summary>
public enum CharacterAnchor
{
    Left,      // Personaje a la izquierda de la pantalla
    Center,    // Personaje centrado
    Right,     // Personaje a la derecha
    Custom     // Posición personalizada mediante coordenadas (Vector2)
}
