using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoleSpawnData
{
    /// <summary>
    /// 将要生成的位置
    /// </summary>
    public Vector3 pos;

    /// <summary>
    /// 将要生成什么对象
    /// </summary>
    public GameObject obj;

    public RoleSpawnData(Vector3 pos, GameObject obj)
    {
        this.pos = pos;
        this.obj = obj;
    }
}
