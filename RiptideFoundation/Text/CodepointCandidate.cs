using System.Collections;
using System.Text;

namespace RiptideFoundation.Text;

public enum CodepointCandidateType {
    Codepoint,
    Range,
    String,
}

public readonly struct CodepointCandidate : IEnumerable<int> {
    public readonly CodepointCandidateType Type;

    public readonly int Codepoint;
    public readonly string Characters;
    public readonly IntegerRange<int> Range;

    public CodepointCandidate() {
        Type = CodepointCandidateType.Codepoint;
        Characters = string.Empty;
    }

    public CodepointCandidate(int codepoint) {
        Type = CodepointCandidateType.Codepoint;
        Codepoint = codepoint;
        Characters = string.Empty;
    }

    public CodepointCandidate(Rune rune) : this(rune.Value) { }

    public CodepointCandidate(string characters) {
        Type = CodepointCandidateType.String;
        Characters = characters;
    }

    public CodepointCandidate(IntegerRange<int> range) {
        Type = CodepointCandidateType.Range;
        Range = range;
        Characters = string.Empty;
    }

    public Enumerator GetEnumerator() => new(this);

    IEnumerator<int> IEnumerable<int>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static implicit operator CodepointCandidate(int codepoint) => new(codepoint);
    public static implicit operator CodepointCandidate(Rune rune) => new(rune);
    public static implicit operator CodepointCandidate(string characters) => new(characters);
    public static implicit operator CodepointCandidate(IntegerRange<int> range) => new(range);
    
    public struct Enumerator : IEnumerator<int> {
        private CodepointCandidate _candidate;
        private int _state;
        private int _nextCharacterIndex;
        
        public int Current { get; private set; }
        object IEnumerator.Current => Current;
        
        internal Enumerator(CodepointCandidate candidate) {
            _candidate = candidate;
        }
    
        public bool MoveNext() {
            switch (_candidate.Type) {
                case CodepointCandidateType.Codepoint:
                    switch (_state) {
                        case 0:
                            Current = _candidate.Codepoint;
                            _state = 1;
                            return true;
                        
                        case 1: return false;
                    }

                    return false;
                
                case CodepointCandidateType.Range:
                    var range = _candidate.Range;

                    switch (_state) {
                        case 0:
                            if (range.Max <= range.Min) break;
                            
                            Current = range.Min;
                            _state = 1;
                            return true;
                        case 1:
                            if (unchecked(++Current) == range.Max) break;
                            return true;
                    }

                    _state = -1;
                    return false;
                
                case CodepointCandidateType.String:
                    var characters = _candidate.Characters;
                    
                    if (_nextCharacterIndex >= characters.Length) {
                        Current = 0;
                        return false;
                    }

                    Rune rune;
                    while (!Rune.TryGetRuneAt(characters, _nextCharacterIndex, out rune)) {
                        _nextCharacterIndex++;
                        
                        if (_nextCharacterIndex >= characters.Length) {
                            Current = 0;
                            return false;
                        }
                    }
                    
                    _nextCharacterIndex += rune.Utf16SequenceLength;
                    Current = rune.Value;
                    return true;
                
                default: return false;
            }
        }
        
        public void Reset() { }
        public void Dispose() { }
    }
}