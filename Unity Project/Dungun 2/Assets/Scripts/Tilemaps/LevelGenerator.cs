using NavMeshPlus.Components;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelGenerator
{
    
    //Sig: The tilemaps that are used as prefabs for different tilemaps
    private Tilemap prefabGroundTilemap, prefabWallTilemap, 
        prefabDecorationTilemap, prefabAStarTilemap; 
    
    //Sig: Settings for the level-generation
    public int roomsConsideredInCycle = 3;
    public int maxTilesConsideredInAStar = 200;

    //Sig: A lookup-table for the corresponding tiles to each tile-type
    Dictionary<TileType, BaseTile> tileLookupMap;

    //Sig: Key is indexed with first byte being 0, second byte being the value of the roomType casted to a byte, third byte being the number of entrances, and the fourth byte being the direction of one of the entances.
    //Sig: North -> 0, West -> 1, South -> 2, East -> 3
    Dictionary<uint, List<int>> roomIndexLookupMap;
    ScriptableRoom[] allRooms; // An array containing all the rooms saved 

    //Sig: Diffent graphs used in level generation
    ScriptableLevelGraph levelGraph; //Sig: The origonal graph loaded to make the level
    List<CompositeNode> compositeAdjecenyList; //Sig: The composited graph made from the orignal graph
    List<List<ComponentGraphEdge>> componentAdjecenyList; // Sig: The component graph made from the orignal graph and the composite graph

    bool[] generated; //Sig: An array denoting if a room in the graph has been generated.

    ComponentTilemap currentComponentTilemap; //Sig: The tilemaps of the current component that is being assembeld
    ComponentTilemap[] componentTilemaps; //Sig: An array of tilemaps containing layouts of the generated components

    public LevelGenerator(Tilemap groundTilemap, Tilemap wallTilemap, Tilemap decorationTilemap, 
        Tilemap aStarTilemap, Dictionary<TileType, BaseTile> tileLookupTable)
    {
        prefabGroundTilemap = groundTilemap;
        prefabWallTilemap = wallTilemap;
        prefabDecorationTilemap = decorationTilemap;
        prefabAStarTilemap = aStarTilemap;
        tileLookupMap = tileLookupTable;
    }

    //Sig:
    /// <summary>
    /// A function for generating a level, using the custom level generation system.
    /// </summary>
    /// <param name="levelIndex">The index of the folder of graphs the generator should take from </param>
    /// <returns>The completed tilemap containing the new level</returns>
    public ComponentTilemap GenerateLevel(int levelIndex)
    {
        //Sig: Clears all the tilemaps
        prefabGroundTilemap.ClearAllTiles();
        prefabWallTilemap.ClearAllTiles();
        prefabDecorationTilemap.ClearAllTiles();
        prefabAStarTilemap.ClearAllTiles();

        //Sig: Pulls a graph from the saved ones
        int NumOfGraphs = Resources.LoadAll<ScriptableLevelGraph>($"Graphs/Level {levelIndex}").Length;
        int graphIndex = UnityEngine.Random.Range(0, NumOfGraphs);
        levelGraph = Resources.Load<ScriptableLevelGraph>($"Graphs/Level {levelIndex}/Graph {graphIndex}");

        Debug.Log($"Retrived graph from level {levelIndex} num {graphIndex}");

        //Sig: Initialization of all globally used values
        InitalizeRoomMap(); //Sig: Makes a dictionary that can be used to lookup room indecies. 
        levelGraph.Initalize(); //Sig: Makes the component graph and the composite graph.
        compositeAdjecenyList = levelGraph.compositeAdjecenyList; //Sig: Gets the composite graph 
        componentAdjecenyList = levelGraph.componentAdjecenyList; //Sig: Gets the component graph
        generated = Enumerable.Repeat(false, compositeAdjecenyList.Count).ToArray();
        componentTilemaps = new ComponentTilemap[componentAdjecenyList.Count];

        //Sig: Builds all of the component tilemaps.
        for (int i = 0; i < generated.Length; i++)
        {
            if (generated[i]) { continue; } //Sig: If this node has already been made into a room, then skip it.

            //Sig: Clones the current visited array, to be able to ctrl-Z the produced component
            bool[] previousVisited = (bool[])generated.Clone();

            //Sig: Try to generate a componenttilemap, starting at the i'th node.
            ComponentTilemap newComponentTilemap = BuildComponentTilemap(i, out bool sucess);

            //Sig: If the generation didn't succed, then roll back all changes.
            if (!sucess)
            {
                i--;
                generated = previousVisited;
                continue;
            }

            //Sig: If the gneration did succed, then save the new component to the correct place in the array.
            int componentIndex = levelGraph.nodeComponents[i];
            componentTilemaps[componentIndex] = newComponentTilemap;

            //Sig: Renames the tilemaps for debugging purposes
            newComponentTilemap.groundTilemap.gameObject.name = $"ground of component {componentIndex}";
            newComponentTilemap.wallTilemap.gameObject.name = $"walls of component {componentIndex}";
            newComponentTilemap.decorationTilemap.gameObject.name = $"decoration of component {componentIndex}";
        }

        //Sig: Assembles the component tilemaps to one complete level.
        bool assemblingSucess =
            AssembleComponentTilemaps(out ComponentTilemap completeComponentTilemap);

        //Sig: If the assembling wasn't a sucess, then retry the level generation.
        if (!assemblingSucess) 
        {
            Debug.Log("Assembling of components failed, retrying level generation");
            return GenerateLevel(levelIndex); 
        }

        return completeComponentTilemap;
    }

    //Sig: 
    /// <summary>
    /// Assembles the list of component tilemaps using a BFS on the component graph
    /// </summary>
    /// <param name="completeComponentTilemap">The produced tilemap</param>
    /// <returns>A bool, which is true if the assembling succed, and false if it failed.</returns>
    public bool AssembleComponentTilemaps(out ComponentTilemap completeComponentTilemap)
    {
        //Sig: Initialization of completeComponentTilemap
        completeComponentTilemap = componentTilemaps[0].Clone();

        //Sig: Initialization of the BFS
        Queue<int> componentsToGo = new Queue<int>();
        bool[] componentVisited = Enumerable.Repeat(false, componentAdjecenyList.Count).ToArray();
        componentsToGo.Enqueue(0);
        componentVisited[0] = true;

        bool sucess = true;
        while (componentsToGo.Count > 0)
        {
            //Sig: Gets the first node in the queue.
            int currentNode = componentsToGo.Dequeue();

            List<ComponentGraphEdge> neighbours = componentAdjecenyList[currentNode];
            
            //Sig: If there isn't any neighbours to the current node, then just skip it.
            if (neighbours == null) { continue; }

            //Sig: Combines each neighbour to the current component with the complete component.
            foreach (ComponentGraphEdge neighbour in neighbours)
            {
                //Sig: If the neighbour already has been combined, then skip it.
                if (componentVisited[neighbour.componentIndex]) { continue; }
                componentVisited[neighbour.componentIndex] = true;

                //Sig: Places and connects the component in completeComponentTilemap
                sucess = PlaceComponent(completeComponentTilemap, neighbour);

                //Sig: Add the new node to the BFS queue
                componentsToGo.Enqueue(neighbour.componentIndex);

                //Sig: If the placement of the component didn't succed, then break out.
                if (!sucess) { break; } 
            }

            //Sig: If the placement of the component didn't succed, then break out.
            if (!sucess) { break; }
        }

        //Sig: Delete all of the temporary tilemaps
        for (int i = 0; i < componentTilemaps.Length; i++)
        {
            componentTilemaps[i].DeleteMap();
        }

        //Sig: If the assembling didn't succed, then delete the tilemap containing the failed level and return false.
        if (!sucess)
        {
            completeComponentTilemap.DeleteMap();
            return false;
        }

        //Sig: If the assembling suceeded, then return true.
        return true;
    }

    //Sig:
    /// <summary>
    /// Places a componentTilamap on a another componentTilemap
    /// </summary>
    /// <param name="mainComponentTilemap">The componentTilemap on which there is to be place another componentTilemap</param>
    /// <param name="connectingEdge">The edge connecting the main componentTilemap to the other componentTilemap</param>
    /// <returns>The combined componentTilemap</returns>
    bool PlaceComponent(ComponentTilemap mainComponentTilemap, ComponentGraphEdge connectingEdge, int maxTries = 20)
    {
        //Sig: temporary variables to make the code easier to read.
        int startNodeIndex = connectingEdge.startRoomIndex;
        int endNodeIndex = connectingEdge.endRoomIndex;
        ComponentTilemap neighbourMap = componentTilemaps[connectingEdge.componentIndex];

        List<(int, Vector2Int)> startRoomEntrances = mainComponentTilemap.freeEntrances[startNodeIndex];
        List<(int, Vector2Int)> endRoomEntrances = neighbourMap.freeEntrances[endNodeIndex];

        int startEntranceIndex = 0;

        //Sig: Finds the opposite entrance direction required for connecting to the first entrance.
        //Sig: (Remember that each direction has a index: 0 - North, 1 - West, 2 - South, 3 - East)
        int requiredEntranceDirection = (startRoomEntrances[startEntranceIndex].Item1 + 2) % 4;
        int endEntranceIndex = 0;

        bool sucess = false;
        int counter = 0;
        
        //Sig: Tries all valid combinations of entrance pairs for the two components
        while (!sucess)
        {
            //Sig: Finds the next valid entrance pair
            while (true)
            {
                //Sig: Goes through the list of the entrances of the end room to find a valid entrance
                for (; endEntranceIndex < endRoomEntrances.Count; endEntranceIndex++)
                {
                    if (requiredEntranceDirection == endRoomEntrances[endEntranceIndex].Item1) 
                    { break; }
                }

                //Sig: If there was found a valid entrance in the current end room entrances list, then break.
                if (endEntranceIndex != endRoomEntrances.Count) { break; }
                else
                {
                    //Sig: If there wasn't, then move to the next entrance in the start room.
                    startEntranceIndex++;
                    endEntranceIndex = 0;

                    //Sig: If the end of the list of entrances in the start room has been reached, then return false.
                    if (startEntranceIndex == startRoomEntrances.Count)
                    { 
                        Debug.LogError("Could not find any suitable connection between components!"); 
                        return false; 
                    }

                    //Sig: Update the required entrance
                    requiredEntranceDirection = (startRoomEntrances[startEntranceIndex].Item1 + 2) % 4;
                }
            }

            //Sig: Try to merge the two components, given the found entrance pair.
            sucess = mainComponentTilemap.MergeComponents(neighbourMap, startNodeIndex, startEntranceIndex, endNodeIndex, endEntranceIndex);
            

            //Sig: If the merge wasn't sucessful, then go to next entrance in the end room
            if (!sucess) { endEntranceIndex++; }

            //Sig: Safegaurd. Makes sure that this doesn't become an inifinate loop.
            counter++; 
            if (counter > maxTries) { Debug.LogError("Given up trying to combine components"); break; }
        }

        return sucess;
    }

    //Sig:
    /// <summary>
    /// Generates a component tilemap based on the composite graph.
    /// </summary>
    /// <param name="startingNode">The node in which the algoritm starts with constructing the tilemap</param>
    /// <param name="sucess">True if the build suceeded, but false if it didn't</param>
    /// <returns>The built componentTilemap</returns>
    ComponentTilemap BuildComponentTilemap(int startingNode, out bool sucess)
    {
        startCycleNode = startingNode;
        generated[startingNode] = true;

        //Sig: Gets a random first room
        ScriptableRoom firstRoom = GetRandomRoom(compositeAdjecenyList[startingNode].type,
            (byte)levelGraph.adjecenyList[startingNode].connections.Count);

        //Sig: Initializes the componentTilemap-object
        currentComponentTilemap = new ComponentTilemap(compositeAdjecenyList.Count, tileLookupMap, prefabGroundTilemap, prefabWallTilemap, prefabDecorationTilemap); // Initalizes the current component tilemap
        currentComponentTilemap.rooms[startingNode] = (new Vector2Int(), firstRoom); // Adds the origen and room to the component
        currentComponentTilemap.LoadRoom(new Vector2Int(), firstRoom); // Loads the room to the test tilemaps
        currentComponentTilemap.freeEntrances[startingNode].AddRange(firstRoom.metaInformation.AllEntrances);

        sucess = true;
        //Sig: Runs the first call of DFS for the current node 
        foreach (int neighbour in compositeAdjecenyList[startingNode].connections)
        {
            //Sig: If the current node already has been generated, then skip it.
            if (generated[neighbour]) { continue; }
            generated[neighbour] = true;

            //Sig: If the node still is denoted as being neither a tree or a cycle, then something as gone wrong.
            if (compositeAdjecenyList[startingNode].id[0] == 'N') 
            { 
                Debug.LogError($"Uninitialized {startingNode} node"); 
            }
            else if (compositeAdjecenyList[startingNode].id[0] == 't')
            {
                //Sig: If the node is a part of a tree, then place that tree.
                sucess = PlaceTree(neighbour, startingNode);
            }
            else if (compositeAdjecenyList[startingNode].id[0] == 'c')
            {
                //Sig: If the node is a part of a cycle, then place that cycle.
                sucess = PlaceCycle(neighbour, startingNode, 0);

                if (!sucess) { LogFailure(); break; }

                //Sig: Try to place the corridor from the first room of the cycle to the last room. 
                sucess = sucess && currentComponentTilemap.
                    AStarCorridorGeneration(currentCycleNode, startCycleNode, tileLookupMap,
                    prefabAStarTilemap, maxTilesConsideredInAStar);
            }

            if (!sucess) { LogFailure(); break; }
        }

        return currentComponentTilemap;
    }

    //Sig:
    /// <summary>
    /// Logs a failure during the building of a componentTilemap
    /// </summary>
    void LogFailure()
    {
        currentComponentTilemap.DeleteMap();
        Debug.Log("Detected overlapping level. Retrying");
    }

    #region RoomManagement

    //Sig:
    /// <summary>
    /// Loads all the saved rooms, and makes the map to lookup in list of rooms
    /// </summary>
    private void InitalizeRoomMap()
    {
        //Sig: Loads all the rooms
        allRooms = Resources.LoadAll<ScriptableRoom>("Rooms");

        roomIndexLookupMap = new Dictionary<uint, List<int>>();

        //Sig: Goes through all the rooms, and places pointers to them in the approriate places in the map.
        for (int i = 0; i < allRooms.Length; i++)
        {
            allRooms[i].InitializeMetaInformation(); //Sig: Makes sure that the meta information of the room is initalized

            RoomMetaInformation roomInfo = allRooms[i].metaInformation;
            byte roomTypeIndex = (byte)allRooms[i].type;
            byte entranceNum = (byte)roomInfo.TotalEntances;

            //Sig: If the room has defined entrances in a direction, then add them to that key in the map
            if (roomInfo.NorthEntrances.Count != 0) { AddToDictonary(roomTypeIndex, entranceNum, 0, i); }
            if (roomInfo.WestEntrances.Count != 0) { AddToDictonary(roomTypeIndex, entranceNum, 1, i); }
            if (roomInfo.SouthEntrances.Count != 0) { AddToDictonary(roomTypeIndex, entranceNum, 2, i); }
            if (roomInfo.EastEntrances.Count != 0) { AddToDictonary(roomTypeIndex, entranceNum, 3, i); }
        }

        //Sig: Adds an element to the map
        void AddToDictonary(byte roomType, byte entrances, byte entranceDir, int roomIndex)
        {
            //Sig: Adds the room to all positions with its maximum entrances or lower. 
            for (uint i = 1; i <= entrances; i++)
            {
                //Sig: Generates the key
                uint key = ((uint)roomType << (8 * 2)) | ((uint)i << 8) | ((uint)entranceDir);

                //Sig: Makes a list if nesseray and adds the element to it.
                if (!roomIndexLookupMap.ContainsKey(key)) { roomIndexLookupMap[key] = new List<int>(); }
                roomIndexLookupMap[key].Add(roomIndex);
            }
        }
    }

    //Sig:
    /// <summary>
    /// Gets a list of different rooms, that all meet the given requirements
    /// </summary>
    /// <param name="entranceDirection">The direction in which at least one of the entrances should point</param>
    /// <param name="entranceNum">The minimum number of entrances the rooms should have.</param>
    /// <param name="type">The type of the rooms</param>
    /// <returns>A list of rooms that meet the given requirements</returns>
    private ScriptableRoom[] GetRoomList(byte entranceDirection, int entranceNum, RoomType type)
    {
        //Sig: Gets the key
        uint key = ((uint)type << (8 * 2)) | ((uint)entranceNum << 8) | ((uint)entranceDirection);

        //Sig: If the key doesn't exist in the map, then some rooms are missing.
        if (!roomIndexLookupMap.ContainsKey(key))
        {
            Debug.LogError($"Missing a room of type {type}, entrance direction {entranceDirection} and {entranceNum} entrances.");
            return null;
        }

        //Sig: Converts the indecies of the rooms to the actual room objects.
        return roomIndexLookupMap[key].Select(e => allRooms[e]).ToArray();
    }

    //Sig: 
    /// <summary>
    /// Gets a shuffled room list, that meets the requirements
    /// </summary>
    /// <param name="parentEntranceIndex"> The index of the used entrance in the parent room</param>
    /// <param name="parentIndex">The index of the parent room</param>
    /// <param name="childIndex">The indes of the child room</param>
    /// <param name="length">The amount of entrances that is returned</param>
    /// <param name="childEntranceId">The entrance direction of the child entrance</param>
    /// <returns>A shuffled list of rooms, that contains length</returns>
    private ScriptableRoom[] GetRandomRoomList(int parentEntranceIndex, int parentIndex, int childIndex, int length, out byte childEntranceId)
    {
        System.Random r = new System.Random();
        ScriptableRoom[] rooms = GetRoomList(parentEntranceIndex, parentIndex, childIndex, out childEntranceId);

        return rooms.OrderBy(e => r.Next()).Take(length).ToArray();
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
            if (!generated[neighbour])
            {
                generated[neighbour] = true;
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
        if (length / 2 <= depth)
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

                if (bestRoomDistance > bestEntranceDistance)
                {
                    bestRoomDistance = bestEntranceDistance;
                    childRoom = newRoom;
                    (parentEntranceIndex, childEntranceIndex) = bestEntrancePair;
                }
            }
        }

        bool sucess = currentComponentTilemap.PlaceRoom(nodeIndex, parentNode, parentEntranceIndex, childEntranceIndex, childRoom);
        if (!sucess) { return false; }

        for (int i = 0; i < neighbours.Count; i++)
        {
            if (!generated[neighbours[i]])
            {
                generated[neighbours[i]] = true;
                if (!PlaceCycle(neighbours[i], nodeIndex, depth + 1)) { return false; };
                //cycleRoomGenerationStack.Push((neighbours[i], nodeIndex, depth + 1));
            }
        }

        return true;
    }

    // Return used parent entrance index, used child entrance index, best child entrance index, distance
    (int, int) GetBestEntrancePair(Vector2Int goal, int roomIndex, ScriptableRoom room, int parentEntranceIndex, out float bestDistance)
    {
        (int, int, Vector2Int)[] possibleChildOrigens =
            currentComponentTilemap.GetChildRoomOrigensInComponentSpace(roomIndex, room);

        (int, int) result = (0, 0);
        bestDistance = int.MaxValue;
        int bestEntranceIndex = 0; // Not used, but could be in the future

        foreach ((int, int, Vector2Int) possibleOrigen in possibleChildOrigens)
        {
            for (int i = 0; i < room.metaInformation.TotalEntances; i++)
            {
                if (i == possibleOrigen.Item2 || parentEntranceIndex != possibleOrigen.Item1) { continue; }
                float newDistance = Vector2.Distance(room.metaInformation.AllEntrances[i].Item2 + possibleOrigen.Item3, goal);

                if (newDistance < bestDistance)
                {
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
}
