// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

Shader "Sprites/ColorSwap"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}

        // these six unused properties are required when a shader
        // is used in the UI system, or you get a warning.
        // look to UI-Default.shader to see these.
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        // see for example
        // http://answers.unity3d.com/questions/980924/ui-mask-with-shader.html
        
        _Color ("Tint", Color) = (1,1,1,1)
        _Greyscale("Greyscale", Range(0, 1)) = 1
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
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

        // required for UI.Mask
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp] 
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        ColorMask [_ColorMask]
         

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
        CGPROGRAM
            #pragma vertex SpriteVert2
            #pragma fragment SpriteFrag2
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

			v2f SpriteVert2(appdata_t IN)
			{
				v2f OUT;

				UNITY_SETUP_INSTANCE_ID(IN);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

				OUT.vertex = UnityFlipSprite(IN.vertex, _Flip);
				OUT.vertex = UnityObjectToClipPos(OUT.vertex);
				OUT.texcoord = IN.texcoord;
				OUT.color = IN.color * _Color * _RendererColor;

	#ifdef PIXELSNAP_ON
				OUT.vertex = UnityPixelSnap(OUT.vertex);
	#endif

				return OUT;
			}

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
            half _Greyscale;

			fixed4 SpriteFrag2(v2f IN) : SV_Target
			{
                fixed4 c = SampleSpriteTexture (IN.texcoord);
                float3 HSV, RGBin, RGBbase;
                RGBin.r = IN.color.r;
				RGBin.g = IN.color.g;
				RGBin.b = IN.color.b;
                RGBbase.r = c.r;
                RGBbase.g = c.g;
                RGBbase.b = c.b;
                fixed3 hsvIN = rgb_to_hsv(RGBin);
                fixed3 hsvBASE = rgb_to_hsv(RGBbase);
                hsvBASE.x = hsvIN.x;
                fixed3 RGBout = hsv_to_rgb(hsvBASE);
                c.a *= IN.color.a;
                if(c.a == 0.01000315){ // For normal appearance of EP1 style core
                    if (c.g > 0 && c.r >= 0.99 && c.b >= 0.99){
                        c.a = 1;
                        c.rgb = lerp(c.rgb, dot(c.rgb, float3(0.3, 0.59, 0.11)), _Greyscale);
                        c.rgb *= c.a;
                    } else if (c.g > 0.9 && c.r > 0.9 && c.b > 0.9){
                        c.a = 1;
                        c.rgb *= c.a;
                    } else {
                        c.a = 0;
                        c.rgb *= c.a;
                    }
                } else if(c.a == 0.01000314) { // For stealth appearance of EP1 style core
                    if (c.g > 0 && c.r >= 0.99 && c.b >= 0.99){
                        c.a = 0.2;
                        c.rgb = lerp(c.rgb, dot(c.rgb, float3(0.3, 0.59, 0.11)), _Greyscale);
                        c.rgb *= c.a;
                    } else if (c.g > 0.9 && c.r > 0.9 && c.b > 0.9){
                        c.a = 0.2;
                        c.rgb *= c.a;
                    } else {
                        c.a = 0;
                        c.rgb *= c.a;
                    }
                } else {
                    c.rgb *= c.a;
                }

				float min = c.a * 0.99f;

				if (c.r > min && c.g < 0.9f && min)
				{
                        fixed4 conColor;
				        conColor.r = RGBout.x;
                        conColor.g = RGBout.y;
                        conColor.b = RGBout.z;
                        conColor.a = c.a;
                        conColor.rgb *= conColor.a;
					    c = conColor;
				}

                return c;
			}
        ENDCG
        }
    }
}
