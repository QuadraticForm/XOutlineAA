Shader "Unlit/XOutlineShader_V3"
{
    Properties
    {
        _Color("Color", Color) = (0, 0, 0, 1)
        _normalFromUvChannel("normalFromUvChannel", Int) = 0

        _Thickness("Thickness", Float) = 1
        _ThicknessSceneUnit("ThicknessSceneUnit", Float) = 0.01
        [ToggleUI]_ViewRelative("ViewRelative", Float) = 0

        _MinThicknessInPixels("MinThicknessInPixels", Float) = 0
        [ToggleUI]_CoverageToAlpha("CoverageToAlpha", Float) = 0

        _BlurRadius("BlurRadius", Float) = 1
        _LongEdgeBlurRadiusMul("LongEdgeBlurRadiusMul", Float) = 10
    }
    SubShader
    {
        // Tags { "RenderType"="Transparent" }
        // Blend One OneMinusSrcAlpha

        Tags { "RenderType"="Opaque" }
        // Blend One OneMinusSrcAlpha

        LOD 100

        Pass
        {
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float4 tangent : TANGENT;
                float2 uv0 : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float2 uv2 : TEXCOORD2;
                float2 uv3 : TEXCOORD3;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;

                noperspective float2 original_position_ss : TEXCOORD0;
                float4 normalAndAlpha : TEXCOORD1; // better pack stuff into float4 for compatibility with older devices
            };

            CBUFFER_START(UnityPerMaterial)
            fixed4 _Color;
            int _normalFromUvChannel;
            float _Thickness;
            float _ThicknessSceneUnit;
            float _ViewRelative;
            float _MinThicknessInPixels;
            float _CoverageToAlpha;
            CBUFFER_END

            float MaxElementVec2(float2 v)
            {
                return max(v.x, v.y);
            }

            float3 NormalFromUV(float2 uv)
            {
                float3 normal = 0;

                normal.z = sqrt(1 - saturate(dot(uv, uv)));
                normal.xy = uv;

                return normalize(normal);
            }

            float3 CalcOffsetNormalVS(appdata v)
            {
                float3 offset_normal_vs = 0;

                if (_normalFromUvChannel == 0)
                {
                    offset_normal_vs = mul((float3x3)UNITY_MATRIX_IT_MV, v.normal);
                }
                else
                {
                    float3 normal_ts = 0;
                    float2 uv = _normalFromUvChannel == 1 ? v.uv1 : (_normalFromUvChannel == 2 ? v.uv2 : v.uv3);

                    normal_ts = NormalFromUV(uv);

                    // Construct TBN basis
                    float3 tangent = v.tangent.xyz;
                    float3 bitangent = cross(v.normal, tangent) * v.tangent.w;
                    float3x3 TBN = float3x3(tangent, bitangent, v.normal);

                    // Transform normal from tangent space to view space
                    offset_normal_vs = normalize(mul((float3x3)UNITY_MATRIX_IT_MV, mul(normal_ts, TBN)));
                }

                return offset_normal_vs;
            }

            void AdjustPosCS(inout float4 pos_cs, inout float4 pos_cs_original, out float alpha)
            {
                float scale_factor = 1;

				//
				// move pos_cs, to ensure minimum thickness in pixels
				//

				//// pixel offset need to be calculated with NDC pos

                float2 pos_ndc_original = pos_cs_original.xy / pos_cs_original.w;
                float2 pos_ndc = pos_cs.xy / pos_cs.w;

                float2 offset_ndc = pos_ndc - pos_ndc_original;
                float2 offset_in_pixels = offset_ndc * _ScreenParams.xy * 0.5; // ndc is 2 times larger than screen space, so * 0.5

				float major_offset_in_pixels = MaxElementVec2(abs(offset_in_pixels));
                scale_factor = max(1, _MinThicknessInPixels / major_offset_in_pixels);

                pos_ndc = pos_ndc_original + offset_ndc * scale_factor;

				//// convert back to clip space position

                pos_cs.xy = pos_ndc.xy * pos_cs.w;

				// Coverage To Alpha

                alpha = _CoverageToAlpha > 0.5f ? 1 / scale_factor : 1;

				// move "original" inward 1 pixel, so in resolve pass, with original_position_ss, 
				// we are garrenteed to sample the "Front Normal", instead of the "Outline Normal"

				scale_factor = -1 / major_offset_in_pixels;

				pos_ndc_original += offset_ndc * scale_factor;

				pos_cs_original.xy = pos_ndc_original.xy * pos_cs_original.w;
            }

            v2f vert (appdata v)
            {
                v2f o;

                // transform the vertex to view space
                float3 pos_vs = mul(UNITY_MATRIX_MV, v.vertex).xyz;

                // calculate thickness
                float thickness = _ThicknessSceneUnit * _Thickness;
                thickness *= _ViewRelative > 0.5f ? abs(pos_vs.z) : 1;

                // calculate normal

                float3 shading_normal_vs = -normalize(mul((float3x3)UNITY_MATRIX_MV, v.normal)); // flip normal to facing camera since we are rendering back faces
                float3 offset_normal_vs = CalcOffsetNormalVS(v);

                // offset position by normal and thickness
                float3 pos_vs_offsetted = pos_vs + offset_normal_vs * thickness;

                float4 pos_cs_original = mul(UNITY_MATRIX_P, float4(pos_vs, 1));
                float4 pos_cs = mul(UNITY_MATRIX_P, float4(pos_vs_offsetted, 1));

                // ensure minimum thickness in pixels

                float alpha = 1;

                AdjustPosCS(pos_cs, pos_cs_original, alpha);

                // output to fragment shader

                o.vertex = pos_cs;

				o.normalAndAlpha.xyz = shading_normal_vs;
                o.normalAndAlpha.w = alpha;

                o.original_position_ss = pos_cs_original.xy / pos_cs_original.w * 0.5 + 0.5;
                #if UNITY_UV_STARTS_AT_TOP
                o.original_position_ss.y = 1 - o.original_position_ss.y; // flip y, this one behaves the same as shader graph
                #endif

                // o.original_position_ss = ComputeScreenPos(pos_cs_original / pos_cs_original.w); // the unity doc is wrong, it told us that this function's input is in clip space, but actually it's NDC(that's clip space / w)

                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }


            struct FragmentOutput
            {
				float4 color : SV_Target0;
                float4 gbuffer1 : SV_Target1;
                float4 gbuffer2 : SV_Target2;
            };

            FragmentOutput frag (v2f i) : SV_Target
            {
                FragmentOutput fragOut;

				// Since we can't set different blending mode for different render target,
				// and we need alpha blending to achieve "Coverage To Alpha" effect,
				// we are forced to use premultiplied alpha as the common blending mode (Blend One OneMinusSrcAlpha)
				// cuz at least it doesn't multiply the alpha value to the color value
				//
				// however, it will add background color to the final color,
				// for the color target this is what we want, but for gbuffers it is not,
				// 
				// ¡ù so, we have to render on black background, 
				// which means we have to render outline first, and Front Normal later.

				// Color

				fragOut.color.rgb = _Color.rgb * i.normalAndAlpha.w; // we are using premultiplied alpha method here
				fragOut.color.a = i.normalAndAlpha.w;

				UNITY_APPLY_FOG(i.fogCoord, fragOut.color);

				// GBuffer 1, normal and original (without outline offset) screen space position

				fragOut.gbuffer1.rg = normalize(i.normalAndAlpha.xyz).xy; 
				fragOut.gbuffer1.ba = i.original_position_ss.xy;

				// GBuffer 2, color and alpha (used for coverage bluring in resolve pass)

				fragOut.gbuffer2.rgb = _Color.rgb;
				fragOut.gbuffer2.a = i.normalAndAlpha.w;
                        
                return fragOut;
            }
            ENDCG
        }
    }
}
