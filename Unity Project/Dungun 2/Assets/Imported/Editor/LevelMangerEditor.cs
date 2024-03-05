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
    }
}
