using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
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
            if (tile.tile.type == MetaTileType.NorthEntrance)
            {
                info.NorthEntrances.Add((Vector2Int)tile.Position);
            }
            if (tile.tile.type == MetaTileType.WestEntrance)
            {
                info.WestEntrances.Add((Vector2Int)tile.Position);
            }
            if (tile.tile.type == MetaTileType.SouthEntrance)
            {
                info.SouthEntrances.Add((Vector2Int)tile.Position);
            }
            if (tile.tile.type == MetaTileType.EastEntrance)
            {
                info.EastEntrances.Add((Vector2Int)tile.Position);
            }
            if(tile.tile.type == MetaTileType.SpawnPoint)
            {
                info.EnemySpawnPoints.Add((Vector2Int)tile.Position);
            }


            if(tile.tile.type >= MetaTileType.NorthEntrance && tile.tile.type <= MetaTileType.EastEntrance)
            {
                info.AllEntrances.Add((tile.tile.type - MetaTileType.NorthEntrance, (Vector2Int)tile.Position));
            }
        }

        metaInformation = info;
    }
}

[Serializable]
public class RoomMetaInformation
{
    public List<Vector2Int> NorthEntrances, WestEntrances, EastEntrances, SouthEntrances;
    public List<(int, Vector2Int)> AllEntrances;
    public List<Vector2Int> EnemySpawnPoints;

    public RoomMetaInformation()
    {
        NorthEntrances = new List<Vector2Int>();
        WestEntrances = new List<Vector2Int>();
        EastEntrances = new List<Vector2Int>();
        SouthEntrances = new List<Vector2Int>();
        EnemySpawnPoints = new List<Vector2Int>();
        AllEntrances = new List<(int, Vector2Int)>();
    }

    public int TotalEntances { get 
        { 
            return NorthEntrances.Count + 
                WestEntrances.Count + 
                EastEntrances.Count + 
                SouthEntrances.Count;  
        } 
    }

    public int GetRandomEntrance(int direction)
    {
        int[] entranceIndices = new int[AllEntrances.Count];
        for(int i = 0; i < AllEntrances.Count; i++)
        {
            entranceIndices[i] = i;   
        }

        int[] selectedEntrances = entranceIndices.Where(e => AllEntrances[e].Item1 == direction).ToArray();
        System.Random r = new System.Random();
        return selectedEntrances[r.Next(0, selectedEntrances.Length)];
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
