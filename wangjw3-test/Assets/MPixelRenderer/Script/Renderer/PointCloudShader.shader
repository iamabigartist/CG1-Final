//Code written by Przemyslaw Zaworski
//https://github.com/przemyslawzaworski

Shader "Point Cloud"
{
	Properties
	{
		main_color("MainColor", Color) = (0.5,0.5,1.0,1.0)
	}
		SubShader
	{
		Pass
		{
			//ZTest Always
			CGPROGRAM
			#pragma vertex vertex_shader
			#pragma fragment fragment_shader
			//#pragma geometry geometry_shader
			#pragma target 5.0
			#include "UnityCG.cginc"

			float4 main_color;
			StructuredBuffer<float3> cloud;

			float size = 0.5f;

			struct v2g
			{
				float4 pos : SV_POSITION;
				float depth : TEXCOORD1;
			};

			struct g2f
			{
				float4 pos : SV_POSITION;
				float depth : TEXCOORD1;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float depth : TEXCOORD1;
			};

			v2f vertex_shader(uint id : SV_VertexID)
			{
				v2f p;
				float3 T = cloud[id];
				p.pos = UnityObjectToClipPos(T);
				p.depth = UnityObjectToViewPos(T).z * 10 + 600;
				return p;
			}

			float4 fragment_shader(v2f p) : COLOR
			{
				return float4(main_color.rgb * p.depth / 1000, 0.4);
			}

				//v2g vertex_shader(uint id : SV_VertexID)
				//{
				//	v2g p;
				//	float3 T = cloud[id];
				//	p.pos = UnityObjectToClipPos(T);
				//	p.depth = UnityObjectToViewPos(T).z * 10 + 600;
				//	return p;
				//}

				//[maxvertexcount(6)]
				//void geometry_shader(point v2g input[1], inout TriangleStream<g2f> triangle_stream)
				//{
				//	v2g v = input[0];
				//	g2f o;
				//	//o.depth = v.depth;
				//	//o.pos = v.pos;
				//	//triangle_stream.Append(o);
				//	for (int i = 0; i < 3; i++)
				//	{
				//		o.pos = v.pos + float4(0.05f * float3(
				//			i == 0,
				//			i == 1,
				//			0),
				//			1);
				//		o.depth = v.depth;
				//		triangle_stream.Append(o);
				//	}
				//	triangle_stream.RestartStrip();

				//	for (int j = 0; j < 3; j++)
				//	{
				//		o.pos = v.pos + float4(0.05f * float3(
				//			j != 2,
				//			j != 0,
				//			0),
				//			1);
				//		o.depth = v.depth;
				//		triangle_stream.Append(o);
				//	}
				//	triangle_stream.RestartStrip();
				//}

				//float4 fragment_shader(g2f p) : COLOR
				//{
				//	return float4(main_color.rgb * p.depth / 1000, 0.4);
				//}

				ENDCG
			}
	}
}