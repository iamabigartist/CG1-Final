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

    //���ϵ���˳ʱ���ǣ�0~7�� 0��7�ǶԽ���

    public void Generate()
    {
        #region GenerateMatrix

        for (int i = 0; i < matrix.Length; i++)
        {
            matrix[i] = Random.value > 0.5;
        }

        #endregion GenerateMatrix

        #region Tetrahedron

        //��һ�����������ж������ڵı� bool[] vertices_mark
        //��1~6 ���������壬ÿ������������±�ó����������αߵ��±� {{1,2,3,3,4,5,-1,-1,-1},{2,3,4},{}}

        #endregion Tetrahedron
    }
}