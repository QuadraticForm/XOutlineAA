Shader "Unlit/XOutlineShader_V3"
{
    Properties
    {
        _Color("Color", Color) = (0, 0, 0, 1)
        _normalFromUvChannel("normalFromUvChannel", Int) = 0

        _Width("Width", Float) = 1
        _WidthSceneUnit("WidthSceneUnit", Float) = 0.01
        [ToggleUI]_ViewRelative("ViewRelative", Float) = 0

        _MinWidthInPixels("MinWidthInPixels", Float) = 0

        [ToggleUI]_CoverageToAlpha("CoverageToAlpha", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        Blend One OneMinusSrcAlpha

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
				// noperspective float2 offseted_position_ss : TEXCOORD1;
                float4 normalAndAlpha : TEXCOORD2; // better pack stuff into float4 for compatibility with older devices
            };

            CBUFFER_START(UnityPerMaterial)
            fixed4 _Color;
            int _normalFromUvChannel;
            float _Width;
            float _WidthSceneUnit;
            float _ViewRelative;
            float _MinWidthInPixels;
            float _CoverageToAlpha;
            CBUFFER_END

			Texture2D _FrontNormalBuffer;
            // Declare the sampler state
            SamplerState sampler_FrontNormalBuffer;


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
				// move pos_cs, to ensure minimum width in pixels
				//

				//// pixel offset need to be calculated with NDC pos

                float2 pos_ndc_original = pos_cs_original.xy / pos_cs_original.w;
                float2 pos_ndc = pos_cs.xy / pos_cs.w;

                float2 offset_ndc = pos_ndc - pos_ndc_original;
                float2 offset_in_pixels = offset_ndc * _ScreenParams.xy * 0.5; // ndc is 2 times larger than screen space, so * 0.5

				float major_offset_in_pixels = MaxElementVec2(abs(offset_in_pixels));
                scale_factor = max(1, _MinWidthInPixels / major_offset_in_pixels);

                pos_ndc = pos_ndc_original + offset_ndc * scale_factor;

				//// convert back to clip space position

                pos_cs.xy = pos_ndc.xy * pos_cs.w;

				// Coverage To Alpha

                alpha = _CoverageToAlpha > 0.5f ? 1 / scale_factor : 1;

				// move "original" inward 1 pixel, so that with original_position_ss, 
				// we are garrenteed to sample the "Front Normal"

				scale_factor = -1 / major_offset_in_pixels;

				pos_ndc_original += offset_ndc * scale_factor;

				pos_cs_original.xy = pos_ndc_original.xy * pos_cs_original.w;
            }

			float2 DirToSpherical(float3 dir)
			{
				dir = normalize(dir); // Normalize the input direction vector
    
				float azi = atan2(dir.y, dir.x); // Compute azimuth angle
				float alt = atan2(dir.z, length(dir.xy)); // Compute altitude angle

				// azimuth [-pi, pi]
				// altitude [-pi/2, pi/2]

				return float2(azi, alt);
			}

            float3 SphericalToDir(float2 spherical)
            {
                float azi = spherical.x;
                float alt = spherical.y;

                float cosAlt = cos(alt);
                float3 dir;
                dir.x = cosAlt * cos(azi);
                dir.y = cosAlt * sin(azi);
                dir.z = sin(alt);

                return normalize(dir);
            }

            v2f vert (appdata v)
            {
                v2f o;

                // transform the vertex to view space
                float3 pos_vs = mul(UNITY_MATRIX_MV, v.vertex).xyz;

                // calculate width
                float width = _WidthSceneUnit * _Width;
                width *= _ViewRelative > 0.5f ? abs(pos_vs.z) : 1;

                // calculate normal

                float3 shading_normal_vs = normalize(mul((float3x3)UNITY_MATRIX_MV, v.normal));
                float3 offset_normal_vs = CalcOffsetNormalVS(v);

                // offset position by normal and width
                float3 pos_vs_offsetted = pos_vs + offset_normal_vs * width;

                float4 pos_cs_original = mul(UNITY_MATRIX_P, float4(pos_vs, 1));
                float4 pos_cs = mul(UNITY_MATRIX_P, float4(pos_vs_offsetted, 1));

                // ensure minimum width in pixels

                float alpha = 1;

                AdjustPosCS(pos_cs, pos_cs_original, alpha);

                // output to fragment shader

                o.vertex = pos_cs;

				o.normalAndAlpha.xyz = shading_normal_vs;
                o.normalAndAlpha.w = alpha;

                o.original_position_ss = pos_cs_original.xy / pos_cs_original.w * 0.5 + 0.5;
				// o.offseted_position_ss = pos_cs.xy / pos_cs.w * 0.5 + 0.5;

                #if UNITY_UV_STARTS_AT_TOP
                o.original_position_ss.y = 1 - o.original_position_ss.y; // flip y, this one behaves the same as shader graph
				// o.offseted_position_ss.y = 1 - o.offseted_position_ss.y; // flip y, this one behaves the same as shader graph
                #endif

                // o.original_position_ss = ComputeScreenPos(pos_cs_original / pos_cs_original.w); // the unity doc is wrong, it told us that this function's input is in clip space, but actually it's NDC(that's clip space / w)

                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

			// Render To Camera Color and GBuffers
            struct FragmentOutput
            {
				float4 color : SV_Target0;

				// xy: normal in spherical coordinates, zw: delta screen space position between offseted and original
                float2 edgeDirection : SV_Target1;
            };

            FragmentOutput frag (v2f i) : SV_Target
            {
                FragmentOutput fragOut;

				// Color

				fragOut.color.a = _Color.a * i.normalAndAlpha.w;

                fragOut.color.rgb = _Color.rgb * fragOut.color.a; // we are using Blend One OneMinusSrcAlpha, so we need to multiply alpha here

				UNITY_APPLY_FOG(i.fogCoord, fragOut.color);

                // Edge Direction

                float2 frontNormalSph = _FrontNormalBuffer.Sample(sampler_FrontNormalBuffer, i.original_position_ss.xy).xy;
                
                float3 frontNormal = SphericalToDir(frontNormalSph);

                float3 edgeDirection = normalize(cross(frontNormal, normalize(i.normalAndAlpha.xyz)));

                fragOut.edgeDirection.rg = DirToSpherical(edgeDirection);
                        
                return fragOut;
            }
            ENDCG
        }
    }
}
