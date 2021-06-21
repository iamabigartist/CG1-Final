using System.Collections;

using UnityEditor;

using UnityEngine;

[System.Serializable]
public class PerlinNoiseTerrainLayer
{
    public float magnitude;
    public float sharpness;
    public float centerHeight;
    public Vector3 noiseScale;
    public float weight;
}

public class NoiseTerrain : MonoBehaviour
{
    public BoxCollider boundingBox;
    public ComputeShader computeNoise;
    public Light sunLight;

    [SerializeField] private float m_noiseStep;
    [SerializeField] private PerlinNoiseTerrainLayer[] m_noiseLayers;
    [SerializeField] private float m_threshold;

    private int m_noiseKernel;
    private int m_clearKernel;
    private ComputeBuffer m_noiseBuffer;
    private MarchingCube1.VolumeMatrix m_volume;

    private Vector3Int m_gridDimension;
    private int m_gridCount;

    //private MarchingCubeRenderer m_renderer;
    private MarchingCube1.MarchingCubeCPUGenerator m_generator;

    private MeshFilter m_meshFilter;
    private Mesh m_mesh;

    private bool m_initialized = false;

    private MarchingCube1.VolumeMatrix vol;

    public Mesh cur_mesh => m_meshFilter.mesh;

    public void Initialize ()
    {
        if (m_initialized) return;
        Vector3 tem = boundingBox.bounds.size;
        m_gridDimension = new Vector3Int(
            Mathf.CeilToInt( tem.x / m_noiseStep ) ,
            Mathf.CeilToInt( tem.y / m_noiseStep ) ,
            Mathf.CeilToInt( tem.z / m_noiseStep ) );
        m_gridCount = m_gridDimension.x * m_gridDimension.y * m_gridDimension.z;
        m_volume = new MarchingCube1.VolumeMatrix( m_gridDimension );

        m_noiseKernel = computeNoise.FindKernel( "PerlinNoise" );
        m_clearKernel = computeNoise.FindKernel( "Clear" );

        m_noiseBuffer = new ComputeBuffer( m_gridCount , sizeof( float ) );
        computeNoise.SetBuffer( m_noiseKernel , "output" , m_noiseBuffer );
        computeNoise.SetBuffer( m_clearKernel , "output" , m_noiseBuffer );

        computeNoise.SetFloat( "gridStep" , m_noiseStep );
        computeNoise.SetInts( "size" , m_gridDimension.x , m_gridDimension.y , m_gridDimension.z );

        //m_renderer = new MarchingCubeRenderer();
        //m_renderer.On( m_volume , Color.white , m_noiseStep , m_threshold , boundingBox.bounds.min , sunLight , m_levelHeight );
        m_generator = new MarchingCube1.MarchingCubeCPUGenerator();
        m_generator.Input( m_volume , m_threshold , new Vector3( m_noiseStep , m_noiseStep , m_noiseStep ) , boundingBox.bounds.min );
        m_meshFilter = GetComponent<MeshFilter>();
        m_mesh = new Mesh();
        m_initialized = true;
    }

    public void Generate ()
    {
        computeNoise.Dispatch( m_clearKernel , Mathf.CeilToInt( m_gridDimension.x / 8f ) , Mathf.CeilToInt( m_gridDimension.y / 8f ) , Mathf.CeilToInt( m_gridDimension.z / 8f ) );
        foreach (PerlinNoiseTerrainLayer layer in m_noiseLayers)
        {
            computeNoise.SetFloat( "magnitude" , layer.magnitude );
            computeNoise.SetFloat( "centerHeight" , layer.centerHeight );
            computeNoise.SetFloat( "sharpness" , layer.sharpness );
            computeNoise.SetFloats( "noiseScale" , layer.noiseScale.x , layer.noiseScale.y , layer.noiseScale.z );
            computeNoise.SetFloat( "weight" , layer.weight );

            computeNoise.Dispatch( m_noiseKernel , Mathf.CeilToInt( m_gridDimension.x / 8f ) , Mathf.CeilToInt( m_gridDimension.y / 8f ) , Mathf.CeilToInt( m_gridDimension.z / 8f ) );
        }
        m_noiseBuffer.GetData( m_volume.data );
        m_generator.Output( out m_mesh );
        m_meshFilter.mesh = m_mesh;
        SetXY2UV();
    }

    private void SetXY2UV ()
    {
        Vector2[] m_uv = new Vector2[m_mesh.vertices.Length];
        for (int i = 0; i < m_mesh.vertices.Length; i++)
        {
            m_uv[i] = new Vector2( m_mesh.vertices[i].x , m_mesh.vertices[i].y );
        }
        m_mesh.uv = m_uv;
    }

    private void Update ()
    {
        if (Input.GetKeyDown( KeyCode.Return ))
        {
            if (!m_initialized) Initialize();
            Generate();
        }
    }

    //private void OnRenderObject ()
    //{
    //    m_renderer.Config( Color.white , m_noiseStep , m_threshold , boundingBox.bounds.min , sunLight , m_levelHeight );
    //    m_renderer.Draw();
    //}

    public MarchingCube1.VolumeMatrix volume => m_volume;
    public float gridStep => m_noiseStep;
    public float threshold => m_threshold;
    public Bounds bounds => boundingBox.bounds;

    private void OnDestroy ()
    {
        m_noiseBuffer.Release();
        m_noiseBuffer.Dispose();
        //m_renderer.Off();
    }
}