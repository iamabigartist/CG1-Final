using System.Collections;

using UnityEngine;

public class FluidSimulatorParticleGPU : MonoBehaviour
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
    [SerializeField] private float m_force1;
    [SerializeField] private float m_force2;
    [SerializeField] private int m_neighbourCount;

    private SPHSimulator.PCISPHSimulatorNeighbour m_simulator;
    private ParticleRenderer m_particle_renderer;

    private bool m_started = false;

    private void Start ()
    {
        m_simulator = new SPHSimulator.PCISPHSimulatorNeighbour(
            m_numParticles , m_viscosity , m_h , m_iterations , m_randomness , generateBox.bounds , boundingBox.bounds , m_force1 , m_force2 , m_neighbourCount );
        m_particle_renderer = new ParticleRenderer();

        m_particle_renderer.On( m_simulator.particle_position_buffer , new Color( 0.2f , 0.9f , 0.0f ) , new Color( 1.0f , 1.0f , 0.8f ) , 5f );
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