using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ScriptableLevelGraph : ScriptableObject
{
    public int graphIndex, levelIndex;
    public List<Node> adjecenyList;
    public List<CompositeNode> compositeAdjecenyList;

    private int[] parents; // -1 = no parent
    private int[] state; // 0 = not processed, 1= being processed, 2 = processed
    private List<List<List<int>>> cycleAdjecenyLists;

    public int[] nodeComponents;
    public List<List<ComponentGraphEdge>> componentAdjecenyList;

    public void Initalize()
    {
        // Initalizes all the values that is used for finding the cycles
        parents = new int[adjecenyList.Count];
        for(int i = 0; i < parents.Length; i++) { parents[i] = -1; }
        state = new int[adjecenyList.Count];
        for (int i = 0; i < adjecenyList.Count; i++) { state[i] = 0; }
        cycleAdjecenyLists = new List<List<List<int>>>();

        RecursevlyFindLoops(0); // Finds all the "simple" cycles, starting in node 0s
        MakeCompositeCycles(); // Makes the "composite" cycles, by applying an XOR gate to each connection
        compositeAdjecenyList = MakeComposite(); // Makes the final composite graph

        MakeComponentGraph();
    }

    private void MakeComponentGraph()
    {
        nodeComponents = Enumerable.Repeat(-1, adjecenyList.Count).ToArray();

        int currentComponentNumber = 0;
        for (int i = 0; i < compositeAdjecenyList.Count; i++)
        {
            if (nodeComponents[i] != -1) { continue; }
            AssignComponentNumber(i, currentComponentNumber);
            currentComponentNumber++;
        }

        componentAdjecenyList = new List<List<ComponentGraphEdge>>();
        for(int i = 0; i < currentComponentNumber; i++) { componentAdjecenyList.Add(new List<ComponentGraphEdge>()); }
        ConstructComponentGraph(0);
    }
    
    private void ConstructComponentGraph(int startNode)
    {
        bool[] visited = Enumerable.Repeat(false, adjecenyList.Count).ToArray();

        Queue<int> nodes = new Queue<int>();
        nodes.Enqueue(startNode);

        while (nodes.Count > 0)
        {
            int currentNode = nodes.Dequeue();
            List<int> neighbours = adjecenyList[currentNode].connections;

            foreach (int neighbour in neighbours)
            {
                if (visited[neighbour]) { continue; }
                visited[neighbour] = true;

                int neighbourComponent = nodeComponents[neighbour];
                int currentNodeComponent = nodeComponents[currentNode];

                if (neighbourComponent != currentNodeComponent) 
                {
                    componentAdjecenyList[neighbourComponent].Add(new ComponentGraphEdge(currentNodeComponent, neighbour, currentNode));
                    componentAdjecenyList[currentNodeComponent].Add(new ComponentGraphEdge(neighbourComponent, currentNode, neighbour));
                }

                nodes.Enqueue(neighbour);
            }
        }
    }

    private void AssignComponentNumber(int startNode, int componentNumber)
    {
        Queue<int> nodes = new Queue<int>();
        nodes.Enqueue(startNode);
        nodeComponents[startNode] = componentNumber;

        while (nodes.Count > 0) 
        {
            int currentNode = nodes.Dequeue();
            List<int> neighbours = compositeAdjecenyList[currentNode].connections;

            foreach(int neighbour in neighbours)
            {
                if (nodeComponents[neighbour] != -1) { continue; }

                nodeComponents[neighbour] = componentNumber;
                nodes.Enqueue(neighbour);
            } 
        }
    }


    private void RecursevlyFindLoops(int currentNode) // Using color meathod, find cycles
    {
        state[currentNode] = 1; // Sets the state of the node to being processed
        List<int> neighbours = adjecenyList[currentNode].connections; // Finds all the neighbours of this node

        for (int i = 0; i < neighbours.Count; i++) // Goes through all the neighbours
        {
            int currentNeigbour = neighbours[i];
            if (currentNeigbour == parents[currentNode]) { continue; } // Checks if we came from that node

            switch (state[currentNeigbour])
            {
                case 0: // If we haven't visited this neighbour, then recursivly call the function on it
                    parents[currentNeigbour] = currentNode;
                    RecursevlyFindLoops(currentNeigbour);
                    break;
                case 1: // If we found a node that is being processed then we must have found a cycle
                    int currentBacktrackNode = currentNode; // Holds the last node we backtracked to
                    
                    // Initalizes another cycle
                    cycleAdjecenyLists.Add(new List<List<int>>());
                    for (int j = 0; j < adjecenyList.Count; j++)
                    {
                        cycleAdjecenyLists[cycleAdjecenyLists.Count - 1].Add(new List<int>());
                    }

                    // Adds a connection between the current node and the node we just found
                    cycleAdjecenyLists[cycleAdjecenyLists.Count - 1][currentNode].Add(currentNeigbour);
                    cycleAdjecenyLists[cycleAdjecenyLists.Count - 1][currentNeigbour].Add(currentNode);
                    // Backtracks to find the cycle
                    while (currentNeigbour != currentBacktrackNode)
                    {
                        int newCurrentBacktrackNode = parents[currentBacktrackNode]; // Finds the next backtrack node
                        // Adds a connection between the two nodes 
                        cycleAdjecenyLists[cycleAdjecenyLists.Count - 1][currentBacktrackNode].Add(newCurrentBacktrackNode);
                        cycleAdjecenyLists[cycleAdjecenyLists.Count - 1][newCurrentBacktrackNode].Add(currentBacktrackNode);
                        currentBacktrackNode = newCurrentBacktrackNode;
                    }
                    break;
            }
        }
        state[currentNode] = 2; // Sets the node to being fully processed
    }

    private void MakeCompositeCycles() // Make the composite cycles
    {
        int startLength = cycleAdjecenyLists.Count;
        // Goes through all the combinations of simple cycles
        for (int i = 1; i < startLength; i++)
        {
            for (int j = 0; j < startLength && j != i; j++)
            {
                // Initizalizes the new adjeceny list
                List<List<int>> newAdjecenyList = new List<List<int>>();
                for (int l = 0; l < adjecenyList.Count; l++)
                {
                    newAdjecenyList.Add(new List<int>());
                }

                // Goes through all the nodes in the two adjeceny lists
                bool overlap = false;
                for (int k = 0; k < adjecenyList.Count; k++)
                {
                    List<int> connections1 = cycleAdjecenyLists[i][k];
                    List<int> connections2 = cycleAdjecenyLists[j][k];

                    connections1.Sort();
                    connections2.Sort();

                    // Uses two pointers two apply the XOR opperation (which is why the connections need to be sorted)
                    int pointer1 = 0;
                    int pointer2 = 0;
                    while (pointer1 < connections1.Count && pointer2 < connections2.Count) // Goes through until one of the pointers hits the end 
                    {
                        if (connections1[pointer1] == connections2[pointer2]) // If the two pointeres refer to the same number, then it shouldn't be added.
                        {
                            overlap = true;
                            pointer1++;
                            pointer2++;
                        }
                        // If the two pointers dont refer to the same number, then incriment the pointer with the lowest value and add that connection
                        else if (connections1[pointer1] < connections2[pointer2])
                        {
                            newAdjecenyList[k].Add(connections1[pointer1]);
                            pointer1++;
                        }
                        else if (connections1[pointer1] > connections2[pointer2])
                        {
                            newAdjecenyList[k].Add(connections2[pointer2]);
                            pointer2++;
                        }
                    }

                    // Add the rest of the connections, which are garentueed to not have overlap
                    for (; pointer1 < connections1.Count; pointer1++)
                    {
                        newAdjecenyList[k].Add(connections1[pointer1]);
                    }
                    for (; pointer2 < connections2.Count; pointer2++)
                    {
                        newAdjecenyList[k].Add(connections2[pointer2]);
                    }
                }

                // If there was any overlap, i.e. a new cycle, add the cycle
                if (overlap)
                {
                    cycleAdjecenyLists.Add(newAdjecenyList);
                }
            }
        }
    }

    private List<CompositeNode> MakeComposite() // Makes the composite graph
    {
        // Gets the sums of all the connections in each of the cycles, i.e. how long they are
        List<KeyValuePair<int,int>> totalConnections = new List<KeyValuePair<int, int>>();
        for(int i = 0; i < cycleAdjecenyLists.Count; i++)
        {
            int sum = 0;
            for(int j = 0; j < cycleAdjecenyLists[i].Count; j++)
            {
                sum += cycleAdjecenyLists[i][j].Count;
            }
            totalConnections.Add(new KeyValuePair<int, int>(i,sum/2));
        }
        // Sorts the refrences to the cycles after length of the cycles 
        totalConnections.Sort((x, y) => (y.Value.CompareTo(x.Value)));
        totalConnections.Reverse();
        // Initalizes the new composite graph 
        List<CompositeNode> newCompositeGraph = new List<CompositeNode>();
        for(int i = 0; i < adjecenyList.Count; i++)
        {
            newCompositeGraph.Add(new CompositeNode());
            newCompositeGraph[i].type = adjecenyList[i].type;
        }

        // Initalizes the visited list
        List<bool> visited = new List<bool>();
        for(int i = 0; i < adjecenyList.Count; i++)
        {
            visited.Add(false);
        }

        // Goes through each of the cycles in order of length, and adds 
        for(int i = 0; i < cycleAdjecenyLists.Count; i++)
        {
            List<List<int>> currentCycle = cycleAdjecenyLists[totalConnections[i].Key];

            bool validCycle = true;
            for(int j = 0; j < currentCycle.Count; j++)
            {
                if(currentCycle[j].Count > 0 && visited[j])
                {
                    validCycle = false;
                    break;
                }
            }
            if (!validCycle)
            {
                continue;
            }

            for (int j = 0; j < currentCycle.Count; j++)
            {
                // If the node hasn't been visited, then add it's connections to the composite graph
                if(currentCycle[j].Count > 0)
                {
                    visited[j] = true;
                    newCompositeGraph[j].connections = currentCycle[j];
                    newCompositeGraph[j].id = $"c{totalConnections[i].Value}";
                }
            }
        }

        // Adds the rest of the connections between unvisited nodes
        for(int i = 0; i < adjecenyList.Count; i++)
        {
            if (!visited[i])
            {
                for(int j = 0; j < adjecenyList[i].connections.Count; j++)
                {
                    if (!visited[adjecenyList[i].connections[j]])
                    {
                        newCompositeGraph[i].connections.Add(adjecenyList[i].connections[j]);
                    }
                }
            }
        }

        for(int i = 0; i < newCompositeGraph.Count; i++)
        {
            if(newCompositeGraph[i].id == "N")
            {
                newCompositeGraph[i].id = "t";
            }
        }

        return newCompositeGraph;
    }
}

[Serializable]
public class Node
{
    public RoomType type;
    public List<int> connections;

    public Node()
    {
        type = RoomType.Normal;
        connections = new List<int>();
    }
}

[Serializable]
public class CompositeNode : Node
{
    public string id;

    public CompositeNode()
    {
        type = RoomType.Normal;
        connections = new List<int>();
        id = "N";
    }
}

[Serializable]
public class ComponentGraphEdge{
    public int componentIndex, startRoomIndex, endRoomIndex;

    public ComponentGraphEdge(int nodeIndex, int startRoomIndex, int endRoomIndex)
    {
        this.componentIndex = nodeIndex;
        this.startRoomIndex = startRoomIndex;
        this.endRoomIndex = endRoomIndex;
    }
}