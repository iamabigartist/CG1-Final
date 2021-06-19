//Code written by Przemyslaw Zaworski
//https://github.com/przemyslawzaworski

Shader "Point Cloud"
{
	Properties
	{
		main_color("MainColor", Color) = (1.0,0.0,0.0,1.0)
		ouline_color("OutlineColor", Color) = (1.0,0.0,0.0,1.0)
		outline_size("OutlineSize",float) = 2
	}
		SubShader
	{
		Pass
		{
			ZTest Always
			CGPROGRAM
			#pragma vertex vertex_shader
			#pragma fragment fragment_shader
			#pragma target 5.0
			#include "UnityCG.cginc"

			float4 outline_color;
			StructuredBuffer<float3> cloud;

			struct particle
			{
				float4 clip_pos : SV_POSITION;
				float z : TEXCOORD1;
			};

			particle vertex_shader(uint id : SV_VertexID)
			{
				particle p;
				float3 T = cloud[id];
				p.clip_pos = UnityObjectToClipPos(T);
				p.z = UnityObjectToViewPos(T).z + 1000;
				return p;
			}

			float4 fragment_shader(particle p) : SV_TARGET
			{
				return float4(main_color.rgb * p.z / 1000, 1.0);
			}

			ENDCG
		}

		Pass
		{
			ZTest Always
			CGPROGRAM
			#pragma vertex vertex_shader
			#pragma fragment fragment_shader
			#pragma target 5.0
			#include "UnityCG.cginc"

			float4 main_color;
			StructuredBuffer<float3> cloud;

			struct particle
			{
				float4 clip_pos : SV_POSITION;
				float z : TEXCOORD1;
			};

			particle vertex_shader(uint id : SV_VertexID)
			{
				particle p;
				float3 T = cloud[id];
				p.clip_pos = UnityObjectToClipPos(T);
				p.z = UnityObjectToViewPos(T).z + 1000;
				return p;
			}

			float4 fragment_shader(particle p) : SV_TARGET
			{
				return float4(main_color.rgb * p.z / 1000, 1.0);
			}

			ENDCG
		}
	}
}