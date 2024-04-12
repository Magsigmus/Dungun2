using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(TilemapManager))]
public class TilemapManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TilemapManager script = (TilemapManager)target;

        if(GUILayout.Button("Save Room"))
        {
            script.SaveRoom();
        }

        if (GUILayout.Button("Load Room"))
        {
            script.ClearMap();
            script.roomName = script.room.name;
            script.roomType = script.room.type;
            script.enemyPrefabs = script.room.enemies;
            script.LoadRoom(script.position, script.room);
        }

        if (GUILayout.Button("Clear Map"))
        {
            script.ClearMap();
        }
    }
}
