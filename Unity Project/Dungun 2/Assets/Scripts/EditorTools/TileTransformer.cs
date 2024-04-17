using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileTransformer : MonoBehaviour
{
    public Tile[] template;
    public MetaTileType type;

#if UNITY_EDITOR
    public void TranformTileToBaseTile(Tile template, MetaTileType type)
    {
        BaseTile result = new BaseTile(template);
        result.type = type;
        AssetDatabase.CreateAsset(result, "Assets/Tiles/Tiles/" + template.name + ".asset");
        AssetDatabase.SaveAssets();
    }
#endif
}
