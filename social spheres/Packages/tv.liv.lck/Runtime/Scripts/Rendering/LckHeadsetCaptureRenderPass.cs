#if LCK_UNITY_URP
using System;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif
using UnityEngine.Rendering.Universal;

namespace Liv.Lck.Rendering
{
    /// <summary>
    /// Render pass that blits the XR camera's color buffer into the LCK headset capture target.
    /// On Unity 6+ uses the Render Graph API via <see cref="RecordRenderGraph"/>.
    /// On older URP versions uses the legacy <see cref="Execute"/> path.
    /// </summary>
    public class LckHeadsetCaptureRenderPass : ScriptableRenderPass
    {
        private const string PassName = "LCK Headset Capture";
        private const int ShaderPassIndex = 1;
        private const int LegacyBlitPassIndex = 0;
        private static readonly int BlitTextureId = Shader.PropertyToID("_BlitTexture");

        private LckHeadsetCamera _headsetCamera;
        private Camera _camera;

#if UNITY_6000_0_OR_NEWER
        private RTHandle _cachedTargetHandle;
        private RenderTexture _cachedTargetRT;
#endif

        public void Setup(LckHeadsetCamera headsetCamera, Camera camera)
        {
            _headsetCamera = headsetCamera;
            _camera = camera;
        }

#if UNITY_6000_0_OR_NEWER
        private class PassData
        {
            public TextureHandle Source;
            public TextureHandle Destination;
            public Material Material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();

            var target = _headsetCamera.ActiveTargetTexture;
            if (target == null || _headsetCamera.MaterialInstance == null)
                return;

            // When URP's active color target is an intermediate RT (e.g. the desktop editor
            // with post-processing), the source image is in the platform's native orientation
            // and our fullscreen triangle needs a Y-flip on UV-at-top platforms. When URP is
            // rendering directly to the back buffer (e.g. XR swapchain on Quest), the image
            // has already been projection-flipped for the final target and we must NOT flip.
            // Empirically verified on Quest Vulkan (isActiveTargetBackBuffer=true, no flip needed)
            // and Windows D3D11 editor (isActiveTargetBackBuffer=false, flip needed).
            _headsetCamera.UpdateMaterialForCapture(_camera,
                isSourceNativeOrientation: !resourceData.isActiveTargetBackBuffer);

            // Cache the RTHandle wrapper to avoid per-frame allocation.
            if (_cachedTargetRT != target)
            {
                _cachedTargetHandle?.Release();

                // Wrap via RenderTargetIdentifier so the import path does not inspect the
                // underlying RT descriptor. The CameraTrackTexture carries both a color
                // and a depth format which the plain ImportTexture overload rejects.
                _cachedTargetHandle = RTHandles.Alloc(new RenderTargetIdentifier(target));
                _cachedTargetRT = target;
            }

            var source = resourceData.activeColorTexture;

            // Provide an explicit RenderTargetInfo with only the color format so the
            // Render Graph does not see the depth/stencil format on the underlying RT.
            var targetInfo = new RenderTargetInfo
            {
                width = target.width,
                height = target.height,
                volumeDepth = target.volumeDepth,
                msaaSamples = target.antiAliasing,
                format = target.graphicsFormat
            };
            var destination = renderGraph.ImportTexture(_cachedTargetHandle, targetInfo);

            using (var builder = renderGraph.AddUnsafePass<PassData>(PassName, out var passData))
            {
                passData.Source = source;
                passData.Destination = destination;
                passData.Material = _headsetCamera.MaterialInstance;

                builder.UseTexture(source);
                builder.UseTexture(destination, AccessFlags.Write);

                // Prevent the render graph optimizer from culling this pass since our destination is
                // an imported external texture that nothing else in the graph reads.
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, UnsafeGraphContext context) =>
                {
                    context.cmd.SetRenderTarget(data.Destination);
                    context.cmd.SetGlobalTexture(BlitTextureId, data.Source);
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                    cmd.DrawProcedural(Matrix4x4.identity, data.Material, ShaderPassIndex,
                        MeshTopology.Triangles, 3);
                });
            }

            _headsetCamera.MarkCapturedByRenderFeature();
        }
#endif

#if UNITY_6000_0_OR_NEWER
        [Obsolete("This pass uses RecordRenderGraph on Unity 6+.", false)]
#endif
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
#if !UNITY_6000_0_OR_NEWER
            var target = _headsetCamera.ActiveTargetTexture;
            if (target == null)
                return;

            // Use URP's camera color target — BuiltinRenderTextureType.CameraTarget does not
            // resolve to the correct buffer inside a render pass.
#if UNITY_2022_1_OR_NEWER
            var source = renderingData.cameraData.renderer.cameraColorTargetHandle;
#else
            var source = renderingData.cameraData.renderer.cameraColorTarget;
#endif
            var material = _headsetCamera.MaterialInstance;

            _headsetCamera.UpdateMaterialForCapture(_camera);

            var cmd = CommandBufferPool.Get(PassName);

            if (material != null && _headsetCamera.UseTextureArrayBlit)
            {
                // Single-pass instanced VR: source is a texture array, use the material to
                // select the correct eye slice via shader pass 1.
                cmd.SetRenderTarget(target);
                cmd.SetGlobalTexture(BlitTextureId, source);
                cmd.DrawProcedural(Matrix4x4.identity, material, ShaderPassIndex,
                    MeshTopology.Triangles, 3);
            }
            else
            {
                cmd.Blit(source, target);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            _headsetCamera.MarkCapturedByRenderFeature();
#endif
        }

        public void Dispose()
        {
#if UNITY_6000_0_OR_NEWER
            _cachedTargetHandle?.Release();
            _cachedTargetHandle = null;
            _cachedTargetRT = null;
#endif
        }
    }
}
#endif
