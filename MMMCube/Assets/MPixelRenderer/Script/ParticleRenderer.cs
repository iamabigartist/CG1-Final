using System.Collections;
using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// This renderer use a input <see cref="ComputeBuffer"/> and the PointCloudShader to render a point cloud
/// </summary>
public class ParticleRenderer : MonoBehaviour
{
    private Material _cloud_render;
    private ComputeBuffer _particle_buffer;

    #region Config

    private Color _main_color;
    private Color _outline_color;
    private float _outline_size;

    #endregion Config

    private void Start ()
    {
        _particle_buffer = null;
        _cloud_render = new Material( Shader.Find( "Point Cloud" ) );
    }

    #region Interface

    /// <summary>
    /// </summary>
    /// <param name="outline_size">The ouline paticle pos = outline_size * paticle pos</param>
    public void SetData ( ComputeBuffer particle_buffer , Color main_color , Color outline_color , float outline_size )
    {
        _particle_buffer = particle_buffer;
        _main_color = main_color;
        _outline_color = outline_color;
        _outline_size = outline_size;
    }

    #endregion Interface

    #region Render

    private void OnRenderObject ()
    {
        if ( _particle_buffer == null ) return;

        _cloud_render.SetBuffer( "cloud" , _particle_buffer );
        _cloud_render.SetPass( 0 );
        Graphics.DrawProceduralNow( MeshTopology.Points , _particle_buffer.count );
    }

    #endregion Render
}