using System.Collections;

namespace RiptideMathematics;

public sealed unsafe class Gradient : IEnumerable<Gradient.Keyframe> {
    private static readonly float PositionApproximationThreshold = float.CreateChecked(0.001);
    
    private readonly List<Keyframe> _keys = [];
    public int KeyCount => _keys.Count;

    public bool Add(in Keyframe key) {
        int index = SearchPosition(key.Position);

        if (index >= 0) return false;

        _keys.Insert(~index, key);
        return true;
    }

    public void Overwrite(in Keyframe key) {
        int index = SearchPosition(key.Position);

        if (index >= 0) {
            _keys[index] = key;
        }
        
        _keys.Insert(~index, key);
    }

    public bool Remove(float position) {
        int idx = SearchPosition(position);
        if (idx < 0) return false;
        
        _keys.RemoveAt(idx);
        
        return true;
    }
    
    // TODO: AddKeys, RemoveKeys
    // TODO: Something like AddPosition(float) that take in the position and calculate color as interpolation between 2 keys.
    
    public Color Evaluate(float position) {
        fixed (Keyframe* keys = CollectionsMarshal.AsSpan(_keys)) {
            switch (_keys.Count) {
                case 0: return Color.Black;
                case 1: return keys->Color;
                default:
                    if (position <= keys->Position) return keys->Color;
                    if (position >= (keys + _keys.Count - 1)->Position) return (keys + _keys.Count - 1)->Color;

                    int idx = SearchPosition(position);
                    if (idx >= 0) return (keys + idx)->Color;
                    
                    var right = keys + ~idx;
                    var left = right - 1;

                    var remap = MathUtils.Remap(position, left->Position, right->Position, 0, 1);
                    return Color.Lerp(left->Color, right->Color, remap);
            }
        }
    }

    public void Evaluate(float min, float max, Span<Color> outputs) {
        fixed (Keyframe* keys = CollectionsMarshal.AsSpan(_keys)) {
            switch (_keys.Count) {
                case 0: outputs.Fill(Color.Black); return;
                case 1: outputs.Fill(keys->Color); return;
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
    }

    public void Evaluate(Span<Color> outputs) {
        fixed (Keyframe* keys = CollectionsMarshal.AsSpan(_keys)) {
            switch (_keys.Count) {
                case 0: outputs.Fill(Color.Black); return;
                case 1: outputs.Fill(keys->Color); return;
                default:
                    switch (outputs.Length) {
                        case 0: return;
                        case 1: outputs[0] = keys->Color; return;
                        default:
                            float step = ((keys + _keys.Count - 1)->Position - keys->Position) / (outputs.Length - 1);

                            for (int i = 0; i < outputs.Length; i++) {
                                outputs[i] = Evaluate(keys->Position + step * i);
                            }
                            return;
                    }
            }
        }
    }
    
    public IEnumerator<Keyframe> GetEnumerator() => _keys.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private int SearchPosition(float position) => SearchPosition(position, PositionApproximationThreshold);
    private int SearchPosition(float position, float threshold) {
        fixed (Keyframe* keys = CollectionsMarshal.AsSpan(_keys)) {
            int min = 0;
            int max = _keys.Count - 1;
        
            while (min <= max) {
                int mid = (min + max) / 2;
        
                float cmp = (keys + mid)->Position;
                
                if (float.Abs(position - cmp) < threshold) {
                    return mid;
                } 
                
                if (position < cmp) {
                    max = mid - 1;
                } else {
                    min = mid + 1;
                }
            }
        
            return ~min;
        }
    }

    public readonly record struct Keyframe(float Position, Color Color);
}