using System.Collections;
using System.Collections.Generic;

using UnityEngine;

[System.Serializable]
public struct TerrainGradient
{
    private AnimationCurve _weight_curve;
    private Gradient _color_curve;

    public float min;
    public float max;

    public int width;

    public void Generate ( out Texture2D texture , out Texture2D weight , AnimationCurve weight_curve , Gradient color_curve )
    {
        _weight_curve = weight_curve;
        _color_curve = color_curve;
        if (color_curve != null)
        {
            Color[] colours = new Color[width];
            for (int i = 0; i < width; i++)
            {
                Color gradientCol = color_curve.Evaluate( i / ( width - 1f ) );
                colours[i] = gradientCol;
            }

            texture = new Texture2D( width , 1 , TextureFormat.RGBA32 , false );
            texture.SetPixels( colours );
            texture.Apply();
        }
        else { texture = null; }

        if (weight_curve != null)
        {
            Color[] colours = new Color[width];
            for (int i = 0; i < width; i++)
            {
                Color gradientWei = new Color( weight_curve.Evaluate( i / ( width - 1f ) ) , 0 , 0 );
                colours[i] = gradientWei;
            }

            weight = new Texture2D( width , 1 , TextureFormat.RGBA32 , false );
            weight.SetPixels( colours );
            weight.Apply();
        }
        else { weight = null; }
    }

    //public readonly Type type;
    //public Type type;

    //public TerrainGradient ( Type type )
    //{
    //    this.type = type;
    //}

    //private float GetRatio ( float value )
    //{
    //    return ( value - min ) / ( max - min );
    //}

    //private float EvaluateVertex ( Vector3 vertex , Vector3 normal , Vector3 tangent )
    //{
    //    switch (type)
    //    {
    //        case Type.Height: return vertex.y;

    //        case Type.Slope: return 1 - Mathf.Abs( normal.y );

    //        case Type.Surface: return 1 + normal.y;
    //    }
    //    return 0;
    //}

    //public void Evaluate ( out Color color , out float weight , Vector3 vertex , Vector3 normal , Vector3 tangent )
    //{
    //    float ratio = GetRatio( EvaluateVertex( vertex , normal , tangent ) );
    //    color = color_curve.Evaluate( ratio );
    //    weight = weight_curve.Evaluate( ratio );
    //}
}