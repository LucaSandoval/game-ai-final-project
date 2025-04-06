using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Component responsible for helping our agents make decisions about where to move in the world.
/// </summary>
public class SpatialComponent : MonoBehaviour
{
    private MovementComponent movementComponent;
    private PathfindingComponent pathfindingComponent;
    private GridTile bestCell;

    [Header("Spatial Function Settings")]
    [SerializeField] private SpatialFunction SpatialFunction;


    [Header("Spatial Component Settings")]
    [SerializeField] private bool PathfindToPositionToggle;
    [SerializeField] private bool DebugToggle;


    private void Start()
    {
        movementComponent = GetComponent<MovementComponent>();
        if (movementComponent == null)
        {
            Debug.LogError("SpatialComponent: MovementComponent is not assigned.");
        }

        pathfindingComponent = GetComponent<PathfindingComponent>();
        if (pathfindingComponent == null)
        {
            Debug.LogError("SpatialComponent: PathfindingComponent is not assigned.");
        }
    }

    private void Update()
    {
        ChoosePosition(PathfindToPositionToggle, DebugToggle);
    }

    public bool ChoosePosition(bool PathfindToPosition, bool Debug) {
        return false;
    }

    public void EvaluateLayer(SpatialLayer Layer, GridMap DistanceMap, GridMap GridMap) {

    }
}
