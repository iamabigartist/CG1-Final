using System.Threading.Tasks;

using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;

namespace SPHSimulator
{
    public struct Collision
    {
        public Vector3Int gridIndex;
        public Vector3 momentum;
        public Vector3 normal;
    }

    public class PCISPHSimulatorNeighbourSolidCouplingErosion
    {
        private readonly static int[] m_triangulation = {//256 16
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1 ,
             3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1 ,
             3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1 ,
             3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1 ,
             9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1 ,
             1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1 ,
             9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1 ,
             2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1 ,
             8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1 ,
             9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1 ,
             4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1 ,
             3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1 ,
             1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1 ,
             4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1 ,
             4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1 ,
             9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1 ,
             1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1 ,
             5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1 ,
             2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1 ,
             9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1 ,
             0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1 ,
             2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1 ,
             10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1 ,
             4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1 ,
             5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1 ,
             5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1 ,
             9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1 ,
             0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1 ,
             1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1 ,
             10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1 ,
             8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1 ,
             2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1 ,
             7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1 ,
             9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1 ,
             2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1 ,
             11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1 ,
             9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1 ,
             5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1 ,
             11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1 ,
             11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1 ,
             1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1 ,
             9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1 ,
             5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1 ,
             2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1 ,
             0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1 ,
             5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1 ,
             6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1 ,
             0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1 ,
             3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1 ,
             6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1 ,
             5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1 ,
             1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1 ,
             10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1 ,
             6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1 ,
             1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1 ,
             8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1 ,
             7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1 ,
             3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1 ,
             5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1 ,
             0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1 ,
             9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1 ,
             8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1 ,
             5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1 ,
             0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1 ,
             6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1 ,
             10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1 ,
             10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1 ,
             8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1 ,
             1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1 ,
             3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1 ,
             0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1 ,
             10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1 ,
             0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1 ,
             3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1 ,
             6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1 ,
             9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1 ,
             8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1 ,
             3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1 ,
             6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1 ,
             0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1 ,
             10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1 ,
             10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1 ,
             1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1 ,
             2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1 ,
             7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1 ,
             7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1 ,
             2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1 ,
             1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1 ,
             11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1 ,
             8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1 ,
             0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1 ,
             7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1 ,
             10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1 ,
             2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1 ,
             6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1 ,
             7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1 ,
             2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1 ,
             1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1 ,
             10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1 ,
             10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1 ,
             0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1 ,
             7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1 ,
             6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1 ,
             8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1 ,
             9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1 ,
             6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1 ,
             1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1 ,
             4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1 ,
             10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1 ,
             8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1 ,
             0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1 ,
             1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1 ,
             8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1 ,
             10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1 ,
             4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1 ,
             10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1 ,
             5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1 ,
             11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1 ,
             9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1 ,
             6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1 ,
             7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1 ,
             3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1 ,
             7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1 ,
             9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1 ,
             3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1 ,
             6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1 ,
             9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1 ,
             1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1 ,
             4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1 ,
             7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1 ,
             6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1 ,
             3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1 ,
             0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1 ,
             6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1 ,
             1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1 ,
             0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1 ,
             11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1 ,
             6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1 ,
             5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1 ,
             9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1 ,
             1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1 ,
             1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1 ,
             10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1 ,
             0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1 ,
             5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1 ,
             10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1 ,
             11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1 ,
             0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1 ,
             9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1 ,
             7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1 ,
             2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1 ,
             8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1 ,
             9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1 ,
             9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1 ,
             1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1 ,
             9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1 ,
             9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1 ,
             5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1 ,
             0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1 ,
             10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1 ,
             2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1 ,
             0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1 ,
             0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1 ,
             9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1 ,
             5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1 ,
             3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1 ,
             5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1 ,
             8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1 ,
             0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1 ,
             9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1 ,
             0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1 ,
             1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1 ,
             3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1 ,
             4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1 ,
             9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1 ,
             11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1 ,
             11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1 ,
             2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1 ,
             9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1 ,
             3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1 ,
             1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1 ,
             4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1 ,
             4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1 ,
             0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1 ,
             3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1 ,
             3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1 ,
             0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1 ,
             9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1 ,
             1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
             0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 ,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
        };

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
        private float m_erosion;

        private ComputeShader m_computePCISPH;

        private float m_massPerParticle;
        private int m_initKernel;
        private int m_predictKernel;
        private int m_correctKernel;
        private int m_forceKernel;
        private int m_finalKernel;
        private float m_preDelta;
        private Vector3Int m_particleDimension;
        private float m_particleStep;

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
        private ComputeBuffer m_volumeBuffer;
        private MarchingCube1.VolumeMatrix m_volume;
        private ComputeBuffer m_triangulationBuffer;

        private ComputeBuffer m_collisionBuffer;
        private Collision[] m_collisionArray;

        private ComputeBuffer m_neighbourBuffer;

        private NativeArray<int> m_neighbourArray;

        private KNN.KnnContainer m_knnContainer;
        private KNN.Jobs.KnnRebuildJob m_rebuildJob;

        public PCISPHSimulatorNeighbourSolidCouplingErosion ( float viscosity , float h , int iterations , Bounds bounds , Vector3Int dimension , float volumeStep , float isovalue , float force1 , float force2 , int neighbourCount , MarchingCube1.VolumeMatrix volume , float damping = 1f , float erosion = 1f )
        {
            m_neighbourCount = neighbourCount;
            m_h = h;
            m_iterations = iterations;
            m_viscosity = viscosity;
            m_boundingBox = bounds;
            m_computePCISPH = Resources.Load<ComputeShader>( "Shaders/PCISPHNeighbourSolidCouplingErosionComputeShader" );
            m_force1 = force1;
            m_force2 = force2;
            m_erosion = erosion;

            m_initKernel = m_computePCISPH.FindKernel( "Initialize" );
            m_predictKernel = m_computePCISPH.FindKernel( "Predict" );
            m_correctKernel = m_computePCISPH.FindKernel( "Correct" );
            m_forceKernel = m_computePCISPH.FindKernel( "Force" );
            m_finalKernel = m_computePCISPH.FindKernel( "Finalize" );

            m_computePCISPH.SetInts( "volumeDimension" , dimension.x , dimension.y , dimension.z );
            m_computePCISPH.SetFloats( "volumeOrigin" , bounds.min.x , bounds.min.y , bounds.min.z );
            m_computePCISPH.SetFloat( "volumeStep" , volumeStep );
            m_computePCISPH.SetFloat( "volumeScale" , 1f / volumeStep );
            m_computePCISPH.SetFloat( "isovalue" , isovalue );
            m_computePCISPH.SetFloat( "damping" , damping );

            m_volumeBuffer = new ComputeBuffer( dimension.x * dimension.y * dimension.z , sizeof( float ) );
            m_volume = new MarchingCube1.VolumeMatrix( dimension );
            m_volume.data = volume.data;
            m_computePCISPH.SetBuffer( m_finalKernel , "volume" , m_volumeBuffer );
            m_triangulationBuffer = new ComputeBuffer( 256 * 16 , sizeof( int ) );
            m_computePCISPH.SetBuffer( m_finalKernel , "triangulation" , m_triangulationBuffer );
            m_triangulationBuffer.SetData( m_triangulation );
        }

        private Vector3 calculateForceAcc ( Vector3 v , float strength )
        {
            Vector3 d = v.normalized;
            float len = v.magnitude;
            if (len <= 0)
            {
                Vector3 ret = new Vector3( 0f , 0f , 0f );
                return ret;
            }
            float a = strength / ( len + 0.0001f );
            a = ( a > strength ) ? strength : a;
            return a * d;
        }

        public void ResetParticles ( float randomness )
        {
            Vector3 min = m_generateBox.min;
            Vector3 origin = m_boundingBox.min;
            float random = Mathf.Clamp( randomness , 0f , 1f ) * m_particleStep;
            float posX = min.x;
            for (int i = 0; i < m_particleDimension.x; i++)
            {
                float posY = min.y;
                for (int j = 0; j < m_particleDimension.y; j++)
                {
                    float posZ = min.z;
                    for (int k = 0; k < m_particleDimension.z; k++)
                    {
                        int index = m_particleDimension.y * m_particleDimension.z * i + m_particleDimension.z * j + k;
                        m_positionNative[index] = new float3(
                            posX + UnityEngine.Random.Range( -random , random ) ,
                            posY + UnityEngine.Random.Range( -random , random ) ,
                            posZ + UnityEngine.Random.Range( -random , random ) );
                        m_positionArray[index] = m_positionNative[index];

                        Vector3 tem1 = new Vector3( origin.x , m_positionArray[index].y , m_positionArray[index].z );
                        Vector3 tem2 = new Vector3( origin.x , m_positionArray[index].y , m_positionArray[index].z );
                        Vector3 a1 = calculateForceAcc( ( m_positionArray[index] - tem1 ) , m_force1 );
                        Vector3 a2 = calculateForceAcc( ( m_positionArray[index] - tem2 ) , m_force2 );
                        m_velocityArray[index] = ( a1 + a2 ) * 0.01f;

                        m_densityArray[index] = INITIAL_DENSITY;

                        posZ += m_particleStep;
                    }
                    posY += m_particleStep;
                }
                posX += m_particleStep;
            }
        }

        public void CreateParticles ( int particleCount , float randomness , Bounds generate )
        {
            m_generateBox = generate;
            Vector3 size = m_generateBox.size;
            Vector3 min = m_generateBox.min;
            float volume = size.x * size.y * size.z;
            m_particleStep = Mathf.Pow( volume / particleCount , 1f / 3f );
            Vector3 particleDimensionFloat = size / m_particleStep;
            m_particleDimension = new Vector3Int(
                Mathf.RoundToInt( particleDimensionFloat.x ) ,
                Mathf.RoundToInt( particleDimensionFloat.y ) ,
                Mathf.RoundToInt( particleDimensionFloat.z ) );
            m_actualNumParticles = m_particleDimension.x * m_particleDimension.y * m_particleDimension.z;
            m_massPerParticle = INITIAL_DENSITY * volume / m_actualNumParticles;

            m_positionNative = new NativeArray<float3>( m_actualNumParticles , Allocator.Persistent );
            m_positionArray = new Vector3[m_actualNumParticles];
            m_velocityArray = new Vector3[m_actualNumParticles];
            m_densityArray = new float[m_actualNumParticles];

            m_knnContainer = new KNN.KnnContainer( m_positionNative , true , Allocator.Persistent );
            m_rebuildJob = new KNN.Jobs.KnnRebuildJob( m_knnContainer );
            m_neighbourArray = new NativeArray<int>( m_actualNumParticles * m_neighbourCount , Allocator.Persistent );

            m_collisionArray = new Collision[m_actualNumParticles];

            Vector3 b_min = m_boundingBox.min;
            Vector3 b_max = m_boundingBox.max;

            float random = Mathf.Clamp( randomness , 0f , 1f ) * m_particleStep;
            float posX = min.x;
            for (int i = 0; i < m_particleDimension.x; i++)
            {
                float posY = min.y;
                for (int j = 0; j < m_particleDimension.y; j++)
                {
                    float posZ = min.z;
                    for (int k = 0; k < m_particleDimension.z; k++)
                    {
                        int index = m_particleDimension.y * m_particleDimension.z * i + m_particleDimension.z * j + k;
                        m_positionNative[index] = new float3(
                            posX + UnityEngine.Random.Range( -random , random ) ,
                            posY + UnityEngine.Random.Range( -random , random ) ,
                            posZ + UnityEngine.Random.Range( -random , random ) );
                        m_positionArray[index] = m_positionNative[index];

                        Vector3 tem1 = new Vector3( b_min.x , m_positionArray[index].y , m_positionArray[index].z );
                        Vector3 tem2 = new Vector3( b_max.x , m_positionArray[index].y , m_positionArray[index].z );
                        Vector3 a1 = calculateForceAcc( ( m_positionArray[index] - tem1 ) , m_force1 );
                        Vector3 a2 = calculateForceAcc( ( m_positionArray[index] - tem2 ) , m_force2 );
                        m_velocityArray[index] = ( a1 + a2 ) * 0.01f;

                        m_densityArray[index] = INITIAL_DENSITY;

                        posZ += m_particleStep;
                    }
                    posY += m_particleStep;
                }
                posX += m_particleStep;
            }

            Vector3 grad;
            float sumDot = 0f; ;
            Kernels.WendlandQuinticC63D kernel = new Kernels.WendlandQuinticC63D( m_h );
            for (float x = -2f * m_h; x <= 2f * m_h; x += m_particleStep)
            {
                for (float y = -2f * m_h; y <= 2f * m_h; y += m_particleStep)
                {
                    for (float z = -2f * m_h; z <= 2f * m_h; z += m_particleStep)
                    {
                        Vector3 point = new Vector3( x , y , z );
                        grad = kernel.GradW( -point );
                        sumDot += Vector3.Dot( grad , grad );
                    }
                }
            }
            m_preDelta = 1f / ( m_massPerParticle * m_massPerParticle * 2f / ( INITIAL_DENSITY * INITIAL_DENSITY ) * sumDot );

            InitializeKernels();
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
            m_collisionBuffer = new ComputeBuffer( m_actualNumParticles , sizeof( int ) * 3 + sizeof( float ) * 6 );

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
            m_computePCISPH.SetBuffer( m_finalKernel , "collision" , m_collisionBuffer );

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
            m_volumeBuffer.Release();
            m_volumeBuffer.Dispose();
            m_triangulationBuffer.Release();
            m_triangulationBuffer.Dispose();
            m_collisionBuffer.Release();
            m_collisionBuffer.Dispose();

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

            m_neighbourBuffer.SetData( m_neighbourArray );
            m_positionBuffer.SetData( m_positionArray );
            m_velocityBuffer.SetData( m_velocityArray );
            m_volumeBuffer.SetData( m_volume.data );
            m_computePCISPH.SetFloat( "dt" , dt );
            m_computePCISPH.SetFloat( "delta" , CalculateDelta( dt ) );

            m_computePCISPH.Dispatch( m_initKernel , Mathf.CeilToInt( m_actualNumParticles / 8f ) , 1 , 1 );
            int it = 0;
            while (it < m_iterations)
            {
                m_computePCISPH.Dispatch( m_predictKernel , Mathf.CeilToInt( m_actualNumParticles / 8f ) , 1 , 1 );
                m_computePCISPH.Dispatch( m_correctKernel , Mathf.CeilToInt( m_actualNumParticles / 8f ) , 1 , 1 );
                m_computePCISPH.Dispatch( m_forceKernel , Mathf.CeilToInt( m_actualNumParticles / 8f ) , 1 , 1 );
                it++;
            }
            m_computePCISPH.Dispatch( m_finalKernel , Mathf.CeilToInt( m_actualNumParticles / 8f ) , 1 , 1 );

            m_positionBuffer.GetData( m_positionArray );
            m_velocityBuffer.GetData( m_velocityArray );
            m_collisionBuffer.GetData( m_collisionArray );

            Parallel.For( 0 , m_actualNumParticles , i =>
            {
                CalculateBoundary( i );
            } );

            float N = Mathf.Sqrt( 1f / 3f );

            for (int i = 0; i < m_actualNumParticles; i++)
            {
                m_positionNative[i] = m_positionArray[i];

                if (m_collisionArray[i].gridIndex.x == -1) continue;

                float projection = Vector3.Dot( m_collisionArray[i].normal , -m_collisionArray[i].momentum );
                float projection2 = projection * projection;

                float dot = Vector3.Dot( -m_collisionArray[i].normal , new Vector3( -N , -N , -N ) );
                if (dot > 0) m_volume[m_collisionArray[i].gridIndex] -= dot * m_erosion * projection2;

                dot = Vector3.Dot( -m_collisionArray[i].normal , new Vector3( N , -N , -N ) );
                if (dot > 0) m_volume[m_collisionArray[i].gridIndex + new Vector3Int( 1 , 0 , 0 )] -= dot * m_erosion * projection2;

                dot = Vector3.Dot( -m_collisionArray[i].normal , new Vector3( -N , N , -N ) );
                if (dot > 0) m_volume[m_collisionArray[i].gridIndex + new Vector3Int( 0 , 1 , 0 )] -= dot * m_erosion * projection2;

                dot = Vector3.Dot( -m_collisionArray[i].normal , new Vector3( -N , -N , N ) );
                if (dot > 0) m_volume[m_collisionArray[i].gridIndex + new Vector3Int( 0 , 0 , 1 )] -= dot * m_erosion * projection2;

                dot = Vector3.Dot( -m_collisionArray[i].normal , new Vector3( N , N , -N ) );
                if (dot > 0) m_volume[m_collisionArray[i].gridIndex + new Vector3Int( 1 , 1 , 0 )] -= dot * m_erosion * projection2;

                dot = Vector3.Dot( -m_collisionArray[i].normal , new Vector3( N , -N , N ) );
                if (dot > 0) m_volume[m_collisionArray[i].gridIndex + new Vector3Int( 1 , 0 , 1 )] -= dot * m_erosion * projection2;

                dot = Vector3.Dot( -m_collisionArray[i].normal , new Vector3( -N , N , N ) );
                if (dot > 0) m_volume[m_collisionArray[i].gridIndex + new Vector3Int( 0 , 1 , 1 )] -= dot * m_erosion * projection2;

                dot = Vector3.Dot( -m_collisionArray[i].normal , new Vector3( N , N , N ) );
                if (dot > 0) m_volume[m_collisionArray[i].gridIndex + new Vector3Int( 1 , 1 , 1 )] -= dot * m_erosion * projection2;
            }
        }

        public ref Vector3[] particlePositionArray => ref m_positionArray;
        public ComputeBuffer particle_position_buffer => m_positionBuffer;
        public KNN.KnnContainer KNNContainer => m_knnContainer;

        public MarchingCube1.VolumeMatrix terrainVolume => m_volume;

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
            Vector3 position = m_positionArray[particle];
            Vector3 velocity = m_velocityArray[particle];
            if (position.y < min.y)
            {
                if (velocity.y < 0f) boundy = -1f;
                m_positionArray[particle].y = min.y;
            }
            else if (position.y > max.y)
            {
                if (velocity.y > 0f) boundy = 1f;
                m_positionArray[particle].y = max.y - Mathf.Epsilon;
            }
            if (position.x < min.x)
            {
                if (velocity.x < 0f) boundx = 1f;
                m_positionArray[particle].x = min.x;
            }
            else if (position.x > max.x)
            {
                if (velocity.x > 0f) boundx = 1f;
                m_positionArray[particle].x = max.x - Mathf.Epsilon;
            }
            if (position.z < min.z)
            {
                if (velocity.z < 0f) boundz = 1f;
                m_positionArray[particle].z = min.z;
            }
            else if (position.z > max.z)
            {
                if (velocity.z > 0f) boundz = 1f;
                m_positionArray[particle].z = max.z - Mathf.Epsilon;
            }
            m_velocityArray[particle].x = velocity.x * boundx;
            m_velocityArray[particle].y = velocity.y * boundy;
            m_velocityArray[particle].z = velocity.z * boundz;
        }
    }
}