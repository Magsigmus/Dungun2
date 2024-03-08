using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ScriptableRoom : ScriptableObject
{
    public List<SavedTile> ground, walls, decorations, meta;
    public RoomType type;
    public Vector2 size;
}

[Serializable]
public class SavedTile
{
    public Vector3Int Position;
    public Tile tile;
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