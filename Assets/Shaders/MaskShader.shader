Shader "Custom/MaskShader"
{
	Properties
	{
		_MainTex ("Main Texture (RGB)", 2D) = "white" {}
	}

	SubShader
	{
		
		Pass
		{

			Blend SrcColor OneMinusDstColor
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			struct v2f
			{
				float4 vertex   : SV_POSITION;
				half2 texcoord  : TEXCOORD0;
			};
			
			v2f vert(appdata_base IN)
			{
				v2f OUT;
				OUT.vertex = mul(UNITY_MATRIX_MVP, IN.vertex);
				OUT.texcoord = IN.texcoord;
	
				return OUT;
			}

			sampler2D _MainTex;

			float4 frag(v2f IN) : SV_Target
			{
				return tex2D(_MainTex, IN.texcoord);
			}
			
			ENDCG
		}
		
		Pass
		{

			Blend SrcAlpha OneMinusSrcAlpha
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			struct v2f
			{
				float4 vertex   : SV_POSITION;
				half2 texcoord  : TEXCOORD0;
			};
			
			v2f vert(appdata_base IN)
			{
				v2f OUT;
				OUT.vertex = mul(UNITY_MATRIX_MVP, IN.vertex);
				OUT.texcoord = IN.texcoord;
	
				return OUT;
			}

			sampler2D _MainTex;

			float4 frag(v2f IN) : SV_Target
			{
				return tex2D(_MainTex, IN.texcoord);;
			}
			
			ENDCG
		}
	}
}
