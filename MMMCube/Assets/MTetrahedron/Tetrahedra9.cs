using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class Tetrahedra9 : MonoBehaviour
{
    #region Reference

    private MeshFilter meshFilter;
    private Mesh mesh;

    #endregion Reference

    public int size = 16;
    private bool[] matrix;

    private void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;
    }

    //从上到下顺时针标记，0~7， 0，7是对角线

    public void Generate()
    {
        #region GenerateMatrix

        for (int i = 0; i < matrix.Length; i++)
        {
            matrix[i] = Random.value > 0.5;
        }

        #endregion GenerateMatrix

        #region Tetrahedron

        //用一个数组标记所有顶点所在的边 bool[] vertices_mark
        //从1~6 遍历四面体，每个四面体根据下标得出所有三角形边的下标 {{1,2,3,3,4,5,-1,-1,-1},{2,3,4},{}}

        #endregion Tetrahedron
    }
}