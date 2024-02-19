namespace RiptideMathematics;

public interface IKeyframe<TValue, TSelf> where TSelf : IKeyframe<TValue, TSelf> {
    float Time { get; }
    TValue Value { get; }

    static abstract TValue Interpolate(in TSelf a, in TSelf b, float evaluateTime);
}