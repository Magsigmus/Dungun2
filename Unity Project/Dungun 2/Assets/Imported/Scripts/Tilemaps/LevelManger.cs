using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.AI;
using System.Linq;
using Priority_Queue;
using Unity.VisualScripting;

public class LevelManger : MonoBehaviour
{
    ScriptableLevelGraph levelGraph; // The uncomposited graph used to make the level
    List<CompositeNode> compositeAdjecenyList; // The composited graph used to make the level
    private bool[] visited; // An array used while going through the composited level graph
    private ComponentTilemap currentComponentTilemap; // The current component that is being assembeld
    private List<ComponentTilemap> allComponents = new List<ComponentTilemap>(); // All the components that have been assembled

    [Header("Tilemaps")]
    public Tilemap testGround, testWalls, testDecor; // The tilemaps used to assemble the components
    public Tilemap AStarTilemap;
    public Tilemap prefabricationTilemap;
    public Sprite pixel;

    [Header("Tiles")]
    public BaseTile groundTile, bottomWallTile, topWallTile, rightWallTile, leftWallTile, 
        upperRightWallTile, upperLeftWallTile, lowerRightWallTile, lowerLeftWallTile, 
        upperRightInwardsWallTile, upperLeftInwardsWallTile, lowerRightInwardsWallTile, lowerLeftInwardsWallTile; // The tiles used to make connections between rooms
    
    private bool[] spawnedEnemies; // An array denoting if there has been spawned enemies in a room yet
    public GameObject entranceTrigger; // A prefab containing an entance trigger

    bool unavoidableOverlap = false; // A bool denoting if there is a unavoidable overlap while placing the last room
    //List<List<List<List<int>>>> sortedRooms; // An array containing pointers to allrooms, which is sorted in the lists after __
    ScriptableRoom[] allRooms; // An array containing all the rooms saved 

    //Sig: Key is indexed with first byte being 0, second byte being the value of the roomType casted to a byte, third byte being the number of entrances, and the fourth byte being the direction of one of the entances.
    //Sig: North -> 0, West -> 1, South -> 2, East -> 3
    Dictionary<uint, List<int>> RoomIndexLoopupMap;

    private IEnumerator aStarCorotine;

    public int levelNumber;

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
        spawnedEnemies = Enumerable.Repeat(false, compositeAdjecenyList.Count).ToArray();
        visited = Enumerable.Repeat(false, compositeAdjecenyList.Count).ToArray();

        // Goes through the first dfs cycle for each of the nodes
        for (int i = 0; i < visited.Length; i++)
        {
            if (visited[i]) continue;

            bool[] lastVisitedAr = visited;
            //startCycleNode = i;

            ClearMap();

            visited[i] = true;

            ScriptableRoom firstRoom = GetRandomRoom(compositeAdjecenyList[i].type, (byte)levelGraph.adjecenyList[i].connections.Count); // Gets a randrom first room

            currentComponentTilemap = new ComponentTilemap(compositeAdjecenyList.Count); // Initalizes the current component tilemap
            currentComponentTilemap.rooms[i] = (new Vector2Int(), firstRoom); // Adds the origen and room to the component

            LoadRoom(testGround, testWalls, testDecor, new Vector2Int(), firstRoom); // Loads the room to the test tilemaps

            RoomMetaInformation info = firstRoom.metaInformation;
            currentComponentTilemap.freeEntrances[i].AddRange(info.AllEntrances.Select(e => (e.Item2, e.Item1.Position)));

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
                    /*
                    List<(int, Vector2Int)> CurrentNodeEntrances = currentComponentTilemap.freeEntrances[currentCycleNode];
                    List<(int, Vector2Int)> StartNodeEntrances = currentComponentTilemap.freeEntrances[startCycleNode];
                        
                    PlaceCycle(neighbour, i, 0);
                        
                    aStarCorotine = AStarCorridorGeneration(
                        CurrentNodeEntrances[0].Item2 + currentComponentTilemap.rooms[currentCycleNode].Item1,
                        StartNodeEntrances[0].Item2 + currentComponentTilemap.rooms[startCycleNode].Item1,
                        CurrentNodeEntrances[0].Item1,
                        StartNodeEntrances[0].Item1
                    );

                    StartCoroutine(aStarCorotine);*/
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

        Debug.Log("Triggered");
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

        RoomIndexLoopupMap = new Dictionary<uint, List<int>>();

        // Goes through all the rooms and makes pointeres for each of them
        for(int i = 0; i < allRooms.Length; i++)
        {
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
            RoomIndexLoopupMap[key].Add(roomIndex);
        }
    }

    // Gets a random room with the entrance direction, entance amount and room type. 
    private ScriptableRoom GetRandomRoom(byte entranceDir, int entranceNum, RoomType type)
    {
        uint key = ((uint)type << (8 * 2)) | ((uint)entranceNum << 8) | ((uint)entranceDir);

        // Gets a random room with the qualites that are requiered
        int count = RoomIndexLoopupMap[key].Count;
        int index = UnityEngine.Random.Range(0, count);
        int roomIndex = RoomIndexLoopupMap[key][index];
        RoomIndexLoopupMap[key].RemoveAt(index);

        return allRooms[roomIndex];
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
    /*
    int startCycleNode;
    int currentCycleNode;
    private void PlaceCycle(int nodeIndex, int parentNode, int depth)
    {
        currentCycleNode = nodeIndex;
        List<int> neighbours = compositeAdjecenyList[nodeIndex].connections;

        int length = int.Parse(compositeAdjecenyList[nodeIndex].id.Substring(1));
        List<KeyValuePair<char, Vector2>> freeParentEntances = currentComponentTilemap.freeEntrances[parentNode]; // Gets all the free entances that are in the parent room
        int parentEntranceIndex = 0;

        if (Mathf.Floor(((float)length)/2) <= depth)
        {
            Vector2 goal = currentComponentTilemap.rooms[startCycleNode].Key + currentComponentTilemap.rooms[startCycleNode].Value.size / 2;

            float bestDist = Vector2.Distance(freeParentEntances[0].Value + (Vector2)currentComponentTilemap.rooms[parentNode].Key, goal);
            for(int i = 0; i < freeParentEntances.Count; i++)
            {
                float currentDist = Vector2.Distance(freeParentEntances[i].Value + (Vector2)currentComponentTilemap.rooms[parentNode].Key, goal);
                if(currentDist < bestDist)
                {
                    parentEntranceIndex = i;
                }
            }
        }
        else
        {
            
            parentEntranceIndex = UnityEngine.Random.Range(0, freeParentEntances.Count); // Finds the index of a random entrance that belongs to the parent room
        }
        
        KeyValuePair<char, Vector2> parentEntrance = freeParentEntances[parentEntranceIndex]; // Finds the parent entrance

        PlaceRoom(nodeIndex, parentNode, levelGraph.adjecenyList[nodeIndex].connections.Count, parentEntrance, parentEntranceIndex);

        for(int i = 0; i < neighbours.Count; i++)
        {
            if (!visited[neighbours[i]])
            {
                visited[neighbours[i]] = true;
                PlaceCycle(neighbours[i], nodeIndex, depth + 1);
            }
        }
    }*/
    
    #endregion

    #region TilePlacement
    
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

                // Places the ground
                testGround.SetTile(tiles[0], groundTile);
                testGround.SetTile(tiles[1], groundTile);
                testGround.SetTile(tiles[2], groundTile);
                testGround.SetTile(tiles[3], groundTile);

                // Places the walls
                if (parentEntranceDir == 0 || parentEntranceDir == 2) // North and South
                {
                    testWalls.SetTile(tiles[2], leftWallTile);
                    testWalls.SetTile(tiles[3], rightWallTile);
                }
                else if (parentEntranceDir == 1 || parentEntranceDir == 3) // West and East
                {
                    testWalls.SetTile(tiles[2], bottomWallTile);
                    testWalls.SetTile(tiles[3], topWallTile);
                }

                tiles[0] += neighbourDirs[parentEntranceDir];
                tiles[1] += neighbourDirs[parentEntranceDir];
                tiles[2] += neighbourDirs[parentEntranceDir];
                tiles[3] += neighbourDirs[parentEntranceDir];
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

    private IEnumerator AStarCorridorGeneration(Vector2Int parentEntrancePos, Vector2Int childEntrancePos, int parentEntranceDir, int childEntranceDir)
    {
        Vector3Int startPos = (Vector3Int)parentEntrancePos + neighbourDirs[parentEntranceDir];
        Vector3Int endPos = (Vector3Int)childEntrancePos + neighbourDirs[childEntranceDir];

        AStarPathFinding(startPos, endPos);

        yield return new WaitForEndOfFrame();
        /*
        Vector3Int currentBacktrackTile = AStarTilemap.GetTile<AStarSearchTile>(endPos).parent; // Gets the parent of the goal node

        List<Vector3Int> allTilePos = new List<Vector3Int>();

        Vector3Int[] parentCorridor = GetAdjecentPairTiles(parentEntrancePos, parentEntranceDir, true);
        Vector3Int[] childCorridor = GetAdjecentPairTiles(childEntrancePos, childEntranceDir, true);

        PlaceFirstEntranceTiles(childEntranceDir, childCorridor);
        PlaceFirstEntranceTiles(parentEntranceDir, parentCorridor);

        while (true)
        {
            Vector3Int[] corridorTiles = GetAdjecentTiles(currentBacktrackTile); // Gets all the tiles around the current backtrack

            // Places the tiles around the current backtrack
            for (int i = 0; i < corridorTiles.Length; i++)
            {
                if (!prefabricationTilemap.HasTile(corridorTiles[i]))
                {
                    prefabricationTilemap.SetTile(corridorTiles[i], groundTile);
                    allTilePos.Add(corridorTiles[i]);
                }
            }

            currentBacktrackTile = AStarTilemap.GetTile<AStarSearchTile>(currentBacktrackTile).parent; // Gets the next backtrack node

            if (currentBacktrackTile == startPos) { break; } // If we have reached the end, then stop
            yield return new WaitForEndOfFrame();
        }

        AStarTilemap.ClearAllTiles();

        PlaceLastEntranceTiles(AddToArray(parentCorridor, ConvertDirCharToVector(parentEntranceDir) * 5));
        PlaceLastEntranceTiles(AddToArray(childCorridor, ConvertDirCharToVector(childEntranceDir) * 5));

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
        

        void PlaceLastEntranceTiles(Vector3Int[] tiles)
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
        }

        Vector3Int[] AddToArray(Vector3Int[] A, Vector3Int item)
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
        */
        IEnumerable<Vector3Int> GetTilesFromMap(Tilemap map)
        {
            foreach (Vector3Int pos in map.cellBounds.allPositionsWithin)
            {
                if (map.HasTile(pos))
                {
                    yield return pos;
                }
            }
        }
    }

    private IEnumerator AStarPathFinding(Vector3Int start, Vector3Int end)
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
                currentTile.gCost = AStarTilemap.GetTile<AStarSearchTile>(currentNode).gCost + GetDeltaGCost(neighbourDir, neighborPos);
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
                AStarTilemap.ClearAllTiles();
                yield break;
            }
            count++;

            yield return new WaitForEndOfFrame();
        }
                
        float GetHCost(Vector3Int pos)
        {
            return Mathf.Abs(pos.x - end.x) + Mathf.Abs(pos.y - end.y);
        }

        float GetDeltaGCost(Vector3Int dir, Vector3Int node)
        {
            // Tests if the current neighbour node is in line with the parent of the current node.
            Vector3Int parentToCurrentNode = AStarTilemap.GetTile<AStarSearchTile>(node).parent;
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
            Vector3Int[] adjecentTiles = GetAdjecentTiles(pos);

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

    private Vector3Int[] GetAdjecentTiles(Vector3Int pos)
    {
        Vector3Int[] result = new Vector3Int[16];
        result[0] = pos;
        result[1] = new Vector3Int(-1,0,0) + pos;
        result[2] = new Vector3Int(0,-1,0) + pos;
        result[3] = new Vector3Int(-1,-1,0) + pos;

        result[4] = new Vector3Int(-2,1,0) + pos;
        result[5] = new Vector3Int(-1,1,0) + pos;
        result[6] = new Vector3Int(0,1,0) + pos;
        result[7] = new Vector3Int(1,1,0) + pos;

        result[8] = new Vector3Int(-2,0,0) + pos;
        result[9] = new Vector3Int(1,0,0) + pos;

        result[10] = new Vector3Int(-2,-1,0) + pos;
        result[11] = new Vector3Int(1,-1,0) + pos;

        result[12] = new Vector3Int(-2,-2,0) + pos;
        result[13] = new Vector3Int(-1,-2,0) + pos;
        result[14] = new Vector3Int(0,-2,0) + pos;
        result[15] = new Vector3Int(1,-2,0) + pos;
        return result;
    }

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
        freeEntrances = Enumerable.Repeat(new List<(int, Vector2Int)>(), nodes).ToList();
        rooms = Enumerable.Repeat((new Vector2Int(), new ScriptableRoom()), nodes).ToList();
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