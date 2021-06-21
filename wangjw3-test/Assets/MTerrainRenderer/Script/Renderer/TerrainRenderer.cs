using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[System.Serializable]
[RequireComponent( typeof( MeshFilter ) ), RequireComponent( typeof( MeshRenderer ) )]
public class TerrainRenderer : MonoBehaviour
{
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Material material;

    public float Glossiness;
    public float Metallic;

    public TerrainGradient height0, height1, slope0;

    public AnimationCurve weight_curve_h0; public Gradient color_curve_h0;
    public AnimationCurve weight_curve_h1; public Gradient color_curve_h1;
    public AnimationCurve weight_curve_s0; public Gradient color_curve_s0;

    private Texture2D height_t0, height_t1, slope_t0;

    private Texture2D height_w0, height_w1, slope_w0;

    private void Awake ()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshRenderer.material = new Material( Shader.Find( "Custom/MTerrain" ) );
        material = meshRenderer.material;
        //height0 = new TerrainGradient();
        //weight_curve_h0 = new AnimationCurve(); color_curve_h0 = new Gradient();
        //height1 = new TerrainGradient();
        //weight_curve_h1 = new AnimationCurve(); color_curve_h1 = new Gradient();
        //slope0 = new TerrainGradient();
        //weight_curve_s0 = new AnimationCurve(); color_curve_s0 = new Gradient();
    }

    #region Interface

    public void ResetShader ()
    {
        //print( height0 );
        height0.Generate( out height_t0 , out height_w0 , weight_curve_h0 , color_curve_h0 );
        height1.Generate( out height_t1 , out height_w1 , weight_curve_h1 , color_curve_h1 );
        slope0.Generate( out slope_t0 , out slope_w0 , weight_curve_s0 , color_curve_s0 );

        material.SetTexture( "height_texture0" , height_t0 );
        material.SetTexture( "height_texture1" , height_t1 );
        material.SetTexture( "slope_texture0" , slope_t0 );

        material.SetTexture( "height_weight0" , height_w0 );
        material.SetTexture( "height_weight1" , height_w1 );
        material.SetTexture( "slope_weight0" , slope_w0 );

        material.SetVector( "range_h0" , new Vector4( height0.min , height0.max ) );
        material.SetVector( "range_h1" , new Vector4( height1.min , height1.max ) );
        material.SetVector( "range_s0" , new Vector4( slope0.min , slope0.max ) );

        material.SetFloat( "_Glossiness" , Glossiness );
        material.SetFloat( "_Metallic" , Metallic );
    }

    public void Setup ( Mesh mesh )
    {
        ResetShader();
        meshFilter.mesh = mesh;
    }

    #endregion Interface
}