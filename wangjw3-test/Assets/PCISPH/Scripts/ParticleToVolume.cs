using System.Threading.Tasks;
using UnityEngine;

public class ParticleToVolume
{
    private float m_gridStep;

    private Vector3Int m_gridDimension;
    private MarchingCube1.VolumeMatrix m_volume;
    private Vector3 m_localOrigin;
    private int range;
    private SPHSimulator.Kernels.Gaussian3D m_gaussian;

    public ParticleToVolume(float gridStep, float smoothLength, Bounds bounds)
    {
        m_gridStep = gridStep;

        m_localOrigin = bounds.min;
        Vector3 s = bounds.size / gridStep;
        m_gridDimension.x = Mathf.CeilToInt(s.x) + 2;
        m_gridDimension.y = Mathf.CeilToInt(s.y) + 2;
        m_gridDimension.z = Mathf.CeilToInt(s.z) + 2;

        m_volume = new MarchingCube1.VolumeMatrix(m_gridDimension);
        range = Mathf.CeilToInt(3f * smoothLength / m_gridStep);
        m_gaussian = new SPHSimulator.Kernels.Gaussian3D(smoothLength);
    }

    public MarchingCube1.VolumeMatrix volume => m_volume;

    public void Compute(ref Vector3[] particles)
    {
        Parallel.For(0, volume.data.Length, i =>
        {
            m_volume.data[i] = 0f;
        });
        int x, y, z;
        for (int i = 0; i < particles.Length; i++)
        {
            Vector3 localPos = particles[i] - m_localOrigin;
            x = Mathf.Clamp(Mathf.FloorToInt(localPos.x / m_gridStep) + 1, 1, m_gridDimension.x - 2);
            y = Mathf.Clamp(Mathf.FloorToInt(localPos.y / m_gridStep) + 1, 1, m_gridDimension.y - 2);
            z = Mathf.Clamp(Mathf.FloorToInt(localPos.z / m_gridStep) + 1, 1, m_gridDimension.z - 2);

            Parallel.For(-range, range + 1, m =>
            {
                int tX = Mathf.Clamp(x + m, 1, m_gridDimension.x - 2);
                int tY, tZ;
                Vector3 gridCenter;
                for (int n = -range; n <= range; n++)
                {
                    tY = Mathf.Clamp(y + n, 1, m_gridDimension.y - 2);
                    for (int k = -range; k <= range; k++)
                    {
                        tZ = Mathf.Clamp(z + k, 1, m_gridDimension.z - 2);
                        gridCenter.x = (tX - 0.5f) * m_gridStep;
                        gridCenter.y = (tY - 0.5f) * m_gridStep;
                        gridCenter.z = (tZ - 0.5f) * m_gridStep;
                        m_volume[tX, tY, tZ] += m_gaussian.W(gridCenter - localPos);
                    }
                }
            });
        }
    }
}
