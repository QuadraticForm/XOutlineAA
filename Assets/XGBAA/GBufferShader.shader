Shader "XGBAA/GBuffer"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

		_Cull("__cull", Float) = 2.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
			Tags { "LightMode" = "UniversalForward" }

			Cull [_Cull] // Add this line to allow material overrid

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

				// 下面是从当前像素到给定边的轴向距离
                // 边0是顶点0的对边，依此类推
                // 轴向距离是从当前像素到边沿x和y轴的距离
				//
                // below are the axial distance from current pixel to given edge
                // edge 0 is the opposite edge of vertex 0, and so on
                // axial distance is the distance from current pixel to the edge along x and y axes separately
				//
				noperspective float2 distVec0 : DIST_VEC_0;
				noperspective float2 distVec1 : DIST_VEC_1;
				noperspective float2 distVec2 : DIST_VEC_2;
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

			// 从 P 到线段{l0, l1}的最近点的向量
			// delta vector from p to its closest point on line{l0, l1}
			float2 DeltaVec(const float2 p, const float2 l0, const float2 l1)
            {
                float2 lineVec = l1 - l0;
                float2 lineDir = normalize(lineVec);
                float2 pointVec = p - l0;
                
                // project pointVec onto line
                float2 projPoint = l0 + dot(pointVec, lineDir) * lineDir;

                // vector from p to its closest point on line
                return projPoint - p;
            }

            // 计算点 p 到线段 {l0, l1} 在 x 和 y 轴上的距离
            // 注意：这不是点 p 到线段最近点在 x 和 y 轴上的投影距离，而是分别在 x 和 y 轴上的距离
            // Calculate the distance from point p to the line segment {l0, l1} along the x and y axes separately
            // Note: This is not the distance from p to the closest point on the line segment projected onto the x and y axes,
            // but the distance from p to the line segment along the x and y axes separately
			float2 AxialDistVec(const float2 p, const float2 l0, const float2 l1)
			{
				float2 deltaVec = DeltaVec(p, l0, l1);
				

				// float deltaVecLen = length(deltaVec);
				float deltaVecLen2 = dot(deltaVec, deltaVec);

				float2 axialDistVec;

				// 根据相似三角形定理，可以得到下面的等式
				// according to the similar triangle theorem, we can get the following equation:
				//
				// axialDistVec.y / deltaVecLen = deltaVecLen / deltaVec.y
				// axialDistVec.y = deltaVecLen^2 / deltaVec.y
				//
				// same for x axis:
				// axialDistVec.x = deltaVecLen^2 / deltaVec.x

				float2 safeDeltaVec = sign(deltaVec) * max(abs(deltaVec), 0.000001);

				axialDistVec = deltaVecLen2 / safeDeltaVec;

				return axialDistVec;
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
                // now we have the vertices in clip space, we need to
                // perspective divide, get the NDC position, and then scale to screen space

				float2 halfScreenSize = _ScreenParams.xy * 0.5f; 
				// by multiplying halfScreenSize, we can scale distance from NDC to screen space
				// why half? because NDC's range is [-1, 1], that is 2 units in total

                float2 pos0 = (In[0].vertex.xy / In[0].vertex.w) * halfScreenSize;
                float2 pos1 = (In[1].vertex.xy / In[1].vertex.w) * halfScreenSize;
                float2 pos2 = (In[2].vertex.xy / In[2].vertex.w) * halfScreenSize;

                // axial distance between each vertex and its opposite edge

                float2 distVec0 = AxialDistVec(pos0, pos1, pos2);
                float2 distVec1 = AxialDistVec(pos1, pos2, pos0);
                float2 distVec2 = AxialDistVec(pos2, pos0, pos1);

                Stream.Append( GsMakeVert(In[0], distVec0, 0, 0) );
                Stream.Append( GsMakeVert(In[1], 0, distVec1, 0) );
                Stream.Append( GsMakeVert(In[2], 0, 0, distVec2) );
            }

            fixed4 frag (PsIn i) : SV_Target
            {
				fixed4 col = fixed4(0, 0, 0, 1);

				//
				// find the closest axial distance
				//

				// find the closest axial distance separately on x and y axes

                float2 distVec = i.distVec0;

                float2 absDistVec1 = abs(i.distVec1);
                float2 absDistVec2 = abs(i.distVec2);

                distVec.x = (absDistVec1.x < abs(distVec.x)) ? i.distVec1.x : distVec.x;
                distVec.x = (absDistVec2.x < abs(distVec.x)) ? i.distVec2.x : distVec.x;

                distVec.y = (absDistVec1.y < abs(distVec.y)) ? i.distVec1.y : distVec.y;
                distVec.y = (absDistVec2.y < abs(distVec.y)) ? i.distVec2.y : distVec.y;

				// X IMPROVEMENT draw edges only

				float2 absDistVec = abs(distVec);

				if (min(absDistVec.x, absDistVec.y) >= 1)
				{
					discard;
				}

				// X TODO, for degenerate triangles, 
				// we should output the axial distance to the outter most edge? 
				// not the closest edge

				//
				// output
				//

				col.rg = distVec; // direct output, no packing, so MUST output to a signed rg Texture

				return col;
            }
            ENDCG
        }
    }
}
