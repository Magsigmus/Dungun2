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

        if(GUILayout.Button("Place Next Room (TREE)"))
        {
            (int, int) vals = script.roomGenerationStack.Peek();
            script.roomGenerationStack.Pop();
            script.PlaceTree(vals.Item1, vals.Item2);
        }

        if (GUILayout.Button("Place Next Room (CYCLE)"))
        {
            (int, int, int) vals = script.cycleRoomGenerationStack.Peek();
            script.cycleRoomGenerationStack.Pop();
            script.PlaceCycle(vals.Item1, vals.Item2, vals.Item3);
        }

    }
}