using UnityEngine;

public class TerrainLoader : MonoBehaviour
{
    [SerializeField] private string path;

    private Mesh m_mesh;

    private void Start ()
    {
        MarchingCube1.VolumeMatrix volume = MarchingCube1.VolumeMatrix.LoadFromFile( path );
        MarchingCube1.MarchingCubeCPUGenerator generator = new MarchingCube1.MarchingCubeCPUGenerator();
        generator.Input( volume , 0f , Vector3.one * 0.1f );
        m_mesh = new Mesh();
        generator.Output( out m_mesh );
    }

    private void Update ()
    {
        if ( Input.GetKeyDown( KeyCode.Return ) )
        {
            GetComponent<TerrainRenderer>().Setup( m_mesh );
        }
    }
}