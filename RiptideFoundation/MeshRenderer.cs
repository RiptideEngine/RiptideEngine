// using RPCSToolkit;
//
// namespace RiptideFoundation;
//
// [EntityComponent("938f8802-3762-4be8-ba39-480089e3283f")]
// public sealed unsafe class MeshRenderer : Renderer {
//     public MeshRenderer() {
//     }
//
//     public override void Render(CommandList cmdList) {
//         if (Mesh == null || Mesh.IndexBuffer == null) return;
//         if (PipelineState == null) return;
//
//         List<int> renderSubmeshes = ListPool<int>.Shared.Get();
//
//         try {
//             RenderAllSubmeshes(renderSubmeshes, cmdList);
//         } finally {
//             ListPool<int>.Shared.Return(renderSubmeshes);
//         }
//     }
//
//     private void RenderAllSubmeshes(List<int> submeshList, CommandList cmdList) {
//         var cam = Entity.Scene.EnumerateRootEntities().First(x => x.Name == "Camera").GetComponent<Camera>()!;
//         var viewProj = cam.ViewMatrix * cam.ProjectionMatrix;
//
//         //var frustum = FrustumCulling.CalculateFrustumPlanes(viewProj);
//
//         //int idx = 0;
//         //foreach (ref readonly var submesh in Mesh!.Submeshes) {
//         //    // TODO: Account in entity's transformation
//
//         //    switch (submesh.Shape.Type) {
//         //        case MeshBoundaryShapeType.AABB:
//         //            if (FrustumCulling.Test(frustum, submesh.Shape.AABB)) {
//         //                submeshList.Add(idx);
//         //            }
//         //            break;
//
//         //        case MeshBoundaryShapeType.Sphere:
//         //            if (FrustumCulling.Test(frustum, submesh.Shape.Sphere)) {
//         //                submeshList.Add(idx);
//         //            }
//         //            break;
//         //    }
//
//         //    idx++;
//         //}
//
//         //if (submeshList.Count == 0) return;
//
//         //cmdList.SetPipelineState(PipelineState!);
//
//         //cmdList.SetGraphicsBindingSchematic(Unsafe.As<GraphicalShader>(PipelineState!.Shader));
//
//         //Matrix4x4* matrices = stackalloc Matrix4x4[] {
//         //    Entity.LocalToWorldMatrix,
//         //    Entity.LocalToWorldMatrix * viewProj,
//         //};
//
//         //cmdList.SetGraphicsDynamicConstantBuffer("_Transformation", new ReadOnlySpan<byte>(matrices, 128));
//
//         //cmdList.SetIndexBuffer(Mesh!.IndexBuffer!, Mesh.IndexFormat, 0);
//         //Graphics.RenderingPipeline.BindMesh(cmdList, Mesh);
//
//         //foreach (var submeshIndex in submeshList) {
//         //    var submesh = Mesh.Submeshes[submeshIndex];
//
//         //    cmdList.DrawIndexed(submesh.IndexCount, 1, submesh.StartIndex, 0);
//         //}
//     }
// }