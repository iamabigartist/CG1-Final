using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class BigBallParticleShow : MonoBehaviour
{
    private ParticleRenderer particleRenderer;

    public Color main_color;
    public Color outline_color;
    public float oultine_size;
    public int particle_num;

    private ComputeShader _random_move_shader;

    private ComputeBuffer _particle_cloud;

    // Start is called before the first frame update
    private void Start ()
    {
        //_particle_cloud = new ComputeBuffer( particle_num , 3 * sizeof( float ) , ComputeBufferType.Structured );
        //for ( int i = 0; i < particle_num; i++ )
        //{
        //    while ( true )
        //    {
        //        Vector3 v = new Vector3(
        //            Random.Range( 0 , 200.0f ) ,
        //            Random.Range( 0 , 200.0f ) ,
        //            Random.Range( 0 , 200.0f ) );
        //    }
        //}
    }

    // Update is called once per frame
    private void Update ()
    {
        //_random_move_shader.
        //_random_move_shader.Dispatch(0,)
    }
}