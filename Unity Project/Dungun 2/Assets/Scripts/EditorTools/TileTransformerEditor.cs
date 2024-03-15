using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[CustomEditor(typeof(TileTransformer))]
public class TileTransformerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TileTransformer script = (TileTransformer)target;

        if (GUILayout.Button("Tranform Tile!"))
        {
            foreach(Tile tile in script.template)
            {
                script.TranformTileToBaseTile(tile, script.type);
            }
        }
    }
}
