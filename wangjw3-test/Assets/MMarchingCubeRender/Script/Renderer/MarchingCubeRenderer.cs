using UnityEngine;
using MarchingCube1;
using Unity.Mathematics;

/// <summary>
/// This renderer use a input <see cref="VolumeMatrix"/> and the MarchingCubeShader to render the
/// MarchingCube in realtime.
/// </summary>
public class MarchingCubeRenderer
{
    private Material _cube_render;
    private ComputeBuffer _volume_buffer;
    private VolumeMatrix _volume_matrix;
    private int[] _index2xyz;
    private ComputeBuffer _index_buffer;

    #region Config

    private Color _main_color;
    private float _cube_size;
    private float _iso_value;
    private Vector3 _origin_pos;

    private float _zero_level;

    #endregion Config

    #region Interface

    public MarchingCubeRenderer ()
    {
        _volume_buffer = null;
        _cube_render = new Material( Shader.Find( "Volume2MarchingCube" ) );
    }

    /// <summary>
    /// </summary>
    /// <param name="outline_size">The ouline paticle pos = outline_size * paticle pos</param>
    public void On ( VolumeMatrix matrix , Color main_color , float cube_size , float iso_value , Vector3 origin_pos , float zero_level = 0 )
    {
        Off();

        _volume_matrix = matrix;
        _volume_buffer = new ComputeBuffer( _volume_matrix.count , sizeof( float ) );
        _index_buffer = new ComputeBuffer( _volume_matrix.count * 3 , sizeof( int ) );
        _index2xyz = new int[ _volume_matrix.count * 3 ];
        //Init the
        for ( int z = 0; z < _volume_matrix.size.z; z++ )
        {
            for ( int y = 0; y < _volume_matrix.size.y; y++ )
            {
                for ( int x = 0; x < _volume_matrix.size.x; x++ )
                {
                    int i = _volume_matrix.index( x , y , z );
                    _index2xyz[ 3 * i ] = x;
                    _index2xyz[ 3 * i + 1 ] = y;
                    _index2xyz[ 3 * i + 2 ] = z;
                }
            }
        }

        Config( main_color , cube_size , iso_value , origin_pos , zero_level );
    }

    public void Config ( Color main_color , float cube_size , float iso_value , Vector3 origin_pos , float zero_level = 0 )
    {
        _main_color = main_color;
        _cube_size = cube_size;
        _iso_value = iso_value;
        _origin_pos = origin_pos;
        _zero_level = zero_level;
    }

    public void Off ()
    {
        _volume_matrix = null;
        if ( _volume_buffer != null ) { _volume_buffer.Release(); }
        if ( _index_buffer != null ) { _index_buffer.Release(); }
    }

    public void Draw ()
    {
        if ( _volume_matrix == null ) return;

        _volume_buffer.SetData( _volume_matrix.data );
        _index_buffer.SetData( _index2xyz );

        //The MarchingCube Original Data
        _cube_render.SetBuffer( "volume" , _volume_buffer );
        _cube_render.SetBuffer( "index2xyz" , _index_buffer );

        //The Marching Cube Info
        _cube_render.SetFloat( "volume_size_x" , _volume_matrix.size.x );
        _cube_render.SetFloat( "volume_size_y" , _volume_matrix.size.y );
        _cube_render.SetFloat( "volume_size_z" , _volume_matrix.size.z );

        //The Config
        _cube_render.SetColor( "main_color" , _main_color );
        _cube_render.SetFloat( "iso_value" , _iso_value );
        _cube_render.SetFloat( "cube_size" , _cube_size );
        _cube_render.SetVector( "origin_pos" , new Vector4( _origin_pos.x , _origin_pos.y , _origin_pos.z , 0 ) );
        _cube_render.SetFloat( "zero_level" , _zero_level );

        _cube_render.SetPass( 0 );
        Graphics.DrawProceduralNow( MeshTopology.Points , _volume_matrix.voxel_count );
    }

    ~MarchingCubeRenderer ()
    {
        Off();
    }

    #endregion Interface
}