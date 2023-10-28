using RiptideMathematics;

namespace RiptideEngine.SceneGraph;

partial class Entity {
    private Vector3 _localPosition = Vector3.Zero, _localScale = Vector3.One;
    private Quaternion _localRotation = Quaternion.Identity;

    private Vector3 _globalPosition = Vector3.Zero, _globalScale = Vector3.One;
    private Quaternion _globalRotation = Quaternion.Identity;

    private Matrix4x4 _localToWorldMatrix = Matrix4x4.Identity;
    private Matrix4x4 _worldToLocalMatrix = Matrix4x4.Identity;

    public Vector3 LocalPosition {
        get => _localPosition;
        set {
            _localPosition = value;
            _internalFlags |= EntityInternalFlags.TransformDirty;
        }
    }

    public Quaternion LocalRotation {
        get => _localRotation;
        set {
            _localRotation = value;
            _internalFlags |= EntityInternalFlags.TransformDirty;
        }
    }

    public Vector3 LocalScale {
        get => _localScale;
        set {
            _localScale = value;
            _internalFlags |= EntityInternalFlags.TransformDirty;
        }
    }

    public Vector3 GlobalPosition {
        get {
            if (_internalFlags.HasFlag(EntityInternalFlags.TransformDirty)) UpdateLocalToWorld();
            return _globalPosition;
        }
        set {
            if (Parent != null) {
                LocalPosition = Vector3.Transform(value, _worldToLocalMatrix);
            } else {
                LocalPosition = value;
            }
        }
    }

    public Quaternion GlobalRotation {
        get {
            if (_internalFlags.HasFlag(EntityInternalFlags.TransformDirty)) UpdateLocalToWorld();
            return _globalRotation;
        }
        set {
            if (Parent != null) {
                LocalRotation = Quaternion.Inverse(_globalRotation) * value;
            } else {
                LocalRotation = value;
            }
        }
    }

    public Vector3 GlobalScale {
        get {
            if (_internalFlags.HasFlag(EntityInternalFlags.TransformDirty)) UpdateLocalToWorld();
            return _globalScale;
        }
        set {
            if (Parent != null) {
                LocalScale = Vector3.TransformNormal(value, _worldToLocalMatrix);
            } else {
                LocalScale = value;
            }
        }
    }

    public Matrix4x4 LocalToWorldMatrix {
        get {
            if (_internalFlags.HasFlag(EntityInternalFlags.TransformDirty)) UpdateLocalToWorld();

            return _localToWorldMatrix;
        }
    }
    public Matrix4x4 WorldToLocalMatrix {
        get {
            if (_internalFlags.HasFlag(EntityInternalFlags.TransformDirty)) UpdateLocalToWorld();

            return _worldToLocalMatrix;
        }
    }

    private void UpdateLocalToWorld() {
        if (_internalFlags.HasFlag(EntityInternalFlags.TransformDirty)) {
            ForceUpdateLocalToWorld();
            return;
        }

        foreach (var child in _children) {
            child.UpdateLocalToWorld();
        }
    }

    private void ForceUpdateLocalToWorld() {
        //var localMatrix = Matrix4x4.CreateScale(_localScale) * Matrix4x4.CreateFromQuaternion(_localRotation) * Matrix4x4.CreateTranslation(_localPosition);
        var localMatrix = MathUtils.CreateModel(_localPosition, _localRotation, _localScale);

        if (Parent != null) {
            var l2w = Parent.LocalToWorldMatrix;

            _globalPosition = Vector3.Transform(_localPosition, l2w);
            _globalRotation = Quaternion.CreateFromRotationMatrix(_localToWorldMatrix) * _localRotation;
            _globalScale = Vector3.TransformNormal(_localScale, l2w);

            _localToWorldMatrix = l2w * localMatrix;
            Matrix4x4.Invert(_localToWorldMatrix, out _worldToLocalMatrix);
        } else {
            _globalPosition = _localPosition;
            _globalRotation = _localRotation;
            _globalScale = _localScale;

            _localToWorldMatrix = _worldToLocalMatrix = Matrix4x4.Identity;
        }

        //if (Parent != null) {
        //    _localToWorldMatrix = Parent.LocalToWorldMatrix * localMatrix;
        //} else {
        //    _localToWorldMatrix = localMatrix;
        //}

        //_globalPosition = Vector3.Transform(_localPosition, _localToWorldMatrix);
        //_globalRotation = Quaternion.CreateFromRotationMatrix(_localToWorldMatrix) * _localRotation;
        //_globalScale = Vector3.TransformNormal(_localScale, _localToWorldMatrix);

        _internalFlags &= ~EntityInternalFlags.TransformDirty;

        foreach (var child in _children) {
            child.ForceUpdateLocalToWorld();
        }
    }
}