using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using System;
using System.Collections.Specialized;

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
        newRoom.ground = GetTilesFromMap(groundMap).ToArray();
        newRoom.walls = GetTilesFromMap(wallMap).ToArray();
        newRoom.decorations = GetTilesFromMap(decorMap).ToArray();
        newRoom.meta = GetTilesFromMap(metaMap).ToArray();
        newRoom.type = roomType;
        newRoom.enemies = enemyPrefabs;

        //Sig: Finds the offset of the room, and its size
        upperRight = (Vector2Int)newRoom.ground[0].Position;
        lowerLeft = (Vector2Int)newRoom.ground[0].Position;
        foreach (SavedTile tile in newRoom.ground)
        {
            upperRight.x = Math.Max(upperRight.x, tile.Position.x);
            upperRight.y = Math.Max(upperRight.y, tile.Position.y);

            lowerLeft.x = Math.Min(lowerLeft.x, tile.Position.x);
            lowerLeft.y = Math.Min(lowerLeft.y, tile.Position.y);
        }
        newRoom.size = upperRight - lowerLeft;

        Debug.Log(upperRight);

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
                array[i].Position -= offset;
            }
            return array;
        }

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
                        tile = map.GetTile<BaseTile>(pos)
                    };
                }
            }
        }
    }
#endif

    public void LoadRoom(Vector3Int origenPos, ScriptableRoom room)
    {
        foreach (SavedTile tile in room.ground)
        {
            groundMap.SetTile(tile.Position + origenPos, tile.tile);
        }
        foreach (SavedTile tile in room.walls)
        {
            wallMap.SetTile(tile.Position + origenPos, tile.tile);
        }
        foreach (SavedTile tile in room.decorations)
        {
            decorMap.SetTile(tile.Position + origenPos, tile.tile);
        }
        foreach (SavedTile tile in room.meta)
        {
            metaMap.SetTile(tile.Position + origenPos, tile.tile);
        }
    }

    public void LoadRoom(Vector2Int origenPos, ScriptableRoom room)
    {
        LoadRoom(new Vector3Int(origenPos.x, origenPos.y, 0), room);
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