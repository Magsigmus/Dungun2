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
    public Sprite pixel;

    [Header("Tiles")]
    public BaseTile temporaryTile;
    public BaseTile groundTile, northWallTile, southWallTile, westWallTile, eastWallTile;
    public BaseTile northWestOutwardsCornerWallTile, northEastOutwardsCornerWallTile, southEastOutwardsCornerWallTile, southWestOutwardsCornerWallTile,
        northEastInwardsCornerWallTile, northWestInwardsCornerWallTile, southEastInwardsCornerWallTile, southWestInwardsCornerWallTile; // The tiles used to make connections between rooms

    [Header("Level Generation Setting")]
    public int roomsConsideredInCycle = 3;
    public int levelNumber;
    public BaseTileTypePair[] tileLookup;
    //private bool[] spawnedEnemies; // An array denoting if there has been spawned enemies in a room yet
    //public GameObject entranceTrigger; // A prefab containing an entance trigger

    //Sig: Key is indexed with first byte being 0, second byte being the value of the roomType casted to a byte, third byte being the number of entrances, and the fourth byte being the direction of one of the entances.
    //Sig: North -> 0, West -> 1, South -> 2, East -> 3
    Dictionary<uint, List<int>> roomIndexLookupMap;
    Dictionary<TileType, BaseTile> tileLookupMap;
    ScriptableRoom[] allRooms; // An array containing all the rooms saved 
    List<ComponentTilemap> componentTilemaps = new List<ComponentTilemap>();

    private IEnumerator aStarCorotine;

    public Stack<(Vector2Int, Vector2Int, int, int)> aStarStack = new Stack<(Vector2Int, Vector2Int, int, int)>();

    //Sig: Generates a level from a level index
    public void GenerateLevel(int levelIndex)
    {
        testGround.ClearAllTiles();
        testWalls.ClearAllTiles();
        testDecor.ClearAllTiles();

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
        //spawnedEnemies = Enumerable.Repeat(false, compositeAdjecenyList.Count).ToArray();
        visited = Enumerable.Repeat(false, compositeAdjecenyList.Count).ToArray();

        // Goes through the first dfs cycle for each of the nodes
        for (int i = 0; i < visited.Length; i++)
        {
            if (visited[i]) continue;

            bool[] lastVisitedAr = visited;
            startCycleNode = i;

            //ClearMap();

            visited[i] = true;

            ScriptableRoom firstRoom = GetRandomRoom(compositeAdjecenyList[i].type, (byte)levelGraph.adjecenyList[i].connections.Count); // Gets a randrom first room
            firstRoom.InitializeMetaInformation();

            currentComponentTilemap = new ComponentTilemap(compositeAdjecenyList.Count, tileLookupMap, testGround, testWalls, testDecor); // Initalizes the current component tilemap
            currentComponentTilemap.rooms[i] = (new Vector2Int(), firstRoom); // Adds the origen and room to the component

            currentComponentTilemap.LoadRoom(new Vector2Int(), firstRoom); // Loads the room to the test tilemaps

            currentComponentTilemap.freeEntrances[i].AddRange(firstRoom.metaInformation.AllEntrances);

            // Runs dfs for the current node 
            foreach (int neighbour in compositeAdjecenyList[i].connections)
            {
                if (visited[neighbour]) { continue; }

                visited[neighbour] = true;
                if (compositeAdjecenyList[i].id[0] == 'N')
                {
                    Debug.LogError("FUCK");
                }
                else if (compositeAdjecenyList[i].id[0] == 't')
                {
                    PlaceTree(neighbour, i);
                }
                else if (compositeAdjecenyList[i].id[0] == 'c')
                {
                    PlaceCycle(neighbour, i, 0);

                    (int, Vector2Int) endNodeEntrance = currentComponentTilemap.freeEntrances[currentCycleNode][0];
                    (int, Vector2Int) startNodeEntrance = currentComponentTilemap.freeEntrances[startCycleNode][0];
                    endNodeEntrance.Item2 += currentComponentTilemap.rooms[currentCycleNode].Item1;
                    startNodeEntrance.Item2 += currentComponentTilemap.rooms[startCycleNode].Item1;

                    currentComponentTilemap.AStarCorridorGeneration(startNodeEntrance, endNodeEntrance);
                }
            }
        }

        // PLACE TRIGGERS 

        //Debug.Log("Triggered");
        //navMesh.BuildNavMesh();
        // Gets all the tiles in the tilemaps
        /*
        IEnumerable<SavedTile> GetTilesFromMap(Tilemap map)
        {
            foreach (Vector3Int pos in map.cellBounds.allPositionsWithin)
            {
                if (map.HasTile(pos))
                {
                    yield return new SavedTile()
                    {
                        Position = pos,
                        tile = map.GetTile<BaseTile>(pos)
                    };
                }
            }
        }*/
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

    private ScriptableRoom GetRandomRoom(int parentEntranceIndex, int parentIndex, int childIndex, out int childEntranceIndex)
    {
        List<(int, Vector2Int)> freeParentEntances = currentComponentTilemap.freeEntrances[parentIndex];
        (int, Vector2Int) selectedEntrance = freeParentEntances[parentEntranceIndex];
        byte childEntranceId = (byte)((selectedEntrance.Item1 + 2) % 4);
        int degree = levelGraph.adjecenyList[childIndex].connections.Count;
        RoomType roomType = compositeAdjecenyList[childIndex].type;

        ScriptableRoom room = GetRandomRoom(childEntranceId, degree, roomType);
        childEntranceIndex = room.metaInformation.GetRandomEntrance(childEntranceId);

        return room;
    }

    // Gets a random room with the entrance direction, entance amount and room type. 
    private ScriptableRoom GetRandomRoom(byte entranceDir, int entranceNum, RoomType type)
    {
        uint key = ((uint)type << (8 * 2)) | ((uint)entranceNum << 8) | ((uint)entranceDir);

        if (!roomIndexLookupMap.ContainsKey(key)) { 
            Debug.LogError($"Missing a room of type {type}, entrance direction {entranceDir} and {entranceNum} entrances."); 
        }
        
        // Gets a random room with the qualites that are requiered
        int count = roomIndexLookupMap[key].Count;
        int index = UnityEngine.Random.Range(0, count);
        int roomIndex = roomIndexLookupMap[key][index];
        ScriptableRoom room = allRooms[roomIndex];
        //RoomIndexLookupMap[key].RemoveAt(index);

        return room;
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
    private void PlaceTree(int nodeIndex, int parentNode)
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
        Vector2Int startingRoomMidpoint = startRoom.Item1 + startRoom.Item2.size / 2;

        //Sig: If more than half of the cycle is has been looked at, then try and connect the start and current rooms together, by picking an exit that is closest to the start room.
        if (length/2 <= depth)
        {
            parentEntranceIndex = FindClosestEntrance(startingRoomMidpoint, parentNode, out float trash);
        }
        else
        {
            // Finds the index of a random entrance that belongs to the parent room
            parentEntranceIndex = UnityEngine.Random.Range(0, freeParentEntances.Count); 
        }

        ScriptableRoom childRoom = GetRandomRoom(parentEntranceIndex, parentNode, nodeIndex, out int childEntranceIndex);

        if (length / 2 <= depth)
        {
            float bestRoomDistance = float.MaxValue;

            for (int i = 0; i < roomsConsideredInCycle; i++)
            {
                ScriptableRoom newRoom = GetRandomRoom(
                    parentEntranceIndex, parentNode, nodeIndex, out int trash);
                (int, int) bestEntrancePair = 
                    GetBestEntrancePair(
                        startingRoomMidpoint, nodeIndex, newRoom, 
                        out float bestEntranceDistance);
                
                if(bestRoomDistance < bestEntranceDistance)
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
                PlaceCycle(neighbours[i], nodeIndex, depth + 1);
            }
        }
    }

    // Return used parent entrance index, used child entrance index, best child entrance index, distance
    (int,int) GetBestEntrancePair(Vector2Int goal, int roomIndex, ScriptableRoom room, out float bestDistance)
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
                if(i == possibleOrigen.Item2) { continue; }
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
    /*
    #region CorridorGeneration

    private void PlaceCorridor(Vector2Int parentEntrancePos, Vector2Int childEntrancePos, byte parentEntranceDir, byte childEntranceDir)
    {
        Vector3Int[] tiles = GetAdjecentPairTiles(parentEntrancePos, parentEntranceDir, true).Select(e=>(Vector3Int)e).ToArray();
        if ((parentEntrancePos.x == childEntrancePos.x) != (parentEntrancePos.y == childEntrancePos.y))
        {
            int offset = Mathf.Abs(parentEntrancePos.x - childEntrancePos.x + parentEntrancePos.y - childEntrancePos.y);
            for (int i = 0; i < offset; i++)
            {
                // Checks if there can be placed the corridor
                unavoidableOverlap = testGround.HasTile(tiles[0]) || unavoidableOverlap;
                unavoidableOverlap = testGround.HasTile(tiles[1]) || unavoidableOverlap;
                unavoidableOverlap = testGround.HasTile(tiles[2]) || unavoidableOverlap;
                unavoidableOverlap = testGround.HasTile(tiles[3]) || unavoidableOverlap;
                unavoidableOverlap = testGround.HasTile(tiles[4]) || unavoidableOverlap;

                // Places the ground
                testGround.SetTile(tiles[0], groundTile);
                testGround.SetTile(tiles[1], groundTile);
                testGround.SetTile(tiles[2], groundTile);
                testGround.SetTile(tiles[3], groundTile);
                testGround.SetTile(tiles[4], groundTile);

                // Places the walls
                if (parentEntranceDir == 0 || parentEntranceDir == 2) // North and South
                {
                    testWalls.SetTile(tiles[3], westWallTile);
                    testWalls.SetTile(tiles[4], eastWallTile);
                }
                else if (parentEntranceDir == 1 || parentEntranceDir == 3) // West and East
                {
                    testWalls.SetTile(tiles[3], southWallTile);
                    testWalls.SetTile(tiles[4], northWallTile);
                }

                tiles[0] += neighbourDirs[parentEntranceDir];
                tiles[1] += neighbourDirs[parentEntranceDir];
                tiles[2] += neighbourDirs[parentEntranceDir];
                tiles[3] += neighbourDirs[parentEntranceDir];
                tiles[4] += neighbourDirs[parentEntranceDir];
            }
        }
        else
        {
            Debug.LogError("BUG: Corridors not placed correctly");
        }
    }

    // Init of the ways the A* algorithm can move
    Vector3Int[] neighbourDirs = new Vector3Int[4] {
        new Vector3Int(0, 1), // North
        new Vector3Int(-1, 0), // West
        new Vector3Int(0, -1), // South
        new Vector3Int(1, 0) // East
    };

    public void AStarCorridorGeneration(Vector2Int parentEntrancePos, Vector2Int childEntrancePos, int parentEntranceDir, int childEntranceDir)
    {
        Vector3Int startPos = (Vector3Int)parentEntrancePos + neighbourDirs[parentEntranceDir] * 3;
        Vector3Int endPos = (Vector3Int)childEntrancePos + neighbourDirs[childEntranceDir] * 3;

        List<Vector3Int> path1 = AStarPathFinding(startPos, endPos);
        float path1Cost = AStarTilemap.GetTile<AStarSearchTile>(endPos).fCost;
        AStarTilemap.ClearAllTiles();

        List<Vector3Int> path2 = AStarPathFinding(endPos, startPos);
        float path2Cost = AStarTilemap.GetTile<AStarSearchTile>(startPos).fCost;
        AStarTilemap.ClearAllTiles();

        List<Vector3Int> chosenPath = (path1Cost < path2Cost) ? path1 : path2;


        chosenPath.Insert(0, (Vector3Int)parentEntrancePos + neighbourDirs[parentEntranceDir] * 3);
        chosenPath.Insert(0, (Vector3Int)parentEntrancePos + neighbourDirs[parentEntranceDir] * 2);
        chosenPath.Insert(0, (Vector3Int)parentEntrancePos + neighbourDirs[parentEntranceDir]);
        chosenPath.Add((Vector3Int)childEntrancePos + neighbourDirs[childEntranceDir] * 3);
        chosenPath.Add((Vector3Int)childEntrancePos + neighbourDirs[childEntranceDir] * 2);
        chosenPath.Add((Vector3Int)childEntrancePos + neighbourDirs[childEntranceDir]);

        //prefabricationTilemap.ClearAllTiles();

        MakeCorridor(chosenPath.ToArray());
    }

    private void MakeCorridor(Vector3Int[] corridorPoints)
    {
        //Tilemap prefabicationTilemap = new Tilemap();

        List<Vector3Int> groundTiles = new List<Vector3Int>();
        List<Vector3Int> wallTiles = new List<Vector3Int>();

        for(int i = 1; i <  corridorPoints.Length; i++)
        {
            Vector3Int[] newGroundTiles = GetAdjecentTiles(corridorPoints[i], 1);
            
            foreach(Vector3Int newGroundTile in newGroundTiles)
            {
                if (testGround.HasTile(newGroundTile)) { continue; }
                groundTiles.Add(newGroundTile);
                testGround.SetTile(newGroundTile, groundTile);
            }
        }

        for (int i = 0; i < corridorPoints.Length; i++)
        {
            AStarTilemap.SetTile(corridorPoints[i], temporaryTile);
            Vector3Int[] newTiles = GetAdjecentTiles(corridorPoints[i], 2);

            foreach (Vector3Int newWallTile in newTiles)
            {
                if (testGround.HasTile(newWallTile)) { continue; }
                wallTiles.Add(newWallTile);
                testWalls.SetTile(newWallTile, temporaryTile);

                groundTiles.Add(newWallTile);
                testGround.SetTile(newWallTile, groundTile);
            }
        }

        foreach(Vector3Int newWallTile in wallTiles)
        {
            testWalls.SetTile(newWallTile, PickCorrectTile(newWallTile));
        }
        
        BaseTile PickCorrectTile(Vector3Int pos)
        {
            bool[,] isThereAGroundTilePlacedAt = CheckIfTilesArePresent(testGround, 1, pos);
            bool[,] isThereAWallTilePlacedAt = CheckIfTilesArePresent(testWalls, 1, pos);

            //Sig: If there is placed a wall on top of the ground, then count it as if the ground wasn't there.
            for(int x = 0; x <= 2; x++)
            {
                for (int y = 0; y <= 2; y++)
                {
                    isThereAGroundTilePlacedAt[x, y] = 
                        isThereAGroundTilePlacedAt[x, y] && !isThereAWallTilePlacedAt[x, y];
                }
            }

            //Sig: Check inwards corners
            if (isThereAGroundTilePlacedAt[2,2] && isThereAWallTilePlacedAt[1, 2] && isThereAWallTilePlacedAt[2, 1])
            {
                return southWestInwardsCornerWallTile;
            }
            if (isThereAGroundTilePlacedAt[0, 2] && isThereAWallTilePlacedAt[1, 2] && isThereAWallTilePlacedAt[0, 1])
            {
                return southEastInwardsCornerWallTile;
            }
            if (isThereAGroundTilePlacedAt[2, 0] && isThereAWallTilePlacedAt[1, 0] && isThereAWallTilePlacedAt[2, 1])
            {
                return northWestInwardsCornerWallTile;
            }
            if(isThereAGroundTilePlacedAt[0, 0] && isThereAWallTilePlacedAt[1, 0] && isThereAWallTilePlacedAt[0, 1])
            {
                return northEastInwardsCornerWallTile;
            }
            //Sig: Check outwards corners
            if (isThereAGroundTilePlacedAt[0,1] && isThereAGroundTilePlacedAt[1,0] && isThereAWallTilePlacedAt[1,2] && isThereAWallTilePlacedAt[2, 1])
            {
                return northEastOutwardsCornerWallTile;
            }
            if (isThereAGroundTilePlacedAt[2, 1] && isThereAGroundTilePlacedAt[1,0] && isThereAWallTilePlacedAt[1, 2] && isThereAWallTilePlacedAt[0, 1])
            {
                return northWestOutwardsCornerWallTile;
            }
            if (isThereAGroundTilePlacedAt[2, 1] && isThereAGroundTilePlacedAt[1, 2] && isThereAWallTilePlacedAt[1, 0] && isThereAWallTilePlacedAt[0, 1])
            {
                return southWestOutwardsCornerWallTile;
            }
            if (isThereAGroundTilePlacedAt[0, 1] && isThereAGroundTilePlacedAt[1, 2] && isThereAWallTilePlacedAt[1, 0] && isThereAWallTilePlacedAt[2, 1])
            {
                return southEastOutwardsCornerWallTile;
            }
            //Sig: Check for normal walls
            if (isThereAWallTilePlacedAt[0,1] && isThereAWallTilePlacedAt[2,1] && isThereAGroundTilePlacedAt[1, 0])
            {
                return northWallTile;
            }
            if (isThereAWallTilePlacedAt[0, 1] && isThereAWallTilePlacedAt[2, 1] && isThereAGroundTilePlacedAt[1, 2])
            {
                return southWallTile;
            }
            if (isThereAWallTilePlacedAt[1, 0] && isThereAWallTilePlacedAt[1, 2] && isThereAGroundTilePlacedAt[0, 1])
            {
                return eastWallTile;
            }
            if (isThereAWallTilePlacedAt[1, 0] && isThereAWallTilePlacedAt[1, 2] && isThereAGroundTilePlacedAt[2, 1])
            {
                return westWallTile; 
            }

            Debug.LogError("CANT FIND THE FITTING TILE FOR THE REQUESTED AREA!");
            return temporaryTile;
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
    }
    
    private List<Vector3Int> AStarPathFinding(Vector3Int start, Vector3Int end)
    {
        // Implementation of the A* algorithm
        SimplePriorityQueue<Vector3Int, float> queue = new SimplePriorityQueue<Vector3Int, float>(); // Init of the priority queue (a prioritsed binery heap)

        // Init of the start tile 
        AStarSearchTile startTile = new AStarSearchTile();
        startTile.gCost = 0;
        startTile.hCost = GetHCost(start);
        AStarTilemap.SetTile(start, startTile);

        queue.Enqueue(start, startTile.fCost); // Adds the start tile to the queue

        int count = 0; // Measures how many times the loop has looked at a tile

        while (queue.Count > 0)
        {
            // Gets and removes the first element of the queue
            Vector3Int currentNode = queue.Dequeue();

            // Goes Through all the neighbours to the current node
            foreach (Vector3Int neighbourDir in neighbourDirs)
            {
                Vector3Int neighborPos = currentNode + neighbourDir; // Gets the pos of the neighbour

                if (CheckForOverlap(neighborPos)) { continue; }

                // Marks the neighbour as visited, and with a G-, H-, Fcost and parent.
                AStarSearchTile currentTile = new AStarSearchTile();

                float newGCost = AStarTilemap.GetTile<AStarSearchTile>(currentNode).gCost;
                newGCost += GetDeltaGCost(neighbourDir, currentNode);

                currentTile.gCost = newGCost;
                currentTile.hCost = GetHCost(neighborPos);
                currentTile.parent = currentNode;
                currentTile.color = Color.green;
                currentTile.sprite = pixel;
                AStarTilemap.SetTile(neighborPos, currentTile);

                queue.Enqueue(neighborPos, currentTile.fCost); // Adds that neighbour to the queue 
            }

            if (currentNode == end) { break; } // If we have reached the goal, then stop

            // If we have looked at more than 10000 nodes, then stop
            if (count > 10000)
            {
                Debug.Log("Could not find any path using A*!");
                AStarTilemap.ClearAllTiles();
                break;
            }
            count++;
        }

        //Sig: Does the traceback to find the positions for tiles

        List<Vector3Int> tracebackTiles = new List<Vector3Int>();
        Vector3Int currentTracebackTile = end;

        do
        {
            tracebackTiles.Add(currentTracebackTile);
            currentTracebackTile = AStarTilemap.GetTile<AStarSearchTile>(currentTracebackTile).parent;
        }
        while (currentTracebackTile != start);

        return tracebackTiles;

        float GetHCost(Vector3Int pos)
        {
            return Mathf.Abs(pos.x - end.x) + Mathf.Abs(pos.y - end.y);
        }

        float GetDeltaGCost(Vector3Int dir, Vector3Int node)
        {
            // Tests if the current neighbour node is in line with the parent of the current node.
            AStarSearchTile tile = AStarTilemap.GetTile<AStarSearchTile>(node);
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

        bool CheckForOverlap(Vector3Int pos)
        {
            // Gets all the tiles that has to be clear of ground, to make sure that the corridor is not going through an established room
            Vector3Int[] adjecentTiles = GetAdjecentTiles(pos, 2);

            bool overlapping = AStarTilemap.HasTile(pos);
            for (int j = 0; j < adjecentTiles.Length; j++)
            {
                bool temp = testGround.HasTile(adjecentTiles[j]) || testWalls.HasTile(adjecentTiles[j]);
                overlapping = temp || overlapping;
            }

            return overlapping;
        }
    }
    
    private Vector2Int[] GetAdjecentPairTiles(Vector2Int pos, byte dir, bool fullCorridor)
    {
        Vector2Int rotatedDir = RotateVector90Degrees((Vector2Int)neighbourDirs[dir]);
        if(rotatedDir.x + rotatedDir.y > 0) { rotatedDir = -rotatedDir; }

        List<Vector2Int> result = new List<Vector2Int>();
        result.Add(pos);
        result.Add(pos + rotatedDir);
        result.Add(pos - rotatedDir);
        if (fullCorridor)
        {
            result.Add(pos + rotatedDir * 2);
            result.Add(pos - rotatedDir * 2);
        }

        return result.ToArray();

        Vector2Int RotateVector90Degrees(Vector2Int v)
        {
            return new Vector2Int(-v.y, v.x);
        }
    }

    private Vector3Int[] GetAdjecentTiles(Vector3Int pos, int distToBoxEdge)
    {
        Vector3Int[] result = new Vector3Int[25];

        int i = 0;
        for(int x = -distToBoxEdge; x <= distToBoxEdge; x++)
        {
            for(int y = -distToBoxEdge; y <= distToBoxEdge; y++)
            {
                result[i] = new Vector3Int(x, y, 0) + pos;
                i++;
            }
        }

        return result;
    }

    #endregion

    #region RoomTilemapPlacement

    private void PlaceRoom(int nodeIndex, int parentNode, int parentEntranceIndex, int nodeEntanceIndex, ScriptableRoom room)
    {
        (int, Vector2Int) parentEntrance = currentComponentTilemap.freeEntrances[parentNode][parentEntranceIndex];
        (SavedTile, int) selectedEntrance = room.metaInformation.AllEntrances[nodeEntanceIndex];

        ScriptableRoom parentRoom = currentComponentTilemap.rooms[parentNode].Item2;

        // Gets the position of the new room
        Vector2Int currentRoomOrigen = new Vector2Int();
        switch (parentEntrance.Item1)
        {
            case 0: // North
                currentRoomOrigen = new Vector2Int(
                    parentEntrance.Item2.x - selectedEntrance.Item1.Position.x,
                    room.size.y);
                break;
            case 1: // West
                currentRoomOrigen = new Vector2Int(
                    -parentRoom.size.x,
                    parentEntrance.Item2.y - selectedEntrance.Item1.Position.y);
                break;
            case 2: // South
                currentRoomOrigen = new Vector2Int(
                    parentEntrance.Item2.x - selectedEntrance.Item1.Position.x,
                    -parentRoom.size.y);
                break;
            case 3: // East
                currentRoomOrigen = new Vector2Int(
                    room.size.x,
                    parentEntrance.Item2.y - selectedEntrance.Item1.Position.y);
                break;
        }
        // Applies the parents origen to that offset
        currentRoomOrigen += currentComponentTilemap.rooms[parentNode].Item1;

        // Moves the room away from the entrance untill it fits in the tilemap
        int offset = 0;
        bool overlap = true;
        while (overlap)
        {
            bool overlapInCurrentCycle = false;
            for (int i = 0; i < room.ground.Length; i++)
            {
                Vector3Int checkTilePos = room.ground[i].Position +
                    (Vector3Int)currentRoomOrigen +
                    (offset * neighbourDirs[parentEntrance.Item1]);
                bool temp = testGround.HasTile(checkTilePos);
                if (temp)
                {
                    overlapInCurrentCycle = true;
                    break;
                }
            }
            overlap = overlapInCurrentCycle;
            offset++;
        }

        // applies that offset, so the room can be made 
        currentRoomOrigen += (Vector2Int)(offset * neighbourDirs[parentEntrance.Item1]);

        // Gets the component(local) position of the entrance
        Vector2Int parentEntrancePos = currentComponentTilemap.rooms[parentNode].Item1 + parentEntrance.Item2;
        Vector2Int childEntrancePos = (Vector2Int)selectedEntrance.Item1.Position + currentRoomOrigen;

        PlaceCorridor(childEntrancePos, parentEntrancePos, (byte)selectedEntrance.Item2, (byte)parentEntrance.Item1);

        LoadRoom(testGround, testWalls, testDecor, currentRoomOrigen, room); // Spawns the room

        // Saves the room and the origen of it (local in the component)
        currentComponentTilemap.rooms[nodeIndex] = (currentRoomOrigen, room);

        // Corrects the amout of free entrances
        List<(SavedTile, int)> newEntrances = room.metaInformation.AllEntrances;
        for (int i = 0; i < newEntrances.Count; i++) // Adds the entrances from the new room 
        {
            if (nodeEntanceIndex == i) { continue; }
            (int, Vector2Int) temp = (newEntrances[i].Item2, (Vector2Int)newEntrances[i].Item1.Position);
            currentComponentTilemap.freeEntrances[nodeIndex].Add(temp);
        }
        currentComponentTilemap.freeEntrances[parentNode].RemoveAt(parentEntranceIndex); // Removes the used enteance
    }

    private void PlaceRandomRoom(int nodeIndex, int parentNode, int degree, int parentEntranceIndex) // RET DENNE KODE, SÅ DEN BRUGER PLACECORRIDOR()
    {
        (int, Vector2Int) parentEntrance = currentComponentTilemap.freeEntrances[parentNode][parentEntranceIndex];

        // Finds the needed entrance id
        byte neededEntrenceId = (byte)((parentEntrance.Item1 + 2) % 4);

        // MAKE IT POSSIBLE TO MAKE TURNS IN THE TREE

        // Gets a random new room, with the specified characteritics
        ScriptableRoom currentRoom = GetRandomRoom(neededEntrenceId, degree, compositeAdjecenyList[nodeIndex].type); 

        List<(SavedTile, int)> entrances = currentRoom.metaInformation.AllEntrances;

        // Gets the other entrance that connects to the parent entence
        (SavedTile, int) selectedEntrance = entrances.Where((e, i) => e.Item2 == neededEntrenceId).First();
        int currentEntranceIndex = entrances.IndexOf(selectedEntrance);

        // If there isn't a entrance that connects to the parent entrance, then return
        if (currentEntranceIndex == -1) { Debug.Log("FUCK"); return; }

        PlaceRoom(nodeIndex, parentNode, parentEntranceIndex, currentEntranceIndex, currentRoom);
    }

    // Loads a room onto the test tilemaps
    public void LoadRoom(Tilemap ground, Tilemap walls, Tilemap decor, Vector2Int inputPos, ScriptableRoom room)
    {
        Vector3Int origenPos = new Vector3Int(inputPos.x, inputPos.y, 0);

        foreach (SavedTile tile in room.ground)
        {
            ground.SetTile(tile.Position + origenPos, tile.tile);
        }
        foreach (SavedTile tile in room.walls)
        {
            walls.SetTile(tile.Position + origenPos, tile.tile);
        }
        foreach (SavedTile tile in room.decorations)
        {
            decor.SetTile(tile.Position + origenPos, tile.tile);
        }
    }

    // Removes all the tiles from a tilemap
    public void ClearMap()
    {
        Tilemap[] maps = FindObjectsOfType<Tilemap>();

        foreach (Tilemap map in maps)
        {
            map.ClearAllTiles();
        }
    }
    
    // Called from a entrance trigger, spawn enemies in a room
    public void SpawnEnemies(int nodeIndex)
    {
        if (!spawnedEnemies[nodeIndex]) // Checks if the enemies in a room has been spawned
        {
            // Spawns each enemy
            for (int i = 0; i < currentComponentTilemap.rooms[nodeIndex].Value.enemySpawnPoints.Count; i++)
            {
                Instantiate(currentComponentTilemap.rooms[nodeIndex].Value.enemySpawnPoints[i].enemy, currentComponentTilemap.rooms[nodeIndex].Value.enemySpawnPoints[i].spawnPoint + currentComponentTilemap.rooms[nodeIndex].Key, Quaternion.identity);
            }
        }
        
        spawnedEnemies[nodeIndex] = true;
    }

    #endregion*/
}

[Serializable]
public class BaseTileTypePair
{
    public BaseTile tile;
    public TileType type;
}

public class ComponentTilemap
{
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

        //PlaceCorridor(childEntrancePos, parentEntrancePos, (byte)childEntrance.Item1, (byte)parentEntrance.Item1);
        Vector3Int[] path = GetStraightPath((Vector3Int)childEntrancePos, (Vector3Int)parentEntrancePos);
        PlaceCorridor(path);
        
        List<Vector3Int> overlapPath = path.ToList();
        overlapPath.RemoveRange(0, 3);
        overlapPath.RemoveRange(overlapPath.Count - 4, 3);
        if (CheckOverlap(overlapPath.ToArray(), 2)) { return false; }

        if (LoadRoom(childRoomOrigen, childRoom)) { return false; } // Spawns the room

        // Saves the room and the origen of it (local in the component)
        rooms[childIndex] = (childRoomOrigen, childRoom);

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
            return TileType.WestWall;
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
            startPosition.x += -Math.Sign(difference.x);
        }

        for (; difference.y != 0; difference.y += -Math.Sign(difference.y))
        {
            path.Add(startPosition);
            startPosition.y += -Math.Sign(difference.y);
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

    public void AStarCorridorGeneration((int, Vector2Int) parentEntrance, (int, Vector2Int) childEntrance)
    {
        Vector3Int parentDirection = neighbourDirs[parentEntrance.Item1];
        Vector3Int parentPosition = (Vector3Int)parentEntrance.Item2;
        Vector3Int startPos = parentPosition + parentDirection * 3;

        Vector3Int childDirection = neighbourDirs[childEntrance.Item1];
        Vector3Int childPosition = (Vector3Int)childEntrance.Item2;
        Vector3Int endPos = childPosition + childDirection * 3;

        AStarTilemapSearch search = new AStarTilemapSearch(AStarCheckOverlap);

        List<Vector3Int> path1 = search.AStarPathFinding(startPos, endPos, out float path1Cost);
        search.tilemap.ClearAllTiles();

        List<Vector3Int> path2 = search.AStarPathFinding(endPos, startPos, out float path2Cost);
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
    SouthEastOutwardsCorner
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

    public AStarTilemapSearch(Func<Vector3Int,bool> overlapFunction)
    {
        this.tilemap = new Tilemap();
        this.overlapFunction = overlapFunction;
    }

    public List<Vector3Int> AStarPathFinding(Vector3Int start, Vector3Int end, out float finalFCost)
    {
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
            foreach (Vector3Int neighbourDir in neighbourDirs)
            {
                Vector3Int neighborPos = currentNode + neighbourDir; // Gets the pos of the neighbour

                if (overlapFunction(neighborPos)) { continue; }

                // Marks the neighbour as visited, and with a G-, H-, Fcost and parent.
                AStarSearchTile currentTile = new AStarSearchTile();

                float newGCost = tilemap.GetTile<AStarSearchTile>(currentNode).gCost;
                newGCost += GetDeltaGCost(neighbourDir, currentNode);

                currentTile.gCost = newGCost;
                currentTile.hCost = GetHCost(neighborPos);
                currentTile.parent = currentNode;
                //currentTile.color = Color.green;
                //currentTile.sprite = pixel;
                tilemap.SetTile(neighborPos, currentTile);

                queue.Enqueue(neighborPos, currentTile.fCost); // Adds that neighbour to the queue 
            }

            if (currentNode == end) { break; } // If we have reached the goal, then stop

            // If we have looked at more than 10000 nodes, then stop
            if (count > 5000)
            {
                Debug.Log("Could not find any path using A*!");
                tilemap.ClearAllTiles();
                break;
            }
            count++;
        }

        //Sig: Does the traceback to find the positions for tiles

        List<Vector3Int> tracebackTiles = new List<Vector3Int>();
        Vector3Int currentTracebackTile = end;

        do
        {
            tracebackTiles.Add(currentTracebackTile);
            currentTracebackTile = tilemap.GetTile<AStarSearchTile>(currentTracebackTile).parent;
        }
        while (currentTracebackTile != start);

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