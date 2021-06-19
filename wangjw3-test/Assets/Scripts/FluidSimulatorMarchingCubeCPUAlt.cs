using System.Collections;

using UnityEngine;

public class FluidSimulatorMarchingCubeCPUAlt : MonoBehaviour
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
    [SerializeField, Range( 0f , 1f )] private float m_threshold;

    private MeshFilter m_meshFilter;

    private SPHSimulator.PCISPHSimulatorSlowAlt m_simulator;
    private ParticleToVolume m_converter;
    private MarchingCube1.MarchingCubeCPUGenerator m_generator;
    private Mesh m_mesh;

    private bool m_started = false;
    private Vector3[] vs;
    private int[] tris;
    private bool m_visualize = false;

    private void Start ()
    {
        m_meshFilter = GetComponent<MeshFilter>();

        m_simulator = new SPHSimulator.PCISPHSimulatorSlowAlt(
            m_numParticles , m_viscosity , m_h , m_iterations , m_randomness , generateBox.bounds , boundingBox.bounds );
        m_converter = new ParticleToVolume( m_gridStep , m_smoothLength , boundingBox.bounds );
        m_generator = new MarchingCube1.MarchingCubeCPUGenerator();
        m_mesh = new Mesh();

        m_converter.Compute( ref m_simulator.particlePositionArray );
        m_generator.Input( m_converter.volume , m_threshold );
        m_generator.Output( out m_mesh , out vs , out tris );
        m_meshFilter.mesh = m_mesh;
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
            m_converter.Compute( ref m_simulator.particlePositionArray );
            m_generator.Input( m_converter.volume , m_threshold );
            m_generator.Output( out m_mesh , out vs , out tris );
            m_meshFilter.mesh = m_mesh;
            m_visualize = false;
        }
    }
}