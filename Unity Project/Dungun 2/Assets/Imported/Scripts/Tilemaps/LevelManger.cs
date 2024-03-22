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
    
    //private bool[] spawnedEnemies; // An array denoting if there has been spawned enemies in a room yet
    //public GameObject entranceTrigger; // A prefab containing an entance trigger

    bool unavoidableOverlap = false; // A bool denoting if there is a unavoidable overlap while placing the last room
    //List<List<List<List<int>>>> sortedRooms; // An array containing pointers to allrooms, which is sorted in the lists after __
    ScriptableRoom[] allRooms; // An array containing all the rooms saved 

    //Sig: Key is indexed with first byte being 0, second byte being the value of the roomType casted to a byte, third byte being the number of entrances, and the fourth byte being the direction of one of the entances.
    //Sig: North -> 0, West -> 1, South -> 2, East -> 3
    Dictionary<uint, List<int>> RoomIndexLookupMap;

    private IEnumerator aStarCorotine;

    public int levelNumber;

    public Stack<(Vector2Int, Vector2Int, int, int)> aStarStack = new Stack<(Vector2Int, Vector2Int, int, int)>();

    //Sig: Generates a level from a level index
    public void GenerateLevel(int levelIndex)
    {
        //Sig: Pulls a graph from the saved ones
        int NumOfGraphs = Resources.LoadAll<ScriptableLevelGraph>($"Graphs/Level {levelIndex}").Length;
        int tempindex = UnityEngine.Random.Range(0, NumOfGraphs);
        levelGraph = Resources.Load<ScriptableLevelGraph>($"Graphs/Level {levelIndex}/Graph {tempindex}");

        //Sig: Initializes the corutine and clears the tilemap
        if(aStarCorotine != null) { StopCoroutine(aStarCorotine); }
        AStarTilemap.ClearAllTiles();

        Debug.Log($"Retrived graph from level {levelIndex} num {tempindex}");

        InitalizeRoomLists(); //Sig: Makes an array filled with pointers to a room array
        ClearMap(); // Removes all tiles

        levelGraph.Initalize(); // Makes the composite adjeceny list 
        compositeAdjecenyList = levelGraph.compositeAdjecenyList;

        // Initialization
        unavoidableOverlap = false;
        //spawnedEnemies = Enumerable.Repeat(false, compositeAdjecenyList.Count).ToArray();
        visited = Enumerable.Repeat(false, compositeAdjecenyList.Count).ToArray();

        // Goes through the first dfs cycle for each of the nodes
        for (int i = 0; i < visited.Length; i++)
        {
            if (visited[i]) continue;

            bool[] lastVisitedAr = visited;
            startCycleNode = i;

            ClearMap();

            visited[i] = true;

            ScriptableRoom firstRoom = GetRandomRoom(compositeAdjecenyList[i].type, (byte)levelGraph.adjecenyList[i].connections.Count); // Gets a randrom first room
            firstRoom.InitializeMetaInformation();

            currentComponentTilemap = new ComponentTilemap(compositeAdjecenyList.Count); // Initalizes the current component tilemap
            currentComponentTilemap.rooms[i] = (new Vector2Int(), firstRoom); // Adds the origen and room to the component

            LoadRoom(testGround, testWalls, testDecor, new Vector2Int(), firstRoom); // Loads the room to the test tilemaps

            RoomMetaInformation info = firstRoom.metaInformation;
            foreach((SavedTile, int) entrance in info.AllEntrances)
            {
                currentComponentTilemap.freeEntrances[i].Add((entrance.Item2, (Vector2Int)entrance.Item1.Position));
            }

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

                    List<(int, Vector2Int)> CurrentNodeEntrances = currentComponentTilemap.freeEntrances[currentCycleNode];
                    List<(int, Vector2Int)> StartNodeEntrances = currentComponentTilemap.freeEntrances[startCycleNode];

                    aStarStack.Push((
                        CurrentNodeEntrances[0].Item2 + currentComponentTilemap.rooms[currentCycleNode].Item1,
                        StartNodeEntrances[0].Item2 + currentComponentTilemap.rooms[startCycleNode].Item1,
                        CurrentNodeEntrances[0].Item1,
                        StartNodeEntrances[0].Item1
                    ));

                    //StartCoroutine(aStarCorotine);
                }
            }

            if (unavoidableOverlap) // Checks if the current component is valid
            {
                // If it isn't try to make the level again
                i--;
                visited = lastVisitedAr;
            }
            else
            {
                currentComponentTilemap.decoration = GetTilesFromMap(testDecor).ToList();
                currentComponentTilemap.walls = GetTilesFromMap(testWalls).ToList();
                currentComponentTilemap.ground = GetTilesFromMap(testGround).ToList();
                // If the level is vaild build the navmesh, and place the triggeres for enemyspawns
                allComponents.Add(currentComponentTilemap);
            }
        }

        // PLACE TRIGGERS 

        //Debug.Log("Triggered");
        //navMesh.BuildNavMesh();
        // Gets all the tiles in the tilemaps
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
        }
    }

    #region RoomManagement
    // Initalizes the sortedrooms and allrooms
    private void InitalizeRoomLists()
    {
        allRooms = Resources.LoadAll<ScriptableRoom>("Rooms");

        RoomIndexLookupMap = new Dictionary<uint, List<int>>();

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
            if (!RoomIndexLookupMap.ContainsKey(key)) { RoomIndexLookupMap[key] = new List<int>(); }
            RoomIndexLookupMap[key].Add(roomIndex);
        }
    }

    // Gets a random room with the entrance direction, entance amount and room type. 
    private ScriptableRoom GetRandomRoom(byte entranceDir, int entranceNum, RoomType type)
    {
        uint key = ((uint)type << (8 * 2)) | ((uint)entranceNum << 8) | ((uint)entranceDir);

        if (!RoomIndexLookupMap.ContainsKey(key)) { 
            Debug.LogError($"Missing a room of type {type}, entrance direction {entranceDir} and {entranceNum} entrances."); 
        }
        
        // Gets a random room with the qualites that are requiered
        int count = RoomIndexLookupMap[key].Count;
        int index = UnityEngine.Random.Range(0, count);
        int roomIndex = RoomIndexLookupMap[key][index];
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
        (int, Vector2Int) parentEntrance = freeParentEntances[parentEntranceIndex]; // Finds the parent entrance

        PlaceRoom(nodeIndex, parentNode, levelGraph.adjecenyList[nodeIndex].connections.Count, parentEntrance, parentEntranceIndex);

        // Goes through all the neighbours and calls this function on them, DFS style
        foreach(int neighbour in compositeAdjecenyList[nodeIndex].connections)
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

        //Sig: If more than half of the cycle is has been looked at, then try and connect the start and current rooms together, by picking an exit that is closest to the start room.
        if (length/2 <= depth)
        {
            (Vector2Int, ScriptableRoom) startRoom = currentComponentTilemap.rooms[startCycleNode];
            Vector2Int goal = startRoom.Item1 + startRoom.Item2.size / 2;
            parentEntranceIndex = FindClosestEntrance(goal, parentNode);
        }
        else
        {
            // Finds the index of a random entrance that belongs to the parent room
            parentEntranceIndex = UnityEngine.Random.Range(0, freeParentEntances.Count); 
        }
        
        (int, Vector2Int) parentEntrance = freeParentEntances[parentEntranceIndex]; // Finds the parent entrance

        PlaceRoom(nodeIndex, parentNode, levelGraph.adjecenyList[nodeIndex].connections.Count, parentEntrance, parentEntranceIndex);
                
        for(int i = 0; i < neighbours.Count; i++)
        {
            if (!visited[neighbours[i]])
            {
                visited[neighbours[i]] = true;
                PlaceCycle(neighbours[i], nodeIndex, depth + 1);
            }
        }
    }

    //Sig: Finds the closest free entrance to a point
    int FindClosestEntrance(Vector2Int goal, int roomIndex)
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

        return result;
    }

    #endregion

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
                    testWalls.SetTile(tiles[3], eastWallTile);
                    testWalls.SetTile(tiles[4], westWallTile);
                }
                else if (parentEntranceDir == 1 || parentEntranceDir == 3) // West and East
                {
                    testWalls.SetTile(tiles[3], northWallTile);
                    testWalls.SetTile(tiles[4], southWallTile);
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

    public IEnumerator AStarCorridorGeneration(Vector2Int parentEntrancePos, Vector2Int childEntrancePos, int parentEntranceDir, int childEntranceDir)
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

        chosenPath.Insert(0, (Vector3Int)parentEntrancePos + neighbourDirs[parentEntranceDir] * 2);
        chosenPath.Insert(0, (Vector3Int)parentEntrancePos + neighbourDirs[parentEntranceDir]);
        chosenPath.Add((Vector3Int)childEntrancePos + neighbourDirs[parentEntranceDir] * 2);
        chosenPath.Add((Vector3Int)childEntrancePos + neighbourDirs[parentEntranceDir]);

        //prefabricationTilemap.ClearAllTiles();

        MakeCorridor(chosenPath.ToArray());

        /*
        foreach(Vector3Int tile in allTilePos)
        {
            bool up = prefabricationTilemap.HasTile(tile + Vector3Int.up),
                down = prefabricationTilemap.HasTile(tile + Vector3Int.down),
                right = prefabricationTilemap.HasTile(tile + Vector3Int.right),
                left = prefabricationTilemap.HasTile(tile + Vector3Int.left),
                upperRight = prefabricationTilemap.HasTile(tile + Vector3Int.up + Vector3Int.right),
                upperLeft = prefabricationTilemap.HasTile(tile + Vector3Int.up + Vector3Int.left),
                lowerRight = prefabricationTilemap.HasTile(tile + Vector3Int.down + Vector3Int.right),
                lowerLeft = prefabricationTilemap.HasTile(tile + Vector3Int.down + Vector3Int.left);
            // NormalWalls 
            if (right && upperRight && lowerRight && !left)
            {
                testWalls.SetTile(tile, leftWallTile);
            }
            else if (left && upperLeft && lowerLeft && !right)
            {
                testWalls.SetTile(tile, rightWallTile);
            }
            else if (up && upperLeft && upperRight && !down)
            {
                testWalls.SetTile(tile, bottomWallTile);
            }
            else if (down && lowerLeft && lowerRight && !up)
            {
                testWalls.SetTile(tile, topWallTile);
            }
            // Outwards Corners
            else if ((!lowerLeft) && up && right && upperRight && upperLeft && lowerRight)
            {
                testWalls.SetTile(tile, upperRightWallTile);
            }
            else if ((!lowerRight) && up && left && upperLeft && upperRight && lowerLeft)
            {
                testWalls.SetTile(tile, upperLeftWallTile);
            }
            else if ((!upperLeft) && down && right && lowerRight && lowerLeft && upperRight)
            {
                testWalls.SetTile(tile, lowerRightWallTile);
            }
            else if ((!upperRight) && down && left && lowerLeft && lowerRight && upperLeft)
            {
                testWalls.SetTile(tile, lowerLeftWallTile);
            }
            // Inwards corners
            else if ((!lowerLeft) && (!lowerRight) && (!upperLeft) && (!left) && (!down) && upperRight)
            {
                testWalls.SetTile(tile, upperRightInwardsWallTile);
            }
            else if ((!lowerRight) && (!lowerLeft) && (!upperRight) && (!right) && (!down) && upperLeft)
            {
                testWalls.SetTile(tile, upperLeftInwardsWallTile);
            }
            else if ((!upperRight) && (!upperLeft) && (!lowerRight) && (!up) && (!right) && lowerLeft)
            {
                testWalls.SetTile(tile, lowerLeftInwardsWallTile);
            }
            else if ((!upperLeft) && (!upperRight) && (!lowerLeft) && (!up) && (!left) && lowerRight)
            {
                testWalls.SetTile(tile, lowerRightInwardsWallTile);
            }
            // Ground tile
            else
            {
                testGround.SetTile(tile, groundTile);
            }

            yield return new WaitForEndOfFrame();
        }

        prefabricationTilemap.ClearAllTiles();
        
        */
        /*void PlaceLastEntranceTiles(Vector3Int[] tiles)
        {
            if ((prefabricationTilemap.HasTile(tiles[0]) || prefabricationTilemap.HasTile(tiles[1])) &&
                ((!prefabricationTilemap.HasTile(tiles[2])) || (!prefabricationTilemap.HasTile(tiles[3]))))
            {
                for (int i = 0; i < tiles.Length; i++)
                {
                    if (!prefabricationTilemap.HasTile(tiles[i]))
                    {
                        prefabricationTilemap.SetTile(tiles[i], groundTile);
                        allTilePos.Add(tiles[i]);
                    }
                }
            }
        }*/

        /*Vector3Int[] AddToArray(Vector3Int[] A, Vector3Int item)
        {
            for (int i = 0; i < A.Length; i++)
            {
                A[i] += item;
            }
            return A;
        }

        void PlaceFirstEntranceTiles(char dir, Vector3Int[] tiles)
        {
            foreach (Vector3Int tile in AddToArray(tiles, -ConvertDirCharToVector(dir)))
            {
                prefabricationTilemap.SetTile(tile, groundTile);
            }

            Vector3Int offset = ConvertDirCharToVector(dir);

            testGround.SetTile(tiles[0], groundTile);
            testGround.SetTile(tiles[1], groundTile);

            for (int j = 0; j < tiles.Length; j++)
            {
                if (!prefabricationTilemap.HasTile(tiles[j] + offset))
                {
                    prefabricationTilemap.SetTile(tiles[j] + offset, groundTile);
                    if (j == 2 || j == 3)
                    {
                        allTilePos.Add(tiles[j] + offset);
                    }
                    else
                    {
                        testGround.SetTile(tiles[j] + offset, groundTile);
                    }
                }
            }

            offset += ConvertDirCharToVector(dir);

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < tiles.Length; j++)
                {
                    if (!prefabricationTilemap.HasTile(tiles[j] + offset))
                    {
                        prefabricationTilemap.SetTile(tiles[j] + offset, groundTile);
                        allTilePos.Add(tiles[j] + offset);
                    }
                }

                offset += ConvertDirCharToVector(dir);
            }
        }
        
        IEnumerable<Vector3Int> GetTilesFromMap(Tilemap map)
        {
            foreach (Vector3Int pos in map.cellBounds.allPositionsWithin)
            {
                if (map.HasTile(pos))
                {
                    yield return pos;
                }
            }
        }*/
    }

    private IEnumerator MakeCorridor(Vector3Int[] corridorPoints)
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

            //Sig: Check inwards corners
            if (isThereAGroundTilePlacedAt[2,2] && isThereAWallTilePlacedAt[1, 2] && isThereAWallTilePlacedAt[2, 1])
            {
                return northWestInwardsCornerWallTile;
            }
            if (isThereAGroundTilePlacedAt[0, 2] && isThereAWallTilePlacedAt[1, 2] && isThereAWallTilePlacedAt[0, 1])
            {
                return northEastInwardsCornerWallTile;
            }
            if (isThereAGroundTilePlacedAt[2, 0] && isThereAWallTilePlacedAt[1, 0] && isThereAWallTilePlacedAt[2, 1])
            {
                return southWestInwardsCornerWallTile;
            }
            if(isThereAGroundTilePlacedAt[0, 0] && isThereAWallTilePlacedAt[1, 0] && isThereAWallTilePlacedAt[0, 1])
            {
                return southEastInwardsCornerWallTile;
            }
            //Sig: Check outwards corners
            if (isThereAGroundTilePlacedAt[0,1] && isThereAGroundTilePlacedAt[1,0] && isThereAWallTilePlacedAt[1,2] && isThereAWallTilePlacedAt[2, 1])
            {
                return southEastOutwardsCornerWallTile;
            }
            if (isThereAGroundTilePlacedAt[2, 1] && isThereAGroundTilePlacedAt[1,0] && isThereAWallTilePlacedAt[1, 2] && isThereAWallTilePlacedAt[0, 1])
            {
                return southWestOutwardsCornerWallTile;
            }
            if (isThereAGroundTilePlacedAt[2, 1] && isThereAGroundTilePlacedAt[1, 2] && isThereAWallTilePlacedAt[1, 0] && isThereAWallTilePlacedAt[0, 1])
            {
                return northWestOutwardsCornerWallTile;
            }
            if (isThereAGroundTilePlacedAt[0, 1] && isThereAGroundTilePlacedAt[1, 2] && isThereAWallTilePlacedAt[1, 0] && isThereAWallTilePlacedAt[2, 1])
            {
                return northEastOutwardsCornerWallTile;
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

    private IEnumerator AStarPathFinding(Vector3Int start, Vector3Int end, out List<Vector3Int> path)
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
            yield return new WaitForEndOfFrame();

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

    private void PlaceRoom(int nodeIndex, int parentNode, int degree, (int, Vector2Int) parentEntrance, int parentEntranceIndex) // RET DENNE KODE, SÅ DEN BRUGER PLACECORRIDOR()
    {
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

        ScriptableRoom parentRoom = currentComponentTilemap.rooms[parentNode].Item2;

        // Gets the position of the new room
        Vector2Int currentRoomOrigen = new Vector2Int();
        switch (parentEntrance.Item1)
        {
            case 0: // North
                currentRoomOrigen = new Vector2Int(
                    parentEntrance.Item2.x - selectedEntrance.Item1.Position.x,
                    currentRoom.size.y);
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
                    (int)currentRoom.size.x, 
                    (int)(parentEntrance.Item2.y - selectedEntrance.Item1.Position.y));
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
            for (int i = 0; i < currentRoom.ground.Length; i++)
            {
                Vector3Int checkTilePos = currentRoom.ground[i].Position + 
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

        PlaceCorridor(childEntrancePos, parentEntrancePos, neededEntrenceId, (byte)parentEntrance.Item1);

        LoadRoom(testGround, testWalls, testDecor, currentRoomOrigen, currentRoom); // Spawns the room

        // Saves the room and the origen of it (local in the component)
        currentComponentTilemap.rooms[nodeIndex] = (currentRoomOrigen, currentRoom);

        // Corrects the amout of free entrances
        List<(SavedTile, int)> newEntrances = currentRoom.metaInformation.AllEntrances;
        for (int i = 0; i < newEntrances.Count; i++) // Adds the entrances from the new room 
        {
            if (currentEntranceIndex == i) { continue; }
            (int, Vector2Int) temp = (newEntrances[i].Item2, (Vector2Int)newEntrances[i].Item1.Position);
            currentComponentTilemap.freeEntrances[nodeIndex].Add(temp);
        }
        currentComponentTilemap.freeEntrances[parentNode].RemoveAt(parentEntranceIndex); // Removes the used enteance
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
    /*
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
    }*/

    #endregion
}

public class ComponentTilemap
{
    public List<SavedTile> ground, walls, decoration;
    public List<List<(int, Vector2Int)>> freeEntrances; // Char: North = 0, West = 1, South = 2, East = 3
    public List<(Vector2Int, ScriptableRoom)> rooms; // Vector2 to hold the origen of the scripableroom

    public ComponentTilemap(int nodes)
    {
        ground = new List<SavedTile>();
        walls = new List<SavedTile>();
        decoration = new List<SavedTile>();
        freeEntrances = new List<List<(int, Vector2Int)>>();
        while (freeEntrances.Count < nodes) { freeEntrances.Add(new List<(int, Vector2Int)>()); }
        rooms = new List<(Vector2Int, ScriptableRoom)>();
        while (rooms.Count < nodes) { rooms.Add((new Vector2Int(), new ScriptableRoom())); }
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