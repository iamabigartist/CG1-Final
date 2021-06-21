Shader "Custom/MTerrain"
{
	Properties
	{
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
	}
		SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 200

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Standard fullforwardshadows

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		struct Input
		{
			float2 uv_MainTex;
			float3 worldPos;
			float3 worldNormal;
		};

		half _Glossiness;
		half _Metallic;
		sampler2D height_texture0; sampler2D height_weight0; float2 range_h0;
		sampler2D height_texture1; sampler2D height_weight1; float2 range_h1;
		sampler2D slope_texture0; sampler2D slope_weight0; float2 range_s0;

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			// Albedo comes from a texture tinted by color
			float h0 = smoothstep(range_h0.x, range_h0.y, IN.worldPos.y);
			float h1 = smoothstep(range_h1.x, range_h1.y, IN.worldPos.y);
			float s0 = smoothstep(range_s0.x, range_s0.y, IN.worldNormal.y);

			float3 color_h0 = tex2D(height_texture0, float2(h0,.5));
			float3 color_h1 = tex2D(height_texture1, float2(h1, .5));
			float3 color_s0 = tex2D(slope_texture0, float2(s0, .5));

			float weight_h0 = tex2D(height_weight0, float2(h0, .5));
			float weight_h1 = tex2D(height_weight1, float2(h1, .5));
			float weight_s0 = tex2D(slope_weight0, float2(s0, .5));

			o.Albedo =
				color_h0 * weight_h0.x +
				color_h1 * weight_h1.x +
				color_s0 * weight_s0.x;

			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
		}
		ENDCG
	}
		FallBack "Diffuse"
}