using System.Threading.Tasks;

using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;

namespace SPHSimulator
{
    public class PCISPHSimulatorNeighbour
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
        private int m_neighbourCount;

        private ComputeShader m_computePCISPH;

        private float m_massPerParticle;
        private int m_initKernel;
        private int m_predictKernel;
        private int m_correctKernel;
        private int m_forceKernel;
        private int m_finalKernel;
        private float m_preDelta;

        private NativeArray<float3> m_positionNative;
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

        private ComputeBuffer m_neighbourBuffer;
        private NativeArray<int> m_neighbourArray;

        private KNN.KnnContainer m_knnContainer;
        private KNN.Jobs.KnnRebuildJob m_rebuildJob;
        private KNN.Jobs.QueryKNearestJob m_queryJob;

        public PCISPHSimulatorNeighbour ( int particleCount , float viscosity , float h , int iterations , float randomness , Bounds generate , Bounds bounds , float force1 , float force2 , int neighbourCount )
        {
            m_neighbourCount = neighbourCount;
            m_h = h;
            m_iterations = iterations;
            m_viscosity = viscosity;
            m_generateBox = generate;
            m_boundingBox = bounds;
            m_computePCISPH = Resources.Load<ComputeShader>( "Shaders/PCISPHSlowNeighbourComputeShader" );
            m_force1 = force1;
            m_force2 = force2;

            m_initKernel = m_computePCISPH.FindKernel( "Initialize" );
            m_predictKernel = m_computePCISPH.FindKernel( "Predict" );
            m_correctKernel = m_computePCISPH.FindKernel( "Correct" );
            m_forceKernel = m_computePCISPH.FindKernel( "Force" );
            m_finalKernel = m_computePCISPH.FindKernel( "Finalize" );

            CreateParticles( particleCount , randomness );
            InitializeKernels();
        }

        private Vector3 calculateForceAcc ( Vector3 v , float strength )
        {
            Vector3 d = v.normalized;
            float len = v.magnitude;
            if ( len <= 0 )
            {
                Vector3 ret = new Vector3( 0f , 0f , 0f );
                return ret;
            }
            float a = strength / ( len + 0.0001f );
            a = ( a > strength ) ? strength : a;
            return a * d;
        }

        private void CreateParticles ( int particleCount , float randomness )
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

            m_positionNative = new NativeArray<float3>( m_actualNumParticles , Allocator.Persistent );
            m_positionArray = new Vector3[ m_actualNumParticles ];
            m_velocityArray = new Vector3[ m_actualNumParticles ];
            m_densityArray = new float[ m_actualNumParticles ];

            m_knnContainer = new KNN.KnnContainer( m_positionNative , false , Allocator.Persistent );
            m_rebuildJob = new KNN.Jobs.KnnRebuildJob( m_knnContainer );
            m_neighbourArray = new NativeArray<int>( m_actualNumParticles * m_neighbourCount , Allocator.Persistent );

            Vector3 b_min = m_boundingBox.min;
            Vector3 b_max = m_boundingBox.max;

            float random = Mathf.Clamp( randomness , 0f , 1f ) * step;
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
                        m_positionNative[ index ] = new float3(
                            posX + UnityEngine.Random.Range( -random , random ) ,
                            posY + UnityEngine.Random.Range( -random , random ) ,
                            posZ + UnityEngine.Random.Range( -random , random ) );
                        m_positionArray[ index ] = m_positionNative[ index ];

                        Vector3 tem1 = new Vector3( b_min.x , m_positionArray[ index ].y , m_positionArray[ index ].z );
                        Vector3 tem2 = new Vector3( b_max.x , m_positionArray[ index ].y , m_positionArray[ index ].z );
                        Vector3 a1 = calculateForceAcc( ( m_positionArray[ index ] - tem1 ) , m_force1 );
                        Vector3 a2 = calculateForceAcc( ( m_positionArray[ index ] - tem2 ) , m_force2 );
                        m_velocityArray[ index ] = ( a1 + a2 ) * 0.01f;

                        m_densityArray[ index ] = INITIAL_DENSITY;

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
            m_neighbourBuffer = new ComputeBuffer( m_actualNumParticles * m_neighbourCount , sizeof( int ) );

            m_computePCISPH.SetBuffer( m_initKernel , "position" , m_positionBuffer );
            m_computePCISPH.SetBuffer( m_initKernel , "velocity" , m_velocityBuffer );
            m_computePCISPH.SetBuffer( m_initKernel , "Aext" , m_accelerationExternalBuffer );
            m_computePCISPH.SetBuffer( m_initKernel , "Ap" , m_accelerationPressureBuffer );
            m_computePCISPH.SetBuffer( m_initKernel , "p" , m_pressureBuffer );
            m_computePCISPH.SetBuffer( m_initKernel , "d" , m_densityBuffer );
            m_computePCISPH.SetBuffer( m_initKernel , "neighbours" , m_neighbourBuffer );

            m_computePCISPH.SetBuffer( m_predictKernel , "position" , m_positionBuffer );
            m_computePCISPH.SetBuffer( m_predictKernel , "velocity" , m_velocityBuffer );
            m_computePCISPH.SetBuffer( m_predictKernel , "prePosition" , m_predictedPositionBuffer );
            m_computePCISPH.SetBuffer( m_predictKernel , "preVelocity" , m_predictedVelocityBuffer );
            m_computePCISPH.SetBuffer( m_predictKernel , "Aext" , m_accelerationExternalBuffer );
            m_computePCISPH.SetBuffer( m_predictKernel , "Ap" , m_accelerationPressureBuffer );

            m_computePCISPH.SetBuffer( m_correctKernel , "prePosition" , m_predictedPositionBuffer );
            m_computePCISPH.SetBuffer( m_correctKernel , "p" , m_pressureBuffer );
            m_computePCISPH.SetBuffer( m_correctKernel , "d" , m_densityBuffer );
            m_computePCISPH.SetBuffer( m_correctKernel , "neighbours" , m_neighbourBuffer );

            m_computePCISPH.SetBuffer( m_forceKernel , "prePosition" , m_predictedPositionBuffer );
            m_computePCISPH.SetBuffer( m_forceKernel , "p" , m_pressureBuffer );
            m_computePCISPH.SetBuffer( m_forceKernel , "d" , m_densityBuffer );
            m_computePCISPH.SetBuffer( m_forceKernel , "Ap" , m_accelerationPressureBuffer );
            m_computePCISPH.SetBuffer( m_forceKernel , "neighbours" , m_neighbourBuffer );

            m_computePCISPH.SetBuffer( m_finalKernel , "position" , m_positionBuffer );
            m_computePCISPH.SetBuffer( m_finalKernel , "velocity" , m_velocityBuffer );
            m_computePCISPH.SetBuffer( m_finalKernel , "Aext" , m_accelerationExternalBuffer );
            m_computePCISPH.SetBuffer( m_finalKernel , "Ap" , m_accelerationPressureBuffer );

            m_computePCISPH.SetInt( "particleCount" , m_actualNumParticles );
            m_computePCISPH.SetFloats( "gravity" , 0f , -9.81f , 0f );
            m_computePCISPH.SetFloat( "particleMass" , m_massPerParticle );
            m_computePCISPH.SetFloat( "h" , m_h );
            m_computePCISPH.SetFloat( "d0" , INITIAL_DENSITY );
            m_computePCISPH.SetFloat( "u" , m_viscosity );
            m_computePCISPH.SetInt( "iterations" , m_iterations );
            m_computePCISPH.SetInt( "neighbourCount" , m_neighbourCount );

            m_densityBuffer.SetData( m_densityArray );
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
            m_neighbourBuffer.Release();
            m_neighbourBuffer.Dispose();

            m_positionNative.Dispose();
            m_knnContainer.Dispose();
            m_neighbourArray.Dispose();
        }

        public void Step ( float dt )
        {
            m_rebuildJob.Schedule().Complete();
            KNN.Jobs.QueryKNearestBatchJob query = new KNN.Jobs.QueryKNearestBatchJob(
                m_knnContainer , m_positionNative , m_neighbourArray );
            query.ScheduleBatch( m_positionNative.Length , m_positionNative.Length >> 5 ).Complete();

            //int[] array = m_neighbourArray.ToArray();
            m_neighbourBuffer.SetData( m_neighbourArray );
            m_positionBuffer.SetData( m_positionArray );
            m_velocityBuffer.SetData( m_velocityArray );
            m_computePCISPH.SetFloat( "dt" , dt );
            m_computePCISPH.SetFloat( "delta" , CalculateDelta( dt ) );

            m_computePCISPH.Dispatch( m_initKernel , Mathf.CeilToInt( m_actualNumParticles / 8f ) , 1 , 1 );
            int it = 0;
            while ( it < m_iterations )
            {
                m_computePCISPH.Dispatch( m_predictKernel , Mathf.CeilToInt( m_actualNumParticles / 8f ) , 1 , 1 );
                m_computePCISPH.Dispatch( m_correctKernel , Mathf.CeilToInt( m_actualNumParticles / 8f ) , 1 , 1 );
                m_computePCISPH.Dispatch( m_forceKernel , Mathf.CeilToInt( m_actualNumParticles / 8f ) , 1 , 1 );
                it++;
            }
            m_computePCISPH.Dispatch( m_finalKernel , Mathf.CeilToInt( m_actualNumParticles / 8f ) , 1 , 1 );

            m_positionBuffer.GetData( m_positionArray );
            m_velocityBuffer.GetData( m_velocityArray );

            Parallel.For( 0 , m_actualNumParticles , i =>
            {
                CalculateBoundary( i );
            } );

            for ( int i = 0; i < m_actualNumParticles; i++ )
            {
                m_positionNative[ i ] = m_positionArray[ i ];
            }
        }

        public ref Vector3[] particlePositionArray => ref m_positionArray;
        public ComputeBuffer particle_position_buffer => m_positionBuffer;

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