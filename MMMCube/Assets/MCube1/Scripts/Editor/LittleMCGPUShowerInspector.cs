using UnityEditor;

using UnityEngine;

namespace MarchingCube1
{
    [CustomEditor( typeof( LittleMCGPUShower ) )]
    public class LittleMCGPUShowerInspector : Editor
    {
        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI();
            var shower = ( LittleMCGPUShower ) target;
            if ( GUILayout.Button( "Genrate" ) )
            {
                shower.Generate();
            }
        }
    }
}