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
using Unity.VisualScripting;

//Sig:
/// <summary>
/// A class that controls the level and manages the general game
/// </summary>
public class LevelManger : MonoBehaviour
{
    [Header("General")]
    public GameObject player; // Sig: A referance to the player
    public float timeToSpawnEnemy = 0.5f; // Sig: The time from when a player enteres a room to when the enemies spawn

    [Header("Tilemaps")]
    public Tilemap groundTilemap; // Sig: The ground tilemap
    public Tilemap wallTilemap; // Sig: The wall tilemap
    public Tilemap decorationTilemap; // Sig: The decoration tilemap
    public Tilemap aStarTilemap; // Sig: The tilemap used for the A*-algorithm
    public Tilemap spriteMaskTilemap; // Sig: The tilemap which the sprite mask hides
    public Tilemap overlayTilemap; // Sig: The tilemap rendered on top of everything, that gets cleared when a room is revealed
    
    [Header("Level Generation Settings")]
    public int levelNumber; // Sig: The level index, that controls where the graphs are taken from
    public bool buildCompletely = true; // Sig: If true, then normal level gen. It false, then entrance triggers and finishing touches are not spawned.
    public int maxTilesConsideredInAStar = 200; // Sig: The maximum amount of tiles that is considered when making an A*-corridor
    public int roomsConsideredInCycle = 3; // Sig: The maximum amout of rooms considered in cycle-generation
    public GameObject enemySpawnTriggerPrefab; // Sig: A prefab of the entrance trigger
    public NavMeshSurface navMesh; // Sig: A reference to the nav mesh
    public BaseTileTypePair[] tileLookup; // Sig: A editable table of keys and values for the tile map

    [Header("Visual effects settings")]
    public float expandingAnimationTime = 2f; // Sig: The time it takes for the animation of expanding the sprite mask when entering a room
    public float spawningWallAnimationTime = 0.5f; // Sig: The time it takes for the animation overlaid on pop-up walls when spawning
    public float spawningEnemyAnimationTime = 0.3f; // Sig: The time it takes for the animation overlaid on enemies when they spawn
    public GameObject spawingWallEffectPrefab; // Sig: A prefab that makes the visual effect for spawning pop-up walls
    public GameObject spawingEnemyEffectPrefab; // Sig: A prefab that makes the visual effect for spawning enemies

    private int lockedRoom = -1; // Sig: The currently locked room. A value of -1 means that no room is locked.
    private ComponentTilemap level; // Sig: The fully-assembled level.
    private bool[] spawnedEnemies = null; // Sig: An array denoting if there has been spawned enemies in a room yet
    private bool[] discoveredCorridors = null; // Sig: An array denoting whether the player has discovered each of the corridors
    private Queue<(Vector2Int, SavedTile[])> revealQueue; // Sig: A queue containing each area that is currently being revealed
    private Dictionary<TileType, List<BaseTile>> tileLookupMap; // Sig: A dictionary containing each basetile of each tile type
    private float timer = 0; // Sig: A timer that how long there is untill the room discovery animation is finished.
    private bool animating = false; // Sig: True if the animation for revealing a new room is playing
    private bool spawningEnemies = false; // Sig: True if currently spawning enemies
    private bool switchingScene = false; // Sig: True if currently switching scenes
    private PlayerBehaviour playerBehaviour; // Sig: A reference to the playerbehaviour script
    private int bossRoomIndex = 0; // Sig: The index of the boss room
    private Vector3Int[] neighbourDirs = new Vector3Int[4] { // Sig: The four different directions
        new Vector3Int(0, 1), // North
        new Vector3Int(-1, 0), // West
        new Vector3Int(0, -1), // South
        new Vector3Int(1, 0) // East
    };

    private void Start()
    {
        // Sig: Gets the reference to the player behaviour
        playerBehaviour = player.GetComponent<PlayerBehaviour>();
        playerBehaviour.manager = this; // Sig: Sets the reference to the level manager in the player

        //ComponentTilemap map = new ComponentTilemap(1, InitializeTileTable(tileLookup), groundTilemap, wallTilemap, decorationTilemap);
        //map.PlaceCorridor(map.GetStraightPath(new Vector3Int(), new Vector3Int(20, 10, 0)));

        //Sig: Starts the level generation
        StartCoroutine(GenerateLevel(levelNumber));
    }

    //Sig: 
    /// <summary>
    /// Generates a level from a level index
    /// </summary>
    /// <param name="levelIndex">The index of the level</param>
    public IEnumerator GenerateLevel(int levelIndex)
    {
        //Sig: Initialization
        tileLookupMap = InitializeTileTable(tileLookup);

        LevelGenerator levelGenerator = new LevelGenerator(groundTilemap, wallTilemap,
            decorationTilemap, aStarTilemap, tileLookupMap);

        levelGenerator.roomsConsideredInCycle = roomsConsideredInCycle;
        levelGenerator.maxTilesConsideredInAStar = maxTilesConsideredInAStar;

        //Sig: Generates the level
        ComponentTilemap newLevel = levelGenerator.GenerateLevel(levelIndex);

        if (!buildCompletely) { yield break; } //Sig: Stops the function

        //Sig: Spawns the entrance triggers in each of the used entrances
        newLevel.SpawnEntranceTriggers(enemySpawnTriggerPrefab);
        level = newLevel;

        //Sig: Initialization
        spawnedEnemies = Enumerable.Repeat(false, level.rooms.Count).ToArray();
        discoveredCorridors = Enumerable.Repeat(false, level.corridorGround.Count).ToArray();
        revealQueue = new Queue<(Vector2Int, SavedTile[])>();

        //Sig: Places the tilemaps used for the discovery animation
        BuildOverlappingTilemaps(level);

        //Sig: Gets the entrance room
        (Vector2Int, ScriptableRoom) entranceRoom = level.rooms.Where(e => e.Item2.type == RoomType.Entrance).First();
        
        //Sig: Finds all the tiles in the entrance room, and sets their BaseTile to null so it clears the tiles instead of loading any
        SavedTile[] punchThroughTiles = entranceRoom.Item2.ground.Select(e => new SavedTile(e.position, null)).ToArray();
        TilemapUtility.LoadTiles(entranceRoom.Item1, spriteMaskTilemap, punchThroughTiles);
        TilemapUtility.LoadTiles(entranceRoom.Item1, overlayTilemap, punchThroughTiles);

        //Sig: Makes sure that no enemies spawn in the entrance room
        spawnedEnemies[level.rooms.IndexOf(entranceRoom)] = true;

        //Sig: Gets the index of the boss room 
        bossRoomIndex = level.rooms.
            Select((element, index) => (element.Item2.type == RoomType.Boss, index)).
            Where(e => e.Item1).First().Item2;

        //Sig: Waits for a frame to make the baking of the nav mesh actually successful 
        yield return new WaitForNextFrameUnit(); 
        navMesh.BuildNavMesh();
    }

    //Sig:
    /// <summary>
    /// Builds the tilemaps used in the discovery animation
    /// </summary>
    /// <param name="level">The level from which the tilemaps should be made</param>
    private void BuildOverlappingTilemaps(ComponentTilemap level)
    {
        //Sig: Gets all the positions where tiles are placed on the level, and places the defualt wall in their place on the overlap tilemaps
        SavedTile[] tiles = TilemapUtility.GetTilesFromMap(level.groundTilemap).
            Select(e => new SavedTile(e.position, tileLookupMap[TileType.DefaultWall][0])).ToArray();
        TilemapUtility.LoadTiles(level.origin, spriteMaskTilemap, tiles);
        TilemapUtility.LoadTiles(level.origin, overlayTilemap, tiles);
    }

    //Sig:
    /// <summary>
    /// Converts the a list of key and value pairs to a dictionary
    /// </summary>
    /// <param name="keyValueList">The list of key value pairs</param>
    /// <returns>A dictionary with those key value pairs</returns>
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

    // Sig: Called from the entrance trigger
    /// <summary>
    /// Spawn enemies in a room
    /// </summary>
    /// <param name="nodeIndex">The rooms index</param>
    public void SpawnEnemies(int nodeIndex)
    {
        //Sig: Gets the required enemies from the saved room object
        (Vector2Int, ScriptableRoom) room = level.rooms[nodeIndex];
        IntGameobjectPair[] enemies = room.Item2.enemies;
        int numberOfEnemies = enemies.Select(e => e.enemyCount).Sum();

        //Sig: Gets all positons of the spawnpoints 
        Vector2Int[] spawnPoints = room.Item2.metaInformation.EnemySpawnPoints.
            Select(e => e + room.Item1).ToArray();

        //Sig: If there are no enemies to spawn or no spawnpoints, then dont do any thing
        if(numberOfEnemies == 0 || spawnPoints.Length == 0) { return; }

        //Sig: Randomises the spawn points
        System.Random r = new System.Random();
        Vector2Int[] randomisedSpawnPoints = 
            Enumerable.Repeat(spawnPoints, (numberOfEnemies / spawnPoints.Length) + 1).
            SelectMany(e => e).OrderBy(e => r.Next()).ToArray();

        Debug.Log($"Spawning Enemies in room {nodeIndex}");

        //Sig: Spawns all the enemies
        int counter = 0;
        for(int i = 0; i < enemies.Length; i++)
        {
            NavMeshAgent enemyNavMesh = enemies[i].prefab.GetComponent<NavMeshAgent>();
            
            if (!enemyNavMesh) 
            { 
                Debug.LogError("You fucked up. You spwaned a(n) "+ enemies[i].prefab.name+" instead of an enemy! \nFIX IT NOW!!!"); 
                continue; 
            }  //rasj: skip if dev fucked up

            enemyNavMesh.enabled = false; // Sig: An active nav mesh agent fucks the position of the enemy up

            for (int j = 0; j < enemies[i].enemyCount; j++)
            {
                
                GameObject newEnemy = Instantiate(enemies[i].prefab);
                //newEnemy.GetComponent<EnemyCombatBehaviour>().defaultTarget = player.transform;

                //Sig: Moves the enemy to the spawn position
                Vector2 spawnPosition = (Vector2)randomisedSpawnPoints[counter] + new Vector2(0.5f, 0.5f);
                newEnemy.transform.position = spawnPosition;

                //Instantiate(spawingEnemyEffectPrefab, spawnPosition, Quaternion.identity);
                //Sig: Spawns the visual effect for spawning an enemy and destroys it after some time.
                Destroy(Instantiate(spawingEnemyEffectPrefab, spawnPosition, Quaternion.identity), spawningEnemyAnimationTime);

                Debug.Log($"Spawned an enemy at {newEnemy.transform.position}");
                
                //Sig: Activates the nav mesh agent of the enemy
                newEnemy.GetComponent<NavMeshAgent>().enabled = true;

                counter++;
            }
            counter++;
        }

    }
    
    //Sig:
    /// <summary>
    /// Handles the discovery of a room
    /// </summary>
    /// <param name="roomIndex">The index of the room that is discovered</param>
    /// <param name="entrance">The entrance from which the room is discovered</param>
    public IEnumerator DiscoverRoom(int roomIndex, (int, Vector2Int) entrance)
    {
        //Sig: Animates the discovery of the corridor
        int corridorIndex = level.corridorIndecies[roomIndex][entrance];
        if (!discoveredCorridors[corridorIndex])
        {
            discoveredCorridors[corridorIndex] = true;
            AnimateDiscovery(new Vector2Int(), level.corridorGround[corridorIndex]);
        }

        //Sig: If there is already spawned enemies in this room or if there is still living enimes, then don't do anything further.
        if (spawnedEnemies[roomIndex] || GameObject.FindGameObjectsWithTag("Enemy").Length != 0) { yield break; }
        spawnedEnemies[roomIndex] = true;

        //Sig:
        (Vector2Int, ScriptableRoom) entranceRoom = level.rooms[roomIndex];
        AnimateDiscovery(entranceRoom.Item1, entranceRoom.Item2.ground.ToList());

        (Vector2Int, ScriptableRoom) room = level.rooms[roomIndex];
        IntGameobjectPair[] enemies = room.Item2.enemies;
        int numberOfEnemies = enemies.Select(e => e.enemyCount).Sum();

        if (numberOfEnemies == 0) { yield break; }

        LockRoom(roomIndex);
        lockedRoom = roomIndex;

        spawningEnemies = true;

        yield return new WaitForSeconds(timeToSpawnEnemy);

        SpawnEnemies(roomIndex);

        spawningEnemies = false;
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

            Vector2Int origen = entrance.Item2 + direction * 2 + level.rooms[roomIndex].Item1;

            TilemapUtility.LoadTiles(origen, wallTilemap, tiles);

            //Sig: Cant convert from Vector2Int to Vector3, so gotta go through Vector2
            Vector3 spawingPositon = (Vector3)(Vector2)origen + new Vector3(0.5f, 0.5f);
            SpawnWallEffect(spawingPositon, tiles);
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

            Vector2Int origen = entrance.Item2 + direction * 2 + level.rooms[roomIndex].Item1;
            TilemapUtility.LoadTiles(origen, wallTilemap, tiles);

            Vector3 spawingPositon = (Vector3)(Vector2)origen + new Vector3(0.5f, 0.5f);
            SpawnWallEffect(spawingPositon, tiles);
        }
    }

    private void SpawnWallEffect(Vector3 origen, SavedTile[] tiles)
    {
        foreach (SavedTile tile in tiles)
        {
            //Instantiate(spawingEnemyEffectPrefab, spawingPositon, Quaternion.identity);
            Destroy(Instantiate(spawingWallEffectPrefab, origen + tile.position, Quaternion.identity), spawningWallAnimationTime);
        }
    }

    public void AnimateDiscovery(Vector2Int origen, List<SavedTile> tiles)
    {
        while(revealQueue.Count > 0) { RevealNextRoomPermentantly(); }
        playerBehaviour.StartExpandAnimation();

        SavedTile[] punchThroughTiles = tiles.Select(e => new SavedTile(e.position, null)).ToArray();

        TilemapUtility.LoadTiles(origen, overlayTilemap, punchThroughTiles);

        revealQueue.Enqueue((origen, punchThroughTiles));
        timer = expandingAnimationTime;
        animating = true;
    }

    public void RevealNextRoomPermentantly()
    {
        if (revealQueue.Count == 0) { return; }
        (Vector2Int, SavedTile[]) callInfo = revealQueue.Dequeue();
        TilemapUtility.LoadTiles(callInfo.Item1, spriteMaskTilemap, callInfo.Item2);
    }

    private void Update()
    {
        if(switchingScene) { return; }

        if(timer < 0 && animating)
        {
            RevealNextRoomPermentantly();
            animating = false;
        }

        if(GameObject.FindGameObjectsWithTag("Enemy").Length == 0 && !spawningEnemies && lockedRoom != -1)
        {
            if(bossRoomIndex == lockedRoom)
            {
                StartCoroutine(playerBehaviour.SwitchScene("Win"));
                switchingScene = true;
            }

            OpenRoom(lockedRoom);
            lockedRoom = -1;
        }

        timer -= Time.deltaTime;
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