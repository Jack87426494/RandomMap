using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerateData
{
    //地图大小设置
    public MapSizeType mapSizeType;
    //怪物数量设置
    public EnemyNumType enemyNumType;
    //是否使用随机种子
    public bool isUseRandomSeed;
    //随机种子的值
    public int randomSeed;
    //水的概率设置
    public float waterProbability;
}
