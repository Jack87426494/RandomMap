using NavMeshPlus.Components;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapGenerator : MonoBehaviour
{
    [Header("需要在地图上真随机生成的物体")]
    public List<MapSpawnData> mapSpawnDataList;

    [Header("需要在地图上使用柏林噪声生成的物体，序号越小越靠近水源")]
    public List<MapSpawnData> mapSpawnDataPerlinList;

    [Header("地板的瓦片")]
    public List<MapSpawnTileData> groundTileDataList;

    [Header("水的瓦片")]
    public TileBase waterTile;

    [Header("地板地图")]
    public Tilemap groundMap;

    [Header("水地图")]
    public Tilemap waterMap;

    [Header("物品地图")]
    public Tilemap itemMap;

    [Header("地图的宽")]
    public int width;

    [Header("地图的高")]
    public int height;

    [Header("水出现的概率")]
    [Range(0, 1f)]
    public float waterProbability;

    [Header("是否启动自定义随机种子")]
    public bool isUseCustomSeed;

    [Header("自定义随机种子")]
    public int randomSeed;

    [Header("柏林噪声采样的间隙倍数")]
    public float perlinInterval;

    [Header("剔除周围只有一个陆地格子的陆地格子的情况，遍历的次数")]
    public int removeGroundCount;

    [Header("用于烘焙地图")]
    public NavMeshSurface navMeshSurface;

    //存储地图上每一个点的噪声值
    private float[,] mapData;

    //将要生成的可移动角色
    private List<RoleSpawnData> roleSpawnDataList =new List<RoleSpawnData>();

    //单例方便调用
    private static MapGenerator instance;

    public static MapGenerator Instance
    {
        get
        {
            if(instance !=null)
            {
                return instance;
            }
            return null;
        }
    }

    private void Awake()
    {
        instance = this;
    }

    //生成数据
    private void GenerateData()
    {
        //提前清除移动角色数据
        roleSpawnDataList.Clear();

        //如果没有使用随机种子就自动生成一个
        if (!isUseCustomSeed)
        {
            randomSeed = Time.time.GetHashCode();
        }
        //初始化随机种子
        UnityEngine.Random.InitState(randomSeed);

        //初始化地图数据
        mapData = new float[width, height];

        //得到一个随机值
        float randomInt = Random.Range(-10000, 10000);
        //噪声值
        float noiseValue;

        for(int i=1;i<height-1;++i)
        {
            for(int j=1;j<width-1;++j)
            {
                //得到噪声值
                noiseValue = Mathf.PerlinNoise(randomInt + i * perlinInterval, randomInt + j * perlinInterval);
                //设置地图数据
                mapData[i, j] = noiseValue;
            }
        }
    }

    /// <summary>
    /// 创建地图
    /// </summary>
    public void GenerateMap()
    {
        //清除地图
        ClearMap();

        //生成地图数据
        GenerateData();

        //地图数据的处理
        //剔除周围一个同类格子的情况（画面不合理）
        for(int i=0;i<removeGroundCount;++i)
        {
            RemoveOneGround();
        }

        //真随机生成物体的总权重值
        float weightTotal = 0;
        for(int i=0;i<mapSpawnDataList.Count;++i)
        {
            weightTotal += mapSpawnDataList[i].weight;
        }

        //柏林噪声生成物体的总权重值
        float perlinTotal = 0;
        for (int i = 0; i < mapSpawnDataPerlinList.Count; ++i)
        {
            perlinTotal += mapSpawnDataPerlinList[i].weight;
        }

        //地板的总权重值
        float groundTotal = 0;
        for (int i = 0; i < groundTileDataList.Count; ++i)
        {
            groundTotal += groundTileDataList[i].weight;
        }

        //一个格子的权重值
        float nowWeight;
        float temp = 0;
        
        //物体是否生成成功
        bool isGenerateObj = false;
        //地板的噪声值占比
        float perlinGroundper;
        GameObject tempObj;

        //生成地图
        for (int i = 0; i < height; ++i)
        {
            for (int j = 0; j < width; ++j)
            {
                isGenerateObj = false;
                perlinGroundper = (mapData[i, j] - waterProbability) / (1 - waterProbability);
                if (isGround(i, j))
                {
                    //生成地板
                    temp = 0;

                    for (int w = 0; w < groundTileDataList.Count; ++w)
                    {
                        temp += groundTileDataList[w].weight;
                        if ((temp / groundTotal) > perlinGroundper)
                        {
                            groundMap.SetTile(new Vector3Int(i, j), groundTileDataList[w].tile);
                            break;
                        }
                    }
                    

                    //必须是周围八个方位都是陆地的情况
                    if (CheakGroundCountAroundEight(i, j) != 8)
                    {
                        continue;
                    }

                    //真随机的方式生成地图上的物品
                    nowWeight = Random.Range(0, weightTotal);
                    temp = 0;
                    for (int w = 0; w < mapSpawnDataList.Count; ++w)
                    {
                        temp += mapSpawnDataList[w].weight;
                        if (temp > nowWeight)
                        {
                            if( mapSpawnDataList[w].resName == "")
                            {
                                break;
                            }

                            if (mapSpawnDataList[w].obj == null)
                            {
                                mapSpawnDataList[w].obj = Resources.Load<GameObject>(mapSpawnDataList[w].resName);
                            }

                            if (mapSpawnDataList[w].obj == null)
                            {
                                break;
                            }

                            if (mapSpawnDataList[w].obj.tag != "Enemy")
                            {
                                tempObj = Instantiate(mapSpawnDataList[w].obj);
                                tempObj.GetComponent<SpriteRenderer>().sortingOrder = (width - j);
                                tempObj.transform.position = itemMap.GetCellCenterWorld(new Vector3Int(i, j));
                                tempObj.transform.SetParent(itemMap.transform, false);
                            }
                            else
                            {
                                //加入待生成移动角色列表，在烘焙地图过后再生成出来
                                roleSpawnDataList.Add(new RoleSpawnData(itemMap.GetCellCenterWorld(new Vector3Int(i, j)),
                                    mapSpawnDataList[w].obj));
                            }
                            

                            isGenerateObj = true;

                            break;
                        }
                    }

                    //如果已经随机生成了物体就跳过这一个格子
                    if(isGenerateObj)
                    {
                        continue;
                    }

                    //柏林噪声的方式生成地图上的物品
                    temp = 0;
                    for (int w = 0; w < mapSpawnDataPerlinList.Count; ++w)
                    {
                        temp += mapSpawnDataPerlinList[w].weight;
                        if ((temp/perlinTotal) > perlinGroundper)
                        {
                            if(mapSpawnDataPerlinList[w].resName=="")
                            {
                                break;
                            }

                            if(mapSpawnDataPerlinList[w].obj==null)
                            {
                                mapSpawnDataPerlinList[w].obj = Resources.Load<GameObject>(mapSpawnDataPerlinList[w].resName);
                            }

                            if(mapSpawnDataPerlinList[w].obj==null)
                            {
                                break;
                            }

                            if (mapSpawnDataPerlinList[w].obj.tag != "Enemy")
                            {
                                tempObj = Instantiate(mapSpawnDataPerlinList[w].obj);
                                tempObj.GetComponent<SpriteRenderer>().sortingOrder = (width - j);
                                tempObj.transform.position = itemMap.GetCellCenterWorld(new Vector3Int(i, j));
                                tempObj.transform.SetParent(itemMap.transform, false);
                            }
                            else
                            {
                                //加入待生成移动角色列表，在烘焙地图过后再生成出来
                                roleSpawnDataList.Add(new RoleSpawnData(itemMap.GetCellCenterWorld(new Vector3Int(i, j)),
                                    mapSpawnDataPerlinList[w].obj));
                            }

                            break;
                        }
                    }

                }
                else
                {
                    //生成背景
                    waterMap.SetTile(new Vector3Int(i, j), waterTile);
                }
                
            }
        }

        //烘焙地图
        Invoke("BuildNavMeshAsync", 1f);
    }

    private void BuildNavMeshAsync()
    {
        navMeshSurface.BuildNavMesh();
        //根据数据生成可移动的角色
        GenerateRole();
    }

    //根据数据生成可移动的角色
    private void GenerateRole()
    {

        GameObject tempObj;
        BaseRole tempRole;
        for (int i=0;i<roleSpawnDataList.Count;++i)
        {
            tempObj = Instantiate(roleSpawnDataList[i].obj);
            tempRole = tempObj.GetComponent<BaseRole>();

            //方便调试
            if(Application.isPlaying)
            {
                tempRole.isDragIn = false;
            }
            else
            {
                tempRole.isDragIn = true;
            }

            tempRole.Init((obj) =>
            {
                obj.agent.enabled = false;
                tempObj.transform.position = roleSpawnDataList[i].pos;
                obj.agent.enabled = true;
            });
            tempRole.deadCAction += (role) =>
            {
                UIMgr.Instance.GetPanel<GamePanel>().SetRemain();
            };

            tempObj.transform.SetParent(itemMap.transform, false);
        }
    }


    /// <summary>
    /// 是否是陆地
    /// </summary>
    /// <param name="i"></param>
    /// <param name="j"></param>
    /// <returns></returns>
    private bool isGround(int i,int j)
    {
        //超出范围直接返回
        if (i < 0 || i >= width || j < 0 || j >= height)
        {
            return false;
        }

        return mapData[i, j] > waterProbability;
    }

    /// <summary>
    /// 剔除周围一个同类格子的情况（画面不合理）
    /// </summary>
    private void RemoveOneGround()
    {
        for (int i = 0; i < height; ++i)
        {
            for (int j = 0; j < width; ++j)
            {
                //检查周围上下左右四个方位有只有一个是陆地就变成水区域
                if (CheakGroundCountAroundFour(i, j) < 2)
                {
                    mapData[i,j] = 0;
                }
            }
        }
    }

    /// <summary>
    /// 检查周围上下左右四个方位有几个格子是陆地
    /// </summary>
    /// <param name="i">格子的横坐标</param>
    /// <param name="j">格子的纵坐标</param>
    /// <returns>返回周围四个方位有几个格子</returns>
    private int CheakGroundCountAroundFour(int i, int j)
    {
        int ret = 0;
     
        //左
        if(isGround(i - 1, j))
        {
            ++ret;
        }

        //右
        if(isGround(i + 1,j))
        {
            ++ret;
        }

        //上
        if(isGround(i,j - 1))
        {
            ++ret;
        }

        //下
        if(isGround(i,j + 1))
        {
            ++ret;
        }

        return ret;
    }

    /// <summary>
    /// 检查周围八个方位有几个格子是陆地
    /// </summary>
    /// <param name="i">格子的横坐标</param>
    /// <param name="j">格子的纵坐标</param>
    /// <returns>返回周围八个方位有几个格子</returns>
    private int CheakGroundCountAroundEight(int i, int j)
    {
        int ret = 0;

        //左
        if (isGround(i - 1, j))
        {
            ++ret;
        }

        //左上
        if (isGround(i - 1, j - 1))
        {
            ++ret;
        }

        //左下
        if (isGround(i - 1, j + 1))
        {
            ++ret;
        }

        //右
        if (isGround(i + 1, j))
        {
            ++ret;
        }

        //右上
        if (isGround(i + 1, j - 1))
        {
            ++ret;
        }

        //右下
        if (isGround(i + 1, j + 1))
        {
            ++ret;
        }

        //上
        if (isGround(i, j - 1))
        {
            ++ret;
        }

        //下
        if (isGround(i, j + 1))
        {
            ++ret;
        }

        return ret;
    }

    /// <summary>
    /// 清除地图
    /// </summary>
    public void ClearMap()
    {
        groundMap.ClearAllTiles();
        itemMap.ClearAllTiles();
        waterMap.ClearAllTiles();
        Transform parentTransform = itemMap.transform;
        //清除所有创建的Prefab
        for (int i = parentTransform.childCount - 1; i >= 0; i--)
        {
            // 获取子对象的 Transform 组件，并销毁
            DestroyImmediate(parentTransform.GetChild(i).gameObject);
        }
    }

    /// <summary>
    /// 设置生成地图的数据
    /// </summary>
    public void SetMapData(MapGenerateData mapGenerateData)
    {
        //水的概率
        waterProbability = mapGenerateData.waterProbability;
        //是否使用自定义随机种子
        isUseCustomSeed = mapGenerateData.isUseRandomSeed;
        //随机种子
        randomSeed = mapGenerateData.randomSeed;

        GameObject tempObj;
        float mutiply = 1;

        //怪物数量
        switch(mapGenerateData.enemyNumType)
        {
            case EnemyNumType.Few:
                mutiply = 0.4f;
                break;
            case EnemyNumType.Mid:
                mutiply = 0.8f;
                break;
            case EnemyNumType.Much:
                mutiply = 1.2f;
                break;
            case EnemyNumType.VeryMuch:
                mutiply = 1.6f;
                break;
        }

        for(int i=0;i<mapSpawnDataList.Count;++i)
        {
            tempObj = mapSpawnDataList[i].obj;

            if (tempObj == null)
            {
                continue;
            }
            
            if (tempObj.CompareTag("Enemy"))
            {
                mapSpawnDataList[i].weight *= mutiply;
            }
        }

        for (int i = 0; i < mapSpawnDataPerlinList.Count; ++i)
        {
            tempObj = mapSpawnDataPerlinList[i].obj;

            if (tempObj == null)
            {
                continue;
            }

            if (tempObj.CompareTag("Enemy"))
            {
                mapSpawnDataPerlinList[i].weight *= mutiply;
            }
        }

        //地图大小
        switch (mapGenerateData.mapSizeType)
        {
            case MapSizeType.Small:
                width = 50;
                height = 50;
                break;
            case MapSizeType.Mid:
                width = 75;
                height = 75;
                break;
            case MapSizeType.Big:
                width = 100;
                height = 100;
                break;
            case MapSizeType.VeryBig:
                width = 125;
                height = 125;
                break;
        }
    }
}
