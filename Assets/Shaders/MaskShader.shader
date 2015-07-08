Shader "Custom/MaskShader"
{
	Properties
	{
		_MainTex ("Main Texture (RGB)", 2D) = "white" {}
		_MaskTex ("Masking Texture (A)", 2D) = "white" {}
	}

	SubShader
	{
		
		Pass
		{

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
			sampler2D _MaskTex;

			float4 frag(v2f IN) : SV_Target
			{
				float4 c = tex2D(_MainTex, IN.texcoord);
				c.rgb *= tex2D(_MainTex, IN.texcoord).a;
				
				return c;
			}
			
			ENDCG
		}
	}
}
