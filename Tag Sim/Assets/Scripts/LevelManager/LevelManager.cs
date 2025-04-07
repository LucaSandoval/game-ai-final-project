using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelManager : MonoBehaviour
{
    // Singleton
    private static LevelManager _instance;

    public static LevelManager Instance;

    private List<GameObject> enemies;
    private GameObject player;
    private bool isGameOver = false;
    //private bool isGamePaused = false;
    private GridComponent grid;
    private Tilemap tilemap;
    private GridMap gridMap;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        enemies = new List<GameObject>(GameObject.FindGameObjectsWithTag("Enemy"));
        player = GameObject.FindGameObjectWithTag("Player");
        grid = GridComponent.Instance;
        tilemap = grid.GetTilemap();
        gridMap = grid.GetGridMap();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isGameOver)
        {
            CheckForCollision();
        }

    }

    private void CheckForCollision()
    {
        if (player == null) return;

        GridTile playerTile = grid.GetGridTileAtWorldPosition(player.transform.position);

        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;

            GridTile enemyTile = grid.GetGridTileAtWorldPosition(enemy.transform.position);

            // if player and enemy are found on same tile
            if (playerTile == enemyTile)
            {
                isGameOver = true;
                Debug.Log("Game Over! The player was caught.");
                // functions for when game ends go here bro
                break;
            }
        }
    }

    private void SetEnemyOccupancy() 
    {
        foreach (GameObject enemy in enemies)
        {
            if (enemy == null) continue;
            GridTile enemyTile = grid.GetGridTileAtWorldPosition(enemy.transform.position);
            enemyTile.Occupied = true;
        }
    }

    private void ResetOccupancy() 
    {
        foreach (GridTile tile in gridMap.GetTiles())
        {
            tile.Occupied = false;
        }
    }
}
