using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Tile Name", menuName = "2D/Tiles/Level Tile")]
public class BaseTile : Tile
{
    public TileType type;

    public BaseTile() { }

    public BaseTile(Tile template)
    {
        base.sprite = template.sprite;
        base.transform = template.transform;
        base.flags = template.flags;
        base.color = template.color;
        base.colliderType = template.colliderType;
        //base.gameObject = template.gameObject;
        base.name = template.name;
        base.hideFlags = template.hideFlags;
    }
}

public enum TileType
{
    // Ground
    Ground = 0,

    // Decor    
    Decoration = 10,

    // Walls
    Wall = 20,

    // Meta
    NorthEntrance = 30,
    WestEntrance = 31,
    SouthEntrance = 32,
    EastEntrance = 33,
    SpawnPoint = 35,

    // Debug
    Debug = 40
}