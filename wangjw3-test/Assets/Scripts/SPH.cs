using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[System.Serializable]
public struct Particle
{
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 acceleration;
    public float density;
    public float pressure;
    public int neighbourCount;
}

public class SPH : MonoBehaviour
{
    const int MAX_NEIGHBOUR = 1024;

    public ComputeShader computeSPH;
    public BoxCollider generateBox;
    public BoxCollider boundingBox;
    public GameObject particleObject;

    [SerializeField] private int m_numParticles;
    [SerializeField] private float m_initialDensity;
    [SerializeField] private float m_particleRadius;
    [SerializeField] private float m_gridSize;
    [SerializeField] private float m_dampingRate;
    [SerializeField] private float m_pressureConstant;

    //Particles
    private int m_actualNumParticles;
    private ComputeBuffer m_particleBuffer;
    public Particle[] m_particles;
    private Transform[] m_objects;
    private float m_massPerParticle;
    private float m_particleRadius3;

    //Range search
    private List<int>[,,] m_grid;
    private ComputeBuffer m_neighbourBuffer;
    private int[] m_neighbours;
    private Vector3Int m_gridDimension;
    private Vector3Int[] m_gridIndices;
    private float m_particleRadius2;

    private float m_dt;
    private int m_pressureKernelIndex;
    private int m_forceKernelIndex;
    private Bounds m_generateBox;
    private Bounds m_boundingBox;

    private bool m_started = false;

    private void Start()
    {
        m_dt = Time.fixedDeltaTime;
        m_pressureKernelIndex = computeSPH.FindKernel("ComputePressure");
        m_forceKernelIndex = computeSPH.FindKernel("ComputeForce");
        m_generateBox = generateBox.bounds;
        m_boundingBox = boundingBox.bounds;

        CreateParticles();
        InitializeAccelerate();
        InitializeKernels();
    }

    private void Update()
    {
        if (Input.GetButtonDown("Submit")) m_started = true;
        if (m_started)
        {
            m_dt = Time.deltaTime;
            Simulate();
        }
    }

    private void FixedUpdate()
    {
        //if (m_started) Simulate();
        //m_started = false;
    }

    private void OnDisable()
    {
        m_neighbourBuffer.Release();
        m_neighbourBuffer.Dispose();
        m_particleBuffer.Release();
        m_particleBuffer.Dispose();
        Debug.Log("Buffer disposed.");
    }

    private void CreateParticles()
    {

        Vector3 size = m_generateBox.size;
        Vector3 min = m_generateBox.min;
        float step = Mathf.Pow(size.x * size.y * size.z / m_numParticles, 1f / 3f);
        Vector3 particleCountFloat = size / step;
        Vector3Int particleCount = new Vector3Int(Mathf.RoundToInt(particleCountFloat.x), Mathf.RoundToInt(particleCountFloat.y), Mathf.RoundToInt(particleCountFloat.z));
        m_actualNumParticles = particleCount.x * particleCount.y * particleCount.z;
        m_massPerParticle = (m_initialDensity * m_generateBox.size.x * m_generateBox.size.y * m_generateBox.size.z) / m_actualNumParticles;
        m_particleRadius3 = m_particleRadius * m_particleRadius * m_particleRadius;

        m_particles = new Particle[m_actualNumParticles];
        m_objects = new Transform[m_actualNumParticles];

        float posX = min.x;
        for (int i = 0; i < particleCount.x; i++)
        {
            float posY = min.y;
            for (int j = 0; j < particleCount.y; j++)
            {
                float posZ = min.z;
                for (int k = 0; k < particleCount.z; k++)
                {
                    int index = particleCount.y * particleCount.z * i + particleCount.z * j + k;
                    m_objects[index] = Instantiate(particleObject, transform).transform;
                    m_particles[index].position.x = posX + Random.Range(-0.1f, 0.1f);
                    m_particles[index].position.y = posY + Random.Range(-0.1f, 0.1f);
                    m_particles[index].position.z = posZ + Random.Range(-0.1f, 0.1f);
                    m_particles[index].velocity = Vector3.zero;
                    m_particles[index].acceleration = Vector3.zero;
                    m_particles[index].pressure = 0f;
                    m_objects[index].position = m_particles[index].position;

                    posZ += step;
                }
                posY += step;
            }
            posX += step;
        }
    }

    private void InitializeAccelerate()
    {
        m_particleRadius2 = m_particleRadius * m_particleRadius;
        Vector3 size = m_boundingBox.size;
        m_gridDimension = new Vector3Int(Mathf.CeilToInt(size.x / m_gridSize), Mathf.CeilToInt(size.y / m_gridSize), Mathf.CeilToInt(size.z / m_gridSize));

        m_grid = new List<int>[m_gridDimension.x, m_gridDimension.y, m_gridDimension.z];
        m_neighbours = new int[MAX_NEIGHBOUR * m_actualNumParticles];
        m_gridIndices = new Vector3Int[m_actualNumParticles];

        for (int i = 0; i < m_gridDimension.x; i++)
        {
            for (int j = 0; j < m_gridDimension.y; j++)
            {
                for (int k = 0; k < m_gridDimension.z; k++)
                {
                    m_grid[i, j, k] = new List<int>();
                }
            }
        }
    }

    private void InitializeKernels()
    {
        m_particleBuffer = new ComputeBuffer(m_actualNumParticles, sizeof(float) * 11 + sizeof(int));
        computeSPH.SetBuffer(m_pressureKernelIndex, "particles", m_particleBuffer);
        computeSPH.SetBuffer(m_forceKernelIndex, "particles", m_particleBuffer);

        m_neighbourBuffer = new ComputeBuffer(m_actualNumParticles * MAX_NEIGHBOUR, sizeof(int));
        computeSPH.SetBuffer(m_pressureKernelIndex, "neighbours", m_neighbourBuffer);
        computeSPH.SetBuffer(m_forceKernelIndex, "neighbours", m_neighbourBuffer);
        //computeSPH.SetFloat("particleCount", m_actualNumParticles);
        computeSPH.SetFloat("particleRadius", m_particleRadius);
        //computeSPH.SetFloat("particleMass", m_massPerParticle);
        //computeSPH.SetFloat("gamma", 7f);
        //computeSPH.SetFloat("karpa", 1000f);

        computeSPH.SetFloat("particleRadius2", m_particleRadius2);
        computeSPH.SetFloat("densityMultiplier", (m_massPerParticle * 315f) / (64f * Mathf.PI * m_particleRadius3 * m_particleRadius3 * m_particleRadius3));
        computeSPH.SetFloat("pressureConstant", m_pressureConstant);
        computeSPH.SetFloat("initialDensity", m_initialDensity);

        computeSPH.SetFloat("forceCoefficient", (m_massPerParticle * 45.0f) / (Mathf.PI * m_particleRadius3 * m_particleRadius3));
        computeSPH.SetFloat("viscosityCoefficient", 0.0001f);
        computeSPH.SetFloats("gravity", 0f, -9.81f, 0f);
    }

    private void Simulate()
    {
        SearchNeighbours();
        m_particleBuffer.SetData(m_particles);
        m_neighbourBuffer.SetData(m_neighbours);
        computeSPH.Dispatch(m_pressureKernelIndex, Mathf.CeilToInt(m_actualNumParticles / 64f), 1, 1);
        computeSPH.Dispatch(m_forceKernelIndex, Mathf.CeilToInt(m_actualNumParticles / 64f), 1, 1);
        m_particleBuffer.GetData(m_particles);

        Parallel.For(0, m_actualNumParticles, i =>
        {
            m_particles[i].velocity += m_particles[i].acceleration * m_dt;
            m_particles[i].velocity *= m_dampingRate;
            m_particles[i].position += m_particles[i].velocity * m_dt;
            CalculateBoundary(i);
        });

        for (int i = 0; i < m_actualNumParticles; i++)
        {
            m_objects[i].position = m_particles[i].position;
        }
    }

    private Vector3Int GetGridIndex(int particle)
    {
        Vector3 localPos = m_particles[particle].position - m_boundingBox.min;
        return new Vector3Int(Mathf.Clamp(Mathf.FloorToInt(localPos.x / m_gridSize), 0, m_gridDimension.x - 1), Mathf.Clamp(Mathf.FloorToInt(localPos.y / m_gridSize), 0, m_gridDimension.y - 1), Mathf.Clamp(Mathf.FloorToInt(localPos.z / m_gridSize), 0, m_gridDimension.z - 1));
    }

    private void GetNeighbour(int particle)
    {
        Vector3Int tempIndex = Vector3Int.zero;
        int neighbourCount = 0;
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    tempIndex.x = x + m_gridIndices[particle].x;
                    if (tempIndex.x < 0 || tempIndex.x >= m_gridDimension.x) continue;
                    tempIndex.y = y + m_gridIndices[particle].y;
                    if (tempIndex.y < 0 || tempIndex.y >= m_gridDimension.y) continue;
                    tempIndex.z = z + m_gridIndices[particle].z;
                    if (tempIndex.z < 0 || tempIndex.z >= m_gridDimension.z) continue;

                    foreach (int other in m_grid[tempIndex.x, tempIndex.y, tempIndex.z])
                    {
                        if (other != particle && (m_particles[particle].position - m_particles[other].position).sqrMagnitude < m_particleRadius2)
                        {
                            m_neighbours[MAX_NEIGHBOUR * particle + neighbourCount] = other;
                            neighbourCount++;
                            if (neighbourCount == MAX_NEIGHBOUR)
                            {
                                m_particles[particle].neighbourCount = MAX_NEIGHBOUR;
                                return;
                            }
                        }
                    }
                }
            }
        }
        m_particles[particle].neighbourCount = neighbourCount;
    }

    private void SearchNeighbours()
    {
        foreach (List<int> list in m_grid) list.Clear();

        for (int particle = 0; particle < m_actualNumParticles; particle++)
        {
            m_gridIndices[particle] = GetGridIndex(particle);
            m_grid[m_gridIndices[particle].x, m_gridIndices[particle].y, m_gridIndices[particle].z].Add(particle);
        }
        Parallel.For(0, m_actualNumParticles, i =>
        {
            GetNeighbour(i);
        });
    }

    private void CalculateBoundary(int particle)
    {
        float boundx = 1f;
        float boundy = 1f;
        float boundz = 1f;
        Vector3 min = m_boundingBox.min;
        Vector3 max = m_boundingBox.max;
        Vector3 position = m_particles[particle].position;
        Vector3 velocity = m_particles[particle].velocity;
        if (position.y < min.y)
        {
            if (velocity.y < 0f) boundy = 0f;
            m_particles[particle].position.y = min.y;
        }
        else if (position.y > max.y)
        {
            if (velocity.y > 0f) boundy = 0f;
            m_particles[particle].position.y = max.y;
        }
        if (position.x < min.x)
        {
            if (velocity.x < 0f) boundx = 0f;
            m_particles[particle].position.x = min.x;
        }
        else if (position.x > max.x)
        {
            if (velocity.x > 0f) boundx = 0f;
            m_particles[particle].position.x = max.x;
        }
        if (position.z < min.z)
        {
            if (velocity.z < 0f) boundz = 0f;
            m_particles[particle].position.z = min.z;
        }
        else if (position.z > max.z)
        {
            if (velocity.z > 0f) boundz = 0f;
            m_particles[particle].position.z = max.z;
        }
        m_particles[particle].velocity.x = velocity.x * boundx;
        m_particles[particle].velocity.y = velocity.y * boundy;
        m_particles[particle].velocity.z = velocity.z * boundz;
    }
}
