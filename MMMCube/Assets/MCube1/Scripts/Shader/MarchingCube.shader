Shader "Custom/MarchingCube"
{
	Properties
	{
		_main_color("MainColor",Color) = (0.0,0.0,0.0,0.0)
	}
		SubShader
	{
		CGPROGRAM

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 5.0

		struct Input
		{
			float2 uv_MainTex;
		};

		fixed4 _main_color;

		void surf(Input IN, inout SurfaceOutputStandard o)
		{
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _main_color;
			o.Albedo = c.rgb;
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
			o.Alpha = c.a;
		}
		ENDCG
	}
}