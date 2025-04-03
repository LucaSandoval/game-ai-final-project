using UnityEngine;

public class DiffusionBehaviour : MonoBehaviour
{
    [SerializeField] private float forgetPlayerDelay = 2f;
    private float lastSeenPlayerTime = -Mathf.Infinity;
    private bool isChasingPlayer = false;

    private PathfindingComponent pathfinding;
    private PerceptionComponent perception;
    private GridComponent grid;
    private OccupancyMap occupancyMap;

    private void Start()
    {
        pathfinding = GetComponent<PathfindingComponent>();
        perception = GetComponent<PerceptionComponent>();
        grid = GridComponent.Instance;
        occupancyMap = new OccupancyMap(grid.GetGridData());
    }

    private void Update()
    {
        UpdateOccupancyMap();
        UpdateBehavior();
    }

    private void UpdateOccupancyMap()
    {
        if (perception.GetSeenPlayer() != null)
        {
            lastSeenPlayerTime = Time.time;

            // Clear entire map except for player's position
            GridTile playerTile = grid.GetGridTileAtWorldPosition(perception.GetSeenPlayer().position);
            if (playerTile != null)
                occupancyMap.ClearAndSetSingle(playerTile);
        }
        else
        {
            // Clear visible tiles (we know the player isn't in them)
            occupancyMap.ClearVisible(perception.GetVisibleTiles());

            // Then diffuse probabilities to expand search
            occupancyMap.Diffuse();
        }
    }

    private void UpdateBehavior()
    {
        Transform player = perception.GetSeenPlayer();

        if (player != null)
        {
            isChasingPlayer = true;
            pathfinding.SetDestination(player.position);
        }
        else if (Time.time - lastSeenPlayerTime > forgetPlayerDelay)
        {
            isChasingPlayer = false;
            // Get the highest probability tile and move toward it
            GridTile bestGuess = occupancyMap.GetMostLikelyTile();
            if (bestGuess != null)
                pathfinding.SetDestination(bestGuess.WorldPosition);
        }
    }
}
