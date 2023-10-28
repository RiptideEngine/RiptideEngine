﻿namespace RiptideFoundation;

public sealed unsafe class TestRenderingPipeline : RenderingPipeline {
    public override void ExecuteRenderingOperation(in RenderingOperationData info) {
        var context = Graphics.RenderingContext;
        var factory = context.Factory;

        var cmdList = factory.CreateCommandList();

        var screenSize = Screen.Size;

        cmdList.SetRenderTarget(info.OutputRenderTarget, info.OutputDepthTexture);
        
        if (info.OutputCameras.Length > 0) {
            cmdList.ClearRenderTarget(info.OutputRenderTarget, info.OutputCameras[0].ClearColor);
            cmdList.ClearDepthTexture(info.OutputDepthTexture, DepthTextureClearFlags.All, 1, 0);

            foreach (var camera in info.OutputCameras) {
                var vp = camera.Viewport;
                cmdList.SetViewport(Rectangle.Create(screenSize * vp.GetPosition(), screenSize * vp.GetSize()));

                var sr = camera.ScissorRect;
                cmdList.SetScissorRect(Bound2D.Create(screenSize * sr.GetMinimum(), screenSize * sr.GetMaximum()).ToInt32());

                var frustum = FrustumCulling.CalculateFrustumPlanes(camera.ViewMatrix * camera.ProjectionMatrix);

                foreach (var scene in RuntimeFoundation.SceneGraphService.Context.EnumerateScenes()) {
                    foreach (var rootEntity in scene.EnumerateRootEntities()) {
                        RenderRendererComponents(rootEntity, cmdList);
                    }
                }
            }
        } else {
            cmdList.ClearRenderTarget(info.OutputRenderTarget, Color.Black);
            cmdList.ClearDepthTexture(info.OutputDepthTexture, DepthTextureClearFlags.All, 1, 0);
        }

        cmdList.Close();
        Graphics.AddCommandListExecutionBatch(cmdList);
        cmdList.DecrementReference();

        static void RenderRendererComponents(Entity entity, CommandList cmdList) {
            foreach (var component in entity.EnumerateComponents<Renderer>()) {
                component.Render(cmdList);
            }

            foreach (var child in entity.EnumerateChildren()) {
                RenderRendererComponents(child, cmdList);
            }
        }
    }

    public override void BindMesh(CommandList cmdList, Mesh mesh) {
        int headerLength = "_RIPTIDE_VERTEXBUFFER_C".Length;
        Span<char> name = stackalloc char[headerLength + 1];
        "_RIPTIDE_VERTEXBUFFER_C".CopyTo(name);

        foreach ((var buffer, var descriptor) in mesh.EnumerateVertexBuffers()) {
            name[headerLength] = (char)('0' + descriptor.Channel);

            cmdList.SetGraphicsReadonlyBuffer(name, buffer, descriptor.Stride, GraphicsFormat.Unknown);
        }
    }
}