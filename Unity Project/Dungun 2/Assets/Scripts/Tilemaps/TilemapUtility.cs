using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TilemapUtility
{
    static public void LoadTiles(Vector3Int origenPos, Tilemap map, SavedTile[] tiles)
    {
        foreach(SavedTile tile in tiles)
        {
            map.SetTile(origenPos + tile.position, tile.tile);
        }
    }

    static public void LoadTiles(Vector2Int origenPos, Tilemap map, SavedTile[] tiles)
    {
        LoadTiles((Vector3Int)origenPos, map, tiles);
    }

    static public void LoadRoom(Vector3Int origenPos, ScriptableRoom room, ComponentTilemap map)
    {
        LoadTiles(origenPos, map.groundTilemap, room.ground);
        LoadTiles(origenPos, map.wallTilemap, room.walls);
        LoadTiles(origenPos, map.decorationTilemap, room.decorations);
    }


    static public void LoadRoom(Vector2Int origenPos, ScriptableRoom room, ComponentTilemap map)
    {
        LoadRoom(new Vector3Int(origenPos.x, origenPos.y, 0), room, map);
    }

    // Gets all the tiles in the tilemaps
    public static IEnumerable<SavedTile> GetTilesFromMap(Tilemap map)
    {
        foreach (Vector3Int pos in map.cellBounds.allPositionsWithin)
        {
            if (map.HasTile(pos))
            {
                yield return new SavedTile()
                {
                    position = pos,
                    tile = map.GetTile<BaseTile>(pos)
                };
            }
        }
    }
}
