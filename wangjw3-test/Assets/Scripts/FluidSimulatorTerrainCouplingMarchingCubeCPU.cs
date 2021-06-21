using System.Collections;

using UnityEngine;

public class FluidSimulatorTerrainCouplingMarchingCubeCPU : MonoBehaviour
{
    public BoxCollider generateBox;
    public NoiseTerrain terrain;
    public Light sunLight;

    [SerializeField] private bool m_frameByFrame;
    [SerializeField] private int m_numParticles;
    [SerializeField] private float m_h;
    [SerializeField] private int m_iterations;
    [SerializeField] private float m_dt;
    [SerializeField, Range( 0f , 1f )] private float m_randomness;
    [SerializeField] private float m_viscosity;
    [SerializeField] private float m_fluidThreshold;
    [SerializeField] private float m_gridStep;
    [SerializeField] private float m_smoothLength;
    [SerializeField] private int m_k;
    [SerializeField] private int m_visualizeStep;
    [SerializeField] private float m_damping;

    private SPHSimulator.PCISPHSimulatorNeighbourSolidCoupling m_simulator;

    private ParticleToVolumeFast m_converter;

    private MarchingCube1.MarchingCubeCPUGenerator m_generator;

    private MeshFilter m_meshFilter;
    private Mesh m_mesh;

    private bool m_started = false;
    private int m_stepCounter = 0;

    private void Start ()
    {
        terrain.Initialize();
        terrain.Generate();
        m_simulator = new SPHSimulator.PCISPHSimulatorNeighbourSolidCoupling(
            m_numParticles , m_viscosity , m_h , m_iterations , m_randomness , generateBox.bounds , terrain.bounds , terrain.volume.size , terrain.gridStep , terrain.threshold , 0f , 0f , 50 , m_damping );
        m_simulator.SetVolumeData( terrain.volume.data );

        m_converter = new ParticleToVolumeFast( m_gridStep , m_smoothLength , terrain.bounds , m_k );
        m_converter.Compute( m_simulator.KNNContainer , m_simulator.particlePositionArray );

        m_generator = new MarchingCube1.MarchingCubeCPUGenerator();
        m_generator.Input( m_converter.volume , m_fluidThreshold , new Vector3( m_gridStep , m_gridStep , m_gridStep ) , terrain.bounds.min );

        m_mesh = new Mesh();
        m_meshFilter = GetComponent<MeshFilter>();

        m_generator.Output( out m_mesh );
        m_meshFilter.mesh = m_mesh;
    }

    private void Update ()
    {
        if ( Input.GetButtonDown( "Submit" ) ) m_started = true;
        //if ( Input.GetKey( KeyCode.Space ) ) m_visualize = true;
        if ( m_started )
        {
            Step();
            if ( m_frameByFrame ) m_started = false;
        }
    }

    //private void OnRenderObject ()
    //{
    //    m_renderer.Config( Color.blue , m_gridStep , m_fluidThreshold , terrain.bounds.min , sunLight , m_zerovalue );
    //    m_renderer.Draw();
    //}

    private void OnDisable ()
    {
        m_simulator.DisposeBuffer();
        //m_renderer.Off();
    }

    private void Step ()
    {
        float dt = m_dt < Mathf.Epsilon ? Time.deltaTime : m_dt;
        m_simulator.Step( dt );
        m_stepCounter++;
        if ( m_stepCounter >= m_visualizeStep )
        {
            m_converter.Compute( m_simulator.KNNContainer , m_simulator.particlePositionArray );
            m_generator.Input( m_converter.volume , m_fluidThreshold , new Vector3( m_gridStep , m_gridStep , m_gridStep ) , terrain.bounds.min );
            m_generator.Output( out m_mesh );
            m_meshFilter.mesh = m_mesh;
            m_stepCounter = 0;
        }
    }
}