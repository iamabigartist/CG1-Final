using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;



public class PCISPH : MonoBehaviour
{
    [System.Serializable]
    public struct Particle
    {
        public Vector3 position;
        public Vector3 prePosition;
        public Vector3 velocity;
        public Vector3 preVelocity;
        public Vector3 Aext;
        public Vector3 Ap;
        public float pressure;
        public float density;
        public int neighbourCount;
    }

    const int MAX_NEIGHBOUR = 256;

    public ComputeShader computeSPH;
    public BoxCollider generateBox;
    public BoxCollider boundingBox;
    public GameObject particleObject;

    [SerializeField] private int m_numParticles;
    [SerializeField] private float m_initialDensity;
    [SerializeField] private float m_particleRadius;
    [SerializeField] private float m_gridSize;
    [SerializeField] private float m_h;
    [SerializeField] private int m_iterations;
    [SerializeField] private float bounce;
    //[SerializeField] private float m_dampingRate;
    //[SerializeField] private float m_pressureConstant;

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

    private int m_initKernel;
    private int m_predictKernel;
    private int m_correctKernel;
    private int m_forceKernel;
    private int m_finalKernel;
    private Bounds m_generateBox;
    private Bounds m_boundingBox;

    private float m_preDelta;

    private bool m_started = false;

    private void Start()
    {
        m_initKernel = computeSPH.FindKernel("Initialize");
        m_predictKernel = computeSPH.FindKernel("Predict");
        m_correctKernel = computeSPH.FindKernel("Correct");
        m_forceKernel = computeSPH.FindKernel("Force");
        m_finalKernel = computeSPH.FindKernel("Finalize");
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
            Simulate();
            //m_started = false;
        }
        if (Input.GetKey(KeyCode.L))
        {
            for (int i = 0; i < m_actualNumParticles; i++)
            {
                m_objects[i].position = m_particles[i].position;
            }
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
        m_particleRadius2 = m_particleRadius * m_particleRadius;
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
                    m_particles[index].position.x = posX;
                    m_particles[index].position.y = posY;
                    m_particles[index].position.z = posZ;
                    m_particles[index].position += new Vector3(Random.Range(-0.02f, 0.02f), Random.Range(-0.02f, 0.02f), Random.Range(-0.02f, 0.02f));
                    m_particles[index].velocity = Vector3.zero;
                    m_objects[index].position = m_particles[index].position;

                    posZ += step;
                }
                posY += step;
            }
            posX += step;
        }

        Vector3 grad;
        Vector3 sumGrad = Vector3.zero;
        float sumDot = 0;
        int count = 0;
        float density = 0;
        for (float x = -m_gridSize; x <= m_gridSize; x += step)
        {
            for (float y = -m_gridSize; y <= m_gridSize; y += step)
            {
                for (float z = -m_gridSize; z <= m_gridSize; z += step)
                {
                    Vector3 point = new Vector3(x, y, z);
                    if (point.sqrMagnitude < m_particleRadius2 && point.sqrMagnitude > Mathf.Epsilon)
                    {
                        density += W(point, m_h);
                        grad = GradW(point, m_h);
                        sumGrad += grad;
                        sumDot += Vector3.Dot(grad, grad);
                        count += 1;
                    }
                }
            }
        }
        Debug.Log("density = " + m_massPerParticle * density);
        Debug.Log("Counted " + count + " neighbours");
        m_preDelta = -1f / (m_massPerParticle * m_massPerParticle * 2f / (m_initialDensity * m_initialDensity) * (-Vector3.Dot(sumGrad, sumGrad) - sumDot));
    }

    private void InitializeAccelerate()
    {
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
        m_particleBuffer = new ComputeBuffer(m_actualNumParticles, sizeof(float) * 20 + sizeof(int));
        computeSPH.SetBuffer(m_initKernel, "particles", m_particleBuffer);
        computeSPH.SetBuffer(m_predictKernel, "particles", m_particleBuffer);
        computeSPH.SetBuffer(m_correctKernel, "particles", m_particleBuffer);
        computeSPH.SetBuffer(m_forceKernel, "particles", m_particleBuffer);
        computeSPH.SetBuffer(m_finalKernel, "particles", m_particleBuffer);

        m_neighbourBuffer = new ComputeBuffer(m_actualNumParticles * MAX_NEIGHBOUR, sizeof(int));
        computeSPH.SetBuffer(m_initKernel, "neighbours", m_neighbourBuffer);
        computeSPH.SetBuffer(m_predictKernel, "neighbours", m_neighbourBuffer);
        computeSPH.SetBuffer(m_correctKernel, "neighbours", m_neighbourBuffer);
        computeSPH.SetBuffer(m_forceKernel, "neighbours", m_neighbourBuffer);
        computeSPH.SetBuffer(m_finalKernel, "neighbours", m_neighbourBuffer);

        computeSPH.SetFloats("gravity", 0f, -9.81f, 0f);
        computeSPH.SetFloat("particleMass", m_massPerParticle);
        computeSPH.SetFloat("h", m_h);
        computeSPH.SetFloat("d0", m_initialDensity);
    }

    const float PI32 = 5.5683278f;

    private float W(Vector3 v, float h)
    {
        float r2 = v.sqrMagnitude;
        float h2 = h * h;
        return 1f / (h2 * h * PI32) * (2.5f - r2) * Mathf.Exp(-r2 / h2);
    }

    private Vector3 GradW(Vector3 v, float h)
    {
        float r2 = v.sqrMagnitude;
        float h2 = h * h;
        float n = -2f * Mathf.Exp(-r2 / h2) / (h2 * h * PI32) * ((2.5f - r2) / h2 + 1f);
        return n * v;
    }

    private float CalculateDelta(float dt)
    {
        return m_preDelta * (1f / (dt * dt));
    }

    private void Simulate()
    {
        SearchNeighbours();
        m_particleBuffer.SetData(m_particles);
        m_neighbourBuffer.SetData(m_neighbours);

        float dt = Time.deltaTime;
        //Debug.Log("dt = " + dt);
        computeSPH.SetFloat("dt", dt);
        computeSPH.SetFloat("delta", CalculateDelta(dt));
        computeSPH.Dispatch(m_initKernel, Mathf.CeilToInt(m_actualNumParticles / 8f), 1, 1);
        int it = 0;
        while (it < m_iterations)
        {
            computeSPH.Dispatch(m_predictKernel, Mathf.CeilToInt(m_actualNumParticles / 8f), 1, 1);
            computeSPH.Dispatch(m_correctKernel, Mathf.CeilToInt(m_actualNumParticles / 8f), 1, 1);
            computeSPH.Dispatch(m_forceKernel, Mathf.CeilToInt(m_actualNumParticles / 8f), 1, 1);
            it++;
        }
        computeSPH.Dispatch(m_finalKernel, Mathf.CeilToInt(m_actualNumParticles / 8f), 1, 1);
        m_particleBuffer.GetData(m_particles);

        Parallel.For(0, m_actualNumParticles, i =>
        {
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
            if (velocity.y < 0f) boundy = bounce;
            m_particles[particle].position.y = min.y;
        }
        else if (position.y > max.y)
        {
            if (velocity.y > 0f) boundy = bounce;
            m_particles[particle].position.y = max.y - Mathf.Epsilon;
        }
        if (position.x < min.x)
        {
            if (velocity.x < 0f) boundx = bounce;
            m_particles[particle].position.x = min.x;
        }
        else if (position.x > max.x)
        {
            if (velocity.x > 0f) boundx = bounce;
            m_particles[particle].position.x = max.x - Mathf.Epsilon;
        }
        if (position.z < min.z)
        {
            if (velocity.z < 0f) boundz = bounce;
            m_particles[particle].position.z = min.z;
        }
        else if (position.z > max.z)
        {
            if (velocity.z > 0f) boundz = bounce;
            m_particles[particle].position.z = max.z - Mathf.Epsilon;
        }
        m_particles[particle].velocity.x = velocity.x * boundx;
        m_particles[particle].velocity.y = velocity.y * boundy;
        m_particles[particle].velocity.z = velocity.z * boundz;
    }
}
