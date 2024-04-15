using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.AI;
using System.Linq;
using NavMeshPlus.Components;

public class LevelManger : MonoBehaviour
{
    [Header("Tilemaps")]
    public Tilemap groundTilemap;
    public Tilemap wallTilemap, decorationTilemap; // The tilemaps used to assemble the components
    public Tilemap aStarTilemap;
    //public Tilemap prefabricationTilemap;
    
    [Header("Level Generation Settings")]
    public int roomsConsideredInCycle = 3;
    public int levelNumber;
    public BaseTileTypePair[] tileLookup;
    public GameObject enemySpawnTriggerPrefab;
    public NavMeshSurface navMesh;
    public bool buildCompletely = true;
    public int maxTilesConsideredInAStar = 200;

    private ComponentTilemap level;
    private bool[] spawnedEnemies = null; // An array denoting if there has been spawned enemies in a room yet

    //Sig: Generates a level from a level index
    public void GenerateLevel(int levelIndex)
    {
        LevelGenerator levelGenerator = new LevelGenerator(groundTilemap, wallTilemap, 
            decorationTilemap, aStarTilemap, InitializeTileTable(tileLookup));

        ComponentTilemap newLevel = levelGenerator.GenerateLevel(levelIndex);

        if (!buildCompletely) { return; }
        newLevel.SpawnEntranceTriggers(enemySpawnTriggerPrefab);
        navMesh.BuildNavMesh();
        level = newLevel;
    }

    private Dictionary<TileType, BaseTile> InitializeTileTable(BaseTileTypePair[] keyValueList)
    {
        Dictionary<TileType, BaseTile> tileLookupMap = new Dictionary<TileType, BaseTile>();
         
        foreach(BaseTileTypePair value in keyValueList)
        {
            tileLookupMap[value.type] = value.tile;
        }

        return tileLookupMap;
    }
    
    // Called from a entrance trigger, spawn enemies in a room
    public void SpawnEnemies(int nodeIndex)
    {
        //Sig:  Checks if the enemies in a room has been spawned
        if (spawnedEnemies == null) { return; }
        if (spawnedEnemies[nodeIndex]) { return; }

        //Sig: Gets the required enemies from the saved room object
        (Vector2Int, ScriptableRoom) room = level.rooms[nodeIndex];
        IntGameobjectPair[] enemies = room.Item2.enemies;
        int numberOfEnemies = enemies.Select(e => e.enemyCount).Sum();

        Vector2Int[] spawnPoints = room.Item2.metaInformation.EnemySpawnPoints.
            Select(e => e + room.Item1).ToArray();

        if(numberOfEnemies == 0 || spawnPoints.Length == 0) { return; }

        //Sig: Randomises the spawn points
        System.Random r = new System.Random();
        Vector2Int[] randomisedSpawnPoints = 
            Enumerable.Repeat(spawnPoints, (numberOfEnemies / spawnPoints.Length) + 1).
            SelectMany(e => e).OrderBy(e => r.Next()).ToArray();

        Debug.Log($"Spawning Enemies in room {nodeIndex}");

        //Sig: Spawns all the enemies
        int c = 0;
        for(int i = 0; i < enemies.Length; i++)
        {
            enemies[i].prefab.GetComponent<NavMeshAgent>().enabled = false;
            for (int j = 0; j < enemies[i].enemyCount; j++)
            {
                GameObject newEnemy = Instantiate(enemies[i].prefab);
                newEnemy.transform.position = (Vector2)randomisedSpawnPoints[c] + new Vector2(0.5f, 0.5f);
                Debug.Log($"Spawned an enemy at {newEnemy.transform.position}");
                newEnemy.GetComponent<NavMeshAgent>().enabled = true;
                c++;
            }
            c++;
        }

        spawnedEnemies[nodeIndex] = true;
    }
}

[Serializable]
public class BaseTileTypePair
{
    public BaseTile tile;
    public TileType type;
}

public enum TileType
{
    Error,

    Ground,

    NorthWall,
    WestWall,
    SouthWall,
    EastWall,
    
    SouthWestInwardsCorner,
    SouthEastInwardsCorner,
    NorthWestInwardsCorner,
    NorthEastInwardsCorner,
    
    NorthEastOutwardsCorner,
    NorthWestOutwardsCorner,
    SouthWestOutwardsCorner,
    SouthEastOutwardsCorner,

    DebugNorth,
    DebugWest,
    DebugSouth,
    DebugEast
}