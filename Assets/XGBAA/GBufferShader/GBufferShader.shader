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

				// original

				Stream.Append( GsMakeVert(In[0], distVec0, 0, 0) );
				Stream.Append( GsMakeVert(In[1], 0, distVec1, 0) );
				Stream.Append( GsMakeVert(In[2], 0, 0, distVec2) );

				return;

				// TEST sub-pixel triangle
				/*
				float minX = min(min(pos0.x, pos1.x), pos2.x);
				float maxX = max(max(pos0.x, pos1.x), pos2.x);

				float minY = min(min(pos0.y, pos1.y), pos2.y);
				float maxY = max(max(pos0.y, pos1.y), pos2.y);

				bool isSubP = (maxX - minX < 1 || maxY - minY < 1);

				isSubP = false;

				if (isSubP)
				{
					float2 invalid_distVec = float2(2, 2);

					// this is a sub-pixel triangle
					Stream.Append( GsMakeVert(In[0], invalid_distVec, invalid_distVec, invalid_distVec) );
					Stream.Append( GsMakeVert(In[1], invalid_distVec, invalid_distVec, invalid_distVec) );
					Stream.Append( GsMakeVert(In[2], invalid_distVec, invalid_distVec, invalid_distVec) );
				}
				else
				{
					Stream.Append( GsMakeVert(In[0], distVec0, 0, 0) );
					Stream.Append( GsMakeVert(In[1], 0, distVec1, 0) );
					Stream.Append( GsMakeVert(In[2], 0, 0, distVec2) );
				}
				*/
            }

			// 像素着色器
			// 经典的 GBAA 实现，即（对于每个轴），只输出最近的距离
			// Pixel Shader
			// Classical GBAA implementation, which is (for each axis), only output the closest distance
			/*
            fixed4 frag (PsIn i) : SV_Target
            {
				// Default Value
				// gbuffer stores the pixel-edge distance,
				// 0 means on the edge, 1 means 1 pixel away from the edge,
				// 1 is the maximum value GBufferShader will output,
				// and all value outside [-1, 1] is disregarded by the resolve pass
				// so 2 is large enough to be used as a invalid value
				fixed4 col = fixed4(2, 2, 2, 2);

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

				//
				// output
				//

				col.rg = distVec; // direct output, no packing, so MUST output to a signed rg Texture

				return col;
            }
			*/

			
			// 针对单个轴，从像素距离 3 条边的距离中挑出有效的 2 个，
			// x 为负值，y 为正值
			/*
				首先，因为我们在运行 Pixel Shader，所以像素一定在三角形内部，
				所以，针对每个轴（下面以 x 轴为例），像素距离三条边的距离一定有正有负，即像素的左右都有边，
				对于同时有两个值的那一边，只有距离近的值是有效的，
				例如同时有两个负值，表示像素左边有两条边，但绝对值更大的一条在 x 轴上距离像素最近的点并不在线段中，而在其延长线上
			*/
			float2 PickValidEdgeDists(const float3 dists)
			{
				// 找到正值和负值
                float3 dists_is_positive = max(0, sign(dists));		//（sign 如果输入是 0，输出也是 0，很适合用在这里）
				float3 dists_is_negative = max(0, -sign(dists)); 

                float3 positives = dists * dists_is_positive + 2 * dists_is_negative; // 将负值变为一个大到可以忽略的值
                float3 negatives = dists * dists_is_negative - 2 * dists_is_positive; // 将正值变为一个小到可以忽略的值

                // 选择最靠近 0 的正值和负值
                float closestPositiveX = min(min(positives.x, positives.y), positives.z);
				float closestNegativeX = max(max(negatives.x, negatives.y), negatives.z);

				return float2(closestNegativeX, closestPositiveX);
			}

			// 改进的像素着色器
			// 改进的 GBAA 实现，即（对于每个轴），同时输出左右（上下）两个边的距离
			fixed4 frag (PsIn i) : SV_Target
            {
				// 无效值
				// Invalid Value
				/*					
					gbuffer 存储像素到边缘的距离，
					0 表示在边缘上，1 表示距离边缘 1 个像素，
					所有在 [-1, 1] 之外的值都会被 resolve pass 忽略
					因此 2 足够大，可以用作无效值

					gbuffer stores the pixel-edge distance,
					0 means on the edge, 1 means 1 pixel away from the edge,
					and all value outside [-1, 1] is disregarded by the resolve pass
					so 2 is large enough to be used as a invalid value
				*/
				float invalid_value = 2.0;
				fixed4 col = float4(-invalid_value, invalid_value, -invalid_value, invalid_value);

				// 针对单个轴，从像素距离 3 条边的距离中挑出有效的 2 个，
				// x 为负值，y 为正值
				/*
					首先，因为我们在运行 Pixel Shader，所以像素一定在三角形内部，
					所以，针对每个轴（下面以 x 轴为例），像素距离三条边的距离一定有正有负，即像素的左右都有边，
					对于同时有两个值的那一边，只有距离近的值是有效的，
					例如同时有两个负值，表示像素左边有两条边，但绝对值更大的一条在 x 轴上距离像素最近的点并不在线段中，而在其延长线上
				*/

				float2 distX = PickValidEdgeDists(float3(i.distVec0.x, i.distVec1.x, i.distVec2.x));
				float2 distY = PickValidEdgeDists(float3(i.distVec0.y, i.distVec1.y, i.distVec2.y));

				//
				// output
				// direct output, no packing, so MUST output to a signed 4 channel float Texture
				//
				
				col.rg = distX; 
				col.ba = distY;

				return col;
            }

            ENDCG
        }
    }
}
