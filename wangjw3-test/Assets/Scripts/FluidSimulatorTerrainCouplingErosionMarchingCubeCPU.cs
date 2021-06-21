using System.Collections;

using UnityEngine;

public class FluidSimulatorTerrainCouplingErosionMarchingCubeCPU : MonoBehaviour
{
    public BoxCollider generateBox;
    public MeshFilter terrainMeshFilter;

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
    [SerializeField] private float m_erosion;

    private SPHSimulator.PCISPHSimulatorNeighbourSolidCouplingErosion m_simulator;

    private ParticleToVolumeFast m_converter;
    private NoiseTerrainModule m_terrain;

    private MarchingCube1.MarchingCubeCPUGenerator m_generator;

    private MeshFilter m_meshFilter;
    private Mesh m_mesh;

    private Mesh m_terrainMesh;

    private bool m_started = false;
    private int m_stepCounter = 0;

    private void Start ()
    {
        m_terrain = GetComponent<NoiseTerrainModule>();
        m_terrain.Initialize();
        m_terrain.Generate();
        m_simulator = new SPHSimulator.PCISPHSimulatorNeighbourSolidCouplingErosion(
            m_numParticles , m_viscosity , m_h , m_iterations , m_randomness , generateBox.bounds , m_terrain.bounds , m_terrain.volume.size , m_terrain.gridStep , m_terrain.threshold , 0f , 0f , 50 , m_terrain.volume , m_damping , m_erosion );

        m_converter = new ParticleToVolumeFast( m_gridStep , m_smoothLength , m_terrain.bounds , m_k );
        m_converter.Compute( m_simulator.KNNContainer , m_simulator.particlePositionArray );

        m_generator = new MarchingCube1.MarchingCubeCPUGenerator();
        m_generator.Input( m_converter.volume , m_fluidThreshold , new Vector3( m_gridStep , m_gridStep , m_gridStep ) , m_terrain.bounds.min );

        m_mesh = new Mesh();
        m_meshFilter = GetComponent<MeshFilter>();

        m_generator.Output( out m_mesh );
        m_meshFilter.mesh = m_mesh;

        m_terrainMesh = new Mesh();
        m_generator.Input( m_simulator.terrainVolume , m_terrain.threshold , new Vector3( m_terrain.gridStep , m_terrain.gridStep , m_terrain.gridStep ) , m_terrain.bounds.min );
        m_generator.Output( out m_terrainMesh );
        terrainMeshFilter.mesh = m_terrainMesh;
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

    private void OnDisable ()
    {
        m_converter.Dispose();
        m_simulator.DisposeBuffer();
    }

    private void Step ()
    {
        float dt = m_dt < Mathf.Epsilon ? Time.deltaTime : m_dt;
        m_simulator.Step( dt );
        m_stepCounter++;
        if ( m_stepCounter >= m_visualizeStep )
        {
            m_converter.Compute( m_simulator.KNNContainer , m_simulator.particlePositionArray );
            m_generator.Input( m_converter.volume , m_fluidThreshold , new Vector3( m_gridStep , m_gridStep , m_gridStep ) , m_terrain.bounds.min );
            m_generator.Output( out m_mesh );
            m_generator.Input( m_simulator.terrainVolume , m_terrain.threshold , new Vector3( m_terrain.gridStep , m_terrain.gridStep , m_terrain.gridStep ) , m_terrain.bounds.min );
            m_generator.Output( out m_terrainMesh );
            m_meshFilter.mesh = m_mesh;
            terrainMeshFilter.mesh = m_terrainMesh;
            m_stepCounter = 0;
        }
    }
}