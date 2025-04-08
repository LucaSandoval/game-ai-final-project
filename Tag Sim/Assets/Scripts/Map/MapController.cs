using UnityEngine;
using UnityEngine.Tilemaps;

public class MapController : MonoBehaviour
{
    public static int LevelID;

    [Header("Map Parent")]
    [SerializeField] private GameObject MapParent;
    [Header("Enemy Parent")]
    [SerializeField] private GameObject EnemyParent;
    [Header("Player Starting Positions")]
    [SerializeField] private Vector2[] StartPositions;

    private GameObject player;

    private void Awake()
    {
        LevelID = 4;
        // Enable the correct level and enemy set / disable incorrect ones
        EnableChildForLevelID(MapParent);
        EnableChildForLevelID(EnemyParent);

        // Position player
        player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            int targetId = (LevelID >= StartPositions.Length) ? 0 : LevelID;
            Debug.Log("Using Start Position: " + targetId + " with position: " + StartPositions[targetId].x + ", " + StartPositions[targetId].y);
            player.transform.position = StartPositions[targetId];
        }
    }

    private void EnableChildForLevelID(GameObject parent)
    {
        if (!parent) return;

        int targetId = (LevelID >= parent.transform.childCount) ? 0 : LevelID;

        for (int i = 0; i < parent.transform.childCount; i++)
        {
            parent.transform.GetChild(i).gameObject.SetActive(i == targetId);
        }
    }

    public Tilemap GetCurrentMap()
    {
        if (!MapParent) return null;

        int targetId = (LevelID >= MapParent.transform.childCount) ? 0 : LevelID;
        return MapParent.transform.GetChild(targetId).GetComponent<Tilemap>();
    }
}
