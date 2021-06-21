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
    public void On ( ComputeBuffer particle_buffer , Color main_color )
    {
        _particle_buffer = particle_buffer;
        Config( main_color );
    }

    public void Config ( Color main_color )
    {
        _main_color = main_color;
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

        _cloud_render.SetPass( 0 );
        Graphics.DrawProceduralNow( MeshTopology.Points , _particle_buffer.count );
    }

    #endregion Interface
}