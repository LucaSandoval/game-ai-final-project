using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    // Singleton
    private static LevelManager _instance;

    public static LevelManager Instance;

    private List<GameObject> enemies;
    private GameObject player;
    private bool isGameOver = false;
    //private bool isGamePaused = false;

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

        GridComponent grid = GridComponent.Instance;
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
}
