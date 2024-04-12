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
    public bool buildCompletely = true;
    public int maxTilesConsideredInAStar = 200;

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

        ComponentTilemap completeComponentTilemap;

        // Goes through the first dfs cycle for each of the nodes
        for (int i = 0; i < visited.Length; i++)
        {
            bool[] previousVisited = (bool[])visited.Clone();
            //if (roomGenerationStack.TryPeek(out (int, int) trash1)) { continue; }
            //if (cycleRoomGenerationStack.TryPeek(out (int, int,int) trash11)) { continue; }
            if (visited[i]) continue;

            startCycleNode = i;
            visited[i] = true;

            // Gets a randrom first room
            ScriptableRoom firstRoom = GetRandomRoom(compositeAdjecenyList[i].type, 
                (byte)levelGraph.adjecenyList[i].connections.Count); 

            //Sig: Initializes the componentTilemap-object
            currentComponentTilemap = new ComponentTilemap(compositeAdjecenyList.Count, tileLookupMap, testGround, testWalls, testDecor); // Initalizes the current component tilemap
            currentComponentTilemap.rooms[i] = (new Vector2Int(), firstRoom); // Adds the origen and room to the component
            currentComponentTilemap.LoadRoom(new Vector2Int(), firstRoom); // Loads the room to the test tilemaps
            currentComponentTilemap.freeEntrances[i].AddRange(firstRoom.metaInformation.AllEntrances);
            
            bool sucess = true;

            // Runs dfs for the current node 
            foreach (int neighbour in compositeAdjecenyList[i].connections)
            {
                if (!sucess) { break; }
                //if (roomGenerationStack.TryPeek(out (int,int) trash2)) { continue; }
                //if (cycleRoomGenerationStack.TryPeek(out (int,int,int) trash22)) { continue; }
                if (visited[neighbour]) { continue; }

                visited[neighbour] = true;
                if (compositeAdjecenyList[i].id[0] == 'N')
                {
                    Debug.LogError("FUCK");
                }
                else if (compositeAdjecenyList[i].id[0] == 't')
                {
                    sucess = PlaceTree(neighbour, i);
                    //roomGenerationStack.Push((neighbour, i));
                }
                else if (compositeAdjecenyList[i].id[0] == 'c')
                {
                    sucess = PlaceCycle(neighbour, i, 0);

                    if(!sucess) { continue; }

                    /*(int, Vector2Int) endNodeEntrance = currentComponentTilemap.freeEntrances[currentCycleNode][0];
                    (int, Vector2Int) startNodeEntrance = currentComponentTilemap.freeEntrances[startCycleNode][0];
                    endNodeEntrance.Item2 += currentComponentTilemap.rooms[currentCycleNode].Item1;
                    startNodeEntrance.Item2 += currentComponentTilemap.rooms[startCycleNode].Item1;*/

                    sucess = sucess && currentComponentTilemap.
                        AStarCorridorGeneration(currentCycleNode, startCycleNode, tileLookupMap, 
                        AStarTilemap, maxTilesConsideredInAStar);
                }
            }

            if(!sucess) { 
                i--;
                visited = previousVisited;
                currentComponentTilemap.ClearMap();
                Debug.Log("Detected overlapping level. Retrying");  
                continue; 
            }

            //if(i == 0) { }

            componentTilemaps.Add(currentComponentTilemap);
        }

        if(!buildCompletely) { return; }

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
            for(uint i = 1; i <= entrances; i++)
            {
                uint key = ((uint)roomType << (8 * 2)) | ((uint)i << 8) | ((uint)entranceDir);
                if (!roomIndexLookupMap.ContainsKey(key)) { roomIndexLookupMap[key] = new List<int>(); }
                roomIndexLookupMap[key].Add(roomIndex);
            }
        }
    }

    private ScriptableRoom[] GetRoomList(byte entranceDir, int entranceNum, RoomType type)
    {
        uint key = ((uint)type << (8 * 2)) | ((uint)entranceNum << 8) | ((uint)entranceDir);

        if (!roomIndexLookupMap.ContainsKey(key))
        {
            Debug.LogError($"Missing a room of type {type}, entrance direction {entranceDir} and {entranceNum} entrances.");
        }

        ScriptableRoom[] result = roomIndexLookupMap[key].Select(e => allRooms[e]).ToArray();
        return result;
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
        ScriptableRoom room = roomList[UnityEngine.Random.Range(0, roomList.Length)];
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
    public bool PlaceTree(int nodeIndex, int parentNode)
    {
        List<(int, Vector2Int)> freeParentEntances = currentComponentTilemap.freeEntrances[parentNode]; // Gets all the free entances that are in the parent room
        int parentEntranceIndex = UnityEngine.Random.Range(0, freeParentEntances.Count); // Finds the index of a random entrance that belongs to the parent room

        ScriptableRoom room = GetRandomRoom(parentEntranceIndex, parentNode, nodeIndex, out int childEntranceIndex);
        bool sucess = currentComponentTilemap.
            PlaceRoom(nodeIndex, parentNode, parentEntranceIndex, childEntranceIndex, room);

        // Goes through all the neighbours and calls this function on them, DFS style
        foreach (int neighbour in compositeAdjecenyList[nodeIndex].connections)
        {
            if (!visited[neighbour])
            {
                visited[neighbour] = true;
                sucess = sucess && PlaceTree(neighbour, nodeIndex);
                //roomGenerationStack.Push((neighbour, nodeIndex));
            }
        }

        return sucess;
    }
    
    int startCycleNode;
    int currentCycleNode;

    public bool PlaceCycle(int nodeIndex, int parentNode, int depth)
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

        bool sucess = currentComponentTilemap.PlaceRoom(nodeIndex, parentNode, parentEntranceIndex, childEntranceIndex, childRoom);
        if(!sucess) { return false; }

        for(int i = 0; i < neighbours.Count; i++)
        {
            if (!visited[neighbours[i]])
            {
                visited[neighbours[i]] = true;
                if (!PlaceCycle(neighbours[i], nodeIndex, depth + 1)) { return false; };
                //cycleRoomGenerationStack.Push((neighbours[i], nodeIndex, depth + 1));
            }
        }

        return true;
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