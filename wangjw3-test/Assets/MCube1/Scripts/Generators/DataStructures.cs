using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace MarchingCube1
{
    public class Particles
    {
        public readonly int size;
        public Vector3[] data;
    }

    /// <summary>
    /// A uniform partice filed
    /// </summary>
    public class VolumeMatrix
    {
        public readonly Vector3Int size;
        public float[] data;

        public VolumeMatrix ( Vector3Int size )
        {
            this.size = size;
            data = new float[ size.x * size.y * size.z ];
        }

        public float this[ int x , int y , int z ]
        {
            get => data[ x + y * size.x + z * size.y * size.x ];
            set
            {
                data[ x + y * size.x + z * size.y * size.x ] = value;
            }
        }

        public int count => size.x * size.y * size.z;

        public int voxel_count => ( size.x - 1 ) * ( size.y - 1 ) * ( size.z - 1 );

        public int index ( int x , int y , int z )
        {
            return x + y * size.x + z * size.y * size.x;
        }
    }

    /// <summary>
    /// A Marching Cube mesh with its generation info
    /// </summary>
    public class MarchingCubeMesh
    {
        #region Input

        public VolumeMatrix volume_matrix;

        #endregion Input

        #region Config

        public float iso_value;

        #endregion Config

        #region Info

        /// <summary>
        /// Matrix marking every particle whether it is bigger than the isovalue
        /// </summary>
        public bool[] mark_matrix;

        /// <summary>
        /// Array that store the actual index of each vertex on the edges
        /// </summary>
        public int[] vertices_indices;

        /// <summary>
        /// The start indices of the 3 axis edges' vertices
        /// </summary>
        public Vector3Int start_indices;

        #endregion Info

        #region Data

        public Mesh mesh;

        #endregion Data
    }
}