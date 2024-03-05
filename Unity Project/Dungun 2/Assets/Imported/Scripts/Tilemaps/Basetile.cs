using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "New Tile Name", menuName = "2D/Tiles/Level Tile")]
public class Basetile : Tile
{
    public TileType type;
}

public enum TileType
{
    // grund
    Ground = 0,
    Trap = 1,

    // Decor    
    BreakableObj = 10,

    // Walls
    Wall = 20
}