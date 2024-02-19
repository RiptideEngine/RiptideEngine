using RiptideFoundation.Rendering;

namespace RiptideEditorV2.UI;

internal sealed unsafe class DefaultUIBatcher : UIBatcher {
    public override void BuildBatch(MaterialPipeline pipeline, InterfaceElement root, Matrix3x2 transformation, in BatchingOperation op) {
        RecursivelyBuildBatch(pipeline, root, transformation, op);
        
        static void RecursivelyBuildBatch(MaterialPipeline pipeline, InterfaceElement element, Matrix3x2 transformation, in BatchingOperation op) {
            var transform = Matrix3x2.CreateTranslation(element.ResolvedLayout.Rectangle.Position);
            
            if (element is VisualElement ve && ve.Pipeline == pipeline) {
                op.AppendUniqueCandidate(ve, out var builder);
                ve.GenerateMesh(builder, transformation * transform);
            }

            foreach (var child in element) {
                RecursivelyBuildBatch(pipeline, child, transform, op);
            }
        }
    }
}