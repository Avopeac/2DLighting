Shader "Custom/2DLightShader"
{
	Properties
	{
		_MainTex ("Sprite Texture", 2D) = "white" {}
		_Inner("Inner color", Color) = (1,1,1,1)
		_Outer("Outer color", Color) = (1,1,1,1)
	}

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
		
		//Light pass
		Pass
		{
		
			Blend SrcAlpha One
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				half2 texcoord  : TEXCOORD0;
			};
			
			v2f vert(appdata_full IN)
			{
				v2f OUT;
				OUT.vertex = mul(UNITY_MATRIX_MVP, IN.vertex);
				OUT.texcoord = IN.texcoord;
			
				return OUT;
			}

			sampler2D _MainTex;
			float4 _Inner;
			float4 _Outer;

			fixed4 frag(v2f IN) : SV_Target
			{			
				float4 mask = tex2D(_MainTex, IN.texcoord);
				
				float4 inner = mask * _Inner;
				float4 outer = mask * _Outer;
				
				return lerp(inner, outer, IN.texcoord.x);
			}
			
			ENDCG
		}
	}
}
