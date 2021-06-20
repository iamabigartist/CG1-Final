using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class AppendStructureBufferTest : MonoBehaviour
{
    private ComputeShader append_test;

    private void Start ()
    {
        //    const int group_size = 16;
        // int triangle_num = 64;
        //int repeat_num = 5;
        //int[] triangle_indices;
        //int[] indices;
        //int[] result_triangle_indices;
        //int[] result_num;
        //int[] result_stride;
        //append_test = Resources.Load<ComputeShader>( "ComputeShaders/AppendTest" );

        //result_triangle_indices = new int[ triangle_num * repeat_num ];
        //indices = new int[ triangle_num ];
        //triangle_indices = new int[ triangle_num ];
        //result_num = new int[ triangle_num * repeat_num ];
        //result_stride = new int[ triangle_num * repeat_num ];
        //for ( int i = 0; i < triangle_num; i++ )
        //{
        //    triangle_indices[ i ] = i;
        //}

        //var clear0 = new int[ triangle_num ];
        //for ( int i = 0; i < triangle_num; i++ ) { clear0[ i ] = 0; }
        //var clear1 = new int[ triangle_num * repeat_num ];
        //for ( int i = 0; i < triangle_num * repeat_num; i++ ) { clear1[ i ] = -1; }
        //ComputeBuffer cb_indices, cb_result_triangle_indices, cb_triangle_indices, cb_num, cb_stride;
        //cb_indices = new ComputeBuffer( triangle_num , sizeof( int ) );
        //cb_result_triangle_indices = new ComputeBuffer( triangle_num * repeat_num , sizeof( int ) , ComputeBufferType.Append );
        //cb_triangle_indices = new ComputeBuffer( triangle_num , sizeof( int ) );
        //cb_num = new ComputeBuffer( triangle_num * repeat_num , sizeof( int ) );
        //cb_stride = new ComputeBuffer( triangle_num * repeat_num , sizeof( int ) );

        //cb_triangle_indices.SetData( triangle_indices );
        //cb_num.SetData( clear1 ); cb_indices.SetData( clear0 );
        //cb_result_triangle_indices.SetData( clear1 );
        //cb_stride.SetData( clear1 );

        //append_test.SetBuffer( 0 , "result_triangle_indices" , cb_result_triangle_indices );
        //append_test.SetBuffer( 0 , "indices" , cb_indices );
        //append_test.SetBuffer( 0 , "triangle_indices" , cb_triangle_indices );
        //append_test.SetBuffer( 0 , "num" , cb_num );
        //append_test.SetBuffer( 0 , "stride" , cb_stride );
        //append_test.SetInt( "triangle_num" , triangle_num );
        //append_test.SetInt( "repeat_num" , repeat_num );

        //append_test.Dispatch( 0 , Mathf.CeilToInt( triangle_num / ( float ) group_size ) , 1 , 1 );

        //cb_indices.GetData( indices );
        //cb_result_triangle_indices.GetData( result_triangle_indices );
        //cb_num.GetData( result_num );
        //cb_stride.GetData( result_stride );

        //print( $"result: [{String.Join( "," , indices )} ]" );
        //print( $"result: [{String.Join( "," , result_triangle_indices )} ]" );
        //print( $"num: [{String.Join( "," , result_num )} ]" );
        //print( $"stride: [{String.Join( "," , result_stride )} ]" );

        //cb_indices.Release();
        //cb_num.Release();
        //cb_result_triangle_indices.Release();
        //cb_stride.Release();
        //cb_triangle_indices.Release();

        int num = 1600;

        append_test = Resources.Load<ComputeShader>( "ComputeShaders/AppendTest2" );
        var clear0 = new int[ num ]; for ( int i = 0; i < num; i++ ) { clear0[ i ] = 0; }

        var varies1 = new int[ num ]; for ( int i = 0; i < num; i++ ) { varies1[ i ] = i; }

        ComputeBuffer result = new ComputeBuffer( num , sizeof( int ) , ComputeBufferType.Append );
        ComputeBuffer dict = new ComputeBuffer( num , sizeof( int ) );
        result.SetData( clear0 ); result.SetCounterValue( 0 );
        dict.SetData( varies1 );
        append_test.SetBuffer( 0 , "result" , result );
        append_test.SetBuffer( 0 , "dict" , dict );
        append_test.Dispatch( 0 , Mathf.CeilToInt( num / 8 ) , 1 , 1 );

        int[] a_result = new int[ result.count ];
        result.GetData( a_result );
        print( $"[ {String.Join( "," , a_result )} ]" );
    }
}