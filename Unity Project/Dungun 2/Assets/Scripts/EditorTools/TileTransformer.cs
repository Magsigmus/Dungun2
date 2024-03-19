using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileTransformer : MonoBehaviour
{
    public Tile[] template;
    public TileType type;

    public void TranformTileToBaseTile(Tile template, TileType type)
    {
        BaseTile result = new BaseTile(template);
        result.type = type;
        AssetDatabase.CreateAsset(result, "Assets/Tiles/Tiles/" + template.name + ".asset");
        AssetDatabase.SaveAssets();
    }

}
