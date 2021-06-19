using UnityEngine;

/// <summary>
/// This renderer use a input <see cref="ComputeBuffer"/> and the PointCloudShader to render a point cloud
/// </summary>
public class ParticleRenderer
{
    private Material _cloud_render;
    private ComputeBuffer _particle_buffer;

    #region Config

    private Color _main_color;
    private Color _outline_color;
    private float _outline_size;

    #endregion Config

    #region Interface

    public ParticleRenderer ()
    {
        _particle_buffer = null;
        _cloud_render = new Material( Shader.Find( "Point Cloud" ) );
    }

    /// <summary>
    /// </summary>
    /// <param name="outline_size">The ouline paticle pos = outline_size * paticle pos</param>
    public void On ( ComputeBuffer particle_buffer , Color main_color , Color outline_color , float outline_size )
    {
        _particle_buffer = particle_buffer;
        Config( main_color , outline_color , outline_size );
    }

    public void Config ( Color main_color , Color outline_color , float outline_size )
    {
        _main_color = main_color;
        _outline_color = outline_color;
        _outline_size = outline_size;
    }

    public void Off ()
    {
        _particle_buffer = null;
    }

    public void Draw ()
    {
        if ( _particle_buffer == null ) return;

        _cloud_render.SetBuffer( "cloud" , _particle_buffer );
        _cloud_render.SetColor( "MainColor" , _main_color );
        _cloud_render.SetColor( "OutlineColor" , _outline_color );
        _cloud_render.SetFloat( "OutlineSize" , _outline_size );

        _cloud_render.SetPass( 0 );
        Graphics.DrawProceduralNow( MeshTopology.Points , _particle_buffer.count );
        _cloud_render.SetPass( 1 );
        Graphics.DrawProceduralNow( MeshTopology.Points , _particle_buffer.count );

        Debug.Log( "Done" );
    }

    #endregion Interface
}