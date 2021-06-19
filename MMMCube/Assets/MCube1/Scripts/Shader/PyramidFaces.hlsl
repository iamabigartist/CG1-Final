// MIT License

// Copyright (c) 2020 NedMakesGames

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

// Make sure this file is not included twice
#ifndef PYRAMIDFACES_INCLUDED
#define PYRAMIDFACES_INCLUDED

// Include helper functions from URP
#include "NMGGeometryHelpers.hlsl"

// This structure is created by the renderer and passed to the Vertex function
// It holds data stored on the model, per vertex
struct Attributes
{
    float4 positionOS : POSITION; // Position in object space
    float2 uv : TEXCOORD0; // UVs
};
// Other common semantics include NORMAL, TANGENT, COLOR

// This structure is generated by the vertex function and passed to the geometry function
struct VertexOutput
{
    float3 positionWS : TEXCOORD0; // Position in world space
    float2 uv : TEXCOORD1; // UVs
};

// This structure is generated by the geometry function and passed to the fragment function
// Remember the renderer averages these values between the three points on the triangle
struct GeometryOutput
{
    float3 positionWS : TEXCOORD0; // Position in world space
    float3 normalWS : TEXCOORD1; // Normal vector in world space
    float2 uv : TEXCOORD2; // UVs

    float4 positionCS : SV_POSITION; // Position in clip space
};

// The _MainTex property. The sampler and scale/offset vector is also created
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
float4 _MainTex_ST;
// The pyramid height property
float _PyramidHeight;

// Vertex functions

VertexOutput Vertex ( Attributes input )
{
    // Initialize an output struct
    VertexOutput output = ( VertexOutput ) 0;

    // Use this URP functions to convert position to world space
    // The analogous function for normals is GetVertexNormalInputs
    VertexPositionInputs vertexInput = GetVertexPositionInputs( input.positionOS.xyz );
    output.positionWS = vertexInput.positionWS;

    // TRANSFORM_TEX is a macro which scales and offsets the UVs based on the _MainTex_ST variable
    output.uv = TRANSFORM_TEX( input.uv, _MainTex );
    return output;
}

// Geometry functions

GeometryOutput SetupVertex ( float3 positionWS, float3 normalWS, float2 uv )
{
    // Setup an output struct
    GeometryOutput output = ( GeometryOutput ) 0;
    output.positionWS = positionWS;
    output.normalWS = normalWS;
    output.uv = uv;
    // This function calculates clip space position, taking the shadow caster pass into account
    output.positionCS = CalculatePositionCSWithShadowCasterLogic( positionWS, normalWS );
    return output;
}

void SetupAndOutputTriangle ( inout TriangleStream<GeometryOutput> outputStream, VertexOutput a, VertexOutput b, VertexOutput c )
{
    // Restart the triangle strip, signaling the next appends are disconnected from the last
    outputStream.RestartStrip();
    // Since we extrude the center face, the normal must be recalculated
    float3 normalWS = GetNormalFromTriangle( a.positionWS, b.positionWS, c.positionWS );
    // Add the output data to the output stream, creating a triangle
    outputStream.Append( SetupVertex( a.positionWS, normalWS, a.uv ) );
    outputStream.Append( SetupVertex( b.positionWS, normalWS, b.uv ) );
    outputStream.Append( SetupVertex( c.positionWS, normalWS, c.uv ) );
}

// We create three triangles from one, so there will be 9 vertices
[maxvertexcount( 9 )]
void Geometry ( triangle VertexOutput inputs [ 3 ], inout TriangleStream<GeometryOutput> outputStream )
{
    // Create a fake VertexOutput for the center vertex
    VertexOutput center = ( VertexOutput ) 0;
    // We need the triangle's normal to extrude the center point
    float3 triNormal = GetNormalFromTriangle( inputs [ 0 ].positionWS, inputs [ 1 ].positionWS, inputs [ 2 ].positionWS );
    // Find the center position and extrude by _PyramidHeight along the normal
    center.positionWS = GetTriangleCenter( inputs [ 0 ].positionWS, inputs [ 1 ].positionWS, inputs [ 2 ].positionWS ) + triNormal * _PyramidHeight;
    // Average the UVs as well
    center.uv = GetTriangleCenter( inputs [ 0 ].uv, inputs [ 1 ].uv, inputs [ 2 ].uv );

    // Create the three triangles.
    // Triangles must wind clockwise or they will not render by default
    SetupAndOutputTriangle( outputStream, inputs [ 0 ], inputs [ 1 ], center );
    SetupAndOutputTriangle( outputStream, inputs [ 1 ], inputs [ 2 ], center );
    SetupAndOutputTriangle( outputStream, inputs [ 2 ], inputs [ 0 ], center );
}

// Fragment functions

// The SV_Target semantic tells the compiler that this function outputs the pixel color
float4 Fragment ( GeometryOutput input ) : SV_Target
{

#ifdef SHADOW_CASTER_PASS
    // If in the shadow caster pass, we can just return now
    // It's enough to signal that should will cast a shadow
    return 0;
#else
    // Initialize some information for the lighting function
    InputData lightingInput = ( InputData ) 0;
    lightingInput.positionWS = input.positionWS;
    lightingInput.normalWS = input.normalWS; // No need to renormalize, since triangles all share normals
    lightingInput.viewDirectionWS = GetViewDirectionFromPosition( input.positionWS );
    lightingInput.shadowCoord = CalculateShadowCoord( input.positionWS, input.positionCS );

    // Read the main texture
    float3 albedo = SAMPLE_TEXTURE2D( _MainTex, sampler_MainTex, input.uv ).rgb;

    // Call URP's simple lighting function
    // The arguments are lightingInput, albedo color, specular color, smoothness, emission color, and alpha
    return UniversalFragmentBlinnPhong( lightingInput, albedo, 1, 0, 0, 1 );
#endif
}

#endif