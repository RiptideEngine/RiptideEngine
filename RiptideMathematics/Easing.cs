namespace RiptideMathematics;

public static class Easing {
    public static T Evaluate<T>(T x, EasingMode mode) where T : INumber<T>, ITrigonometricFunctions<T>, IPowerFunctions<T>, IRootFunctions<T> {
        return mode switch {
            EasingMode.SineIn => EvaluateSineIn(x),
            EasingMode.SineOut => EvaluateSineOut(x),
            EasingMode.Sine => EvaluateSine(x),
            EasingMode.QuadIn => EvaluateQuadIn(x),
            EasingMode.QuadOut => EvaluateQuadOut(x),
            EasingMode.Quad => EvaluateQuad(x),
            EasingMode.CubicIn => EvaluateCubicIn(x),
            EasingMode.CubicOut => EvaluateCubicOut(x),
            EasingMode.Cubic => EvaluateCubic(x),
            EasingMode.QuartIn => EvaluateQuartIn(x),
            EasingMode.QuartOut => EvaluateQuartOut(x),
            EasingMode.Quart => EvaluateQuart(x),
            EasingMode.QuintIn => EvaluateQuintIn(x),
            EasingMode.QuintOut => EvaluateQuintOut(x),
            EasingMode.Quint => EvaluateQuint(x),
            EasingMode.ExpoIn => EvaluateExpoIn(x),
            EasingMode.ExpoOut => EvaluateExpoOut(x),
            EasingMode.Expo => EvaluateExpo(x),
            EasingMode.CircIn => EvaluateCircIn(x),
            EasingMode.CircOut => EvaluateCircOut(x),
            EasingMode.Circ => EvaluateCirc(x),
            EasingMode.BackIn => EvaluateBackIn(x),
            EasingMode.BackOut => EvaluateBackOut(x),
            EasingMode.Back => EvaluateBack(x),
            EasingMode.ElasticIn => EvaluateElasticIn(x),
            EasingMode.ElasticOut => EvaluateElasticOut(x),
            EasingMode.Elastic => EvaluateElastic(x),
            EasingMode.BounceIn => EvaluateBounceIn(x),
            EasingMode.BounceOut => EvaluateBounceOut(x),
            EasingMode.Bounce => EvaluateBounce(x),
            _ => x
        };
    }
    
    public static T EvaluateSineIn<T>(T x) where T : INumber<T>, ITrigonometricFunctions<T> => T.One - T.SinPi(x / T.CreateChecked(2));
    public static T EvaluateSineOut<T>(T x) where T : INumber<T>, ITrigonometricFunctions<T> => T.SinPi(x / T.CreateChecked(2));
    public static T EvaluateSine<T>(T x) where T : INumber<T>, ITrigonometricFunctions<T> => (-T.CosPi(x) + T.One) / T.CreateChecked(2);

    public static T EvaluateQuadIn<T>(T x) where T : INumber<T> => x * x;
    public static T EvaluateQuadOut<T>(T x) where T : INumber<T> => T.One - (T.One - x) * (T.One - x);
    public static T EvaluateQuad<T>(T x) where T : INumber<T> => x < T.CreateChecked(0.5) ? T.CreateChecked(2) * x * x : T.One - (T.CreateChecked(-2) * x + T.CreateChecked(2)) * (T.CreateChecked(-2) * x + T.CreateChecked(2)) / T.CreateChecked(2);

    public static T EvaluateCubicIn<T>(T x) where T : INumber<T> => x * x * x;
    public static T EvaluateCubicOut<T>(T x) where T : INumber<T> => T.One - (T.One - x) * (T.One - x) * (T.One - x);
    public static T EvaluateCubic<T>(T x) where T : INumber<T> {
        if (x < T.CreateChecked(0.5)) return T.CreateChecked(4) * x * x * x;

        T p = T.CreateChecked(-2) * x + T.CreateChecked(2);
        return T.One - p * p * p / T.CreateChecked(2);
    }

    public static T EvaluateQuartIn<T>(T x) where T : INumber<T> => x * x * x * x;
    public static T EvaluateQuartOut<T>(T x) where T : INumber<T> {
        T ix = T.One - x;
        return T.One - ix * ix * ix * ix;
    }
    public static T EvaluateQuart<T>(T x) where T : INumber<T> {
        if (x < T.CreateChecked(0.5)) return T.CreateChecked(8) * x * x * x * x;

        T p = T.CreateChecked(-2) * x + T.CreateChecked(2);
        return T.One - p * p * p * p / T.CreateChecked(2);
    }

    public static T EvaluateQuintIn<T>(T x) where T : INumber<T> => x * x * x * x * x;
    public static T EvaluateQuintOut<T>(T x) where T : INumber<T> {
        T ix = T.One - x;
        return T.One - ix * ix * ix * ix * ix;
    }
    public static T EvaluateQuint<T>(T x) where T : INumber<T> {
        if (x < T.CreateChecked(0.5)) return T.CreateChecked(16) * x * x * x * x * x;

        T p = T.CreateChecked(-2) * x + T.CreateChecked(2);
        return T.One - p * p * p * p * p / T.CreateChecked(2);
    }

    public static T EvaluateExpoIn<T>(T x) where T : INumber<T>, IPowerFunctions<T> => T.IsZero(x) ? T.Zero : T.Pow(T.CreateChecked(2), T.CreateChecked(10) * x - T.CreateChecked(10));
    public static T EvaluateExpoOut<T>(T x) where T : INumber<T>, IPowerFunctions<T> => x == T.One ? T.One : T.One - T.Pow(T.CreateChecked(2), T.CreateChecked(-10) * x);
    public static T EvaluateExpo<T>(T x) where T : INumber<T>, IPowerFunctions<T> {
        return T.IsZero(x) ? T.Zero
            : x == T.One ? T.One
            : x < T.CreateChecked(0.5) ? T.Pow(T.CreateChecked(2), T.CreateChecked(20) * x - T.CreateChecked(10)) / T.CreateChecked(2)
            : (T.CreateChecked(2) - T.Pow(T.CreateChecked(2), T.CreateChecked(-20) * x + T.CreateChecked(10))) / T.CreateChecked(2);
    }

    public static T EvaluateCircIn<T>(T x) where T : INumber<T>, IRootFunctions<T> => T.One - T.Sqrt(T.One - x * x);
    public static T EvaluateCircOut<T>(T x) where T : INumber<T>, IRootFunctions<T> => T.Sqrt(T.One - (x - T.One) * (x - T.One));
    public static T EvaluateCirc<T>(T x) where T : INumber<T>, IRootFunctions<T> {
        return (x < T.CreateChecked(0.5) ? T.One - T.Sqrt(T.One - T.CreateChecked(4) * x * x) : T.Sqrt(T.One - (T.CreateChecked(-2) * x + T.CreateChecked(2)) * (T.CreateChecked(-2) * x + T.CreateChecked(2))) + T.One) / T.CreateChecked(2);
    }

    public static T EvaluateBackIn<T>(T x) where T : INumber<T> => T.CreateChecked(2.70158) * x * x * x - T.CreateChecked(1.70158) * x * x;
    public static T EvaluateBackOut<T>(T x) where T : INumber<T> {
        T xm1 = x - T.One;
        return T.One + T.CreateChecked(2.70158) * xm1 * xm1 * xm1 + T.CreateChecked(1.70158) * xm1 * xm1;
    }
    public static T EvaluateBack<T>(T x) where T : INumber<T> {
        T c2 = T.CreateChecked(2.5949095);
        T two = T.CreateChecked(2);

        return (x < T.CreateChecked(0.5)
            ? T.CreateChecked(4) * x * x * ((c2 + T.One) * two * x - c2)
            : (two * x - two) * (two * x - two) * ((c2 + T.One) * (x * two - two) + c2) + two) / two;
    }

    public static T EvaluateElasticIn<T>(T x) where T : INumber<T>, IPowerFunctions<T>, ITrigonometricFunctions<T> {
        return T.IsZero(x) ? T.Zero : x == T.One ? T.One : -T.Pow(T.CreateChecked(2), T.CreateChecked(10) * x - T.CreateChecked(10)) * T.SinPi((x * T.CreateChecked(10) - T.CreateChecked(10.75)) * T.CreateChecked(0.666666));
    }
    public static T EvaluateElasticOut<T>(T x) where T : INumber<T>, IPowerFunctions<T>, ITrigonometricFunctions<T> {
        return T.IsZero(x) ? T.Zero : x == T.One ? T.One : T.Pow(T.CreateChecked(2), T.CreateChecked(-10) * x) * T.SinPi((x * T.CreateChecked(10) - T.CreateChecked(0.75f)) * T.CreateChecked(0.666666)) + T.One;
    }
    public static T EvaluateElastic<T>(T x) where T : INumber<T>, IPowerFunctions<T>, ITrigonometricFunctions<T> {
        return T.IsZero(x) ? T.Zero : x == T.One ? T.One : x < T.CreateChecked(0.5) ?
            -(T.Pow(T.CreateChecked(2), T.CreateChecked(20) * x - T.CreateChecked(10)) * T.SinPi((T.CreateChecked(20) * x - T.CreateChecked(11.125)) * T.CreateChecked(0.444444))) / T.CreateChecked(2) :
            (T.Pow(T.CreateChecked(2), T.CreateChecked(-20) * x + T.CreateChecked(10)) * T.SinPi((T.CreateChecked(20) * x - T.CreateChecked(11.125)) * T.CreateChecked(0.444444))) / T.CreateChecked(2) + T.One;
    }
    
    public static T EvaluateBounceIn<T>(T x) where T : INumber<T> => T.One - EvaluateBounceOut(x);
    public static T EvaluateBounceOut<T>(T x) where T : INumber<T> {
        T n1 = T.CreateChecked(7.5625);
        T d1 = T.CreateChecked(2.75);

        if (x < T.One / d1) return n1 * x * x;
        if (x < T.CreateChecked(2) / d1) return n1 * (x -= T.CreateChecked(1.5) / d1) * x + T.CreateChecked(0.75);
        if (x < T.CreateChecked(2.5) / d1) return n1 * (x -= T.CreateChecked(2.25) / d1) * x + T.CreateChecked(0.9375);
        return n1 * (x -= T.CreateChecked(2.625) / d1) * x + T.CreateChecked(0.984375);
    }
    public static T EvaluateBounce<T>(T x) where T : INumber<T> => (x < T.CreateChecked(0.5) ? T.One - EvaluateBounceOut(T.One - x - x) : T.One + EvaluateBounceOut(x + x - T.One)) / T.CreateChecked(2);
}