using UnityEngine;

/// <summary>
/// Represents a Grid tile, and all potential data associated with it.
/// </summary>
public class GridTile
{
    public float Value;
    public bool Traversable;
    public bool PlayerTraversable;
    public bool EnemyTraversable;
    public Vector2 WorldPosition;
    public Vector2Int GridCoordinate;
    public bool Occupied;
    public bool Visible;
    public float LastSeenTime = -Mathf.Infinity; // Make it so AI start with having no idea where the player could be hiding
    public bool IsTargetGuess = false;
}
