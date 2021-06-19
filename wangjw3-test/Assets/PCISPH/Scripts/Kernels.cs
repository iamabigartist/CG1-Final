using UnityEngine;

namespace SPHSimulator
{
    namespace Kernels
    {
        public abstract class Kernel3D
        {
            protected float m_h;

            public Kernel3D(float h)
            {
                m_h = h;
            }

            public abstract float W(Vector3 v);

            public abstract Vector3 GradW(Vector3 v);
        }

        public class WendlandQuinticC63D : Kernel3D
        {
            const float PI32 = 5.5683278f;

            public WendlandQuinticC63D(float h) : base(h) { }

            public override float W(Vector3 v)
            {
                float r = v.magnitude;
                if (r < Mathf.Epsilon || r > 2) return 0f;
                float q = r / m_h;
                float q2 = q * q;
                float h2 = m_h * m_h;
                float alpha = 1365f / (512f * Mathf.PI * h2 * m_h);
                return alpha * Mathf.Pow(1f - q * 0.5f, 8f) * (4f * q2 * q + 6.25f * q2 + 4f * q + 1f);
            }

            public override Vector3 GradW(Vector3 v)
            {
                float r = v.magnitude;
                if (r < Mathf.Epsilon || r > 2) return Vector3.zero;
                float q = r / m_h;
                float q2 = q * q;
                float h2 = m_h * m_h;
                float alpha = 1365f / (512f * Mathf.PI * h2 * m_h);
                float temp = 1f - 0.5f * q;
                float temp7 = Mathf.Pow(temp, 7f);
                float n = ((12f * q + 12.5f + 4f / q) * temp7 * temp - 4f / q * temp7 * (4f * q2 * q + 6.25f * q2 + 4f * q + 1f)) / h2;
                return n * v * alpha;
            }
        }

        public class Gaussian3D : Kernel3D
        {
            const float PI32 = 5.5683278f;

            private float m_h2;

            public Gaussian3D(float h) : base(h) {
                m_h2 = h * h;
            }

            public override float W(Vector3 v)
            {
                float r2 = v.sqrMagnitude;
                if (r2 > m_h2 * 9f) return 0f;
                return 1f / (PI32 * m_h2 * m_h) * Mathf.Exp(-r2 / m_h2);
            }

            public override Vector3 GradW(Vector3 v)
            {
                throw new System.NotImplementedException();
            }
        }
    }
}