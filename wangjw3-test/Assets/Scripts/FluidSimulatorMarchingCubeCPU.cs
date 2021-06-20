using System.Collections;

using UnityEngine;

public class FluidSimulatorMarchingCubeCPU : MonoBehaviour
{
    public BoxCollider generateBox;
    public BoxCollider boundingBox;
    public ComputeShader computeNoise;


    [SerializeField] private bool m_frameByFrame;
    [SerializeField] private int m_numParticles;
    [SerializeField] private float m_h;
    [SerializeField] private int m_iterations;
    [SerializeField] private float m_dt;
    [SerializeField, Range( 0f , 1f )] private float m_randomness;
    [SerializeField] private float m_viscosity;
    [SerializeField] private float m_gridStep;
    [SerializeField] private float m_smoothLength;
    [SerializeField] private float m_force1;
    [SerializeField] private float m_force2;
    [SerializeField, Range(0f, 10f)] private float m_threshold;
    [SerializeField] private int m_neighbourCount;
    [SerializeField] private int m_k;

    private MeshFilter m_meshFilter;

    private SPHSimulator.PCISPHSimulatorNeighbour m_simulator;
    private ParticleToVolumeFast m_converter;
    private MarchingCube1.MarchingCubeCPUGenerator m_generator;
    private Mesh m_mesh;

    private bool m_started = false;
    private Vector3[] vs;
    private int[] tris;
    private bool m_visualize = false;

    // noiseShader
    private int m_noiseKernel;
    private ComputeBuffer m_noiseBuffer;
    public float[] m_noise;

    private void Start ()
    {
        m_meshFilter = GetComponent<MeshFilter>();
        
        m_simulator = new SPHSimulator.PCISPHSimulatorNeighbour(
            m_numParticles, m_viscosity, m_h, m_iterations, m_randomness, generateBox.bounds, boundingBox.bounds, m_force1, m_force2, m_neighbourCount);
        m_converter = new ParticleToVolumeFast(m_gridStep, m_smoothLength, boundingBox.bounds, m_k);
        m_generator = new MarchingCube1.MarchingCubeCPUGenerator();
        m_mesh = new Mesh();

        m_converter.Compute(m_simulator.KNNContainer, m_simulator.particlePositionArray);
        m_generator.Input(m_converter.volume, m_threshold, Vector3.one);
        m_generator.Output(out m_mesh, out vs, out tris);
        m_meshFilter.mesh = m_mesh;

        Vector3 tem = boundingBox.bounds.max - boundingBox.bounds.min;
        Vector3Int size = new Vector3Int(
            Mathf.CeilToInt(tem.x / m_noiseStep),
            Mathf.CeilToInt(tem.y / m_noiseStep),
            Mathf.CeilToInt(tem.z / m_noiseStep)
            );
        m_noise = new float[size.x * size.y * size.z];
        
        for(int i = 0; i < size.z; ++i)
        {
            int a = i;
            for(int j = 0; j < size.y; ++j)
            {
                int b = j;
                for(int k = 0; k < size.x; ++k)
                {
                    m_noise[k + b * size.x + a * size.y * size.z] = 0f;
                }
            }
        }
        m_noiseBuffer = new ComputeBuffer(size.x * size.y * size.z, sizeof(float) * 3);
        computeNoise.SetBuffer(m_noiseKernel, "_output", m_noiseBuffer);
        m_noiseBuffer.SetData(m_noise);
        m_noiseKernel = computeNoise.FindKernel("Classic3D");

        computeNoise.Dispatch(m_noiseKernel, size.x * size.y * size.z, 1, 1);
        m_noiseBuffer.GetData(m_noise);

    }

    private void OnDestroy()
    {
        m_simulator.DisposeBuffer();
        m_converter.Dispose();
        Debug.Log("Buffer disposed!");
    }

    private void Update ()
    {
        if ( Input.GetButtonDown( "Submit" ) ) m_started = true;
        if ( Input.GetKey( KeyCode.Space ) ) m_visualize = true;
        if ( m_started )
        {
            Step();
            if ( m_frameByFrame ) m_started = false;
        }
    }

    private void Step ()
    {
        float dt = m_dt < Mathf.Epsilon ? Time.deltaTime : m_dt;
        m_simulator.Step( dt );
        if ( m_visualize )
        {
            m_converter.Compute(m_simulator.KNNContainer, m_simulator.particlePositionArray);
            m_generator.Input(m_converter.volume, m_threshold, Vector3.one);
            m_generator.Output(out m_mesh, out vs, out tris);
            m_meshFilter.mesh = m_mesh;
            m_visualize = false;
        }
    }
}