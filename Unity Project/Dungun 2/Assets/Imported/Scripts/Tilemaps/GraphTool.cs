using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public class GraphTool : MonoBehaviour
{
    public int graphIndex, levelIndex;
    public List<Node> adjecenyList;
#if UNITY_EDITOR
    public void SaveGraph()
    {
        // Creates a new scriptable object for the graph to be saved in
        ScriptableLevelGraph newGraph = ScriptableObject.CreateInstance<ScriptableLevelGraph>();

        // Fills that object with information
        newGraph.adjecenyList = adjecenyList;
        newGraph.levelIndex = levelIndex;
        newGraph.graphIndex = graphIndex;

        // Saves the scriptableObject to disk
        ScriptableObjectUtility.SaveGraphFile(newGraph);
    }
#endif
}
