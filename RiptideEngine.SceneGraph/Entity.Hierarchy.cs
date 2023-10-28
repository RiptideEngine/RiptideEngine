namespace RiptideEngine.SceneGraph;

public enum ReorderingType {
    Swap,
    Insert,
}

partial class Entity {
    public Entity? Parent { get; internal set; } = null!;
    private readonly List<Entity> _children;

    public int Depth { get; internal set; }
    public int ChildCount => _children.Count;

    public void SetParent(Entity? parent) {
        if (ReferenceEquals(this, parent)) return;
        if (ReferenceEquals(parent, Parent)) return;

        _internalFlags |= EntityInternalFlags.TransformDirty;

        if (parent == null) {
            NullifyParent();
            return;
        }

        SetParentImpl(parent);

        static void UpdateChildrenDepth(Entity entity, int depth) {
            entity.Depth = depth;

            foreach (var child in entity._children) {
                UpdateChildrenDepth(child, depth + 1);
            }
        }
        void NullifyParent() {
            Parent?._children.Remove(this);
            Parent = null;

            Scene.RootEntities.Add(this);
            UpdateChildrenDepth(this, 0);
        }

        void SetParentImpl(Entity parent) {
            if (Parent == null) {
                FromRootToParent(parent);
                return;
            }

            SwitchBetweenParents(parent);

            void FromRootToParent(Entity parent) {
                if (parent.IsDescendantOf(this)) {
                    parent.SetParent(Parent);
                    Scene.RootEntities.Remove(this);
                    Parent = parent;
                    Parent._children.Add(this);

                    UpdateChildrenDepth(this, parent.Depth + 1);

                    return;
                }

                Scene.RootEntities.Remove(this);
                Parent = parent;
                Parent._children.Add(this);

                UpdateChildrenDepth(this, parent.Depth + 1);
            }
            void SwitchBetweenParents(Entity parent) {
                if (parent.IsDescendantOf(this)) {
                    parent.SetParent(Parent);
                    Parent._children.Remove(this);
                    Parent = parent;
                    Parent._children.Add(this);

                    UpdateChildrenDepth(this, parent.Depth + 1);

                    return;
                }

                Parent._children.Remove(this);
                Parent = parent;
                Parent._children.Add(this);

                UpdateChildrenDepth(this, parent.Depth + 1);
            }
        }
    }

    public int GetChildIndex(Entity entity) => _children.IndexOf(entity);

    public int GetSiblingIndex() {
        if (_internalFlags.HasFlag(EntityInternalFlags.Destroyed)) return -1;

        int idx;

        if (Parent == null) {
            idx = Scene.GetIndexOfEntity(this);
        } else {
            idx = Parent.GetChildIndex(this);
        }

        Debug.Assert(idx != -1);
        return idx;
    }

    public void SetSiblingIndex(int index, ReorderingType reorderingType = ReorderingType.Swap) {
        if (_internalFlags.HasFlag(EntityInternalFlags.Destroyed)) return;

        if (Parent == null) {
            var childIndex = Scene.RootEntities.IndexOf(this);
            Debug.Assert(childIndex != -1);

            switch (reorderingType) {
                case ReorderingType.Swap:
                    index = int.Clamp(index, 0, Scene.RootEntityCount);
                    (Scene.RootEntities[index], Scene.RootEntities[childIndex]) = (Scene.RootEntities[childIndex], Scene.RootEntities[index]);
                    break;

                case ReorderingType.Insert:
                    Scene.RootEntities.RemoveAt(childIndex);
                    Scene.RootEntities.Insert(int.Clamp(index, 0, Scene.RootEntityCount), this);
                    break;
            }
        } else {
            var childIndex = Parent._children.IndexOf(this);
            Debug.Assert(childIndex != -1);

            switch (reorderingType) {
                case ReorderingType.Swap:
                    index = int.Clamp(index, 0, Parent._children.Count);
                    (Parent._children[childIndex], Parent._children[index]) = (Parent._children[index], Parent._children[childIndex]);
                    break;

                case ReorderingType.Insert:
                    Parent._children.RemoveAt(childIndex);
                    Parent._children.Insert(int.Clamp(index, 0, Parent._children.Count), this);
                    break;
            }
        }
    }

    public void MoveScene(Scene newScene) {
        if (_internalFlags.HasFlag(EntityInternalFlags.Destroyed)) return;
        if (ReferenceEquals(this, newScene)) return;

        ArgumentNullException.ThrowIfNull(newScene, nameof(newScene));

        if (Parent == null) {
            bool remove = Scene.RootEntities.Remove(this);
            Debug.Assert(remove);

            Scene = newScene;
            newScene.RootEntities.Add(this);
        } else {
            bool remove = Parent._children.Remove(this);
            Debug.Assert(remove);

            Parent = null;
            Scene = newScene;
            newScene.RootEntities.Add(this);
        }
    }

    public IEnumerable<Entity> EnumerateChildren() => _children;
    public Entity GetChild(int index) => _children[index];
}