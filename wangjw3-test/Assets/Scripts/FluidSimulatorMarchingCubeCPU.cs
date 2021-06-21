using System.Collections;

using UnityEngine;

public class FluidSimulatorMarchingCubeCPU : MonoBehaviour
{
    public BoxCollider generateBox;
    public BoxCollider boundingBox;

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
    [SerializeField, Range( 0f , 10f )] private float m_threshold;
    [SerializeField] private int m_neighbourCount;
    [SerializeField] private int m_k;
    [SerializeField] private float m_noiseStep;

    private MeshFilter m_meshFilter;

    private SPHSimulator.PCISPHSimulatorNeighbour m_simulator;
    private ParticleToVolumeFast m_converter;
    private MarchingCube1.MarchingCubeCPUGenerator m_generator;
    private Mesh m_mesh;

    private bool m_started = false;
    private bool m_visualize = false;


    private void Start ()
    {
        m_meshFilter = GetComponent<MeshFilter>();

        m_simulator = new SPHSimulator.PCISPHSimulatorNeighbour(
            m_numParticles , m_viscosity , m_h , m_iterations , m_randomness , generateBox.bounds , boundingBox.bounds , m_force1 , m_force2 , m_neighbourCount );
        m_converter = new ParticleToVolumeFast( m_gridStep , m_smoothLength , boundingBox.bounds , m_k );
        m_generator = new MarchingCube1.MarchingCubeCPUGenerator();
        m_mesh = new Mesh();

        m_converter.Compute( m_simulator.KNNContainer , m_simulator.particlePositionArray );
        m_generator.Input( m_converter.volume , m_threshold, new Vector3(m_gridStep, m_gridStep, m_gridStep), boundingBox.bounds.min);
        m_generator.Output( out m_mesh );
        m_meshFilter.mesh = m_mesh;

        Vector3 tem = boundingBox.bounds.max - boundingBox.bounds.min;
        Vector3Int size = new Vector3Int(
            Mathf.CeilToInt( tem.x / m_noiseStep ) ,
            Mathf.CeilToInt( tem.y / m_noiseStep ) ,
            Mathf.CeilToInt( tem.z / m_noiseStep )
            );
    }

    private void OnDestroy ()
    {
        m_simulator.DisposeBuffer();
        m_converter.Dispose();
        Debug.Log( "Buffer disposed!" );
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
            m_converter.Compute( m_simulator.KNNContainer , m_simulator.particlePositionArray );
            m_generator.Input( m_converter.volume , m_threshold , new Vector3(m_gridStep, m_gridStep, m_gridStep) , boundingBox.bounds.min);
            m_generator.Output( out m_mesh );
            m_meshFilter.mesh = m_mesh;
            m_visualize = false;
        }
    }
}