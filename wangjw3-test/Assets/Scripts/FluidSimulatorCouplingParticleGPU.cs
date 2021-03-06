using System.Collections;

using UnityEngine;

public class FluidSimulatorCouplingParticleGPU : MonoBehaviour
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
    [SerializeField] private float m_threshold;
    [SerializeField] private float m_baseDt;
    [SerializeField] private int m_baseIterations;
    [SerializeField] private int m_k;
    [SerializeField] private float zero_level;
    [SerializeField] private Light m_light;

    private SPHSimulator.PCISPHSimulatorNeighbour m_baseSimulator;
    private ParticleToVolumeFast m_converter;
    private MarchingCubeRenderer m_renderer;

    //private MarchingCube1.MarchingCubeCPUGenerator m_generator;
    private SPHSimulator.PCISPHSimulatorNeighbourSolidCoupling m_simulator;

    private ParticleRenderer m_particle_renderer;

    private bool m_started = false;

    private void Start ()
    {
        m_baseSimulator = new SPHSimulator.PCISPHSimulatorNeighbour( 10000 , 0f , 1f , 5 , 0.1f , generateBox.bounds , boundingBox.bounds , 0f , 0f , 50 );
        m_converter = new ParticleToVolumeFast( m_gridStep , m_smoothLength , boundingBox.bounds , m_k );

        for ( int i = 0; i < m_baseIterations; i++ )
        {
            m_baseSimulator.Step( m_baseDt );
        }
        m_converter.Compute( m_baseSimulator.KNNContainer , m_baseSimulator.particlePositionArray );
        m_baseSimulator.DisposeBuffer();
        m_converter.Dispose();

        m_renderer = new MarchingCubeRenderer();
        m_renderer.On( m_converter.volume , Color.white , m_gridStep , m_threshold , boundingBox.bounds.min , m_light , zero_level );

        m_simulator = new SPHSimulator.PCISPHSimulatorNeighbourSolidCoupling(
            m_numParticles , m_viscosity , m_h , m_iterations , m_randomness , generateBox.bounds , boundingBox.bounds , m_converter.volume.size , m_gridStep , m_threshold , 0f , 0f , 50 );
        m_simulator.SetVolumeData( m_converter.volume.data );

        m_particle_renderer = new ParticleRenderer();

        m_particle_renderer.On( m_simulator.particle_position_buffer , new Color( 0.2f , 0.6f , 0.0f ) );
    }

    private void Update ()
    {
        if ( Input.GetButtonDown( "Submit" ) ) m_started = true;
        if ( m_started )
        {
            Step();
            if ( m_frameByFrame ) m_started = false;
        }
    }

    private void OnRenderObject ()
    {
        m_particle_renderer.Draw();

        m_renderer.Config( Color.white , m_gridStep , m_threshold , boundingBox.bounds.min , m_light , zero_level );
        m_renderer.Draw();
    }

    private void OnDisable ()
    {
        m_simulator.DisposeBuffer();
    }

    private void Step ()
    {
        float dt = m_dt < Mathf.Epsilon ? Time.deltaTime : m_dt;
        m_simulator.Step( dt );
    }
}