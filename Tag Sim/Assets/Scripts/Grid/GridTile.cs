using UnityEngine;

/// <summary>
/// Represents a Grid tile, and all potential data associated with it.
/// </summary>
public class GridTile
{
    public float Value;
    public bool Traversable;
    public Vector2 WorldPosition;
    public Vector2Int GridCoordinate;
}
