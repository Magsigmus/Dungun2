using Priority_Queue;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

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

    public AStarTilemapSearch(Func<Vector3Int, bool> overlapFunction, Tilemap tilemap)
    {
        this.overlapFunction = overlapFunction;
        //this.tilemap = GameObject.Instantiate(tilemapPrefab);
        this.tilemap = tilemap;
    }

    public List<Vector3Int> AStarPathFinding(Vector3Int start, Vector3Int end, int maxTilesConsidered,
        Dictionary<TileType, List<BaseTile>> lookup, out float finalFCost)
    {
        if(overlapFunction(start) || overlapFunction(end)) { finalFCost = -1;  return new List<Vector3Int>(); }

        //Debug.Log($"Start: {start}, End: {end}");

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
                currentTile.sprite = lookup[TileType.DebugNorth + (i + 2) % 4][0].sprite;
                tilemap.SetTile(neighborPos, currentTile);

                queue.Enqueue(neighborPos, currentTile.fCost); // Adds that neighbour to the queue 
            }

            if (currentNode == end) { break; } // If we have reached the goal, then stop

            // If we have looked at more than 200 nodes, then stop
            if (count > maxTilesConsidered)
            {
                LogFailedPath();
                finalFCost = -1;
                return new List<Vector3Int>();
            }
            count++;
        }

        if(!tilemap.HasTile(end)) 
        {
            LogFailedPath();
            finalFCost = -1;
            return new List<Vector3Int>();
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

        tracebackTiles.Add(start);
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

        void LogFailedPath()
        { 
            /*
            AStarSearchTile errorTile = new AStarSearchTile();

            errorTile.sprite = lookup[TileType.DebugEast].sprite;
            tilemap.SetTile(end, errorTile);

            errorTile.sprite = lookup[TileType.DebugSouth].sprite;
            tilemap.SetTile(start, errorTile);

            GameObject failedPath = GameObject.Instantiate(tilemap.gameObject);
            failedPath.name = $"Failed path between {start} and {end}";
            failedPath.transform.parent = tilemap.gameObject.transform.parent;
            failedPath.SetActive(false);
            */
            Debug.Log($"Could not find any path between the points {start} and {end} using A*!");
            tilemap.ClearAllTiles();
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