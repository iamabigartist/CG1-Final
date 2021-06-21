using System.Collections;

using UnityEngine;

public class FluidSimulatorTerrainCouplingErosionMarchingCubeCPU : MonoBehaviour
{
    public BoxCollider generateBox;
    public TerrainRenderer terrainRenderer;

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
    [SerializeField] private float m_force1;
    [SerializeField] private float m_force2;

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

        m_simulator = new SPHSimulator.PCISPHSimulatorNeighbourSolidCouplingErosion( m_viscosity , m_h , m_iterations , m_terrain.bounds , m_terrain.volume.size , m_terrain.gridStep , m_terrain.threshold , m_force1 , m_force2 , 50 , m_terrain.volume , m_damping , m_erosion );

        m_terrainMesh = new Mesh();

        m_generator = new MarchingCube1.MarchingCubeCPUGenerator();
        m_generator.Input( m_simulator.terrainVolume , m_terrain.threshold , new Vector3( m_terrain.gridStep , m_terrain.gridStep , m_terrain.gridStep ) , m_terrain.bounds.min );
        m_generator.Output( out m_terrainMesh );
        terrainRenderer.Setup( m_terrainMesh );
    }

    private void InitFluid ()
    {
        m_simulator.CreateParticles( m_numParticles , m_randomness , generateBox.bounds );

        m_converter = new ParticleToVolumeFast( m_gridStep , m_smoothLength , m_terrain.bounds , m_k );
        m_converter.Compute( m_simulator.KNNContainer , m_simulator.particlePositionArray );

        m_generator.Input( m_converter.volume , m_fluidThreshold , new Vector3( m_gridStep , m_gridStep , m_gridStep ) , m_terrain.bounds.min );
        m_mesh = new Mesh();
        m_meshFilter = GetComponent<MeshFilter>();

        m_generator.Output( out m_mesh );
        m_meshFilter.mesh = m_mesh;
    }

    private void Update ()
    {
        if ( !m_started && Input.GetButtonDown( "Submit" ) )
        {
            m_started = true;
            InitFluid();
        }
        if ( m_started )
        {
            Step();
        }
    }

    private void OnDisable ()
    {
        if ( m_converter != null ) m_converter.Dispose();
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
            terrainRenderer.Setup( m_terrainMesh );
            m_stepCounter = 0;
        }
    }
}