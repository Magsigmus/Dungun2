using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(GraphTool))]
public class GraphToolEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GraphTool script = (GraphTool)target;

        if (GUILayout.Button("Save Graph"))
        {
            script.SaveGraph();
        }
    }
}
