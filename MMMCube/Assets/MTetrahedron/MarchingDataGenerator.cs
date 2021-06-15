using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class MarchingDataGenerator : ScriptableObject
{
    public int cube_situation = 256;
    public int max_triangle = 12;
    public int[,] triangulation;

    private void Init()
    {
        triangulation = new int[cube_situation, max_triangle * 3];
        for (int i = 0; i < cube_situation; i++)
        {
            for (int j = 0; j < max_triangle; j++)
            {
                triangulation[i, j] = -1;
            }
        }
    }

    private void Calculate()
    {
        for (int i = 0; i < cube_situation; i++)
        {
        }
    }

    private void Awake()
    {
        Init();
    }
}