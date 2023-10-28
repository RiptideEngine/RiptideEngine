namespace RiptideEditor;

public enum SelectionChangedTarget {
    SceneEntity,
    Resource,
}

public enum SelectionChangedType {
    Add,
    Remove,
    SelectSingle,
}

public delegate void SelectionChangedCallback(SelectionChangedType type, SelectionChangedTarget target);

public static class Selections {
    public static readonly List<Entity> _selectedEntities;
    public static readonly List<Guid> _selectedResources;

    public static event SelectionChangedCallback? OnSelectionChanged;

    public static int NumSelectedEntities => _selectedEntities.Count;
    public static int NumSelectedGuids => _selectedResources.Count;

    static Selections() {
        _selectedEntities = new();
        _selectedResources = new();
    }

    public static void SelectSingle(Entity entity) {
        _selectedResources.Clear();
        _selectedEntities.Clear();

        _selectedEntities.Add(entity);

        OnSelectionChanged?.Invoke(SelectionChangedType.SelectSingle, SelectionChangedTarget.SceneEntity);
    }
    public static void SelectSingle(Guid rid) {
        _selectedResources.Clear();
        _selectedEntities.Clear();

        _selectedResources.Add(rid);

        OnSelectionChanged?.Invoke(SelectionChangedType.SelectSingle, SelectionChangedTarget.Resource);
    }

    public static bool Add(Entity entity) {
        if (_selectedEntities.Contains(entity)) return false;

        _selectedEntities.Add(entity);
        OnSelectionChanged?.Invoke(SelectionChangedType.Add, SelectionChangedTarget.SceneEntity);
        return true;
    }
    public static bool Add(Guid rid) {
        if (_selectedResources.Contains(rid)) return false;

        _selectedResources.Add(rid);
        OnSelectionChanged?.Invoke(SelectionChangedType.Add, SelectionChangedTarget.Resource);
        return true;
    }

    public static bool Remove(Entity obj) {
        if (_selectedEntities.Remove(obj)) {
            OnSelectionChanged?.Invoke(SelectionChangedType.Remove, SelectionChangedTarget.SceneEntity);
            return true;
        }

        return false;
    }
    public static bool Remove(Guid rid) {
        if (_selectedResources.Remove(rid)) {
            OnSelectionChanged?.Invoke(SelectionChangedType.Remove, SelectionChangedTarget.Resource);
            return true;
        }

        return false;
    }

    public static void Clear() {
        if (_selectedEntities.Count != 0) {
            _selectedEntities.Clear();
            OnSelectionChanged?.Invoke(SelectionChangedType.Remove, SelectionChangedTarget.SceneEntity);
        }

        if (_selectedResources.Count != 0) {
            _selectedResources.Clear();
            OnSelectionChanged?.Invoke(SelectionChangedType.Remove, SelectionChangedTarget.Resource);
        }
    }

    public static bool IsSelected(Entity obj) => _selectedEntities.Contains(obj);
    public static bool IsSelected(Guid obj) => _selectedResources.Contains(obj);

    public static IEnumerable<Entity> EnumerateSelectedEntities() => _selectedEntities;
    public static IEnumerable<Guid> EnumerateSelectedResourceIDs() => _selectedResources;
}