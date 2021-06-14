using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace MarchingCube1
{
    [ExecuteInEditMode]
    public class LittleMarchingCubeShower : MonoBehaviour
    {
        #region Reference

        private MeshFilter meshFilter;

        #endregion Reference

        #region Industry

        private RandomVolumeGenerator volumeGenerator;
        private MarchingCubeCPUGenerator cubeGenerator;
        private VolumeMatrix volume;
        private Vector3[] vertices;
        private int[] triangles;

        #endregion Industry

        #region Config

        public int particle_num;
        public float scale;

        #endregion Config

        // Start is called before the first frame update
        private void Awake()
        {
            meshFilter = GetComponent<MeshFilter>();
            volumeGenerator = new RandomVolumeGenerator();
            cubeGenerator = new MarchingCubeCPUGenerator();
            particle_num = 3;
            scale = 20;
            volume = null;
        }

        public void Generate()
        {
            volumeGenerator.Input(transform.position, Vector3.one * scale, scale / particle_num);
            volumeGenerator.Output(out volume);
            cubeGenerator.Input(volume, 2.5f);
            cubeGenerator.Output(out Mesh mesh, out vertices, out triangles);
            meshFilter.mesh = mesh;
        }

        //private void OnDrawGizmos()
        //{
        //    if (volume != null)
        //    {
        //        for (int i = 0; i < volume.data.Length; i++)
        //        {
        //            Vector4 particle = volume.data[i];
        //            Gizmos.color = particle.w > 2.5f ?
        //                    new Color(1, 0, 0, 0.5f) :
        //                    new Color(0, 0, 1, 0.5f);
        //            Gizmos.DrawCube(transform.position + new Vector3(particle.x, particle.y, particle.z), 0.2f * Vector3.one);
        //        }

        // for (int i = 0; i < meshFilter.mesh.vertices.Length; i++) { Gizmos.color = Color.green;
        // Gizmos.DrawSphere(transform.position + meshFilter.mesh.vertices[i], 0.2f); }

        //        for (int i = 0; i < triangles.Length; i += 3)
        //        {
        //            Vector3 a = vertices[triangles[i]] + transform.position;
        //            Vector3 b = vertices[triangles[i + 1]] + transform.position;
        //            Vector3 c = vertices[triangles[i + 2]] + transform.position;
        //            Gizmos.DrawLine(a, b);
        //            Gizmos.DrawLine(b, c);
        //            Gizmos.DrawLine(a, c);
        //        }
        //    }
        //}
    }
}