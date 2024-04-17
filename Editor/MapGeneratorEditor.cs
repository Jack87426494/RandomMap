using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.DrawDefaultInspector();
        if(GUILayout.Button("GenerateMap"))
        {
            ((MapGenerator)target).GenerateMap();
        }
        if (GUILayout.Button("ClearMap"))
        {
            ((MapGenerator)target).ClearMap();
        }
    }
}
