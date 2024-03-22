using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(LevelManger))]
public class LevelMangerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelManger script = (LevelManger)target;

        if (GUILayout.Button("Generate Level"))
        {
            script.GenerateLevel(script.levelNumber);
        }

        if(GUILayout.Button("Clear map"))
        {
            script.ClearMap();
        }

        if (GUILayout.Button("Make A*-corridor") && script.aStarStack.Count != 0)
        {
            (Vector2Int, Vector2Int, int, int) val = script.aStarStack.Peek(); script.aStarStack.Pop();
            script.AStarCorridorGeneration(val.Item1, val.Item2, val.Item3, val.Item4);
        }
        
    }
}
