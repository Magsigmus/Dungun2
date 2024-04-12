using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ScriptableLevelGraph))]
public class ScriptableGraphEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ScriptableLevelGraph script = (ScriptableLevelGraph)target;

        if (GUILayout.Button("Initalize graph"))
        {
            script.Initalize();
        }
    }
}
