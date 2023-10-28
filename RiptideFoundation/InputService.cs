using Silk.NET.Input;
using Silk.NET.Windowing;

namespace RiptideFoundation;

public interface IInputService : IRiptideService {
    event Action<char>? KeyChar;
    event Action<Key>? KeyDown;
    event Action<Key>? KeyUp;
    event Action<ScrollWheel>? MouseScroll;

    Vector2 MousePosition { get; set; }

    int GetAxis(Key negative, Key positive);
    bool IsKeyHolding(Key key);
    bool IsMouseHolding(MouseButton button);
}

public sealed class InputService : IInputService {
    private IInputContext _inputCtx;

    private IKeyboard _mainKeyboard;
    private IMouse _mainMouse;

    public event Action<char>? KeyChar;
    public event Action<Key>? KeyDown;
    public event Action<Key>? KeyUp;
    public event Action<ScrollWheel>? MouseScroll;

    public Vector2 MousePosition {
        get => _mainMouse.Position;
        set => _mainMouse.Position = value;
    }

    public InputService(IView view) {
        _inputCtx = view.CreateInput();

        _mainKeyboard = _inputCtx.Keyboards[0];
        _mainMouse = _inputCtx.Mice[0];

        _mainKeyboard.KeyChar += OnKeyChar;
        _mainKeyboard.KeyDown += OnKeyDown;
        _mainKeyboard.KeyUp += OnKeyUp;
        _mainMouse.Scroll += OnMouseScroll;
    }

    public int GetAxis(Key negative, Key positive) {
        bool neg = _mainKeyboard.IsKeyPressed(negative);
        bool pos = _mainKeyboard.IsKeyPressed(positive);

        return Unsafe.BitCast<bool, byte>(pos) - Unsafe.BitCast<bool, byte>(neg);
    }

    public bool IsMouseHolding(MouseButton button) {
        return _mainMouse.IsButtonPressed(button);
    }

    public bool IsKeyHolding(Key key) {
        return _mainKeyboard.IsKeyPressed(key);
    }

    private void OnKeyChar(IKeyboard keyboard, char character) {
        KeyChar?.Invoke(character);
    }

    private void OnKeyDown(IKeyboard keyboard, Key key, int repeat) {
        KeyDown?.Invoke(key);
    }

    private void OnKeyUp(IKeyboard keyboard, Key key, int repeat) {
        KeyUp?.Invoke(key);
    }

    private void OnMouseScroll(IMouse mouse, ScrollWheel wheel) {
        MouseScroll?.Invoke(wheel);
    }

    public void Dispose() {
        if (_inputCtx == null) return;

        _mainKeyboard.KeyUp -= OnKeyUp;
        _mainKeyboard.KeyDown -= OnKeyDown;
        _mainKeyboard.KeyChar -= OnKeyChar;

        _mainKeyboard = null!;
        _mainMouse = null!;
        _inputCtx.Dispose(); _inputCtx = null!;
    }
}