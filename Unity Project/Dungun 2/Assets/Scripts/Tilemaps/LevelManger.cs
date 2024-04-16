using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.AI;
using System.Linq;
using NavMeshPlus.Components;
using UnityEngine.InputSystem;
using System.Threading;

public class LevelManger : MonoBehaviour
{
    [Header("General")]
    public GameObject player;
    public float expandingAnimationTime = 2f;

    [Header("Tilemaps")]
    public Tilemap groundTilemap;
    public Tilemap wallTilemap, decorationTilemap; // The tilemaps used to assemble the components
    public Tilemap aStarTilemap;
    public Tilemap spriteMaskTilemap, overlayTilemap;
    
    [Header("Level Generation Settings")]
    public int levelNumber;
    public bool buildCompletely = true;
    public int maxTilesConsideredInAStar = 200;
    public int roomsConsideredInCycle = 3;
    public GameObject enemySpawnTriggerPrefab;
    public NavMeshSurface navMesh;
    public BaseTileTypePair[] tileLookup;

    private int playerRoom = 0;
    private ComponentTilemap level;
    private bool[] spawnedEnemies = null; // An array denoting if there has been spawned enemies in a room yet
    private bool[] discoveredCorridors = null;
    private Queue<(Vector2Int, SavedTile[])> revealQueue;
    private Dictionary<TileType, List<BaseTile>> tileLookupMap;
    private float timer = 0;
    private bool animating = false;
    private List<GameObject> currentEnemies;
    private Vector3Int[] neighbourDirs = new Vector3Int[4] {
        new Vector3Int(0, 1), // North
        new Vector3Int(-1, 0), // West
        new Vector3Int(0, -1), // South
        new Vector3Int(1, 0) // East
    };

    private void Start()
    {
        GenerateLevel(levelNumber);
    }

    //Sig: Generates a level from a level index
    public void GenerateLevel(int levelIndex)
    {
        tileLookupMap = InitializeTileTable(tileLookup);

        LevelGenerator levelGenerator = new LevelGenerator(groundTilemap, wallTilemap, 
            decorationTilemap, aStarTilemap, tileLookupMap);

        levelGenerator.roomsConsideredInCycle = roomsConsideredInCycle;
        levelGenerator.maxTilesConsideredInAStar = maxTilesConsideredInAStar;

        ComponentTilemap newLevel = levelGenerator.GenerateLevel(levelIndex);

        if (!buildCompletely) { return; }
        newLevel.SpawnEntranceTriggers(enemySpawnTriggerPrefab);
        navMesh.BuildNavMesh();
        level = newLevel;

        spawnedEnemies = Enumerable.Repeat(false, level.rooms.Count).ToArray();

        BuildOverlappingTilemaps(level);
        (Vector2Int, ScriptableRoom) entranceRoom = level.rooms.Where(e => e.Item2.type == RoomType.Entrance).First();
        SavedTile[] punchThroughTiles = entranceRoom.Item2.ground.Select(e => new SavedTile(e.position, null)).ToArray();
        TilemapUtility.LoadTiles(entranceRoom.Item1, spriteMaskTilemap, punchThroughTiles);
        TilemapUtility.LoadTiles(entranceRoom.Item1, overlayTilemap, punchThroughTiles);

        discoveredCorridors = Enumerable.Repeat(false, level.corridorGround.Count).ToArray();
        revealQueue = new Queue<(Vector2Int, SavedTile[])>();
        currentEnemies = new List<GameObject>();
    }

    private void BuildOverlappingTilemaps(ComponentTilemap level)
    {
        SavedTile[] tiles = TilemapUtility.GetTilesFromMap(level.groundTilemap).
            Select(e=>new SavedTile(e.position, tileLookupMap[TileType.DefaultWall][0])).ToArray();
        TilemapUtility.LoadTiles(level.origin, spriteMaskTilemap, tiles);
        TilemapUtility.LoadTiles(level.origin, overlayTilemap, tiles);
    }

    private Dictionary<TileType, List<BaseTile>> InitializeTileTable(BaseTileTypePair[] keyValueList)
    {
        Dictionary<TileType, List<BaseTile>> tileLookupMap = new Dictionary<TileType, List<BaseTile>>();
         
        foreach(BaseTileTypePair value in keyValueList)
        {
            if(!tileLookupMap.ContainsKey(value.type)) 
            {
                tileLookupMap[value.type] = new List<BaseTile>();
            }
            tileLookupMap[value.type].Add(value.tile);
        }

        return tileLookupMap;
    }
    
    // Called from a entrance trigger, spawn enemies in a room
    public void SpawnEnemies(int nodeIndex)
    {
        //Sig:  Checks if the enemies in a room has been spawned
        if (spawnedEnemies == null) { return; }
        if (spawnedEnemies[nodeIndex]) { return; }
        spawnedEnemies[nodeIndex] = true;

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
                //newEnemy.GetComponent<EnemyCombatBehaviour>().defaultTarget = player.transform;
                newEnemy.transform.position = (Vector2)randomisedSpawnPoints[c] + new Vector2(0.5f, 0.5f);
                Debug.Log($"Spawned an enemy at {newEnemy.transform.position}");
                newEnemy.GetComponent<NavMeshAgent>().enabled = true;
                c++;
            }
            c++;
        }

    }

    public void DiscoverRoom(int roomIndex, (int, Vector2Int) entrance)
    {
        int corridorIndex = level.corridorIndecies[roomIndex][entrance];
        if (!discoveredCorridors[corridorIndex])
        {
            discoveredCorridors[corridorIndex] = true;
            AnimateDiscovery(new Vector2Int(), level.corridorGround[corridorIndex]);
        }

        if (spawnedEnemies[roomIndex]) { return; }
        playerRoom = roomIndex;

        SpawnEnemies(roomIndex);

        (Vector2Int, ScriptableRoom) entranceRoom = level.rooms[roomIndex];
        AnimateDiscovery(entranceRoom.Item1, entranceRoom.Item2.ground.ToList());

        if(currentEnemies.Count > 0)
        {
            LockRoom(roomIndex);
        }
    }

    private void LockRoom(int roomIndex)
    {
        foreach((int, Vector2Int) entrance in level.usedEntrances[roomIndex])
        {
            Vector2Int direction = (Vector2Int)neighbourDirs[entrance.Item1];
            Vector3Int orthogonalDirection = new Vector3Int(-direction.y, direction.x, 0);

            System.Random r = new System.Random();
            List<BaseTile> popupTiles = tileLookupMap[TileType.PopupWall].OrderBy(e => r.Next()).Take(3).ToList();
            SavedTile[] tiles = new SavedTile[3] 
            {   new SavedTile(new Vector3Int(), popupTiles[0]), 
                new SavedTile(orthogonalDirection, popupTiles[1]), 
                new SavedTile(-orthogonalDirection, popupTiles[2]) };

            TilemapUtility.LoadTiles(entrance.Item2 + direction * 2, wallTilemap, tiles);
        }
    }

    private void OpenRoom(int roomIndex)
    {
        foreach ((int, Vector2Int) entrance in level.usedEntrances[roomIndex])
        {
            Vector2Int direction = (Vector2Int)neighbourDirs[entrance.Item1];
            Vector3Int orthogonalDirection = new Vector3Int(-direction.y, direction.x, 0);

            SavedTile[] tiles = new SavedTile[3]
            {   new SavedTile(new Vector3Int(), null),
                new SavedTile(orthogonalDirection, null),
                new SavedTile(-orthogonalDirection, null) };

            TilemapUtility.LoadTiles(entrance.Item2 + direction * 2, wallTilemap, tiles);
        }
    }

    public void AnimateDiscovery(Vector2Int origen, List<SavedTile> tiles)
    {
        while(revealQueue.Count > 0) { RevealNextRoomPermentantly(); }
        player.GetComponent<PlayerBehaviour>().StartExpandAnimation();

        SavedTile[] punchThroughTiles = tiles.Select(e => new SavedTile(e.position, null)).ToArray();

        TilemapUtility.LoadTiles(origen, overlayTilemap, punchThroughTiles);

        revealQueue.Enqueue((origen, punchThroughTiles));
        timer = expandingAnimationTime;
        animating = true;
    }

    public void RevealNextRoomPermentantly()
    {
        (Vector2Int, SavedTile[]) callInfo = revealQueue.Dequeue();
        TilemapUtility.LoadTiles(callInfo.Item1, spriteMaskTilemap, callInfo.Item2);
    }

    private void Update()
    {
        if(timer > expandingAnimationTime && animating)
        {
            RevealNextRoomPermentantly();
            animating = false;
        }

        currentEnemies.Remove(null);
        if(currentEnemies.Count == 0)
        {
            OpenRoom(playerRoom);
        }
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
    DefaultWall,

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
    DebugEast,

    PopupWall
}