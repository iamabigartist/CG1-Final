using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;



public class PCISPHSlow : MonoBehaviour
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
    }

    public ComputeShader computeSPH;
    public BoxCollider generateBox;
    public BoxCollider boundingBox;
    public GameObject particleObject;

    [SerializeField] private bool m_frameByFrame;
    [SerializeField] private int m_numParticles;
    [SerializeField] private float m_initialDensity;
    [SerializeField] private float m_h;
    [SerializeField] private int m_iterations;
    [SerializeField] private float m_bounce;
    [SerializeField] private float m_dt;
    [SerializeField] private bool m_randomness;
    //[SerializeField] private float m_dampingRate;
    //[SerializeField] private float m_pressureConstant;

    //Particles
    private int m_actualNumParticles;
    private ComputeBuffer m_particleBuffer;
    public Particle[] m_particles;
    private Transform[] m_objects;
    private float m_massPerParticle;

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
        InitializeKernels();
    }

    private void Update()
    {
        if (Input.GetButtonDown("Submit")) m_started = true;
        if (m_started)
        {
            Simulate();
            if (m_frameByFrame) m_started = false;
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
                    if (m_randomness) m_particles[index].position += new Vector3(Random.Range(-0.02f, 0.02f), Random.Range(-0.02f, 0.02f), Random.Range(-0.02f, 0.02f));
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
        for (float x = -2f * m_h; x <= 2f * m_h; x += step)
        {
            for (float y = -2f * m_h; y <= 2f * m_h; y += step)
            {
                for (float z = -2f * m_h; z <= 2f * m_h; z += step)
                {
                    Vector3 point = new Vector3(x, y, z);
                    density += W(-point, m_h);
                    grad = GradW(-point, m_h);
                    sumGrad += grad;
                    sumDot += Vector3.Dot(grad, grad);
                    count += 1;
                }
            }
        }
        Debug.Log("density = " + m_massPerParticle * density);
        Debug.Log("Counted " + count + " neighbours");
        Debug.Log("sumGrad: " + sumGrad + ", sumDot: " + sumDot);
        m_preDelta = -1f / (m_massPerParticle * m_massPerParticle * 2f / (m_initialDensity * m_initialDensity) * (-Vector3.Dot(sumGrad, sumGrad) - sumDot));
    }

    private void InitializeKernels()
    {
        m_particleBuffer = new ComputeBuffer(m_actualNumParticles, sizeof(float) * 20);
        computeSPH.SetBuffer(m_initKernel, "particles", m_particleBuffer);
        computeSPH.SetBuffer(m_predictKernel, "particles", m_particleBuffer);
        computeSPH.SetBuffer(m_correctKernel, "particles", m_particleBuffer);
        computeSPH.SetBuffer(m_forceKernel, "particles", m_particleBuffer);
        computeSPH.SetBuffer(m_finalKernel, "particles", m_particleBuffer);

        computeSPH.SetInt("particleCount", m_actualNumParticles);
        computeSPH.SetFloats("gravity", 0f, -9.81f, 0f);
        computeSPH.SetFloat("particleMass", m_massPerParticle);
        computeSPH.SetFloat("h", m_h);
        computeSPH.SetFloat("d0", m_initialDensity);
    }

    const float PI32 = 5.5683278f;

    private float W(Vector3 v, float h)
    {
        float r = v.magnitude;
        if (r < 0.0001f || r > 2) return 0f;
        float q = r / h;
        float q2 = q * q;
        float h2 = h * h;
        float alpha = 1365f / (512f * Mathf.PI * h2 * h);
        return alpha * Mathf.Pow(1f - q * 0.5f, 8f) * (4f * q2 * q + 6.25f * q2 + 4f * q + 1f);
    }

    private Vector3 GradW(Vector3 v, float h)
    {
        float r = v.magnitude;
        if (r < 0.0001f || r > 2) return Vector3.zero;
        float q = r / h;
        float q2 = q * q;
        float h2 = h * h;
        float alpha = 1365f / (512f * Mathf.PI * h2 * h);
        float temp = 1f - 0.5f * q;
        float temp7 = Mathf.Pow(temp, 7f);
        float n = ((12f * q + 12.5f + 4f / q) * temp7 * temp - 4f / q * temp7 * (4f * q2 * q + 6.25f * q2 + 4f * q + 1f)) / h2;
        return n * v * alpha;
    }

    private float CalculateDelta(float dt)
    {
        return m_preDelta * (1f / (dt));
    }

    private void Simulate()
    {
        m_particleBuffer.SetData(m_particles);

        float dt = m_dt < Mathf.Epsilon ? Time.deltaTime : m_dt;
        computeSPH.SetFloat("dt", dt);
        float delta = CalculateDelta(dt);
        //Debug.Log(delta);
        computeSPH.SetFloat("delta", delta);
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
            if (velocity.y < 0f) boundy = m_bounce;
            m_particles[particle].position.y = min.y;
        }
        else if (position.y > max.y)
        {
            if (velocity.y > 0f) boundy = m_bounce;
            m_particles[particle].position.y = max.y - Mathf.Epsilon;
        }
        if (position.x < min.x)
        {
            if (velocity.x < 0f) boundx = m_bounce;
            m_particles[particle].position.x = min.x;
        }
        else if (position.x > max.x)
        {
            if (velocity.x > 0f) boundx = m_bounce;
            m_particles[particle].position.x = max.x - Mathf.Epsilon;
        }
        if (position.z < min.z)
        {
            if (velocity.z < 0f) boundz = m_bounce;
            m_particles[particle].position.z = min.z;
        }
        else if (position.z > max.z)
        {
            if (velocity.z > 0f) boundz = m_bounce;
            m_particles[particle].position.z = max.z - Mathf.Epsilon;
        }
        m_particles[particle].velocity.x = velocity.x * boundx;
        m_particles[particle].velocity.y = velocity.y * boundy;
        m_particles[particle].velocity.z = velocity.z * boundz;
    }
}
