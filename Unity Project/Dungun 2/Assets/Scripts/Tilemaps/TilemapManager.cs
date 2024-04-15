using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapManager : MonoBehaviour
{
    [Header("Save Information")]
    public int roomIndex;
    public RoomType roomType;
    public string roomName = "";
    public IntGameobjectPair[] enemyPrefabs;

    [Header("Tiles and Tilemaps")]
    public Tilemap groundMap;
    public Tilemap wallMap, decorMap, metaMap;

    [Header("Load Information")]
    public Vector2Int position;
    public ScriptableRoom room;

#if UNITY_EDITOR
    public void SaveRoom()
    {
        if(roomIndex == PlayerPrefs.GetInt("RoomIndex")) 
        {
            roomIndex = PlayerPrefs.GetInt("RoomIndex") + 1;
        }
        PlayerPrefs.SetInt("RoomIndex", roomIndex);

        Resources.LoadAll<ScriptableRoom>("");

        //Sig: Creates a new scriptable object for the room to be saved in
        ScriptableRoom newRoom = ScriptableObject.CreateInstance<ScriptableRoom>();

        Vector2Int upperRight = new Vector2Int(), lowerLeft = new Vector2Int();

        //Sig: Fills that object with information
        newRoom.name = ((roomName == "") ? $"New Room {roomIndex}" : roomName);
        newRoom.ground = TilemapUtility.GetTilesFromMap(groundMap).ToArray();
        newRoom.walls = TilemapUtility.GetTilesFromMap(wallMap).ToArray();
        newRoom.decorations = TilemapUtility.GetTilesFromMap(decorMap).ToArray();
        newRoom.meta = TilemapUtility.GetTilesFromMap(metaMap).ToArray();
        newRoom.type = roomType;
        newRoom.enemies = enemyPrefabs;

        //Sig: Finds the offset of the room, and its size
        upperRight = (Vector2Int)newRoom.ground[0].position;
        lowerLeft = (Vector2Int)newRoom.ground[0].position;
        foreach (SavedTile tile in newRoom.ground)
        {
            upperRight.x = Math.Max(upperRight.x, tile.position.x);
            upperRight.y = Math.Max(upperRight.y, tile.position.y);

            lowerLeft.x = Math.Min(lowerLeft.x, tile.position.x);
            lowerLeft.y = Math.Min(lowerLeft.y, tile.position.y);
        }
        newRoom.size = upperRight - lowerLeft;

        //Debug.Log(upperRight);

        newRoom.ground = ApplyOffset((Vector3Int)upperRight, newRoom.ground);
        newRoom.walls = ApplyOffset((Vector3Int)upperRight, newRoom.walls);
        newRoom.decorations = ApplyOffset((Vector3Int)upperRight, newRoom.decorations);
        newRoom.meta = ApplyOffset((Vector3Int)upperRight, newRoom.meta);

        newRoom.InitializeMetaInformation();

        // Saves the scriptableObject to disk
        ScriptableObjectUtility.SaveRoomFile(newRoom);

        SavedTile[] ApplyOffset(Vector3Int offset, SavedTile[] array)
        {
            for(int i = 0; i < array.Length; i++)
            {
                array[i].position -= offset;
            }
            return array;
        }
    }
#endif

    public void LoadRoom(Vector2Int position, ScriptableRoom room)
    {
        TilemapUtility.LoadTiles(position, groundMap, room.ground);
        TilemapUtility.LoadTiles(position, wallMap, room.walls);
        TilemapUtility.LoadTiles(position, decorMap, room.decorations);
    }

    // Clears the map
    public void ClearMap()
    {
        Tilemap[] maps = FindObjectsOfType<Tilemap>();

        foreach(Tilemap map in maps)
        {
            map.ClearAllTiles();
        }
    }
}

#if UNITY_EDITOR

public static class ScriptableObjectUtility
{
    public static void SaveRoomFile(ScriptableRoom room)
    {
        AssetDatabase.CreateAsset(room, $"Assets/Resources/Rooms/{room.type}/{room.name}.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    public static void SaveGraphFile(ScriptableLevelGraph graph)
    {
        AssetDatabase.CreateAsset(graph, $"Assets/Resources/Graphs/Level {graph.levelIndex}/Graph {graph.graphIndex}.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}

#endif

[Serializable]
public class IntGameobjectPair
{
    public int enemyCount = 0;
    public GameObject prefab = null;

    public IntGameobjectPair(int number, GameObject gameObject)
    {
        enemyCount = number;
        prefab = gameObject;
    }
}