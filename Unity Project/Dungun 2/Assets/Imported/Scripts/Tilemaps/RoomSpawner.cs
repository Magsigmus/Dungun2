using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
public class RoomSpawner : MonoBehaviour
{
    public Tilemap groundMap, wallMap, decorMap;
    public Vector3Int Pos;
    public ScriptableRoom localRoom;

    public void LoadLocalRoom()
    {
        LoadRoom(Pos, localRoom);
    }

    public void LoadRoom(Vector3Int origenPos, ScriptableRoom room)
    {
        Debug.Log(room.ground.Count);

        foreach(SavedTile tile in room.ground)
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
    }

    public void LoadRoom(Vector2Int origenPos, ScriptableRoom room)
    {
        LoadRoom(new Vector3Int(origenPos.x, origenPos.y, 0), room);
    }
}