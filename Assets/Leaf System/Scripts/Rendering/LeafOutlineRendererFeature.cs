using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Experimental.Rendering;

public class LeafOutlineRendererFeature
    : ScriptableRendererFeature
{
    [Header("Materials")]
    public Material compositeMaterial;

    private LeafOutlinePass outlinePass;

    public override void Create()
    {
        outlinePass = new LeafOutlinePass();

        outlinePass.renderPassEvent =
            RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(
        ScriptableRenderer renderer,
        ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType !=
            CameraType.Game)
            return;

        LeafHighlightRenderer source =
            LeafHighlightRenderer.Instance;

        if (source == null ||
            !source.HasHighlights ||
            source.highlightMaterial == null ||
            compositeMaterial == null)
            return;

        outlinePass.Setup(
            source,
            compositeMaterial
        );

        renderer.EnqueuePass(outlinePass);
    }

    private class LeafOutlinePass : ScriptableRenderPass
    {
        private LeafHighlightRenderer source;
        private Material compositeMaterial;

        private readonly List<
            LeafHighlightRenderer.HighlightDraw
        > draws = new();

        private class MaskPassData
        {
            public Material material;

            public LeafHighlightRenderer.HighlightDraw[]
                draws;
        }

        private class CompositePassData
        {
            public TextureHandle mask;
            public Material material;
        }

        public void Setup(
            LeafHighlightRenderer source,
            Material compositeMaterial)
        {
            this.source = source;
            this.compositeMaterial = compositeMaterial;
        }

        public override void RecordRenderGraph(
            RenderGraph renderGraph,
            ContextContainer frameData)
        {
            if (source == null)
                return;

            source.GetDraws(draws);

            if (draws.Count == 0)
                return;

            UniversalResourceData resources =
                frameData.Get<UniversalResourceData>();

            UniversalCameraData cameraData =
                frameData.Get<UniversalCameraData>();

            if (!resources.activeDepthTexture.IsValid())
                return;

            // Create a mask matching the camera
            // depth attachment's dimensions.
            TextureDesc maskDesc =
                resources.activeDepthTexture
                    .GetDescriptor(renderGraph);

            maskDesc.name = "Leaf Selection Mask";

            maskDesc.depthBufferBits = DepthBits.None;
            maskDesc.colorFormat = GraphicsFormat.R8G8B8A8_UNorm;

            maskDesc.clearBuffer = true;
            maskDesc.clearColor = Color.black;
            maskDesc.filterMode = FilterMode.Point;
            maskDesc.bindTextureMS = false;

            TextureHandle mask =
                renderGraph.CreateTexture(maskDesc);

            // PASS 1:
            // Draw selected leaf silhouettes.
            using (var builder =
                renderGraph.AddRasterRenderPass<MaskPassData>(
                    "Leaf Selection Mask",
                    out var passData))
            {
                passData.material =
                    source.highlightMaterial;

                passData.draws = draws.ToArray();

                builder.SetRenderAttachment(
                    mask,
                    0,
                    AccessFlags.Write
                );

                // Test against the existing scene depth.
                // Do not modify the depth buffer.
                builder.SetRenderAttachmentDepth(
                    resources.activeDepthTexture,
                    AccessFlags.Read
                );

                builder.SetRenderFunc(
                    static (
                        MaskPassData data,
                        RasterGraphContext context
                    ) =>
                    {
                        foreach (var draw in data.draws)
                        {
                            context.cmd.DrawMesh(
                                draw.mesh,
                                draw.matrix,
                                data.material,
                                0,
                                0
                            );
                        }
                    }
                );
            }

            // PASS 2:
            // Generate and overlay the outline.
            using (var builder =
                renderGraph.AddRasterRenderPass<
                    CompositePassData>(
                    "Leaf Outline Composite",
                    out var passData))
            {
                passData.mask = mask;
                passData.material = compositeMaterial;

                builder.UseTexture(
                    mask,
                    AccessFlags.Read
                );

                // Preserve the existing camera image
                // for alpha blending.
                builder.SetRenderAttachment(
                    resources.activeColorTexture,
                    0,
                    AccessFlags.ReadWrite
                );

                builder.SetRenderFunc(
                    static (
                        CompositePassData data,
                        RasterGraphContext context
                    ) =>
                    {
                        Blitter.BlitTexture(
                            context.cmd,
                            data.mask,
                            new Vector4(1f, 1f, 0f, 0f),
                            data.material,
                            0
                        );
                    }
                );
            }
        }
    }
}