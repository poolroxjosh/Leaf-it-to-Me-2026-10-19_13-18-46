Shader "Custom/LeafOutlineComposite"
{
    Properties
    {
        [HDR] _OutlineColor (
            "Outline Color",
            Color
        ) = (1.5, 1.5, 1.5, 1)

        _OutlineThickness (
            "Outline Thickness (Pixels)",
            Range(1, 6)
        ) = 2

        _DebugMask (
            "Debug Mask",
            Float
        ) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "LeafOutlineComposite"

            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM

            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineThickness;
                float _DebugMask;
            CBUFFER_END

            half SampleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(
                    _BlitTexture,
                    sampler_PointClamp,
                    uv
                ).r;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.texcoord;

                half center = SampleMask(uv);

                // Optional mask visualization.
                if (_DebugMask > 0.5)
                    return half4(center, center, center, 1);

                float2 offset =
                    _BlitTexture_TexelSize.xy *
                    _OutlineThickness;

                half surrounding = center;

                // Sample surrounding pixels.
                surrounding = max(surrounding,
                    SampleMask(uv + float2(offset.x, 0)));

                surrounding = max(surrounding,
                    SampleMask(uv - float2(offset.x, 0)));

                surrounding = max(surrounding,
                    SampleMask(uv + float2(0, offset.y)));

                surrounding = max(surrounding,
                    SampleMask(uv - float2(0, offset.y)));

                surrounding = max(surrounding,
                    SampleMask(uv + offset * 0.7071));

                surrounding = max(surrounding,
                    SampleMask(uv - offset * 0.7071));

                surrounding = max(surrounding,
                    SampleMask(uv + float2(
                        offset.x, -offset.y) * 0.7071));

                surrounding = max(surrounding,
                    SampleMask(uv + float2(
                        -offset.x, offset.y) * 0.7071));

                // Remove the original silhouette,
                // leaving only its surrounding border.
                half outline = saturate(
                    surrounding - center
                );

                return half4(
                    _OutlineColor.rgb,
                    outline * _OutlineColor.a
                );
            }

            ENDHLSL
        }
    }
}