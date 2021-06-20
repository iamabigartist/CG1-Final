using System.Collections;

using UnityEngine;

public class FluidSimulatorMarchingCubeGPU : MonoBehaviour
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

    private MeshFilter m_meshFilter;

    private SPHSimulator.PCISPHSimulatorNeighbour m_simulator;
    private ParticleToVolume m_converter;
    private MarchingCubeRenderer m_cubeRenderer;

    private bool m_started = false;
    private Vector3[] vs;
    private int[] tris;

    private void Start ()
    {
        m_meshFilter = GetComponent<MeshFilter>();

        m_simulator = new SPHSimulator.PCISPHSimulatorNeighbour(
            m_numParticles , m_viscosity , m_h , m_iterations , m_randomness , generateBox.bounds , boundingBox.bounds , m_force1 , m_force2 , m_neighbourCount );
        m_converter = new ParticleToVolume( m_gridStep , m_smoothLength , boundingBox.bounds );
        Visualise();

        m_cubeRenderer = new MarchingCubeRenderer();
        m_cubeRenderer.On( m_converter.volume , Color.green , 1 , m_threshold );
    }

    private void OnDestroy ()
    {
        m_simulator.DisposeBuffer();
        Debug.Log( "Buffer disposed!" );
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

    private void Visualise ()
    {
        m_converter.Compute( ref m_simulator.particlePositionArray );
    }

    private void Step ()
    {
        float dt = m_dt < Mathf.Epsilon ? Time.deltaTime : m_dt;
        m_simulator.Step( dt );
        Visualise();
    }

    private void OnRenderObject ()
    {
        m_cubeRenderer.Draw();
    }
}