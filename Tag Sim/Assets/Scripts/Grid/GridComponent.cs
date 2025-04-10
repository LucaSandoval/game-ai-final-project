using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Component that manages a gridMap as a physical grid within the world, with tiles occupying space.
/// Provides a series of useful functions for tranlating between world and grid coordinates, tile distance, etc.
/// </summary>
public class GridComponent : Singleton<GridComponent>
{
    [SerializeField] private float TileWorldSize;

    [SerializeField] private bool UseInspectorChildGrid;
    [SerializeField] private GameObject GridParent;

    [SerializeField] private bool DebugDisplayTileValues;
    [SerializeField] private MapController MapController;

    public Tilemap tilemap;
    private GridMap mainGrid;
    private Vector2 gridTopLeft, gridBottomRight;
    private int Width, Height;

    private GridMap debugGrid;
    public PathfindingComponent test;

    private void Start()
    {
        if (test != null)
        {
            debugGrid = test.Dijkstra(test.gameObject.transform.position).Item1;
        }
        else
        {
            Debug.LogWarning("GridComponent: 'test' PathfindingComponent is not assigned.");
        }
    }

    protected override void Awake()
    {
        base.Awake();

        tilemap = MapController.GetCurrentMap();
        if (tilemap != null)
        {
            BoundsInt bounds = tilemap.cellBounds;
            Width = bounds.size.x;
            Height = bounds.size.y;

            GridTile[,] newGridTiles = new GridTile[Width, Height];

            for (int y = bounds.yMax - 1; y > bounds.yMin - 1; y--)
            {
                for (int x = bounds.xMin; x < bounds.xMax; x++)
                {
                    Vector3Int tilePosition = new Vector3Int(x, y, 0);
                    TileBase tile = tilemap.GetTile(tilePosition);

                    if (tile != null)
                    {
                        GridTile newTile = new GridTile();
                        newTile.WorldPosition = tilemap.CellToWorld(tilePosition) + new Vector3(TileWorldSize / 2, TileWorldSize / 2, 0);
                        newTile.GridCoordinate = new Vector2Int(x - bounds.xMin, y - bounds.yMin);
                        newTile.Traversable = !tilemap.GetTile(tilePosition).name.Contains("Wall");
                        newTile.PlayerTraversable = !tilemap.GetTile(tilePosition).name.Contains("Player");
                        newTile.EnemyTraversable = !tilemap.GetTile(tilePosition).name.Contains("Enemy");

                        newGridTiles[x - bounds.xMin, y - bounds.yMin] = newTile;
                    }
                    else
                    {
                        newGridTiles[x - bounds.xMin, y - bounds.yMin] = null;
                    }
                }
            }

            float halfTileWidth = TileWorldSize / 2;
            gridTopLeft = new Vector2(newGridTiles[0, Height - 1].WorldPosition.x - halfTileWidth,
                newGridTiles[0, Height - 1].WorldPosition.y + halfTileWidth);
            gridBottomRight = new Vector2(newGridTiles[Width - 1, 0].WorldPosition.x + halfTileWidth,
                newGridTiles[Width - 1, 0].WorldPosition.y - halfTileWidth);

            mainGrid = new GridMap(Width, Height, newGridTiles);

            // Calculate gridTopLeft and gridBottomRight safely
            //I was getting null reference exceptions here, so I added this check
            GridTile topLeftTile = newGridTiles[0, Height - 1];
            GridTile bottomRightTile = newGridTiles[Width - 1, 0];

            if (topLeftTile != null && bottomRightTile != null)
            {
                gridTopLeft = new Vector2(topLeftTile.WorldPosition.x - halfTileWidth,
                                          topLeftTile.WorldPosition.y + halfTileWidth);
                gridBottomRight = new Vector2(bottomRightTile.WorldPosition.x + halfTileWidth,
                                              bottomRightTile.WorldPosition.y - halfTileWidth);
            }
            else
            {
                Debug.LogWarning(" GridComponent: Could not calculate grid bounds. Top-left or bottom-right tile is null.");
                gridTopLeft = Vector2.zero;
                gridBottomRight = Vector2.zero;
            }
        }
        else if (UseInspectorChildGrid && GridParent)
        {
            GridTile[,] newGridTiles = new GridTile[Width, Height];
            int childId = 0;
            for (int h = 0; h < Height; h++)
            {
                for (int w = 0; w < Width; w++)
                {
                    GridTile newTile = new GridTile();
                    newTile.WorldPosition = GridParent.transform.GetChild(childId).position;
                    newTile.GridCoordinate = new Vector2Int(w, h);
                    newTile.Traversable = true;

                    if (GridParent.transform.GetChild(childId).CompareTag("Wall"))
                    {
                        newTile.Traversable = false;
                    }

                    newGridTiles[w, h] = newTile;
                    childId++;
                }
            }

            float halfTileWidth = TileWorldSize / 2;
            gridTopLeft = new Vector2(newGridTiles[0, 0].WorldPosition.x - halfTileWidth,
                newGridTiles[0, 0].WorldPosition.y + halfTileWidth);
            gridBottomRight = new Vector2(newGridTiles[Width - 1, Height - 1].WorldPosition.x + halfTileWidth,
                newGridTiles[Width - 1, Height - 1].WorldPosition.y - halfTileWidth);

            mainGrid = new GridMap(Width, Height, newGridTiles);
        }
    }

    /// <summary>
    /// Returns a tuple containing the width and height of the grid.
    /// </summary>
    public (int, int) GetGridDimensions()
    {
        return (Width, Height);
    }

    /// <summary>
    /// Gets a reference to a tile in the grid based on its grid position.
    /// </summary>
    public GridTile GetTile(int x, int y)
    {
        return mainGrid.GetTile(x, y);
    }

    /// <summary>
    /// Gets a reference to the underlying GridMap.
    /// </summary>
    public GridMap GetGridData()
    {
        return mainGrid;
    }

    /// <summary>
    /// Returns the tile located at/containing the given world position. Returns null if the 
    /// given position is outside the bounds of the grid.
    /// </summary>
    /// <param name="worldPosition">Position in world coordinates.</param>
    public GridTile GetGridTileAtWorldPosition(Vector2 worldPosition)
    {
        if (tilemap == null || mainGrid == null) return null;

        // Convert world position to tilemap cell position
        Vector3Int cellPosition = tilemap.WorldToCell(worldPosition);

        // Convert cell position to grid indices
        int x = cellPosition.x - tilemap.cellBounds.xMin;
        int y = cellPosition.y - tilemap.cellBounds.yMin;

        // Ensure indices are within bounds
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            return null;
        }

        return mainGrid.GetTile(x, y);
    }


    /// <summary>
    /// Returns the world space distance between two tiles on the grid.
    /// </summary>
    public float DistanceBetweenTiles(GridTile tile1, GridTile tile2)
    {
        if (tile1 == null || tile2 == null) return -1;
        return Vector2.Distance(tile1.WorldPosition, tile2.WorldPosition);
    }

    private void OnGUI()
    {
        if (DebugDisplayTileValues && mainGrid != null)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 30;
            style.normal.textColor = Color.black;
            style.alignment = TextAnchor.MiddleCenter;

            float labelSize = 40f;

            for (int h = 0; h < Height; h++)
            {
                for (int w = 0; w < Width; w++)
                {
                    GridTile tile = mainGrid.GetTile(w, h);
                    Vector2 screenPos = Camera.main.WorldToScreenPoint(tile.WorldPosition);
                    float flippedY = Screen.height - screenPos.y;

                    string label = tile.Value.ToString("F1");

                    if (!string.IsNullOrEmpty(label))
                    {
                        Rect labelRect = new Rect(screenPos.x - labelSize / 2f, flippedY - labelSize / 2f, labelSize, labelSize);
                        GUI.Label(labelRect, label, style);
                    }
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && mainGrid != null)
        {
            for (int h = 0; h < Height; h++)
            {
                for (int w = 0; w < Width; w++)
                {
                    GridTile tile = mainGrid.GetTile(w, h);
                    Vector2 pos = tile.WorldPosition;

                    if (tile.test)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawCube(pos, Vector3.one * 0.4f);
                    }
                    else
                    {
                        Gizmos.color = Color.Lerp(Color.green, Color.red, tile.Value);
                        Gizmos.DrawCube(pos, Vector3.one * 0.4f);
                    }
                }
            }
        }
    }

    public Tilemap GetTilemap()
    {
        return tilemap;
    }

    public GridMap GetGridMap()
    {
        return mainGrid;
    }
}
