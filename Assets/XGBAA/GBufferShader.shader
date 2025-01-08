Shader "XGBAA/GBuffer"
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
			float2 DeltaVec(const float2 l0, const float2 l1, const float2 p)
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
			float2 AxialDistVec(const float2 l0, const float2 l1, const float2 p)
			{
				float2 deltaVec = DeltaVec(l0, l1, p);
				

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

				float2 safeDeltaVec = sign(deltaVec) * max(abs(deltaVec), 0.00001);

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
                // get the screen space position of the triangle vertices
                // perspective divide, get the NDC position, and then scale screen space

                float2 pos0 = (In[0].vertex.xy / In[0].vertex.w) * _ScreenParams.xy;
                float2 pos1 = (In[1].vertex.xy / In[1].vertex.w) * _ScreenParams.xy;
                float2 pos2 = (In[2].vertex.xy / In[2].vertex.w) * _ScreenParams.xy;

                // axial distance between each vertex and its opposite edge

                float2 distVec0 = AxialDistVec(pos1, pos2, pos0);
                float2 distVec1 = AxialDistVec(pos2, pos0, pos1);
                float2 distVec2 = AxialDistVec(pos0, pos1, pos2);

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

				// pick the smaller axis

				distVec *= abs(distVec.x) < abs(distVec.y) ? float2(1, 0) : float2(0, 1);

				//
				// output
				//

				col.rg = distVec; // direct output to a signed rg Texture

				// col.rg = abs(distVec); // debug draw

				return col;
            }
            ENDCG
        }
    }
}
