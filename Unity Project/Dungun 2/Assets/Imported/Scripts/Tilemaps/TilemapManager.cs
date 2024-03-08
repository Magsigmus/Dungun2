using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;

public class TilemapManager : MonoBehaviour
{
    public int roomIndex;
    public RoomType roomType;
    public (Transform, char)[] entrancePoints;
    public Tilemap groundMap, wallMap, decorMap;
#if UNITY_EDITOR
    public void SaveRoom()
    {
        roomIndex = PlayerPrefs.GetInt("RoomIndex") + 1;
        PlayerPrefs.SetInt("RoomIndex", roomIndex);

        Resources.LoadAll<ScriptableRoom>("");

        // Creates a new scriptable object for the room to be saved in
        ScriptableRoom newRoom = ScriptableObject.CreateInstance<ScriptableRoom>();

        Vector2Int size = new Vector2Int();

        // Fills that object with information
        newRoom.name = $"New Room {roomIndex}";
        newRoom.ground = GetTilesFromMap(groundMap).ToList();

        foreach(SavedTile tile in newRoom.ground)
        {
            size.x = Mathf.Max(size.x, -tile.Position.x);
            size.y = Mathf.Max(size.y, -tile.Position.y);
        }

        newRoom.walls = GetTilesFromMap(wallMap).ToList();
        newRoom.decorations = GetTilesFromMap(decorMap).ToList();
        newRoom.type = roomType;
        newRoom.size = size;

        newRoom.entranceIds = new List<char>();
        newRoom.entrancePos = new List<Vector2>();
        for (int i = 0; i < entrancePoints.Length; i++)
        {
            Vector2 pos = entrancePoints[i].Item1.position;
            newRoom.entranceIds.Add(entrancePoints[i].Item2);
            newRoom.entrancePos.Add(pos);
        }

        // Saves the scriptableObject to disk
        ScriptableObjectUtility.SaveRoomFile(newRoom);

        // Gets all the tiles in the tilemaps
        IEnumerable<SavedTile> GetTilesFromMap(Tilemap map)
        {
            foreach(Vector3Int pos in map.cellBounds.allPositionsWithin)
            {
                if (map.HasTile(pos))
                {
                    yield return new SavedTile()
                    {
                        Position = pos,
                        tile = map.GetTile<Basetile>(pos)
                    };
                }
            }
        }
    }
#endif
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