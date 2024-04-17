using UnityEngine;

[System.Serializable]
public class MapSpawnData
{
    [Header("物品生成的路径")]
    public string resName;
    [Header("生成的物品")]
    public GameObject obj;
    [Header("权重")]
    public float weight;
}
