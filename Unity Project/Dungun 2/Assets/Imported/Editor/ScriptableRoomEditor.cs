using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ScriptableRoom))]
public class ScriptableRoomEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ScriptableRoom script = (ScriptableRoom)target;

        if (GUILayout.Button("Initialize information"))
        {
            script.InitializeMetaInformation();
        }

    }
}
