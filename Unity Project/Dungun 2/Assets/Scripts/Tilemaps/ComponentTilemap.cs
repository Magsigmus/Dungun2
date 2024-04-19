using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

//Sig: A class to represent a component of the graph placed on a tilemap.
public class ComponentTilemap
{
    public Vector2Int origin; //Sig: The origen of of this component Tilemap
    public Tilemap groundTilemap, wallTilemap, decorationTilemap; //Sig: References to the different tilemaps
    public List<List<(int, Vector2Int)>> freeEntrances; // Char: North = 0, West = 1, South = 2, East = 3
    public List<(Vector2Int, ScriptableRoom)> rooms; // Vector2 to hold the origen of the scripableroom
    public List<List<SavedTile>> corridorGround, corridorWalls, corridorDecoration;
    public List<Dictionary<(int,Vector2Int),int>> corridorIndecies;
    public Dictionary<TileType, List<BaseTile>> tileLookupTable = new Dictionary<TileType, List<BaseTile>>();
    public List<List<(int, Vector2Int)>> usedEntrances;

    // Init of the ways the A* algorithm can move
    Vector3Int[] neighbourDirs = new Vector3Int[4] {
        new Vector3Int(0, 1), // North
        new Vector3Int(-1, 0), // West
        new Vector3Int(0, -1), // South
        new Vector3Int(1, 0) // East
    };
    
    public ComponentTilemap() { }

    public ComponentTilemap(int nodes, Dictionary<TileType, List<BaseTile>> tileLookupTable)
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
    
    public ComponentTilemap(int nodes, Dictionary<TileType, List<BaseTile>> tileLookupTable, Tilemap ground, Tilemap walls, Tilemap decor)
    {
        this.tileLookupTable = tileLookupTable;
        this.groundTilemap = CloneTilemap(ground, "New Ground Component Tilemap");
        this.wallTilemap = CloneTilemap(walls, "New Walls Component Tilemap");
        this.decorationTilemap = CloneTilemap(decor, "New Decoration Component Tilemap");

        corridorGround = new List<List<SavedTile>>();
        corridorWalls = new List<List<SavedTile>>();
        corridorDecoration = new List<List<SavedTile>>();

        freeEntrances = new List<List<(int, Vector2Int)>>();
        while (freeEntrances.Count < nodes) 
        { freeEntrances.Add(new List<(int, Vector2Int)>()); }
        rooms = new List<(Vector2Int, ScriptableRoom)>();
        while (rooms.Count < nodes) 
        { rooms.Add((new Vector2Int(), new ScriptableRoom())); }
        origin = new Vector2Int();
        corridorIndecies = new List<Dictionary<(int, Vector2Int), int>>();
        while (corridorIndecies.Count < nodes) 
        { corridorIndecies.Add(new Dictionary<(int, Vector2Int), int>()); }
    }

    public ComponentTilemap(ComponentTilemap prefabMap)
    {
        tileLookupTable = prefabMap.tileLookupTable;
        origin = prefabMap.origin;
        groundTilemap = CloneTilemap(prefabMap.groundTilemap);
        wallTilemap = CloneTilemap(prefabMap.wallTilemap);
        decorationTilemap = CloneTilemap(prefabMap.decorationTilemap);
        freeEntrances = prefabMap.freeEntrances.Select(e => e.Select(e => (e.Item1, e.Item2)).ToList()).ToList();
        rooms = prefabMap.rooms.Select(e => (e.Item1, e.Item2)).ToList();

        corridorGround = CloneCorridorTilemap(prefabMap.corridorGround);
        corridorWalls = CloneCorridorTilemap(prefabMap.corridorWalls);
        corridorDecoration = CloneCorridorTilemap(prefabMap.corridorDecoration);

        corridorIndecies = prefabMap.corridorIndecies.Select(e => e.ToDictionary(e => e.Key, e => e.Value)).ToList();

        List<List<SavedTile>> CloneCorridorTilemap(List<List<SavedTile>> map)
        {
            return map.Select(e => e.Select(e => new SavedTile(e.position, e.tile)).ToList()).ToList();
        }

        Tilemap CloneTilemap(Tilemap map){
            GameObject newMap = GameObject.Instantiate(map.gameObject);
            newMap.transform.parent = map.gameObject.transform.parent;
            return newMap.GetComponent<Tilemap>();
        }
    }

    Tilemap CloneTilemap(Tilemap map, string name)
    {
        GameObject newMapObject = GameObject.Instantiate(map.gameObject);
        newMapObject.transform.parent = map.transform.parent;
        newMapObject.name = name;
        return newMapObject.GetComponent<Tilemap>();
    }

    public void ClearMap()
    {
        groundTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
        decorationTilemap.ClearAllTiles();
    }

    public bool LoadRoom(Vector2Int roomOrigen, ScriptableRoom room)
    {
        Vector3Int origenPos = (Vector3Int)roomOrigen;
        if (CheckOverlap(ShiftTiles(room.ground, origenPos).Select(e => e.position).ToArray(), 0)) { return true; }

        SavedTile[] test = ShiftTiles(room.ground, origenPos);

        PlaceTiles(groundTilemap, test);
        PlaceTiles(decorationTilemap, ShiftTiles(room.decorations, origenPos));
        PlaceTiles(wallTilemap, ShiftTiles(room.walls, origenPos));

        return false;

        SavedTile[] ShiftTiles(SavedTile[] tiles, Vector3Int offset)
        { return tiles.Select(e => new SavedTile(e.position + offset, e.tile)).ToArray(); }

        void PlaceTiles(Tilemap map, SavedTile[] tiles)
        { tiles.ToList().ForEach(e => map.SetTile(e.position, e.tile)); }
    }

    public bool PlaceRoom(int childIndex, int parentIndex, int parentEntranceIndex, 
        int childEntanceIndex, ScriptableRoom childRoom)
    {
        (int, Vector2Int) parentEntrance = freeEntrances[parentIndex][parentEntranceIndex];
        (int, Vector2Int) childEntrance = childRoom.metaInformation.AllEntrances[childEntanceIndex];

        (Vector2Int, ScriptableRoom) parentRoom = rooms[parentIndex];

        Vector2Int childRoomOrigen = GetChildRoomOrigenInComponentSpace(parentIndex, childRoom.size, parentEntrance, childEntrance);

        //Sig: Finds and applies a offset, so the room can be made 
        Vector2Int parentEntranceDirection = (Vector2Int)neighbourDirs[parentEntrance.Item1];
        int offset = FindCorrectRoomOffset(childRoom.ground, childRoomOrigen, parentEntranceDirection);
        childRoomOrigen += offset * parentEntranceDirection;

        //Sig: Gets the component-space position of the entrance
        Vector2Int parentEntrancePos = parentRoom.Item1 + parentEntrance.Item2;
        Vector2Int childEntrancePos = childEntrance.Item2 + childRoomOrigen;

        if (LoadRoom(childRoomOrigen, childRoom)) { return false; } // Spawns the room

        // Saves the room and the origen of it (local in the component)
        rooms[childIndex] = (childRoomOrigen, childRoom);

        Vector3Int[] path = GetStraightPath((Vector3Int)childEntrancePos, (Vector3Int)parentEntrancePos);

        if(CheckPathingOverlap(path)) { return false; }

        LogCorridor(parentIndex, childIndex, parentEntrance, childEntrance);
        PlaceTilemapEntranceAndCorridor((parentEntrance.Item1, parentEntrancePos), (childEntrance.Item1, childEntrancePos), path);

        // Corrects the amount of free entrances
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
        foreach (Vector3Int point in points)
        {
            if (GetAdjecentTiles(point, width).
                Select(e => groundTilemap.HasTile(e) || wallTilemap.HasTile(e)).
                Contains(true)) { return true; }
        }

        return false;
    }

    private void PlaceTilemapEntranceAndCorridor((int, Vector2Int) startEntrance, (int, Vector2Int) endEntrance, Vector3Int[] points)
    {
        OpenEntrance(startEntrance);
        OpenEntrance(endEntrance);
        PlaceCorridor(points);
    }

    private void OpenEntrance((int, Vector2Int) entrance)
    {
        Vector3Int dir = neighbourDirs[entrance.Item1];
        dir = new Vector3Int(-dir.y, dir.x, 0);

        groundTilemap.SetTile((Vector3Int)entrance.Item2 + dir * 2, null);
        groundTilemap.SetTile((Vector3Int)entrance.Item2 - dir * 2, null);
        wallTilemap.SetTile((Vector3Int)entrance.Item2 + dir * 2, null);
        wallTilemap.SetTile((Vector3Int)entrance.Item2 - dir * 2, null);

        wallTilemap.SetTile((Vector3Int)entrance.Item2 + dir, null);
        wallTilemap.SetTile((Vector3Int)entrance.Item2, null);
        wallTilemap.SetTile((Vector3Int)entrance.Item2 - dir, null);
    }

    public void PlaceCorridor(Vector3Int[] corridorPoints)
    {
        List<SavedTile> groundTiles = new List<SavedTile>();
        List<SavedTile> wallTiles = new List<SavedTile>();
        List<Vector3Int> unprocessedWallTiles = new List<Vector3Int>();

        for (int i = 0; i < corridorPoints.Length; i++)
        {
            Vector3Int[] newGroundTiles = GetAdjecentTiles(corridorPoints[i], 1);

            foreach (Vector3Int newGroundTile in newGroundTiles)
            {
                groundTiles.Add(new SavedTile(newGroundTile, tileLookupTable[TileType.Ground][0]));
                if (groundTilemap.HasTile(newGroundTile)) { continue; }
                groundTilemap.SetTile(newGroundTile, tileLookupTable[TileType.Ground][0]);
            }
        }

        for (int i = 0; i < corridorPoints.Length; i++)
        {
            Vector3Int[] newTiles = GetAdjecentTiles(corridorPoints[i], 2);

            foreach (Vector3Int newWallTile in newTiles)
            {
                groundTiles.Add(new SavedTile(newWallTile, tileLookupTable[TileType.Ground][0]));
                if (groundTilemap.HasTile(newWallTile)) { continue; }
                unprocessedWallTiles.Add(newWallTile);
                wallTilemap.SetTile(newWallTile, tileLookupTable[TileType.WestWall][0]);

                groundTilemap.SetTile(newWallTile, tileLookupTable[TileType.Ground][0]);
            }
        }

        foreach (Vector3Int newWallTile in unprocessedWallTiles)
        {
            wallTiles.Add(new SavedTile(newWallTile, PickCorrectTile(newWallTile)));
            wallTilemap.SetTile(newWallTile, PickCorrectTile(newWallTile));
        }

        corridorGround.Add(groundTiles);
        corridorWalls.Add(wallTiles);
        corridorDecoration.Add(new List<SavedTile>());
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
        return tileLookupTable[GetCorrectTileType(pos)][0];
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

        Debug.LogWarning("CANT FIND THE FITTING TILE FOR THE REQUESTED AREA!");
        return TileType.Error;
    }

    public Vector3Int[] GetStraightPath(Vector3Int startPosition, Vector3Int endPosition)
    {
        List<Vector3Int> path = new List<Vector3Int>();
        Vector3Int difference = endPosition - startPosition;

        for (; difference.x != 0; difference.x += -Math.Sign(difference.x))
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

    private int FindCorrectRoomOffset(SavedTile[] groundTiles, Vector2Int origen, Vector2Int direction)
    {
        // Moves the room away from the entrance untill it fits in the tilemap
        int offset = 0;
        bool overlap = true;
        while (overlap)
        {
            bool overlapInCurrentCycle = false;
            for (int i = 0; i < groundTiles.Length; i++)
            {
                Vector3Int checkTilePos = groundTiles[i].position +
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

                Vector2Int newRoomOrigen = GetChildRoomOrigenInComponentSpace(parentIndex, childRoom.size, parentEntrance, childEntrance);
                result.Add((i, j, newRoomOrigen));
            }
        }

        return result.ToArray();
    }

    //Sig: UPDATE PARAMETERS
    private Vector2Int GetChildRoomOrigenInComponentSpace(int parentIndex, Vector2Int childSize,
        (int, Vector2Int) parentEntrance, (int, Vector2Int) childEntrance)
    {
        // Gets the position of the new room
        Vector2Int result = parentEntrance.Item2 - childEntrance.Item2;

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

    public bool AStarCorridorGeneration(int parentIndex, int childIndex, Dictionary<TileType, List<BaseTile>> tilelookup, 
        Tilemap aStarTilemap, int maxTilesConsidered = 200)
    {
        List<Vector3Int> startToEnd = GetShortestRoomToRoomPath(parentIndex, childIndex, out float startToEndCost, 
            out (int, Vector2Int) startToEndParentEntrance, out (int, Vector2Int) startToEndChildEntrance), 
            endToStart = GetShortestRoomToRoomPath(childIndex, parentIndex, out float endToStartCost,
            out (int, Vector2Int) endToStartChildEntrance , out (int, Vector2Int) endToStartParentEntrance);
       
        if(startToEndCost == -1 && endToStartCost == -1) { return false; }

        (List<Vector3Int>, (int, Vector2Int), (int, Vector2Int)) endToStartPathInfo =
            (endToStart, endToStartChildEntrance, endToStartParentEntrance);
        (List<Vector3Int>, (int, Vector2Int), (int, Vector2Int)) startToEndPathInfo =
            (startToEnd, startToEndChildEntrance, startToEndParentEntrance);

        (List<Vector3Int>, (int, Vector2Int), (int, Vector2Int)) minPathInfo = 
            (startToEndCost < endToStartCost) ? startToEndPathInfo : endToStartPathInfo;
        
        List<Vector3Int> chosenPath;
        (int, Vector2Int) childEntrance, parentEntrance;
        
        (chosenPath, childEntrance, parentEntrance) =
            (startToEndCost == -1) ? endToStartPathInfo : ((endToStartCost == -1) ? startToEndPathInfo : minPathInfo);

        //groundTilemap.SetTile((Vector3Int)childEntrance.Item2, tileLookupTable[TileType.Error]);
        //groundTilemap.SetTile((Vector3Int)parentEntrance.Item2, tileLookupTable[TileType.Error]);

        OpenEntrance((childEntrance.Item1, childEntrance.Item2 + rooms[childIndex].Item1));
        OpenEntrance((parentEntrance.Item1, parentEntrance.Item2 + rooms[parentIndex].Item1));
        LogCorridor(parentIndex, childIndex, parentEntrance, childEntrance);
        PlaceCorridor(chosenPath.ToArray());

        freeEntrances[parentIndex].Remove(parentEntrance);
        freeEntrances[childIndex].Remove(childEntrance);

        return true;
        
        Vector3Int GetShiftedEntrancePosition(int roomIndex, int entranceIndex, int offset)
        {
            (int, Vector2Int) entrance = freeEntrances[roomIndex][entranceIndex];
            Vector3Int direction = neighbourDirs[entrance.Item1];
            Vector3Int position = (Vector3Int)entrance.Item2;
            return position + direction * offset + (Vector3Int)rooms[roomIndex].Item1;
        }

        List<Vector3Int> GetShortestRoomToRoomPath(int startRoom, int endRoom, out float bestCost, 
            out (int, Vector2Int) bestStartEntrance, out (int, Vector2Int) bestEndEntrance)
        {
            AStarTilemapSearch search =
                new AStarTilemapSearch(pos => CheckOverlap(new Vector3Int[] { pos }, 2), aStarTilemap);
            //search.tilemap.ClearAllTiles();

            List<Vector3Int> path = new List<Vector3Int>();
            bestCost = -1;
            bestStartEntrance = (-1, new Vector2Int());
            bestEndEntrance = (-1, new Vector2Int());
            int bestDistance = -1;

            /*List<(int, Vector3Int)> endRoomEntrances = freeEntrances[endRoom].
                Select((element, index) => (element.Item1, GetShiftedEntrancePosition(endRoom, index, 3))).ToList();
            List<(int, Vector3Int)> startRoomEntrances = freeEntrances[startRoom].
                Select((element, index) => (element.Item1, GetShiftedEntrancePosition(startRoom, index, 3))).ToList();
            */
            for (int i = 0; i < freeEntrances[endRoom].Count; i++)
            {
                Vector3Int endPos = GetShiftedEntrancePosition(endRoom, i, 3);

                freeEntrances[startRoom].Sort((a,b) => 
                    ManhattanDistance((Vector3Int)(a.Item2 + rooms[startRoom].Item1), endPos).
                    CompareTo(ManhattanDistance((Vector3Int)(b.Item2 + rooms[startRoom].Item1), endPos)));

                for (int j = 0; j < freeEntrances[startRoom].Count; j++)
                {
                    Vector3Int startPos = GetShiftedEntrancePosition(startRoom, j, 3);
                    if(bestDistance != -1 && bestDistance < ManhattanDistance(startPos, endPos)) { continue; }
                    
                    List<Vector3Int> newPath = search.
                        AStarPathFinding(startPos, endPos, maxTilesConsidered, tilelookup, out float newCost);

                    if ((bestCost > newCost || bestCost == -1) && newCost != -1) 
                    { 
                        path = newPath;
                        bestCost = newCost;
                        bestDistance = path.Count + 1;

                        bestStartEntrance = (freeEntrances[startRoom][j].Item1, freeEntrances[startRoom][j].Item2);
                        bestEndEntrance = (freeEntrances[endRoom][i].Item1, freeEntrances[endRoom][i].Item2);

                        path.Insert(0, GetShiftedEntrancePosition(startRoom, j, 2));
                        path.Insert(0, GetShiftedEntrancePosition(startRoom, j, 1));
                        path.Insert(0, GetShiftedEntrancePosition(startRoom, j, 0));
                        path.Add(GetShiftedEntrancePosition(endRoom, i, 2));
                        path.Add(GetShiftedEntrancePosition(endRoom, i, 1));
                        path.Add(GetShiftedEntrancePosition(endRoom, i, 0));
                    }

                    search.tilemap.ClearAllTiles();
                }

                search.tilemap.ClearAllTiles();
            }

            return path;
        }
    }

    public void SpawnEntranceTriggers(GameObject entranceTriggerPrefab)
    {
        usedEntrances = new List<List<(int, Vector2Int)>>();
        for(int i = 0; i < rooms.Count; i++) { usedEntrances.Add(new List<(int, Vector2Int)>()); }

        for (int i = 0; i < rooms.Count; i++)
        {
            (Vector2Int, ScriptableRoom) room = rooms[i];
            RoomMetaInformation info = room.Item2.metaInformation;
            foreach ((int, Vector2Int) entrance in info.AllEntrances)
            {
                Vector2Int position = origin + room.Item1 + entrance.Item2;
                if (wallTilemap.HasTile((Vector3Int)position)) { continue; }

                GameObject newTrigger = GameObject.Instantiate(entranceTriggerPrefab);
                newTrigger.transform.position = (Vector2)position + new Vector2(0.5f, 0.5f);
                newTrigger.transform.up = neighbourDirs[GetCorrespondingEntranceDirection(entrance.Item1)];
                EntranceTriggerBehaviour triggerBehaviour = newTrigger.GetComponent<EntranceTriggerBehaviour>();
                triggerBehaviour.roomIndex = i;
                triggerBehaviour.thisEntrance = entrance;
                usedEntrances[i].Add(entrance);
            }
        }
    }

    private void LogCorridor(int parentIndex, int childIndex, (int, Vector2Int) parentEntrance, (int, Vector2Int) childEntrance)
    {
        corridorIndecies[parentIndex].Add(parentEntrance, corridorGround.Count);
        corridorIndecies[childIndex].Add(childEntrance, corridorGround.Count);
    }

    private int ManhattanDistance(Vector3Int point1, Vector3Int point2)
    {
        return Math.Abs(point1.x - point2.x) + Math.Abs(point1.y - point2.y) + Math.Abs(point1.z - point2.z);
    }

    public bool MergeComponents(ComponentTilemap otherComponent, int thisRoomIndex, int thisEntranceIndex, int otherRoomIndex, int otherEntranceIndex)
    {
        Vector2Int localisingVector = this.origin - otherComponent.origin;

        (int, Vector2Int) otherEntrance = otherComponent.freeEntrances[otherRoomIndex][otherEntranceIndex];
        otherEntrance.Item2 += localisingVector;

        (int, Vector2Int) thisEntrance = freeEntrances[thisRoomIndex][thisEntranceIndex];

        Vector2Int thisRoomOffset = (Vector2Int)neighbourDirs[thisEntrance.Item1];
        Vector2Int newOrigen = GetChildRoomOrigenInComponentSpace(thisRoomIndex, otherComponent.GetSize(), thisEntrance, otherEntrance);
        int offset = FindCorrectRoomOffset(GetTilesFromMap(otherComponent.groundTilemap).ToArray(), newOrigen, thisRoomOffset);
        newOrigen += offset * thisRoomOffset;

        bool sucess = LoadComponentTilemap(otherComponent, newOrigen);
        if (!sucess) { return false; }

        Vector3Int thisEntrancePos = (Vector3Int)(thisEntrance.Item2 + rooms[thisRoomIndex].Item1);
        Vector3Int otherEntrancePos = (Vector3Int)(otherEntrance.Item2 + newOrigen);

        Vector3Int[] path = GetStraightPath(thisEntrancePos, otherEntrancePos);
        if (CheckPathingOverlap(path)) { RemoveComponentTilemap(otherComponent, newOrigen); return false; }

        for (int i = 0; i < rooms.Count; i++)
        {
            if ((otherComponent.corridorIndecies[i].Count != 0) && (corridorIndecies[i].Count != 0))
            {
                Debug.LogError("Doubly assigned corridors!");
                //return false;
            }
            if (otherComponent.corridorIndecies[i].Count != 0)
            {
                corridorIndecies[i] = otherComponent.corridorIndecies[i].
                    ToDictionary(e => e.Key, e => e.Value + corridorGround.Count);
            }
        }

        corridorGround.AddRange(Shift2DList((Vector3Int)(newOrigen + localisingVector), otherComponent.corridorGround));
        corridorWalls.AddRange(Shift2DList((Vector3Int)(newOrigen + localisingVector), otherComponent.corridorWalls));
        corridorDecoration.AddRange(Shift2DList((Vector3Int)(newOrigen + localisingVector), otherComponent.corridorDecoration));

        LogCorridor(thisRoomIndex, otherRoomIndex, thisEntrance, otherEntrance);
        PlaceTilemapEntranceAndCorridor((thisEntrance.Item1, (Vector2Int)thisEntrancePos), 
            (otherEntrance.Item1, (Vector2Int)otherEntrancePos), path);

        otherComponent.freeEntrances[otherRoomIndex].RemoveAt(otherEntranceIndex);
        freeEntrances[thisRoomIndex].RemoveAt(thisEntranceIndex);

        for (int i = 0; i < freeEntrances.Count; i++)
        {
            freeEntrances[i].AddRange(otherComponent.freeEntrances[i].
                Select(e => (e.Item1, e.Item2 + localisingVector)));
        }

        for (int i = 0; i < rooms.Count; i++)
        {
            if (rooms[i].Item2.size != new Vector2Int() && otherComponent.rooms[i].Item2.size != new Vector2Int())
            {
                Debug.LogError("Doubly assigned rooms!");
                //return false;
            }

            if(otherComponent.rooms[i].Item2.size == new Vector2Int()) { continue; }

            rooms[i] = (otherComponent.rooms[i].Item1 + localisingVector + newOrigen, otherComponent.rooms[i].Item2);
        }

        return true;

        List<List<SavedTile>> Shift2DList(Vector3Int origen, List<List<SavedTile>> list)
        {
            return list.Select(e => e.Select(e => new SavedTile(e.position + (Vector3Int)newOrigen, e.tile)).ToList()).ToList();
        }
    }

    private bool CheckPathingOverlap(Vector3Int[] path)
    {
        List<Vector3Int> overlapPath = path.ToList();
        if (overlapPath.Count > 6)
        {
            overlapPath.RemoveRange(0, 3);
            overlapPath.RemoveRange(overlapPath.Count - 3, 3);

            if (CheckOverlap(overlapPath.ToArray(), 2)) { return true; }
        }
        return false;
    }

    public bool LoadComponentTilemap(ComponentTilemap map, Vector2Int newRoomOrigen)
    {
        Vector3Int localisingVector = ((Vector3Int)this.origin - (Vector3Int)map.origin) + (Vector3Int)newRoomOrigen; 

        SavedTile[] groundTiles = GetTilesFromMap(map.groundTilemap).ToArray();
        SavedTile[] wallTiles = GetTilesFromMap(map.wallTilemap).ToArray();
        SavedTile[] decorationTiles = GetTilesFromMap(map.decorationTilemap).ToArray();
        groundTiles = groundTiles.Select(e =>
            new SavedTile(e.position + localisingVector, e.tile)).ToArray();
        wallTiles = wallTiles.Select(e =>
            new SavedTile(e.position + localisingVector, e.tile)).ToArray();
        decorationTiles = decorationTiles.Select(e =>
            new SavedTile(e.position + localisingVector, e.tile)).ToArray();

        if (CheckOverlap(groundTiles.Select(e => e.position).ToArray(), 0) ||
            CheckOverlap(groundTiles.Select(e => e.position).ToArray(), 0))
        { return false; }

        foreach(SavedTile tile in groundTiles)
        {
            groundTilemap.SetTile(tile.position, tile.tile);
        }
        foreach (SavedTile tile in wallTiles)
        {
            wallTilemap.SetTile(tile.position, tile.tile);
        }
        foreach (SavedTile tile in decorationTiles)
        {
            decorationTilemap.SetTile(tile.position, tile.tile);
        }

        return true;
    }

    public void RemoveComponentTilemap(ComponentTilemap map, Vector2Int origen)
    {
        Vector3Int localisingVector = (Vector3Int)this.origin - (Vector3Int)map.origin;

        RemoveTilemap(groundTilemap, map.groundTilemap);
        RemoveTilemap(wallTilemap, map.wallTilemap);
        RemoveTilemap(decorationTilemap, map.decorationTilemap);

        void RemoveTilemap(Tilemap thisMap, Tilemap otherMap)
        {
            GetTilesFromMap(otherMap).
                Select(e => e.position + localisingVector + (Vector3Int)origen).
                ToList().ForEach(e => thisMap.SetTile(e, null));
        }
    }

    IEnumerable<SavedTile> GetTilesFromMap(Tilemap map)
    {
        foreach (Vector3Int pos in map.cellBounds.allPositionsWithin)
        {
            if (map.HasTile(pos))
            {
                yield return new SavedTile()
                {
                    position = pos,
                    tile = map.GetTile<BaseTile>(pos)
                };
            }
        }
    }

    Vector2Int GetSize()
    {
        SavedTile[] groundTiles = GetTilesFromMap(groundTilemap).ToArray();
        Vector2Int minPosition = (Vector2Int)groundTiles[0].position, maxPosition = (Vector2Int)groundTiles[0].position;

        foreach(SavedTile tile in groundTiles)
        {
            minPosition.x = Math.Min(tile.position.x, minPosition.x);
            minPosition.y = Math.Min(tile.position.y, minPosition.y);

            maxPosition.x = Math.Max(tile.position.x, maxPosition.x);
            maxPosition.y = Math.Max(tile.position.y, maxPosition.y);
        }

        return maxPosition - minPosition;
    }

    public void DeleteMap()
    {
        GameObject.Destroy(groundTilemap.gameObject);
        GameObject.Destroy(wallTilemap.gameObject);
        GameObject.Destroy(decorationTilemap.gameObject);
    }

    public ComponentTilemap Clone()
    {
        return new ComponentTilemap(this);
    }
}