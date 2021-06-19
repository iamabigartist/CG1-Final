using System.Threading.Tasks;

using UnityEngine;

namespace SPHSimulator
{
    public class PCISPHSimulatorSlow
    {
        private const float INITIAL_DENSITY = 1000f;

        private int m_actualNumParticles;
        private float m_h;
        private int m_iterations;
        private float m_viscosity;
        private Bounds m_generateBox;
        private Bounds m_boundingBox;
        private float m_force1;
        private float m_force2;

        private ComputeShader m_computePCISPH;

        private float m_massPerParticle;
        private int m_stepKernel;
        private float m_preDelta;

        private Vector3[] m_positionArray;
        private ComputeBuffer m_positionBuffer;

        private Vector3[] m_velocityArray;
        private ComputeBuffer m_velocityBuffer;

        private ComputeBuffer m_predictedPositionBuffer;
        private ComputeBuffer m_predictedVelocityBuffer;
        private ComputeBuffer m_accelerationExternalBuffer;
        private ComputeBuffer m_accelerationPressureBuffer;
        private ComputeBuffer m_pressureBuffer;
        private ComputeBuffer m_densityBuffer;
        private float[] m_densityArray;

        public PCISPHSimulatorSlow(int particleCount, float viscosity, float h, int iterations, float randomness, Bounds generate, Bounds bounds, float force1, float force2)
        {
            m_h = h;
            m_iterations = iterations;
            m_viscosity = viscosity;
            m_generateBox = generate;
            m_boundingBox = bounds;
            m_computePCISPH = Resources.Load<ComputeShader>("Shaders/PCISPHSlowComputeShader");
            m_force1 = force1;
            m_force2 = force2;

            m_stepKernel = m_computePCISPH.FindKernel( "Step" );

            CreateParticles( particleCount , randomness );
            InitializeKernels();
        }

        ~PCISPHSimulatorSlow ()
        {
            DisposeBuffer();
        }


        private Vector3 calculateForceAcc(Vector3 v, float strength)
        {
            Vector3 d = v.normalized;
            float len = v.magnitude;
            if (len <= 0)
            {
                Vector3 ret = new Vector3(0f, 0f, 0f);
                return ret;
            }
            float a = strength / (len + 0.0001f);
            a = (a > strength) ? strength : a;
            return a * d;
        }


        private void CreateParticles(int particleCount, float randomness)
        {
            Vector3 size = m_generateBox.size;
            Vector3 min = m_generateBox.min;
            float volume = size.x * size.y * size.z;
            float step = Mathf.Pow( volume / particleCount , 1f / 3f );
            Vector3 particleDimensionFloat = size / step;
            Vector3Int particleDimension = new Vector3Int(
                Mathf.RoundToInt( particleDimensionFloat.x ) ,
                Mathf.RoundToInt( particleDimensionFloat.y ) ,
                Mathf.RoundToInt( particleDimensionFloat.z ) );
            m_actualNumParticles = particleDimension.x * particleDimension.y * particleDimension.z;
            m_massPerParticle = INITIAL_DENSITY * volume / m_actualNumParticles;

            m_positionArray = new Vector3[ m_actualNumParticles ];
            m_velocityArray = new Vector3[ m_actualNumParticles ];
            m_densityArray = new float[ m_actualNumParticles ];

            Vector3 b_min = m_boundingBox.min; 
            Vector3 b_max = m_boundingBox.max;


            float random = Mathf.Clamp(randomness, 0f, 1f) * step;
            float posX = min.x;
            for ( int i = 0; i < particleDimension.x; i++ )
            {
                float posY = min.y;
                for ( int j = 0; j < particleDimension.y; j++ )
                {
                    float posZ = min.z;
                    for ( int k = 0; k < particleDimension.z; k++ )
                    {
                        int index = particleDimension.y * particleDimension.z * i + particleDimension.z * j + k;
                        m_positionArray[index].x = posX + Random.Range(-random, random);
                        m_positionArray[index].y = posY + Random.Range(-random, random);
                        m_positionArray[index].z = posZ + Random.Range(-random, random);

                        Vector3 tem1 = new Vector3(b_min.x, m_positionArray[index].y, m_positionArray[index].z);
                        Vector3 tem2 = new Vector3(b_max.x, m_positionArray[index].y, m_positionArray[index].z);
                        Vector3 a1 = calculateForceAcc((m_positionArray[index] - tem1), m_force1);
                        Vector3 a2 = calculateForceAcc((m_positionArray[index] - tem2), m_force2);
                        m_velocityArray[index] = (a1 + a2) * 0.01f;
                        //m_velocityArray[index] = new Vector3(20f, 0f, 0f);
                        m_densityArray[index] = INITIAL_DENSITY;

                        posZ += step;
                    }
                    posY += step;
                }
                posX += step;
            }
            Vector3 grad;
            float sumDot = 0f; ;
            Kernels.WendlandQuinticC63D kernel = new Kernels.WendlandQuinticC63D( m_h );
            for ( float x = -2f * m_h; x <= 2f * m_h; x += step )
            {
                for ( float y = -2f * m_h; y <= 2f * m_h; y += step )
                {
                    for ( float z = -2f * m_h; z <= 2f * m_h; z += step )
                    {
                        Vector3 point = new Vector3( x , y , z );
                        grad = kernel.GradW( -point );
                        sumDot += Vector3.Dot( grad , grad );
                    }
                }
            }
            m_preDelta = 1f / ( m_massPerParticle * m_massPerParticle * 2f / ( INITIAL_DENSITY * INITIAL_DENSITY ) * sumDot );
        }

        private void InitializeKernels ()
        {
            m_positionBuffer = new ComputeBuffer( m_actualNumParticles , sizeof( float ) * 3 );
            m_velocityBuffer = new ComputeBuffer( m_actualNumParticles , sizeof( float ) * 3 );
            m_predictedPositionBuffer = new ComputeBuffer( m_actualNumParticles , sizeof( float ) * 3 );
            m_predictedVelocityBuffer = new ComputeBuffer( m_actualNumParticles , sizeof( float ) * 3 );
            m_accelerationExternalBuffer = new ComputeBuffer( m_actualNumParticles , sizeof( float ) * 3 );
            m_accelerationPressureBuffer = new ComputeBuffer( m_actualNumParticles , sizeof( float ) * 3 );
            m_pressureBuffer = new ComputeBuffer( m_actualNumParticles , sizeof( float ) );
            m_densityBuffer = new ComputeBuffer( m_actualNumParticles , sizeof( float ) );

            m_computePCISPH.SetBuffer( m_stepKernel , "position" , m_positionBuffer );
            m_computePCISPH.SetBuffer( m_stepKernel , "velocity" , m_velocityBuffer );
            m_computePCISPH.SetBuffer( m_stepKernel , "prePosition" , m_predictedPositionBuffer );
            m_computePCISPH.SetBuffer( m_stepKernel , "preVelocity" , m_predictedVelocityBuffer );
            m_computePCISPH.SetBuffer( m_stepKernel , "Aext" , m_accelerationExternalBuffer );
            m_computePCISPH.SetBuffer( m_stepKernel , "Ap" , m_accelerationPressureBuffer );
            m_computePCISPH.SetBuffer( m_stepKernel , "p" , m_pressureBuffer );
            m_computePCISPH.SetBuffer( m_stepKernel , "d" , m_densityBuffer );

            m_computePCISPH.SetInt( "particleCount" , m_actualNumParticles );
            m_computePCISPH.SetFloats( "gravity" , 0f , -9.81f , 0f );
            m_computePCISPH.SetFloat( "particleMass" , m_massPerParticle );
            m_computePCISPH.SetFloat( "h" , m_h );
            m_computePCISPH.SetFloat( "d0" , INITIAL_DENSITY );
            m_computePCISPH.SetFloat( "u" , m_viscosity );
            m_computePCISPH.SetInt( "iterations" , m_iterations );

            m_densityBuffer.SetData( m_densityArray );
            m_positionBuffer.SetData( m_positionArray );
        }

        #region Interface

        public void DisposeBuffer ()
        {
            m_positionBuffer.Release();
            m_positionBuffer.Dispose();
            m_velocityBuffer.Release();
            m_velocityBuffer.Dispose();
            m_predictedPositionBuffer.Release();
            m_predictedPositionBuffer.Dispose();
            m_predictedVelocityBuffer.Release();
            m_predictedVelocityBuffer.Dispose();
            m_accelerationExternalBuffer.Release();
            m_accelerationExternalBuffer.Dispose();
            m_accelerationPressureBuffer.Release();
            m_accelerationPressureBuffer.Dispose();
            m_pressureBuffer.Release();
            m_pressureBuffer.Dispose();
            m_densityBuffer.Release();
            m_densityBuffer.Dispose();
        }

        public void Step ( float dt )
        {
            m_positionBuffer.SetData( m_positionArray );
            m_velocityBuffer.SetData( m_velocityArray );
            m_computePCISPH.SetFloat( "dt" , dt );
            m_computePCISPH.SetFloat( "delta" , CalculateDelta( dt ) );

            m_computePCISPH.Dispatch( m_stepKernel , Mathf.CeilToInt( m_actualNumParticles / 8f ) , 1 , 1 );

            m_positionBuffer.GetData( m_positionArray );
            m_velocityBuffer.GetData( m_velocityArray );

            Parallel.For( 0 , m_actualNumParticles , i =>
            {
                CalculateBoundary( i );
            } );
        }

        public ref Vector3[] particlePositionArray => ref m_positionArray;
        public ComputeBuffer particle_position_buffer => m_positionBuffer;

        //public int actualParticleCount => m_actualNumParticles;

        #endregion Interface

        private float CalculateDelta ( float dt )
        {
            return m_preDelta / dt;
        }

        private void CalculateBoundary ( int particle )
        {
            float boundx = 1f;
            float boundy = 1f;
            float boundz = 1f;
            Vector3 min = m_boundingBox.min;
            Vector3 max = m_boundingBox.max;
            Vector3 position = m_positionArray[ particle ];
            Vector3 velocity = m_velocityArray[ particle ];
            if ( position.y < min.y )
            {
                if ( velocity.y < 0f ) boundy = -1f;
                m_positionArray[ particle ].y = min.y;
            }
            else if ( position.y > max.y )
            {
                if ( velocity.y > 0f ) boundy = 1f;
                m_positionArray[ particle ].y = max.y - Mathf.Epsilon;
            }
            if ( position.x < min.x )
            {
                if ( velocity.x < 0f ) boundx = 1f;
                m_positionArray[ particle ].x = min.x;
            }
            else if ( position.x > max.x )
            {
                if ( velocity.x > 0f ) boundx = 1f;
                m_positionArray[ particle ].x = max.x - Mathf.Epsilon;
            }
            if ( position.z < min.z )
            {
                if ( velocity.z < 0f ) boundz = 1f;
                m_positionArray[ particle ].z = min.z;
            }
            else if ( position.z > max.z )
            {
                if ( velocity.z > 0f ) boundz = 1f;
                m_positionArray[ particle ].z = max.z - Mathf.Epsilon;
            }
            m_velocityArray[ particle ].x = velocity.x * boundx;
            m_velocityArray[ particle ].y = velocity.y * boundy;
            m_velocityArray[ particle ].z = velocity.z * boundz;
        }
    }
}