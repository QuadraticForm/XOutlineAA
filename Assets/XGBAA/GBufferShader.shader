Shader "XGBAA/GBufferMaterial"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct VsIn
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct GsIn
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            struct PsIn
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;

                // noperspective float4 dists : Dist;
				noperspective float2 distVec0 : DIST_VEC_0; // distance to edge 0, the opposite edge of vertex 0
				noperspective float2 distVec1 : DIST_VEC_1; // distance to edge 1, the opposite edge of vertex 1
				noperspective float2 distVec2 : DIST_VEC_2; // distance to edge 2, the opposite edge of vertex 2
            };

            // constants

            sampler2D _MainTex;
            float4 _MainTex_ST;

            // functions

            GsIn vert (VsIn v)
            {
                GsIn o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

			// delta vector from p to its closest point on line{l0, l1}
			float2 CalcDistVec(const float2 l0, const float2 l1, const float2 p)
            {
                float2 lineVec = l1 - l0;
                float2 lineDir = normalize(lineVec);
                float2 pointVec = p - l0;
                
                // project pointVec onto line
                float2 projPoint = l0 + dot(pointVec, lineDir) * lineDir;

                // vector from p to its closest point on line
                return projPoint - p;
            }



            PsIn GsMakeVert(const GsIn In, const float2 distVec0, const float2 distVec1, const float2 distVec2)
            {
                PsIn Out;
                Out.vertex = In.vertex;
                Out.uv = In.uv;
                UNITY_TRANSFER_FOG(Out, In.vertex);
                
                Out.distVec0 = distVec0;
				Out.distVec1 = distVec1;
				Out.distVec2 = distVec2;
    
                return Out;
            }

			[maxvertexcount(3)]
            void geom(triangle GsIn In[3], inout TriangleStream<PsIn> Stream)
            {
                // get the screen space position of the triangle vertices
                // perspective divide, get the NDC position, and then scale screen space

                float2 pos0 = (In[0].vertex.xy / In[0].vertex.w) * _ScreenParams.xy;
                float2 pos1 = (In[1].vertex.xy / In[1].vertex.w) * _ScreenParams.xy;
                float2 pos2 = (In[2].vertex.xy / In[2].vertex.w) * _ScreenParams.xy;

                // distance between each vertex and its opposite edge (on the major axis)

                float2 distVec0 = CalcDistVec(pos1, pos2, pos0);
                float2 distVec1 = CalcDistVec(pos2, pos0, pos1);
                float2 distVec2 = CalcDistVec(pos0, pos1, pos2);

                Stream.Append( GsMakeVert(In[0], distVec0, 0, 0) );
                Stream.Append( GsMakeVert(In[1], 0, distVec1, 0) );
                Stream.Append( GsMakeVert(In[2], 0, 0, distVec2) );
            }

            fixed4 frag (PsIn i) : SV_Target
            {
				fixed4 col = fixed4(0, 0, 0, 1);

				//
				// debug draw dists
				//
				/*
				col.x = length(i.distVec0 / _ScreenParams.xy);
				col.y = length(i.distVec1 / _ScreenParams.xy);
				col.z = length(i.distVec2 / _ScreenParams.xy);
				return col;
				*/

				// pick the closest edge

				float dist0 = length(i.distVec0);
				float dist1 = length(i.distVec1);
				float dist2 = length(i.distVec2);

				float dist = dist0;
				float2 distVec = i.distVec0;

				if (dist1 < dist)
				{
					distVec = i.distVec1;
					dist = dist1;
				}

				if (dist2 < dist)
				{
					distVec = i.distVec2;
				}

				// debug draw
				// col.xy = abs(distVec) / _ScreenParams.xy; // normalize to [0, 1]

				float major_dist = 0;

				// pick the major axis
				if (abs(distVec.x) > abs(distVec.y))
				{
					distVec.y = 0;

					major_dist = distVec.x;

					col.xy = float2(1,0); // debug color
				}
				else
				{
					distVec.x = 0;

					major_dist = distVec.y;

					col.xy = float2(0,1); // debug color
				}

				// col.xy = abs(distVec); // debug color
				
				// debug draw

				if (abs(major_dist) > 1)
				{
					col.xy = 0;
				}

				return col;
            }
            ENDCG
        }
    }
}
