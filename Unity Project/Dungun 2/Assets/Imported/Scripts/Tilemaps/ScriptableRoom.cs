using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ScriptableRoom : ScriptableObject
{
    public SavedTile[] ground, walls, decorations, meta;
    public RoomType type;
    public Vector2Int size;

    public RoomMetaInformation metaInformation;

    public void InitializeMetaInformation()
    {
        RoomMetaInformation info = new RoomMetaInformation();
        foreach(SavedTile tile in meta)
        {
            if (tile.tile.type == TileType.NorthEntrance)
            {
                info.NorthEntrances.Add(tile);
            }
            if (tile.tile.type == TileType.WestEntrance)
            {
                info.WestEntrances.Add(tile);
            }
            if (tile.tile.type == TileType.SouthEntrance)
            {
                info.SouthEntrances.Add(tile);
            }
            if (tile.tile.type == TileType.EastEntrance)
            {
                info.EastEntrances.Add(tile);
            }
            if(tile.tile.type == TileType.SpawnPoint)
            {
                info.EnemySpawnPoints.Add(tile);
            }


            if(tile.tile.type >= TileType.NorthEntrance && tile.tile.type <= TileType.EastEntrance)
            {
                info.AllEntrances.Add((tile, tile.tile.type - TileType.NorthEntrance));
            }
        }

        metaInformation = info;
    }
}

[Serializable]
public class RoomMetaInformation
{
    public List<SavedTile> NorthEntrances, WestEntrances, EastEntrances, SouthEntrances;
    public List<(SavedTile, int)> AllEntrances;
    public List<SavedTile> EnemySpawnPoints;

    public RoomMetaInformation()
    {
        NorthEntrances = new List<SavedTile>();
        WestEntrances = new List<SavedTile>();
        EastEntrances = new List<SavedTile>();
        SouthEntrances = new List<SavedTile>();
        EnemySpawnPoints = new List<SavedTile>();
        AllEntrances = new List<(SavedTile, int)>();
    }

    public int TotalEntances { get 
        { 
            return NorthEntrances.Count + 
                WestEntrances.Count + 
                EastEntrances.Count + 
                SouthEntrances.Count;  
        } 
    }
}

[Serializable]
public class SavedTile
{
    public Vector3Int Position;
    public BaseTile tile;
}

[Serializable]
public class EnemySpawnPoint
{
    public GameObject enemy;
    public Vector2 spawnPoint;
}

//Sig: Note: Has to be less than 128 elements, because it is converted to a byte further down
public enum RoomType //Sig: HUB HAS TO BE THE FIRST ELEMENT, AND OTHER HAS TO BE THE LAST
{
    Hub,
    Normal,
    Entrance,
    Boss,
    Other
}
