namespace RiptideMathematics;

public sealed class NumericalGraph<T> : Graph<NumericalGraph<T>.Keyframe, T> where T : IFloatingPoint<T> {
    public readonly struct Keyframe(float time, T value, float inTangent, float outTangent) : IKeyframe<T, Keyframe> {
        public float Time { get; } = time;
        public T Value { get; } = value;

        public readonly float InTangent = inTangent;
        public readonly float OutTangent = outTangent;

        public static T Interpolate(in Keyframe left, in Keyframe right, float t) {
            float dt = right.Time - left.Time;
	
            float m0 = left.OutTangent * dt;
            float m1 = right.InTangent * dt;

            float t2 = t * t;
            float t3 = t2 * t;
	
            T a = T.CreateChecked(2 * t3 - 3 * t2 + 1);
            T b = T.CreateChecked(t3 - 2 * t2 + t);
            T c = T.CreateChecked(t3 - t2);
            T d = T.CreateChecked(-2 * t3 + 3 * t2);
	
            return a * left.Value + b * T.CreateChecked(m0) + c * T.CreateChecked(m1) + d * right.Value;
        }
    }
}