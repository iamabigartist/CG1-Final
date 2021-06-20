using System.Threading.Tasks;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class ParticleToVolumeFast
{
    private float m_gridStep;

    private Vector3Int m_gridDimension;
    private MarchingCube1.VolumeMatrix m_volume;
    private Vector3 m_localOrigin;
    private int m_k;
    private int m_count;
    private SPHSimulator.Kernels.Gaussian3D m_gaussian;

    private NativeArray<float3> m_queryPositions;
    private NativeArray<int> m_result;

    public ParticleToVolumeFast(float gridStep, float smoothLength, Bounds bounds, int k)
    {
        m_gridStep = gridStep;

        m_localOrigin = bounds.min;
        Vector3 s = bounds.size / gridStep;
        m_gridDimension.x = Mathf.FloorToInt(s.x) + 1;
        m_gridDimension.y = Mathf.FloorToInt(s.y) + 1;
        m_gridDimension.z = Mathf.FloorToInt(s.z) + 1;

        m_count = m_gridDimension.x * m_gridDimension.y * m_gridDimension.z;
        m_volume = new MarchingCube1.VolumeMatrix(m_gridDimension);
        m_k = k;
        m_gaussian = new SPHSimulator.Kernels.Gaussian3D(smoothLength);

        m_queryPositions = new NativeArray<float3>(m_count, Allocator.Persistent);
        m_result = new NativeArray<int>(m_count * k, Allocator.Persistent);

        float x = m_localOrigin.x;
        for (int i = 0; i < m_gridDimension.x; i++)
        {
            float y = m_localOrigin.y;
            for (int j = 0; j < m_gridDimension.y; j++)
            {
                float z = m_localOrigin.z;
                for (int m = 0; m < m_gridDimension.z; m++)
                {
                    m_queryPositions[i + j * m_gridDimension.x + m * m_gridDimension.y * m_gridDimension.x] = new float3(x, y, z);
                    z += gridStep;
                }
                y += gridStep;
            }
            x += gridStep;
        }
    }

    public MarchingCube1.VolumeMatrix volume => m_volume;

    public void Compute(KNN.KnnContainer container, Vector3[] points)
    {
        KNN.Jobs.QueryKNearestBatchJob query = new KNN.Jobs.QueryKNearestBatchJob(container, m_queryPositions, m_result);
        query.ScheduleBatch(m_queryPositions.Length, m_queryPositions.Length / 32).Complete();
        //Parallel.For(0, m_count, i =>
        //{
        //    m_volume.data[i] = 0f;
        //    for (int j = 0; j < m_k; j++)
        //    {
        //        m_volume.data[i] += m_gaussian.W(m_queryPositions[i] - m_result[m_k * i + j]);
        //    }
        //});
        for (int i = 0; i < m_count; i++)
        {
            m_volume.data[i] = 0f;
            for (int j = 0; j < m_k; j++)
            {
                m_volume.data[i] += m_gaussian.W((Vector3)m_queryPositions[i] - points[m_result[m_k * i + j]]);
            }
        }
    }

    public void Dispose()
    {
        m_queryPositions.Dispose();
        m_result.Dispose();
    }
}
