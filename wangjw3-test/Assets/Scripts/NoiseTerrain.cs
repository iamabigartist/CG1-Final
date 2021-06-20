using System.Collections;

using UnityEngine;

public class NoiseTerrain : MonoBehaviour
{
    public BoxCollider boundingBox;
    public ComputeShader computeNoise;

    [SerializeField] private float m_noiseStep;
    [SerializeField, Range(0f, 10f)] private float m_threshold; 

    // noiseShader
    private int m_noiseKernel;
    private ComputeBuffer m_noiseBuffer;
    public float[] m_noise;

    // marching cube and mesh generation
    private MeshFilter m_meshFilter;
    private MarchingCube1.MarchingCubeCPUGenerator m_generator;
    private Mesh m_mesh;

    private Vector3[] vs;
    private int[] tris;

    MarchingCube1.VolumeMatrix vol;

    private void Start()
    {
        m_generator = new MarchingCube1.MarchingCubeCPUGenerator();
        m_meshFilter = GetComponent<MeshFilter>();

        Vector3 tem = boundingBox.bounds.size;
        Vector3Int size = new Vector3Int(
            Mathf.CeilToInt(tem.x / m_noiseStep),
            Mathf.CeilToInt(tem.y / m_noiseStep),
            Mathf.CeilToInt(tem.z / m_noiseStep)
            );
        m_noise = new float[size.x * size.y * size.z];

        //for (int i = 0; i < size.z; ++i)
        //{
        //    for (int j = 0; j < size.y; ++j)
        //    {
        //        for (int k = 0; k < size.x; ++k)
        //        {
        //            m_noise[k + j * size.x + i * size.y * size.z] = 0f;
        //        }
        //    }
        //}
        m_noiseBuffer = new ComputeBuffer(size.x * size.y * size.z, sizeof(float));
        computeNoise.SetFloat("gridStep", m_noiseStep);
        m_noiseKernel = computeNoise.FindKernel("PerlinNoise");
        computeNoise.SetBuffer(m_noiseKernel, "output", m_noiseBuffer);
        computeNoise.SetInts("size", size.x, size.y, size.z);
        computeNoise.SetFloat("noiseStep", m_noiseStep);

        computeNoise.Dispatch(m_noiseKernel, Mathf.CeilToInt(size.x/ 8f), Mathf.CeilToInt(size.y / 8f), Mathf.CeilToInt(size.z / 8f));
        m_noiseBuffer.GetData(m_noise);

        vol = new MarchingCube1.VolumeMatrix(size);
        vol.data = m_noise;
        m_generator.Input(vol, m_threshold, Vector3.one);
        m_generator.Output(out m_mesh, out vs, out tris);
        m_meshFilter.mesh = m_mesh;
    }

    private void OnDestroy()
    {
        m_noiseBuffer.Dispose();
        Debug.Log("Buffer disposed!");
    }

    private void Update()
    {
        //vol.data = m_noise;
        //m_generator.Input(vol, m_threshold, Vector3.one);
        //m_generator.Output(out m_mesh, out vs, out tris);
        //m_meshFilter.mesh = m_mesh;
    }

}