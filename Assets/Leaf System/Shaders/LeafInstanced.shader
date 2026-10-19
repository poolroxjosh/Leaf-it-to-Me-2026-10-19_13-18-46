
Shader "Custom/LeafInstanced"
{
    Properties
    {
        _BaseMap ("Leaf Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "LeafForward"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Off
            ZWrite On
            Blend Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _BaseColor;
                float _Cutoff;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(LeafProperties)
                UNITY_DEFINE_INSTANCED_PROP(
                    float4, _LeafColor
                )
            UNITY_INSTANCING_BUFFER_END(LeafProperties)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);

                VertexPositionInputs vertexInput =
                    GetVertexPositionInputs(
                        input.positionOS.xyz
                    );

                output.positionCS =
                    vertexInput.positionCS;

                output.uv = TRANSFORM_TEX(
                    input.uv, _BaseMap
                );

                output.color =
                    UNITY_ACCESS_INSTANCED_PROP(
                        LeafProperties, _LeafColor
                    );

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 textureColor =
                    SAMPLE_TEXTURE2D(
                        _BaseMap,
                        sampler_BaseMap,
                        input.uv
                    );

                half4 finalColor =
                    textureColor *
                    _BaseColor *
                    input.color;

                clip(finalColor.a - _Cutoff);

                return finalColor;
            }

            ENDHLSL
        }
    }
}
