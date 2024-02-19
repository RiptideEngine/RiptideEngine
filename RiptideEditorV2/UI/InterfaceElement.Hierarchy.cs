using RiptideEngine.Core.Allocation;

namespace RiptideEditorV2.UI;

partial class InterfaceElement {
    public InterfaceElement? Parent { get; protected set; }
    private readonly List<InterfaceElement> _children = [];
    
    public int ChildCount => _children.Count;
    
    public void SetParent(InterfaceElement parent, ParentingFlags flags = ParentingFlags.None) {
        ArgumentNullException.ThrowIfNull(parent, nameof(parent));
        
        if (Parent == null) {
            Parent = parent;
            Parent._children.Add(this);
            
            SynchronizeDocument(Parent.Document, this);
        } else {
            if (IsParentOf(parent)) throw new InvalidOperationException($"Cannot set {nameof(InterfaceElement)}'s child to be its parent.");
            if (Document != parent.Document) throw new InvalidOperationException("Cross document parent reassignment is not allowed.");
            
            bool removal = Parent._children.Remove(this);
            Debug.Assert(removal, "removal");
            
            Parent = parent;
            
            Parent._children.Add(this);
            SynchronizeDocument(Parent.Document, this);
        }

        if (!flags.HasFlag(ParentingFlags.DontUseDefaultMaterial) && Document != null) {
            ApplyDefaultMaterial(parent);
        }

        static void SynchronizeDocument(InterfaceDocument? document, InterfaceElement element) {
            element.Document = document;

            foreach (var child in element) {
                SynchronizeDocument(document, child);
            }
        }

        void ApplyDefaultMaterial(InterfaceElement element) {
            if (element is VisualElement ve) {
                ve.Pipeline = Document!.Renderer.GetDefaultPipeline(ve.GetType());
                Document.Renderer.MarkElementDirty(ve);
                
                Console.WriteLine("Applying default material");
            }

            foreach (var child in element) {
                ApplyDefaultMaterial(child);
            }
        }
    }

    public bool IsChildOf(InterfaceElement parent) {
        var iteration = Parent;
        while (iteration != null) {
            if (iteration == parent) return true;
            
            iteration = iteration.Parent;
        }

        return false;
    }

    public bool IsParentOf(InterfaceElement child) {
        var stack = StackPool<InterfaceElement>.Shared.Get();
        stack.Push(this);

        while (stack.TryPop(out var pop)) {
            if (pop == child) return true;

            foreach (var iterate in pop) {
                stack.Push(iterate);
            }
        }
        
        StackPool<InterfaceElement>.Shared.Return(stack);

        return false;
    }
}