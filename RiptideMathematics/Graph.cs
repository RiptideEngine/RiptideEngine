using System.Collections;

namespace RiptideMathematics;

public class Graph<TKeyframe, TValue> : IEnumerable<TKeyframe> where TKeyframe : IKeyframe<TValue, TKeyframe> {
    private readonly List<TKeyframe> _keys = [];
    public int KeyCount => _keys.Count;

    public bool Add(in TKeyframe key) {
        int index = SearchPosition(key.Time);

        if (index >= 0) return false;
        
        _keys.Insert(~index, key);
        return true;
    }

    public void Overwrite(in TKeyframe key) {
        int index = SearchPosition(key.Time);

        if (index >= 0) {
            _keys[index] = key;
        }
        
        _keys.Insert(~index, key);
    }

    public bool Remove(float time) {
        int idx = SearchPosition(time);
        if (idx < 0) return false;
        
        _keys.RemoveAt(idx);
        
        return true;
    }

    public TValue Evaluate(float time) {
        switch (_keys.Count) {
            case 0: return default!;
            case 1: return _keys[0].Value;
            default:
                ref var first = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(_keys));
                if (time < first.Time) return first.Value;

                ref var last = ref Unsafe.Add(ref first, _keys.Count - 1);
                if (time > last.Time) return last.Value;
                
                int idx = SearchPosition(time);
                if (idx >= 0) return Unsafe.Add(ref first, idx).Value;

                var right = Unsafe.Add(ref first, ~idx);
                var left = Unsafe.Subtract(ref right, 1);

                return TKeyframe.Interpolate(left, right, time);
        }
    }

    public void Evaluate(float min, float max, Span<TValue> outputs) {
        switch (_keys.Count) {
            case 0: outputs.Clear(); return;
            case 1: outputs.Fill(_keys[0].Value); return;
            default:
                switch (outputs.Length) {
                    case 0: return;
                    case 1: outputs[0] = Evaluate(min); return;
                    default:
                        float step = (max - min) / (outputs.Length - 1);
                        
                        // TODO: Optimize case in which min < keys[0].Position and max > keys[^1].Position via Fill.
                        for (int i = 0; i < outputs.Length; i++) {
                            outputs[i] = Evaluate(min + step * i);
                        }
                        return;
                }
        }
    }
    
    public void Evaluate(Span<TValue> outputs) {
        switch (_keys.Count) {
            case 0: outputs.Clear(); return;
            case 1: outputs.Fill(_keys[0].Value); return;
            default:
                switch (outputs.Length) {
                    case 0: return;
                    case 1: outputs[0] = _keys[0].Value; return;
                    default:
                        ref var first = ref MemoryMarshal.GetReference(CollectionsMarshal.AsSpan(_keys));
                        float step = (Unsafe.Add(ref first, _keys.Count - 1).Time - first.Time) / (outputs.Length - 1);

                        for (int i = 0; i < outputs.Length; i++) {
                            outputs[i] = Evaluate(first.Time + step * i);
                        }
                        return;
                }
        }
    }

    private int SearchPosition(float position) => SearchPosition(position, 0.001f);
    private int SearchPosition(float time, float threshold) {
        var span = CollectionsMarshal.AsSpan(_keys);
        int min = 0;
        int max = _keys.Count - 1;
    
        while (min <= max) {
            int mid = (min + max) / 2;
    
            float cmp = span[mid].Time;
            
            if (float.Abs(time - cmp) < threshold) {
                return mid;
            } 
            
            if (time < cmp) {
                max = mid - 1;
            } else {
                min = mid + 1;
            }
        }

        return ~min;
    }
    
    public IEnumerator<TKeyframe> GetEnumerator() => _keys.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}