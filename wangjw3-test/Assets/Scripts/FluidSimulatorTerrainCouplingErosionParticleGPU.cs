using System.Collections;

using UnityEngine;

public class FluidSimulatorTerrainCouplingErosionParticleGPU : MonoBehaviour
{
    public BoxCollider generateBox;
    public TerrainRenderer terrainRenderer;

    [SerializeField] private int m_numParticles;
    [SerializeField] private float m_h;
    [SerializeField] private int m_iterations;
    [SerializeField] private float m_dt;
    [SerializeField, Range( 0f , 1f )] private float m_randomness;
    [SerializeField] private float m_viscosity;
    [SerializeField] private int m_visualizeStep;
    [SerializeField] private float m_damping;
    [SerializeField] private float m_erosion;
    [SerializeField] private float m_force1;
    [SerializeField] private float m_force2;
    [SerializeField] private int m_stepsToReset;

    private SPHSimulator.PCISPHSimulatorNeighbourSolidCouplingErosion m_simulator;

    private NoiseTerrainModule m_terrain;

    private MarchingCube1.MarchingCubeCPUGenerator m_generator;

    private ParticleRenderer m_renderer;

    private Mesh m_terrainMesh;

    private bool m_started = false;
    private int m_stepCounter = 0;
    private int m_resetCounter = 0;
    private int m_resets = 0;

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

        m_renderer = new ParticleRenderer();
        m_renderer.On( m_simulator.particle_position_buffer , Color.blue );
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
        m_simulator.DisposeBuffer();
        if ( m_renderer != null ) m_renderer.Off();
    }

    private void OnRenderObject ()
    {
        if ( m_renderer != null ) m_renderer.Draw();
    }

    private void Step ()
    {
        float dt = m_dt < Mathf.Epsilon ? Time.deltaTime : m_dt;
        m_simulator.Step( dt );
        m_stepCounter++;
        m_resetCounter++;
        if ( m_stepCounter >= m_visualizeStep )
        {
            m_generator.Input( m_simulator.terrainVolume , m_terrain.threshold , new Vector3( m_terrain.gridStep , m_terrain.gridStep , m_terrain.gridStep ) , m_terrain.bounds.min );
            m_generator.Output( out m_terrainMesh );
            terrainRenderer.Setup( m_terrainMesh );
            m_stepCounter = 0;
        }
        if ( m_resetCounter >= m_stepsToReset )
        {
            m_simulator.ResetParticles( m_randomness );
            m_resetCounter = 0;
            m_resets++;
            if ( m_resets % 5 == 1 )
            {
                m_simulator.terrainVolume.SaveToFile( "C:\\Users\\wjw11\\Desktop\\Temp\\test.bin" );
            }
        }
    }
}