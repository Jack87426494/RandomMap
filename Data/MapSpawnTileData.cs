using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class MapSpawnTileData
{
    [Header("生成的物品")]
    public TileBase tile;
    [Header("权重")]
    public float weight;
}
