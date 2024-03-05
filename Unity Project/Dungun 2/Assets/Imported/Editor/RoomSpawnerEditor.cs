using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(RoomSpawner))]
public class RoomSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        RoomSpawner script = (RoomSpawner)target;

        if (GUILayout.Button("Load Room"))
        {
            script.LoadLocalRoom();
        }
    }
}
