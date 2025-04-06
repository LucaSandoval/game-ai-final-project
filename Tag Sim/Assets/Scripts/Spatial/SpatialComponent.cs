using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.VisualScripting;
using UnityEngine.SocialPlatforms.Impl;

/// <summary>
/// Component responsible for helping our agents make decisions about where to move in the world.
/// </summary>
public class SpatialComponent : MonoBehaviour
{
    private MovementComponent MovementComponent;
    private PathfindingComponent PathfindingComponent;
    private OccupancyMapController OccupancyMapController;

    private GridTile BestCell;

    [Header("Spatial Function Settings")]
    [SerializeField] private SpatialFunction SpatialFunction;


    [Header("Spatial Component Settings")]
    [SerializeField] private bool PathfindToPositionToggle;
    [SerializeField] private bool DebugToggle;


    private void Start()
    {
        MovementComponent = GetComponent<MovementComponent>();
        if (MovementComponent == null)
        {
            Debug.LogError("SpatialComponent: MovementComponent is not assigned.");
        }

        PathfindingComponent = GetComponent<PathfindingComponent>();
        if (PathfindingComponent == null)
        {
            Debug.LogError("SpatialComponent: PathfindingComponent is not assigned.");
        }

        OccupancyMapController = OccupancyMapController.Instance;
        if (OccupancyMapController == null)
        {
            Debug.LogError("SpatialComponent: OccupancyMapController is not assigned.");
        }

        if (SpatialFunction == null)
        {
            Debug.LogError("SpatialComponent: SpatialFunction is not assigned.");
        }
    }

    private void Update()
    {
        ChoosePosition(PathfindToPositionToggle, DebugToggle);
    }

    public bool ChoosePosition(bool PathfindToPosition, bool Debug)
    {
        bool Result = false;

        GridTile LastCell = BestCell;
        BestCell = null;

        GridComponent grid = GridComponent.Instance;

        if (grid == null || PathfindingComponent == null || SpatialFunction == null)
        {
            return false;
        }

        // Get the starting location
        Vector2 StartLoc = grid.GetGridTileAtWorldPosition(transform.position).WorldPosition;
        GridMap GridMap = new GridMap(grid.GetGridDimensions().Item1, grid.GetGridDimensions().Item2, 0.0f);

        // Step 1 - Run Dijkstra's to determine which cells we should be evaluating 
        GridMap DistanceMap = PathfindingComponent.Dijkstra(StartLoc).Item1;

        // Give the last cell a bonus
        GridMap.SetGridValue(LastCell.GridCoordinate.x, LastCell.GridCoordinate.y, SpatialFunction.LastCellBonus);

        // Step 2 - For each layer in our spatial function, evaluate and accumulate the layer in GridMap
        foreach (SpatialLayer Layer in SpatialFunction.Layers)
        {
            EvaluateLayer(Layer, DistanceMap, GridMap);
        }

        // Step 3 - Pick the best cell in GridMap
        float BestScore = float.MinValue;
        (int, int) gridSize = GridMap.GetGridSize();
        for (int y = 0; y < gridSize.Item2; y++)
        {
            for (int x = 0; x < gridSize.Item1; x++)
            {
                float CurrentDistance = DistanceMap.GetGridValue(x, y);

                if (CurrentDistance < float.MaxValue)
                {
                    float CurrentScore = GridMap.GetGridValue(x, y);
                    if (CurrentScore > BestScore)
                    {
                        BestScore = CurrentScore;
                        BestCell = GridMap.GetTile(x, y);
                        Result = true;
                    }
                }
            }
        }

        // Step 4 - If we are pathfinding, set the movement path to the best cell
        if (PathfindToPosition)
        {
            if (BestCell != null)
            {
                PathfindingComponent.SetDestination(BestCell.WorldPosition);
            }
        }

        return Result;
    }

    public void EvaluateLayer(SpatialLayer Layer, GridMap DistanceMap, GridMap GridMap)
    {
        PerceptionComponent PerceptionComponent = GetComponent<PerceptionComponent>();
        if (PerceptionComponent == null)
        {
            Debug.LogError("SpatialComponent: PerceptionComponent is not assigned.");
            return;
        }

        GridTile TargetPosition = OccupancyMapController.GetCurrentTargetState();

        (int, int) gridSize = GridMap.GetGridSize();
        for (int y = 0; y < gridSize.Item2; y++)
        {
            for (int x = 0; x < gridSize.Item1; x++)
            {
                float CellDistance = DistanceMap.GetGridValue(x, y);
                if (CellDistance < float.MaxValue)
                {
                    float Value = 0.0f;

                    switch (Layer.InputType)
                    {
                        case SpatialInput.TargetRange:
                            Value = Vector2.Distance(TargetPosition.WorldPosition, GridMap.GetTile(x, y).WorldPosition);
                            break;
                        case SpatialInput.PathDistance:
                            Value = CellDistance;
                            break;
                        case SpatialInput.LoS:
                            {
                                Value = PerceptionComponent.HasLOS(GridMap.GetTile(x, y)) ? 1.0f : 0.0f;
                                break;
                            }
                        case SpatialInput.None:
                            break;
                    }

                    // Apply the response curve to the value
                    float ModifiedValue = Layer.ResponseCurve.Evaluate(Value);
                    float CurrentValue = 0.0f;
                    float ResultValue = 0.0f;

                    switch (Layer.Operation)
                    {
                        case SpatialOp.Add:
                            ResultValue = CurrentValue + ModifiedValue;
                            break;
                        case SpatialOp.Multiply:
                            ResultValue = CurrentValue * ModifiedValue;
                            break;
                        case SpatialOp.None:
                            ResultValue = CurrentValue;
                            break;
                    }

                    GridMap.SetGridValue(x, y, ResultValue);
                }
            }
        }
    }
}
