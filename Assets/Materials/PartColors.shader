// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Sprites/PartColors"
{
	Properties
	{
		[PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
		_Color ("Tint", Color) = (1,1,1,1)
		[MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
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
		Blend One OneMinusSrcAlpha

		Pass
		{
		CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ PIXELSNAP_ON
			#include "UnityCG.cginc"

			//HSB RGB

			float3 hsv_to_rgb(float3 HSV)
        	{
                float3 RGB = HSV.z;
           
                   float var_h = HSV.x * 6;
                   float var_i = floor(var_h);   // Or ... var_i = floor( var_h )
                   float var_1 = HSV.z * (1.0 - HSV.y);
                   float var_2 = HSV.z * (1.0 - HSV.y * (var_h-var_i));
                   float var_3 = HSV.z * (1.0 - HSV.y * (1-(var_h-var_i)));
                   if      (var_i == 0) { RGB = float3(HSV.z, var_3, var_1); }
                   else if (var_i == 1) { RGB = float3(var_2, HSV.z, var_1); }
                   else if (var_i == 2) { RGB = float3(var_1, HSV.z, var_3); }
                   else if (var_i == 3) { RGB = float3(var_1, var_2, HSV.z); }
                   else if (var_i == 4) { RGB = float3(var_3, var_1, HSV.z); }
                   else                 { RGB = float3(HSV.z, var_1, var_2); }
           
           		return (RGB);
        	}

			float3 rgb_to_hsv(float3 RGB)
        	{
                float3 HSV;
           
				float minChannel, maxChannel;
				if (RGB.x > RGB.y) {
					maxChannel = RGB.x;
					minChannel = RGB.y;
				}
				else {
					maxChannel = RGB.y;
					minChannel = RGB.x;
				}
         
				if (RGB.z > maxChannel) maxChannel = RGB.z;
				if (RGB.z < minChannel) minChannel = RGB.z;
           
                HSV.xy = 0;
                HSV.z = maxChannel;
                float delta = maxChannel - minChannel;             //Delta RGB value
                if (delta != 0) {                    // If gray, leave H  S at zero
                   HSV.y = delta / HSV.z;
                   float3 delRGB;
                   delRGB = (HSV.zzz - RGB + 3*delta) / (6.0*delta);
                   if      ( RGB.x == HSV.z ) HSV.x = delRGB.z - delRGB.y;
                   else if ( RGB.y == HSV.z ) HSV.x = ( 1.0/3.0) + delRGB.x - delRGB.z;
                   else if ( RGB.z == HSV.z ) HSV.x = ( 2.0/3.0) + delRGB.y - delRGB.x;
                }
                return (HSV);
        	} // Reverse order? For converting rgb to hsv...

      		struct Input {
          		float2 uv_MainTex;
      		};
     
      		float _HueShift;
			
			//*HSB RGB

			struct appdata_t
			{
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color    : COLOR;
				float2 texcoord  : TEXCOORD0;
			};
			
			fixed4 _Color;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.vertex = UnityObjectToClipPos(IN.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color * _Color;
				#ifdef PIXELSNAP_ON
				OUT.vertex = UnityPixelSnap (OUT.vertex);
				#endif

				return OUT;
			}

			sampler2D _MainTex;
			sampler2D _AlphaTex;
			float _AlphaSplitEnabled;

			fixed4 SampleSpriteTexture (float2 uv)
			{
				fixed4 color = tex2D (_MainTex, uv);

#if UNITY_TEXTURE_ALPHASPLIT_ALLOWED
				if (_AlphaSplitEnabled)
					color.a = tex2D (_AlphaTex, uv).r;
#endif //UNITY_TEXTURE_ALPHASPLIT_ALLOWED

				return color;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				float3 HSV, RGB;
				fixed4 c = SampleSpriteTexture (IN.texcoord);
				RGB.r = IN.color.r;
				RGB.g = IN.color.g;
				RGB.b = IN.color.b;
				fixed3 hsv = rgb_to_hsv(RGB);
				HSV.x = hsv.x - 0.0333333333333333;
				HSV.y = hsv.y;
				HSV.z = hsv.z + 0.5;
				fixed3 rgb = hsv_to_rgb(HSV);
				c.a *= IN.color.a;
				c.rgb *= c.a;
				fixed4 Color;
				
					if (c.r > (0.125 * IN.color.a) && c.g > (0.125 * IN.color.a) && c.b > (0.125 * IN.color.a))
					{
						c *= IN.color;
					}
<<<<<<< Updated upstream
					else if (c.g > (0.25 * IN.color.a) && c.b > (0.25 * IN.color.a) && c.r <= (1 * IN.color.a) && c.a > 0){
=======
					else if (c.g > (0.225 * IN.color.a) && c.b > (0.225 * IN.color.a) && c.r <= (1 * IN.color.a) && c.a > 0){
>>>>>>> Stashed changes
						Color.r = rgb.x;
						Color.g = rgb.y;
						Color.b = rgb.z;
						Color.a = IN.color.a;
						Color.rgb *= Color.a;
						c = Color;
					}
					else if (c.r < (0.125 * IN.color.a) && c.g < (0.125 * IN.color.a) && c.b < (0.125 * IN.color.a)){

					} else {
						c *= IN.color;
					}
				
				return c;
			}
		ENDCG
		}
	}
}
