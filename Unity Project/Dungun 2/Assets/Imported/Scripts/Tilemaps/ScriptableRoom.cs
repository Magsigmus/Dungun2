using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ScriptableRoom : ScriptableObject
{
    public List<SavedTile> ground, walls, decorations;
    public RoomType type;
    public List<char> entranceIds;
    public List<Vector2> entrancePos;
    public Vector2 size;
    public List<EnemySpawnPoint> enemySpawnPoints;
}

[Serializable]
public class SavedTile
{
    public Vector3Int Position;
    public Basetile tile;
}
[Serializable]
public class EnemySpawnPoint
{
    public GameObject enemy;
    public Vector2 spawnPoint;
}

public enum RoomType // HUB HAS TO BE THE FIRST ELEMENT, AND OTHER HAS TO BE THE LAST
{
    Hub,
    Normal,
    Entrance,
    Boss,
    Other
}