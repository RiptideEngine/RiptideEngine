using Silk.NET.Input;

namespace RiptideFoundation;

/// <summary>
/// Contains extension methods for Silk.NET's objects.
/// </summary>
internal static unsafe class SilkExtensions {
    public static int GetAxis(this IKeyboard keyboard, Key negative, Key positive) {
        bool neg = keyboard.IsKeyPressed(negative);
        bool pos = keyboard.IsKeyPressed(positive);

        return *(byte*)&pos - *(byte*)&neg;
    }
}