using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Component that manages a gridMap as a physical grid within the world, with tiles occupying space.
/// Provides a series of useful functions for tranlating between world and grid coordinates, tile distance etc...
/// </summary>
public class GridComponent : Singleton<GridComponent>
{
    [SerializeField] private float TileWorldSize;

    [SerializeField] private bool UseInspectorChildGrid;
    [SerializeField] private GameObject GridParent;

    [SerializeField] private bool DebugDisplayTileValues;
    [SerializeField] private Tilemap tilemap;

    private GridMap mainGrid;
    private Vector2 gridTopLeft, gridBottomRight;
    private int Width, Height;

    private GridMap debugGrid;
    public PathfindingComponent test;

    private void Start()
    {
        debugGrid = test.Dijkstra(test.gameObject.transform.position).Item1;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            debugGrid = test.Dijkstra(mousePos).Item1;
        }
    }

    protected override void Awake()
    {
        base.Awake();

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

                        newGridTiles[x - bounds.xMin, y - bounds.yMin] = newTile;

                        if (tile.name == "Water")
                        {
                            // Debug.Log($"Tile at ({x - bounds.xMin}, {y - bounds.yMin}) is Water. Setting value to 2.");
                            newTile.Value = 2; 
                        }
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
        if (DebugDisplayTileValues && debugGrid != null)
        {
            GUIStyle style = new GUIStyle();
            style.fontSize = 20;  // Increase font size
            style.normal.textColor = Color.black; // Set text color to black

            for (int h = 0; h < Height; h++)
            {
                for (int w = 0; w < Width; w++)
                {
                    if (debugGrid.GetTile(w, h).Value == float.MaxValue) continue;
                    Vector2 screenPos = Camera.main.WorldToScreenPoint(mainGrid.GetTile(w, h).WorldPosition);
                    float flippedY = Screen.height - screenPos.y;
                    GUI.Label(new Rect(screenPos.x - 20, flippedY + 10, 500, 500), debugGrid.GetTile(w, h).Value.ToString("F1"), style);
                }
            }
        }
    }
}
