Shader "Custom/2DShadowShader"
{
	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
			"PreviewType"="Plane"
			"CanUseSpriteAtlas"="True"
		}

		Cull Off
		Lighting Off
		ZWrite Off
		
		Pass
		{
		
			Blend OneMinusSrcAlpha Zero
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct v2f
			{
				float4 vertex   : SV_POSITION;
			};
			
			v2f vert(appdata_full IN)
			{
				v2f OUT;
				OUT.vertex = mul(UNITY_MATRIX_MVP, IN.vertex);
				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				return fixed4(0,0,0,1);
			}
			
			ENDCG
		}
	}
}
