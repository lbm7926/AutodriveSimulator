﻿Shader "Battlehub/RTHandles/VertexColorBillboard"
{
	Properties{
	}
	SubShader{
		Cull Off
		ZTest Off
		ZWrite Off

		Tags{ "Queue" = "Geometry" "IgnoreProjector" = "True" "RenderType" = "Opaque" }
		Pass{
			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma vertex vert  
			#pragma fragment frag 

			struct vertexInput {
				float4 vertex : POSITION;
				float4 color: COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			struct vertexOutput {
				float4 pos : SV_POSITION;
				float4 color: COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			#include "UnityCG.cginc"
			inline float4 GammaToLinearSpace(float4 sRGB)
			{
				if (IsGammaSpace())
				{
					return sRGB;
				}
				return sRGB * (sRGB * (sRGB * 0.305306011h + 0.682171111h) + 0.012522878h);
			}

			vertexOutput vert(vertexInput input)
			{
				vertexOutput output;
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				float scaleX = length(mul(unity_ObjectToWorld, float4(1.0, 0.0, 0.0, 0.0)));
				float scaleY = length(mul(unity_ObjectToWorld, float4(0.0, 1.0, 0.0, 0.0)));
				output.pos = mul(UNITY_MATRIX_P,
					float4(UnityObjectToViewPos(float3(0.0, 0.0, 0.0)), 1.0)
					- float4(input.vertex.x * scaleX, input.vertex.y * scaleY, 0.0, 0.0));
				output.color = GammaToLinearSpace(input.color);
				output.color.a = input.color.a;
				return output;
			}

			float4 frag(vertexOutput input) : COLOR
			{
				UNITY_SETUP_INSTANCE_ID(input);
				return input.color;
			}

			ENDCG
		}
	}
}
