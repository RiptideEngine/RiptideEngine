namespace RiptideEditorV2.UI;

partial class InterfaceElement : IEnumerable<InterfaceElement> {
    public InterfaceElement this[int index] => _children[index];
    
    public IEnumerable<T> Query<T>() where T : InterfaceElement => _children.OfType<T>();
    public TypeNameFilterEnumerator<T> Query<T>(string name) where T : InterfaceElement => new(_children, name);

    public T? Search<T>() where T : InterfaceElement {
        foreach (var child in _children) {
            if (child is T t) return t;
        }
        
        return null;
    }

    public InterfaceElement? Search(string name) {
        foreach (var child in _children) {
            if (child.Name == name) return child;
        }
        
        return null;
    }
    
    public T? Search<T>(string name) where T : InterfaceElement {
        foreach (var child in _children) {
            if (child.Name == name && child is T t) return t;
        }

        return null;
    }
    
    public List<InterfaceElement>.Enumerator GetEnumerator() => _children.GetEnumerator();
    
    IEnumerator<InterfaceElement> IEnumerable<InterfaceElement>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<InterfaceElement>)this).GetEnumerator();

    public struct TypeNameFilterEnumerator<T> : IEnumerator<T>, IEnumerable<T> {
        private int _index = 0;
        private readonly IReadOnlyList<InterfaceElement> _children;
        private readonly string _name;

        internal TypeNameFilterEnumerator(IReadOnlyList<InterfaceElement> children, string name) {
            _children = children;
            _name = name;
        }

        public IEnumerator<T> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool MoveNext() {
            for (; _index < _children.Count; _index++) {
                if (_children[_index].Name == _name && _children[_index] is T t) {
                    Current = t;
                    return true;
                }
            }

            return false;
        }
        public void Reset() {
            throw new NotSupportedException();
        }

        public T Current { get; private set; } = default!;
        object IEnumerator.Current => Current!;
        
        public void Dispose() { }
    }
}