using UnityEditor;

using UnityEngine;

namespace MarchingCube1
{
    [CustomEditor(typeof(LittleMarchingCubeShower))]
    public class LittleMarchingCubeShowerInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            var shower = (LittleMarchingCubeShower)target;
            if (GUILayout.Button("Genrate"))
            {
                shower.Generate();
            }
        }
    }
}