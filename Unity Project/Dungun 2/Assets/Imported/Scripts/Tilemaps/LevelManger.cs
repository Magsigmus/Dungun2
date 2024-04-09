using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.AI;
using System.Linq;
using Priority_Queue;
using Unity.VisualScripting;
using static UnityEditor.PlayerSettings;
using NavMeshPlus.Components;

public class LevelManger : MonoBehaviour
{
    ScriptableLevelGraph levelGraph; // The uncomposited graph used to make the level
    List<CompositeNode> compositeAdjecenyList; // The composited graph used to make the level
    private bool[] visited; // An array used while going through the composited level graph
    private ComponentTilemap currentComponentTilemap; // The current component that is being assembeld
    private List<ComponentTilemap> allComponents = new List<ComponentTilemap>(); // All the components that have been assembled

    [Header("Tilemaps")]
    public Tilemap testGround;
    public Tilemap testWalls, testDecor; // The tilemaps used to assemble the components
    public Tilemap AStarTilemap;
    //public Tilemap prefabricationTilemap;
    
    [Header("Level Generation Settings")]
    public int roomsConsideredInCycle = 3;
    public int levelNumber;
    public Sprite pixel;
    public float lengthCutoff;
    public BaseTileTypePair[] tileLookup;
    public GameObject enemySpawnTriggerPrefab;
    public NavMeshSurface navMesh;

    private bool[] spawnedEnemies = null; // An array denoting if there has been spawned enemies in a room yet
    //public GameObject entranceTrigger; // A prefab containing an entance trigger

    //Sig: Key is indexed with first byte being 0, second byte being the value of the roomType casted to a byte, third byte being the number of entrances, and the fourth byte being the direction of one of the entances.
    //Sig: North -> 0, West -> 1, South -> 2, East -> 3
    Dictionary<uint, List<int>> roomIndexLookupMap;
    Dictionary<TileType, BaseTile> tileLookupMap;
    ScriptableRoom[] allRooms; // An array containing all the rooms saved 
    List<ComponentTilemap> componentTilemaps = new List<ComponentTilemap>();

    private IEnumerator aStarCorotine;

    public Stack<(int, int)> roomGenerationStack = new Stack<(int, int)>();
    public Stack<(int, int,int)> cycleRoomGenerationStack = new Stack<(int, int,int)>();

    //Sig: Generates a level from a level index
    public void GenerateLevel(int levelIndex)
    {
        testGround.ClearAllTiles();
        testWalls.ClearAllTiles();
        testDecor.ClearAllTiles();
        componentTilemaps.Clear();

        //Sig: Pulls a graph from the saved ones
        int NumOfGraphs = Resources.LoadAll<ScriptableLevelGraph>($"Graphs/Level {levelIndex}").Length;
        int tempindex = UnityEngine.Random.Range(0, NumOfGraphs);
        levelGraph = Resources.Load<ScriptableLevelGraph>($"Graphs/Level {levelIndex}/Graph {tempindex}");

        //Sig: Initializes the corutine and clears the tilemap
        if(aStarCorotine != null) { StopCoroutine(aStarCorotine); }

        Debug.Log($"Retrived graph from level {levelIndex} num {tempindex}");

        InitalizeRoomLists(); //Sig: Makes an array filled with pointers to a room array
        levelGraph.Initalize(); // Makes the composite adjeceny list 
        InitializeTileTable();
        compositeAdjecenyList = levelGraph.compositeAdjecenyList;

        // Initialization
        spawnedEnemies = Enumerable.Repeat(false, compositeAdjecenyList.Count).ToArray();
        visited = Enumerable.Repeat(false, compositeAdjecenyList.Count).ToArray();

        roomGenerationStack = new Stack<(int, int)>();
        cycleRoomGenerationStack = new Stack<(int, int, int)>();

        // Goes through the first dfs cycle for each of the nodes
        for (int i = 0; i < visited.Length; i++)
        {
            if (roomGenerationStack.TryPeek(out (int, int) trash1)) { continue; }
            if (cycleRoomGenerationStack.TryPeek(out (int, int,int) trash11)) { continue; }
            if (visited[i]) continue;

            startCycleNode = i;
            visited[i] = true;

            // Gets a randrom first room
            ScriptableRoom firstRoom = GetRandomRoom(compositeAdjecenyList[i].type, (byte)levelGraph.adjecenyList[i].connections.Count); 

            //Sig: Initializes the componentTilemap-object
            currentComponentTilemap = new ComponentTilemap(compositeAdjecenyList.Count, tileLookupMap, testGround, testWalls, testDecor); // Initalizes the current component tilemap
            currentComponentTilemap.rooms[i] = (new Vector2Int(), firstRoom); // Adds the origen and room to the component
            currentComponentTilemap.LoadRoom(new Vector2Int(), firstRoom); // Loads the room to the test tilemaps
            currentComponentTilemap.freeEntrances[i].AddRange(firstRoom.metaInformation.AllEntrances);

            // Runs dfs for the current node 
            foreach (int neighbour in compositeAdjecenyList[i].connections)
            {
                if (roomGenerationStack.TryPeek(out (int,int) trash2)) { continue; }
                if (cycleRoomGenerationStack.TryPeek(out (int,int,int) trash22)) { continue; }
                if (visited[neighbour]) { continue; }

                visited[neighbour] = true;
                if (compositeAdjecenyList[i].id[0] == 'N')
                {
                    Debug.LogError("FUCK");
                }
                else if (compositeAdjecenyList[i].id[0] == 't')
                {
                    PlaceTree(neighbour, i);
                    //roomGenerationStack.Push((neighbour, i));
                }
                else if (compositeAdjecenyList[i].id[0] == 'c')
                {
                    PlaceCycle(neighbour, i, 0);

                    //(int, Vector2Int) endNodeEntrance = currentComponentTilemap.freeEntrances[currentCycleNode][0];
                    //(int, Vector2Int) startNodeEntrance = currentComponentTilemap.freeEntrances[startCycleNode][0];
                    //endNodeEntrance.Item2 += currentComponentTilemap.rooms[currentCycleNode].Item1;
                    //startNodeEntrance.Item2 += currentComponentTilemap.rooms[startCycleNode].Item1;
                    
                    //currentComponentTilemap.AStarCorridorGeneration(startNodeEntrance, endNodeEntrance, tileLookupMap, AStarTilemap);
                }
            }

            componentTilemaps.Add(currentComponentTilemap);
        }

        // PLACE TRIGGERS 

        foreach(ComponentTilemap component in componentTilemaps)
        {
            component.SpawnEntranceTriggers(enemySpawnTriggerPrefab);
        }

        navMesh.BuildNavMesh();

        //Debug.Log("Triggered");
        //navMesh.BuildNavMesh();
        // Gets all the tiles in the tilemaps
    }

    #region RoomManagement
    private void InitializeTileTable()
    {
        tileLookupMap = new Dictionary<TileType, BaseTile>();
        
        foreach(BaseTileTypePair value in tileLookup)
        {
            tileLookupMap[value.type] = value.tile;
        }
    }
    
    // Initalizes the sortedrooms and allrooms
    private void InitalizeRoomLists()
    {
        allRooms = Resources.LoadAll<ScriptableRoom>("Rooms");

        roomIndexLookupMap = new Dictionary<uint, List<int>>();

        // Goes through all the rooms and makes pointeres for each of them
        for(int i = 0; i < allRooms.Length; i++)
        {
            allRooms[i].InitializeMetaInformation();

            RoomMetaInformation roomInfo = allRooms[i].metaInformation;
            byte roomTypeIndex = (byte)allRooms[i].type;
            byte entranceNum = (byte)roomInfo.TotalEntances;

            if(roomInfo.NorthEntrances.Count != 0) { AddToDict(roomTypeIndex, entranceNum, 0, i); }
            if(roomInfo.WestEntrances.Count != 0) { AddToDict(roomTypeIndex, entranceNum, 1, i); }
            if(roomInfo.SouthEntrances.Count != 0) { AddToDict(roomTypeIndex, entranceNum, 2, i); }
            if(roomInfo.EastEntrances.Count != 0) { AddToDict(roomTypeIndex, entranceNum, 3, i); }
        }

        void AddToDict(byte roomType, byte entrances, byte entranceDir, int roomIndex)
        {
            uint key = ((uint)roomType << (8 * 2)) | ((uint)entrances << 8) | ((uint)entranceDir);
            if (!roomIndexLookupMap.ContainsKey(key)) { roomIndexLookupMap[key] = new List<int>(); }
            roomIndexLookupMap[key].Add(roomIndex);
        }
    }

    private ScriptableRoom[] GetRoomList(byte entranceDir, int entranceNum, RoomType type)
    {
        uint key = ((uint)type << (8 * 2)) | ((uint)entranceNum << 8) | ((uint)entranceDir);

        if (!roomIndexLookupMap.ContainsKey(key))
        {
            Debug.LogError($"Missing a room of type {type}, entrance direction {entranceDir} and {entranceNum} entrances.");
        }

        return roomIndexLookupMap[key].Select(e => allRooms[e]).ToArray();
    }

    private ScriptableRoom[] GetRandomRoomList(int parentEntranceIndex, int parentIndex, int childIndex, int length, out byte childEntranceId)
    {
        System.Random r = new System.Random();
        ScriptableRoom[] rooms = GetRoomList(parentEntranceIndex, parentIndex, childIndex, out childEntranceId);

        return rooms.Take(length).OrderBy(e => r.Next()).ToArray();
    }

    private ScriptableRoom GetRandomRoom(int parentEntranceIndex, int parentIndex, int childIndex, out int childEntranceIndex)
    {
        ScriptableRoom[] roomList = GetRoomList(parentEntranceIndex, parentIndex, childIndex, out byte childEntranceId);
        ScriptableRoom room = roomList[UnityEngine.Random.Range(0, roomList.Length - 1)];
        childEntranceIndex = room.metaInformation.GetRandomEntrance(childEntranceId);

        return room;
    }

    private ScriptableRoom[] GetRoomList(int parentEntranceIndex, int parentIndex, int childIndex, out byte childEntranceId)
    {
        List<(int, Vector2Int)> freeParentEntances = currentComponentTilemap.freeEntrances[parentIndex];
        (int, Vector2Int) selectedEntrance = freeParentEntances[parentEntranceIndex];
        childEntranceId = (byte)((selectedEntrance.Item1 + 2) % 4);
        int degree = levelGraph.adjecenyList[childIndex].connections.Count;
        RoomType roomType = compositeAdjecenyList[childIndex].type;

        return GetRoomList(childEntranceId, degree, roomType);
    }

    // Gets a random room with the entrance direction, entance amount and room type. 
    private ScriptableRoom GetRandomRoom(byte entranceDir, int entranceNum, RoomType type)
    {
        ScriptableRoom[] roomlist = GetRoomList(entranceDir, entranceNum, type);
        return roomlist[UnityEngine.Random.Range(0, roomlist.Length)];
    }

    // Gets a random room with the entrance amount and room type
    private ScriptableRoom GetRandomRoom(RoomType type, byte entranceNum)
    {
        byte dir = (byte)UnityEngine.Random.Range(0, 3);
        return GetRandomRoom(dir, entranceNum, type);
    }
    #endregion

    #region GraphTraversment
    // Places a graph tree component, using dfs
    public void PlaceTree(int nodeIndex, int parentNode)
    {
        List<(int, Vector2Int)> freeParentEntances = currentComponentTilemap.freeEntrances[parentNode]; // Gets all the free entances that are in the parent room
        int parentEntranceIndex = UnityEngine.Random.Range(0, freeParentEntances.Count); // Finds the index of a random entrance that belongs to the parent room

        ScriptableRoom room = GetRandomRoom(parentEntranceIndex, parentNode, nodeIndex, out int childEntranceIndex);
        currentComponentTilemap.PlaceRoom(nodeIndex, parentNode, parentEntranceIndex, childEntranceIndex, room);

        // Goes through all the neighbours and calls this function on them, DFS style
        foreach (int neighbour in compositeAdjecenyList[nodeIndex].connections)
        {
            if (!visited[neighbour])
            {
                visited[neighbour] = true;
                PlaceTree(neighbour, nodeIndex);
                //roomGenerationStack.Push((neighbour, nodeIndex));
            }
        }
    }
    
    int startCycleNode;
    int currentCycleNode;

    public void PlaceCycle(int nodeIndex, int parentNode, int depth)
    {
        currentCycleNode = nodeIndex;
        List<int> neighbours = compositeAdjecenyList[nodeIndex].connections;

        int length = int.Parse(compositeAdjecenyList[nodeIndex].id.Substring(1));
        // Gets all the free entances that are in the parent room
        List<(int, Vector2Int)> freeParentEntances = currentComponentTilemap.freeEntrances[parentNode]; 
        int parentEntranceIndex = 0;

        (Vector2Int, ScriptableRoom) startRoom = currentComponentTilemap.rooms[startCycleNode];
        (Vector2Int, ScriptableRoom) parentRoom = currentComponentTilemap.rooms[parentNode];
        Vector2Int parentRoomMidpoint = parentRoom.Item1 + parentRoom.Item2.size / 2;
        Vector2Int goal = currentComponentTilemap.freeEntrances[startCycleNode][
            FindClosestEntrance(parentRoomMidpoint, startCycleNode, out float _)].Item2;

        //Sig: If more than half of the cycle is has been looked at, then try and connect the start and current rooms together, by picking an exit that is closest to the start room.
        if (length/2 <= depth)
        {
            parentEntranceIndex = FindClosestEntrance(goal, parentNode, out float trash);
        }
        else
        {
            // Finds the index of a random entrance that belongs to the parent room
            parentEntranceIndex = UnityEngine.Random.Range(0, freeParentEntances.Count); 
        }

        ScriptableRoom childRoom = GetRandomRoom(parentEntranceIndex, parentNode, nodeIndex, out int childEntranceIndex);
       
        if ((length * 2) / 3 <= depth)
        {
            ScriptableRoom[] roomList = GetRandomRoomList(parentEntranceIndex, parentNode, nodeIndex, roomsConsideredInCycle, out byte trash1);
            float bestRoomDistance = float.MaxValue;

            for (int i = 0; i < roomList.Length; i++)
            {
                ScriptableRoom newRoom = roomList[i];
                (int, int) bestEntrancePair = 
                    GetBestEntrancePair(
                        goal, parentNode, newRoom, parentEntranceIndex,
                        out float bestEntranceDistance);
                
                if(bestRoomDistance > bestEntranceDistance)
                {
                    bestRoomDistance = bestEntranceDistance;
                    childRoom = newRoom;
                    (parentEntranceIndex, childEntranceIndex) = bestEntrancePair;
                }
            }
        }

        currentComponentTilemap.PlaceRoom(nodeIndex, parentNode, parentEntranceIndex, childEntranceIndex, childRoom);
                
        for(int i = 0; i < neighbours.Count; i++)
        {
            if (!visited[neighbours[i]])
            {
                visited[neighbours[i]] = true;
                //PlaceCycle(neighbours[i], nodeIndex, depth + 1);
                cycleRoomGenerationStack.Push((neighbours[i], nodeIndex, depth + 1));
            }
        }
    }

    // Return used parent entrance index, used child entrance index, best child entrance index, distance
    (int,int) GetBestEntrancePair(Vector2Int goal, int roomIndex, ScriptableRoom room, int parentEntranceIndex, out float bestDistance)
    {
        (int, int, Vector2Int)[] possibleChildOrigens = 
            currentComponentTilemap.GetChildRoomOrigensInComponentSpace(roomIndex, room);

        (int, int) result = (0,0);
        bestDistance = int.MaxValue;
        int bestEntranceIndex = 0; // Not used, but could be in the future

        foreach ((int, int, Vector2Int) possibleOrigen in possibleChildOrigens)
        {
            for (int i = 0; i < room.metaInformation.TotalEntances; i++)
            {
                if(i == possibleOrigen.Item2 || parentEntranceIndex != possibleOrigen.Item1) { continue; }
                float newDistance = Vector2.Distance(room.metaInformation.AllEntrances[i].Item2 + possibleOrigen.Item3, goal);

                if(newDistance < bestDistance) {
                    bestDistance = newDistance;
                    bestEntranceIndex = i;
                    result = (possibleOrigen.Item1, possibleOrigen.Item2);
                }
            }
        }

        return result;
    }

    //Sig: Finds the closest free entrance to a point
    int FindClosestEntrance(Vector2Int goal, int roomIndex, out float dist)
    {
        List<(int, Vector2Int)> freeParentEntances = currentComponentTilemap.freeEntrances[roomIndex];
        (Vector2Int, ScriptableRoom) parentRoom = currentComponentTilemap.rooms[roomIndex];
        int result = 0;

        float bestDist = Vector2.Distance(freeParentEntances[0].Item2 + parentRoom.Item1, goal);
        for (int i = 0; i < freeParentEntances.Count; i++)
        {
            float currentDist = Vector2.Distance(freeParentEntances[i].Item2 + parentRoom.Item1, goal);
            if (currentDist < bestDist) { result = i; }
        }

        dist = bestDist;

        return result;
    }

    #endregion
    
    // Called from a entrance trigger, spawn enemies in a room
    public void SpawnEnemies(int nodeIndex)
    {
        //Sig:  Checks if the enemies in a room has been spawned
        if (spawnedEnemies == null) { return; }
        if (spawnedEnemies[nodeIndex]) { return; }

        //Sig: Gets the required enemies from the saved room object
        (Vector2Int, ScriptableRoom) room = componentTilemaps[0].rooms[nodeIndex];
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

public class ComponentTilemap
{
    public Vector2Int origin;
    public Tilemap groundTilemap, wallTilemap, decorationTilemap;
    public List<List<(int, Vector2Int)>> freeEntrances; // Char: North = 0, West = 1, South = 2, East = 3
    public List<(Vector2Int, ScriptableRoom)> rooms; // Vector2 to hold the origen of the scripableroom
    public Dictionary<TileType, BaseTile> tileLookupTable = new Dictionary<TileType, BaseTile>();

    // Init of the ways the A* algorithm can move
    Vector3Int[] neighbourDirs = new Vector3Int[4] {
        new Vector3Int(0, 1), // North
        new Vector3Int(-1, 0), // West
        new Vector3Int(0, -1), // South
        new Vector3Int(1, 0) // East
    };

    public ComponentTilemap(int nodes, Dictionary<TileType, BaseTile> tileLookupTable)
    {
        this.tileLookupTable = tileLookupTable;
        groundTilemap = new Tilemap();
        wallTilemap = new Tilemap();
        decorationTilemap = new Tilemap();
        freeEntrances = new List<List<(int, Vector2Int)>>();
        while (freeEntrances.Count < nodes) { freeEntrances.Add(new List<(int, Vector2Int)>()); }
        rooms = new List<(Vector2Int, ScriptableRoom)>();
        while (rooms.Count < nodes) { rooms.Add((new Vector2Int(), new ScriptableRoom())); }
        origin = new Vector2Int();
    }

    public ComponentTilemap(int nodes, Dictionary<TileType, BaseTile> tileLookupTable, Tilemap ground, Tilemap walls, Tilemap decor)
    {
        this.tileLookupTable = tileLookupTable;
        this.groundTilemap = ground;
        this.wallTilemap = walls;
        decorationTilemap = decor;
        freeEntrances = new List<List<(int, Vector2Int)>>();
        while (freeEntrances.Count < nodes) { freeEntrances.Add(new List<(int, Vector2Int)>()); }
        rooms = new List<(Vector2Int, ScriptableRoom)>();
        while (rooms.Count < nodes) { rooms.Add((new Vector2Int(), new ScriptableRoom())); }
        origin = new Vector2Int();
    }

    public void ClearMap()
    {
        groundTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
        decorationTilemap.ClearAllTiles();
    }

    public bool LoadRoom(Vector2Int roomOrigen, ScriptableRoom room)
    {
        bool overlap = false;
        Vector3Int origenPos = new Vector3Int(roomOrigen.x, roomOrigen.y, 0);

        foreach (SavedTile tile in room.ground)
        {
            overlap = overlap || groundTilemap.HasTile(tile.Position + origenPos);
            groundTilemap.SetTile(tile.Position + origenPos, tile.tile);
        }
        foreach (SavedTile tile in room.walls)
        {
            overlap = overlap || wallTilemap.HasTile(tile.Position + origenPos);
            wallTilemap.SetTile(tile.Position + origenPos, tile.tile);
        }
        foreach (SavedTile tile in room.decorations)
        {
            decorationTilemap.SetTile(tile.Position + origenPos, tile.tile);
        }

        return overlap;
    }

    public bool PlaceRoom(int childIndex, int parentIndex, int parentEntranceIndex, int childEntanceIndex, ScriptableRoom childRoom)
    {
        (int, Vector2Int) parentEntrance = freeEntrances[parentIndex][parentEntranceIndex];
        (int, Vector2Int) childEntrance = childRoom.metaInformation.AllEntrances[childEntanceIndex];

        (Vector2Int, ScriptableRoom) parentRoom = rooms[parentIndex];

        Vector2Int childRoomOrigen = GetChildRoomOrigenInComponentSpace(parentIndex, childRoom, parentEntrance, childEntrance);

        //Sig: Finds and applies a offset, so the room can be made 
        Vector2Int parentEntranceDirection = (Vector2Int)neighbourDirs[parentEntrance.Item1];
        int offset = FindCorrectRoomOffset(childRoom, childRoomOrigen, parentEntranceDirection);
        childRoomOrigen += offset * parentEntranceDirection;

        //Sig: Gets the component-space position of the entrance
        Vector2Int parentEntrancePos = parentRoom.Item1 + parentEntrance.Item2;
        Vector2Int childEntrancePos = childEntrance.Item2 + childRoomOrigen;

        if (LoadRoom(childRoomOrigen, childRoom)) { return false; } // Spawns the room

        // Saves the room and the origen of it (local in the component)
        rooms[childIndex] = (childRoomOrigen, childRoom);

        //PlaceCorridor(childEntrancePos, parentEntrancePos, (byte)childEntrance.Item1, (byte)parentEntrance.Item1);
        Vector3Int[] path = GetStraightPath((Vector3Int)childEntrancePos, (Vector3Int)parentEntrancePos);
        PlaceCorridor(path);
        
        List<Vector3Int> overlapPath = path.ToList();
        if(overlapPath.Count > 6)
        {
            overlapPath.RemoveRange(0, 3);
            overlapPath.RemoveRange(overlapPath.Count - 4, 3);

            if (CheckOverlap(overlapPath.ToArray(), 2)) { return false; }
        }

        // Corrects the amout of free entrances
        List<(int, Vector2Int)> newEntrances = childRoom.metaInformation.AllEntrances;
        for (int i = 0; i < newEntrances.Count; i++) // Adds the entrances from the new room 
        {
            if (childEntanceIndex == i) { continue; }
            freeEntrances[childIndex].Add(newEntrances[i]);
        }
        freeEntrances[parentIndex].RemoveAt(parentEntranceIndex); // Removes the used enteance

        return true;
    }

    private bool CheckOverlap(Vector3Int[] points, int width)
    {
        foreach(Vector3Int point in points)
        {
            Vector3Int[] adjencentTiles = GetAdjecentTiles(point, width);

            foreach(Vector3Int tilePosition in adjencentTiles)
            {
                if (groundTilemap.HasTile(tilePosition) || groundTilemap.HasTile(tilePosition))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void PlaceCorridor(Vector3Int[] corridorPoints)
    {
        List<Vector3Int> groundTiles = new List<Vector3Int>();
        List<Vector3Int> wallTiles = new List<Vector3Int>();

        for (int i = 1; i < corridorPoints.Length; i++)
        {
            Vector3Int[] newGroundTiles = GetAdjecentTiles(corridorPoints[i], 1);

            foreach (Vector3Int newGroundTile in newGroundTiles)
            {
                if (groundTilemap.HasTile(newGroundTile)) { continue; }
                groundTiles.Add(newGroundTile);
                groundTilemap.SetTile(newGroundTile, tileLookupTable[TileType.Ground]);
            }
        }

        for (int i = 0; i < corridorPoints.Length; i++)
        {
            Vector3Int[] newTiles = GetAdjecentTiles(corridorPoints[i], 2);

            foreach (Vector3Int newWallTile in newTiles)
            {
                if (groundTilemap.HasTile(newWallTile)) { continue; }
                wallTiles.Add(newWallTile);
                wallTilemap.SetTile(newWallTile, tileLookupTable[TileType.WestWall]);

                groundTiles.Add(newWallTile);
                groundTilemap.SetTile(newWallTile, tileLookupTable[TileType.Ground]);
            }
        }

        foreach (Vector3Int newWallTile in wallTiles)
        {
            wallTilemap.SetTile(newWallTile, PickCorrectTile(newWallTile));
        }
    }

    bool[,] CheckIfTilesArePresent(Tilemap map, int distToEdgeOfBox, Vector3Int pos)
    {
        bool[,] result = new bool[distToEdgeOfBox * 2 + 1, distToEdgeOfBox * 2 + 1];

        for (int i = -distToEdgeOfBox; i <= distToEdgeOfBox; i++)
        {
            for (int j = -distToEdgeOfBox; j <= distToEdgeOfBox; j++)
            {
                result[i + distToEdgeOfBox, j + distToEdgeOfBox] = map.HasTile(pos + new Vector3Int(i, j, 0));
            }
        }

        return result;
    }

    BaseTile PickCorrectTile(Vector3Int pos)
    {
        return tileLookupTable[GetCorrectTileType(pos)];
    }

    TileType GetCorrectTileType(Vector3Int pos)
    {
        bool[,] isThereAGroundTilePlacedAt = CheckIfTilesArePresent(groundTilemap, 1, pos);
        bool[,] isThereAWallTilePlacedAt = CheckIfTilesArePresent(wallTilemap, 1, pos);

        //Sig: If there is placed a wall on top of the ground, then count it as if the ground wasn't there.
        for (int x = 0; x <= 2; x++)
        {
            for (int y = 0; y <= 2; y++)
            {
                isThereAGroundTilePlacedAt[x, y] =
                    isThereAGroundTilePlacedAt[x, y] && !isThereAWallTilePlacedAt[x, y];
            }
        }

        //Sig: Check inwards corners
        if (isThereAGroundTilePlacedAt[2, 2] && isThereAWallTilePlacedAt[1, 2] && isThereAWallTilePlacedAt[2, 1])
        {
            return TileType.SouthWestInwardsCorner;
        }
        if (isThereAGroundTilePlacedAt[0, 2] && isThereAWallTilePlacedAt[1, 2] && isThereAWallTilePlacedAt[0, 1])
        {
            return TileType.SouthEastInwardsCorner;
        }
        if (isThereAGroundTilePlacedAt[2, 0] && isThereAWallTilePlacedAt[1, 0] && isThereAWallTilePlacedAt[2, 1])
        {
            return TileType.NorthWestInwardsCorner;
        }
        if (isThereAGroundTilePlacedAt[0, 0] && isThereAWallTilePlacedAt[1, 0] && isThereAWallTilePlacedAt[0, 1])
        {
            return TileType.NorthEastInwardsCorner;
        }
        //Sig: Check outwards corners
        if (isThereAGroundTilePlacedAt[0, 1] && isThereAGroundTilePlacedAt[1, 0] && isThereAWallTilePlacedAt[1, 2] && isThereAWallTilePlacedAt[2, 1])
        {
            return TileType.NorthEastOutwardsCorner;
        }
        if (isThereAGroundTilePlacedAt[2, 1] && isThereAGroundTilePlacedAt[1, 0] && isThereAWallTilePlacedAt[1, 2] && isThereAWallTilePlacedAt[0, 1])
        {
            return TileType.NorthWestOutwardsCorner;
        }
        if (isThereAGroundTilePlacedAt[2, 1] && isThereAGroundTilePlacedAt[1, 2] && isThereAWallTilePlacedAt[1, 0] && isThereAWallTilePlacedAt[0, 1])
        {
            return TileType.SouthWestOutwardsCorner;
        }
        if (isThereAGroundTilePlacedAt[0, 1] && isThereAGroundTilePlacedAt[1, 2] && isThereAWallTilePlacedAt[1, 0] && isThereAWallTilePlacedAt[2, 1])
        {
            return TileType.SouthEastOutwardsCorner;
        }
        //Sig: Check for normal walls
        if (isThereAWallTilePlacedAt[0, 1] && isThereAWallTilePlacedAt[2, 1] && isThereAGroundTilePlacedAt[1, 0])
        {
            return TileType.NorthWall;
        }
        if (isThereAWallTilePlacedAt[0, 1] && isThereAWallTilePlacedAt[2, 1] && isThereAGroundTilePlacedAt[1, 2])
        {
            return TileType.SouthWall;
        }
        if (isThereAWallTilePlacedAt[1, 0] && isThereAWallTilePlacedAt[1, 2] && isThereAGroundTilePlacedAt[0, 1])
        {
            return TileType.EastWall;
        }
        if (isThereAWallTilePlacedAt[1, 0] && isThereAWallTilePlacedAt[1, 2] && isThereAGroundTilePlacedAt[2, 1])
        {
            return TileType.WestWall;
        }

        Debug.LogError("CANT FIND THE FITTING TILE FOR THE REQUESTED AREA!");
        return TileType.Error;
    }

    private Vector3Int[] GetStraightPath(Vector3Int startPosition, Vector3Int endPosition)
    {
        List<Vector3Int> path = new List<Vector3Int>();
        Vector3Int difference = endPosition - startPosition;


        for(; difference.x != 0; difference.x += -Math.Sign(difference.x))
        {
            path.Add(startPosition);
            startPosition.x += Math.Sign(difference.x);
        }

        for (; difference.y != 0; difference.y += -Math.Sign(difference.y))
        {
            path.Add(startPosition);
            startPosition.y += Math.Sign(difference.y);
        }

        path.Add(startPosition);
        return path.ToArray();
    }

    private int FindCorrectRoomOffset(ScriptableRoom room, Vector2Int origen, Vector2Int direction)
    {
        // Moves the room away from the entrance untill it fits in the tilemap
        int offset = 0;
        bool overlap = true;
        while (overlap)
        {
            bool overlapInCurrentCycle = false;
            for (int i = 0; i < room.ground.Length; i++)
            {
                Vector3Int checkTilePos = room.ground[i].Position +
                    (Vector3Int)(origen + (offset * direction));
                overlapInCurrentCycle = groundTilemap.HasTile(checkTilePos);

                if (overlapInCurrentCycle) { break; }
            }
            overlap = overlapInCurrentCycle;
            offset++;
        }

        return offset;
    }

    public (int, int, Vector2Int)[] GetChildRoomOrigensInComponentSpace(int parentIndex, ScriptableRoom childRoom)
    {
        List<(int, int, Vector2Int)> result = new List<(int, int, Vector2Int)>();

        (int, Vector2Int)[] parentEntrances = freeEntrances[parentIndex].ToArray();
        for (int i = 0; i < parentEntrances.Length; i++)
        {
            (int, Vector2Int) parentEntrance = parentEntrances[i];
            (int, Vector2Int)[] childEntrances = childRoom.metaInformation.AllEntrances.ToArray();
            for (int j = 0; j < childEntrances.Length; j++)
            {
                (int, Vector2Int) childEntrance = childEntrances[j];
                if (GetCorrespondingEntranceDirection(parentEntrance.Item1) != childEntrance.Item1) { continue; }

                Vector2Int newRoomOrigen = GetChildRoomOrigenInComponentSpace(parentIndex, childRoom, parentEntrance, childEntrance);
                result.Add((i, j, newRoomOrigen));
            }
        }

        return result.ToArray();
    }

    private Vector2Int GetChildRoomOrigenInComponentSpace(int parentIndex, ScriptableRoom childRoom, 
        (int, Vector2Int) parentEntrance, (int, Vector2Int) childEntrance)
    {
        Vector2Int parentRoomSize = rooms[parentIndex].Item2.size;

        // Gets the position of the new room
        Vector2Int result = new Vector2Int();
        switch (parentEntrance.Item1)
        {
            case 0: // North
                result = new Vector2Int(
                    parentEntrance.Item2.x - childEntrance.Item2.x,
                    childRoom.size.y);
                break;
            case 1: // West
                result = new Vector2Int(
                    -parentRoomSize.x,
                    parentEntrance.Item2.y - childEntrance.Item2.y);
                break;
            case 2: // South
                result = new Vector2Int(
                    parentEntrance.Item2.x - childEntrance.Item2.x,
                    -parentRoomSize.y);
                break;
            case 3: // East
                result = new Vector2Int(
                    childRoom.size.x,
                    parentEntrance.Item2.y - childEntrance.Item2.y);
                break;
        }
        // Applies the parents origen to that offset
        result += rooms[parentIndex].Item1;

        return result;
    }

    private int GetCorrespondingEntranceDirection(int direction)
    {
        return (direction + 2) % 4;
    }

    private Vector3Int[] GetAdjecentTiles(Vector3Int pos, int distToBoxEdge)
    {
        Vector3Int[] result = new Vector3Int[(distToBoxEdge * 2 + 1) * (distToBoxEdge * 2 + 1)];

        int i = 0;
        for (int x = -distToBoxEdge; x <= distToBoxEdge; x++)
        {
            for (int y = -distToBoxEdge; y <= distToBoxEdge; y++)
            {
                result[i] = new Vector3Int(x, y, 0) + pos;
                i++;
            }
        }

        return result;
    }

    public void AStarCorridorGeneration((int, Vector2Int) parentEntrance, (int, Vector2Int) childEntrance, Dictionary<TileType,BaseTile> tilelookup, Tilemap aStarTilemap)
    {
        Vector3Int parentDirection = neighbourDirs[parentEntrance.Item1];
        Vector3Int parentPosition = (Vector3Int)parentEntrance.Item2;
        Vector3Int startPos = parentPosition + parentDirection * 3;

        Vector3Int childDirection = neighbourDirs[childEntrance.Item1];
        Vector3Int childPosition = (Vector3Int)childEntrance.Item2;
        Vector3Int endPos = childPosition + childDirection * 3;

        AStarTilemapSearch search = new AStarTilemapSearch(AStarCheckOverlap, aStarTilemap);

        List<Vector3Int> path1 = search.AStarPathFinding(startPos, endPos, tilelookup, out float path1Cost);
        search.tilemap.ClearAllTiles();

        List<Vector3Int> path2 = search.AStarPathFinding(endPos, startPos, tilelookup, out float path2Cost);
        search.tilemap.ClearAllTiles();

        List<Vector3Int> chosenPath = (path1Cost < path2Cost) ? path1 : path2;

        chosenPath.Insert(0, startPos);
        chosenPath.Insert(0, parentPosition + parentDirection * 2);
        chosenPath.Insert(0, parentPosition + parentDirection);
        chosenPath.Add(endPos);
        chosenPath.Add(childPosition + childDirection * 2);
        chosenPath.Add(childPosition + childDirection);

        PlaceCorridor(chosenPath.ToArray());
    }

    private bool AStarCheckOverlap(Vector3Int pos)
    {
        return CheckOverlap(new Vector3Int[]{ pos }, 2);
    }

    public void SpawnEntranceTriggers(GameObject entranceTriggerPrefab)
    {
        for (int i = 0; i < rooms.Count; i++)
        {
            (Vector2Int, ScriptableRoom) room = rooms[i];
            RoomMetaInformation info = room.Item2.metaInformation;
            foreach((int, Vector2) entrance in info.AllEntrances)
            {
                GameObject newTrigger = GameObject.Instantiate(entranceTriggerPrefab);
                newTrigger.transform.position = (Vector2)(origin + room.Item1 + entrance.Item2) + new Vector2(0.5f,0.5f);
                newTrigger.transform.up = neighbourDirs[GetCorrespondingEntranceDirection(entrance.Item1)];
                newTrigger.GetComponent<EntranceTriggerBehaviour>().roomIndex = i;
            }
        }
    }
}

public class AStarSearchTile : Tile
{
    public Vector3Int parent;
    public float gCost, hCost; 
    public float fCost
    {
        get
        {
            return gCost + hCost;
        }
    }
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

public class AStarTilemapSearch
{
    Vector3Int[] neighbourDirs = new Vector3Int[4] {
        new Vector3Int(0, 1), // North
        new Vector3Int(-1, 0), // West
        new Vector3Int(0, -1), // South
        new Vector3Int(1, 0) // East
    };

    private Func<Vector3Int, bool> overlapFunction;
    public Tilemap tilemap;

    public AStarTilemapSearch(Tilemap tilemap)
    {
        this.tilemap = tilemap;
    }

    public AStarTilemapSearch(Func<Vector3Int,bool> overlapFunction, Tilemap tilemap)
    {
        this.overlapFunction = overlapFunction;
        this.tilemap = tilemap;
    }

    public List<Vector3Int> AStarPathFinding(Vector3Int start, Vector3Int end, Dictionary<TileType, BaseTile> lookup, out float finalFCost)
    {
        Debug.Log($"Start: {start}, End: {end}");

        // Implementation of the A* algorithm
        SimplePriorityQueue<Vector3Int, float> queue = new SimplePriorityQueue<Vector3Int, float>(); // Init of the priority queue (a prioritsed binery heap)

        // Init of the start tile 
        AStarSearchTile startTile = new AStarSearchTile();
        startTile.gCost = 0;
        startTile.hCost = GetHCost(start);
        tilemap.SetTile(start, startTile);

        queue.Enqueue(start, startTile.fCost); // Adds the start tile to the queue

        int count = 0; // Measures how many times the loop has looked at a tile

        while (queue.Count > 0)
        {
            // Gets and removes the first element of the queue
            Vector3Int currentNode = queue.Dequeue();

            // Goes Through all the neighbours to the current node
            for (int i = 0; i < 4; i++)
            {
                Vector3Int neighbourDir = neighbourDirs[i];
                Vector3Int neighborPos = currentNode + neighbourDir; // Gets the pos of the neighbour

                if (overlapFunction(neighborPos) || tilemap.HasTile(neighborPos)) { continue; }

                // Marks the neighbour as visited, and with a G-, H-, Fcost and parent.
                AStarSearchTile currentTile = new AStarSearchTile();

                float newGCost = tilemap.GetTile<AStarSearchTile>(currentNode).gCost;
                newGCost += GetDeltaGCost(neighbourDir, currentNode);

                currentTile.gCost = newGCost;
                currentTile.hCost = GetHCost(neighborPos);
                currentTile.parent = currentNode;
                currentTile.color = Color.green;
                currentTile.sprite = lookup[TileType.DebugNorth + (i + 2) % 4].sprite;
                tilemap.SetTile(neighborPos, currentTile);

                queue.Enqueue(neighborPos, currentTile.fCost); // Adds that neighbour to the queue 
            }

            if (currentNode == end) { break; } // If we have reached the goal, then stop

            // If we have looked at more than 2000 nodes, then stop
            if (count > 2000)
            {
                Debug.Log("Could not find any path using A*!");
                tilemap.ClearAllTiles();
                finalFCost = -1;
                return new List<Vector3Int>();
            }
            count++;
        }

        //Sig: Does the traceback to find the positions for tiles

        List<Vector3Int> tracebackTiles = new List<Vector3Int>();
        Vector3Int currentTracebackTile = end;

        while (currentTracebackTile != start)
        {
            tracebackTiles.Add(currentTracebackTile);
            currentTracebackTile = tilemap.GetTile<AStarSearchTile>(currentTracebackTile).parent;
            //if(tracebackTiles.Count < 3) { continue; }
            //if(currentTracebackTile == tracebackTiles[tracebackTiles.Count - 2]) { Debug.Log("FUCKED IT UP"); break; }
        }
        
        finalFCost = tilemap.GetTile<AStarSearchTile>(end).fCost;

        return tracebackTiles;

        float GetHCost(Vector3Int pos)
        {
            return Mathf.Abs(pos.x - end.x) + Mathf.Abs(pos.y - end.y);
        }

        float GetDeltaGCost(Vector3Int dir, Vector3Int node)
        {
            // Tests if the current neighbour node is in line with the parent of the current node.
            AStarSearchTile tile = tilemap.GetTile<AStarSearchTile>(node);
            if (tile == null) { Debug.LogError($"No A*-tile found at {node}"); }
            Vector3Int parentToCurrentNode = tile.parent;
            Vector3Int diff = new Vector3Int();
            if (node == start)
            {
                diff = dir;
            }
            else { diff = node - parentToCurrentNode; }

            return (dir == diff) ? 0.5f : 1f;
        }
    }
}