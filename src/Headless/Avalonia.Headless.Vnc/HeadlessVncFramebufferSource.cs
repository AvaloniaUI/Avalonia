using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using RemoteViewing.Vnc;
using RemoteViewing.Vnc.Server;

namespace Avalonia.Headless.Vnc
{
    public class HeadlessVncFramebufferSource : IVncFramebufferSource
    {
        public Window Window { get; set; }
        private object _lock = new object();
        public VncFramebuffer _framebuffer = new VncFramebuffer("Avalonia", 1, 1, VncPixelFormat.RGB32);

        private VncButton _previousButtons;
        private RawInputModifiers _keyState;

        public HeadlessVncFramebufferSource(VncServerSession session, Window window)
        {
            Window = window;
            session.PointerChanged += (_, args) =>
            {
                var pt = new Point(args.X, args.Y);
                    
                var buttons = (VncButton)args.PressedButtons;

                MouseButton TranslateButton(VncButton vncButton) =>
                    vncButton switch
                    {
                        VncButton.Left => MouseButton.Left,
                        VncButton.Middle => MouseButton.Middle,
                        VncButton.Right => MouseButton.Right,
                        _ => MouseButton.None
                    };

                var modifiers = (RawInputModifiers)(((int)buttons & 7) << 4);
                
                Dispatcher.UIThread.Post(() =>
                {
                    Window?.MouseMove(pt);
                    foreach (var btn in CheckedButtons)
                        if (_previousButtons.HasFlag(btn) && !buttons.HasFlag(btn))
                            Window?.MouseUp(pt, TranslateButton(btn), modifiers);
                    
                    foreach (var btn in CheckedButtons)
                        if (!_previousButtons.HasFlag(btn) && buttons.HasFlag(btn))
                            Window?.MouseDown(pt, TranslateButton(btn), modifiers);


                    if (buttons == VncButton.ScrollUp)
                        Window?.MouseWheel(pt, Vector.One, modifiers);

                    else if (buttons == VncButton.ScrollDown)
                        Window?.MouseWheel(pt, Vector.One.Negate(), modifiers);

                    _previousButtons = buttons;
                }, DispatcherPriority.Input);
            };

            session.KeyChanged += (_, args) =>
            {
                bool isModifierKey = CheckKeyIsInputModifier(args);
                if (isModifierKey)
                    return;

                var (key, keySymbol) = TranslateKey(args.Keysym);
                if (key == Key.None)
                    return;

                //we only care about text input on key up if not using Ctrl or Alt
                string? inputText = args.Pressed || _keyState.HasFlag(RawInputModifiers.Control) || _keyState.HasFlag(RawInputModifiers.Alt)
                    ? null
                    : keySymbol;

                Dispatcher.UIThread.Post(() =>
                {
                    if (args.Pressed)
                        Window?.KeyPress(key, _keyState, PhysicalKey.None, keySymbol);
                    else
                        Window?.KeyRelease(key, _keyState, PhysicalKey.None, keySymbol);

                    if (inputText != null)
                        Window?.KeyTextInput(inputText);
                }, DispatcherPriority.Input);
            };
        }

        private bool CheckKeyIsInputModifier(KeyChangedEventArgs args)
        {
            RawInputModifiers? toggleModifier = args.Keysym switch
            {
                KeySym.ShiftLeft or KeySym.ShiftRight => RawInputModifiers.Shift,
                KeySym.ControlLeft or KeySym.ControlRight => RawInputModifiers.Control,
                KeySym.AltLeft or KeySym.AltRight => RawInputModifiers.Alt,
                _ => null
            };
            
            if(!toggleModifier.HasValue)
                return false;

            if(args.Pressed)
                _keyState |= toggleModifier.Value;
            else
                _keyState &= ~toggleModifier.Value;

            return true;
        }

        private static (Key key, string? symbol) TranslateKey(KeySym key) =>
            key switch
            {
                KeySym.Backspace => (Key.Back, "\b"),
                KeySym.Tab => (Key.Tab, "\t"),
                KeySym.LineFeed => (Key.LineFeed, null),
                KeySym.Clear => (Key.Clear, null),
                KeySym.Return => (Key.Return, "\r"),
                KeySym.Pause => (Key.Pause, null),
                KeySym.Escape => (Key.Escape, "\u001B"),
                KeySym.Delete => (Key.Delete, null),
                KeySym.Home => (Key.Home, null),
                KeySym.Left => (Key.Left, null),
                KeySym.Up => (Key.Up, null),
                KeySym.Right => (Key.Right, null),
                KeySym.Down => (Key.Down, null),
                KeySym.PageUp => (Key.PageUp, null),
                KeySym.PageDown => (Key.PageDown, null),
                KeySym.End => (Key.End, null),
                KeySym.Begin => (Key.Home, null),
                KeySym.Select => (Key.Select, null),
                KeySym.Print => (Key.Print, null),
                KeySym.Execute => (Key.Execute, null),
                KeySym.Insert => (Key.Insert, null),
                KeySym.Cancel => (Key.Cancel, null),
                KeySym.Help => (Key.Help, null),
                KeySym.Break => (Key.Pause, null),
                KeySym.Num_Lock => (Key.NumLock, null),
                KeySym.NumPadSpace => (Key.Space, null),
                KeySym.NumPadTab => (Key.Tab, null),
                KeySym.NumPadEnter => (Key.Enter, null),
                KeySym.NumPadF1 => (Key.F1, null),
                KeySym.NumPadF2 => (Key.F2, null),
                KeySym.NumPadF3 => (Key.F3, null),
                KeySym.NumPadF4 => (Key.F4, null),
                KeySym.NumPadHome => (Key.Home, null),
                KeySym.NumPadLeft => (Key.Left, null),
                KeySym.NumPadUp => (Key.Up, null),
                KeySym.NumPadRight => (Key.Right, null),
                KeySym.NumPadDown => (Key.Down, null),
                KeySym.NumPadPageUp => (Key.PageUp, null),
                KeySym.NumPadPageDown => (Key.PageDown, null),
                KeySym.NumPadEnd => (Key.End, null),
                KeySym.NumPadBegin => (Key.Home, null),
                KeySym.NumPadInsert => (Key.Insert, null),
                KeySym.NumPadDelete => (Key.Delete, null),
                KeySym.NumPadEqual => (Key.Enter, "="),
                KeySym.NumPadMultiply => (Key.Multiply, "*"),
                KeySym.NumPadAdd => (Key.Add, "+"),
                KeySym.NumPadSeparator => (Key.Separator, NumberFormatInfo.CurrentInfo.NumberGroupSeparator),
                KeySym.NumPadSubtract => (Key.Subtract, "-"),
                KeySym.NumPadDecimal => (Key.Decimal, NumberFormatInfo.CurrentInfo.NumberDecimalSeparator),
                KeySym.NumPadDivide => (Key.Divide, "/"),
                KeySym.NumPad0 => (Key.NumPad0, "0"),
                KeySym.NumPad1 => (Key.NumPad1, "1"),
                KeySym.NumPad2 => (Key.NumPad2, "2"),
                KeySym.NumPad3 => (Key.NumPad3, "3"),
                KeySym.NumPad4 => (Key.NumPad4, "4"),
                KeySym.NumPad5 => (Key.NumPad5, "5"),
                KeySym.NumPad6 => (Key.NumPad6, "6"),
                KeySym.NumPad7 => (Key.NumPad7, "7"),
                KeySym.NumPad8 => (Key.NumPad8, "8"),
                KeySym.NumPad9 => (Key.NumPad9, "9"),
                KeySym.F1 => (Key.F1, null),
                KeySym.F2 => (Key.F2, null),
                KeySym.F3 => (Key.F3, null),
                KeySym.F4 => (Key.F4, null),
                KeySym.F5 => (Key.F5, null),
                KeySym.F6 => (Key.F6, null),
                KeySym.F7 => (Key.F7, null),
                KeySym.F8 => (Key.F8, null),
                KeySym.F9 => (Key.F9, null),
                KeySym.F10 => (Key.F10, null),
                KeySym.F11 => (Key.F11, null),
                KeySym.F12 => (Key.F12, null),
                KeySym.F13 => (Key.F13, null),
                KeySym.F14 => (Key.F14, null),
                KeySym.F15 => (Key.F15, null),
                KeySym.F16 => (Key.F16, null),
                KeySym.F17 => (Key.F17, null),
                KeySym.F18 => (Key.F18, null),
                KeySym.F19 => (Key.F19, null),
                KeySym.F20 => (Key.F20, null),
                KeySym.F21 => (Key.F21, null),
                KeySym.F22 => (Key.F22, null),
                KeySym.F23 => (Key.F23, null),
                KeySym.F24 => (Key.F24, null),
                KeySym.ShiftLeft => (Key.LeftShift, null),
                KeySym.ShiftRight => (Key.RightShift, null),
                KeySym.ControlLeft => (Key.LeftCtrl, null),
                KeySym.ControlRight => (Key.RightCtrl, null),
                KeySym.CapsLock => (Key.CapsLock, null),
                KeySym.AltLeft => (Key.LeftAlt, null),
                KeySym.AltRight => (Key.RightAlt, null),
                KeySym.Space => (Key.Space, " "),
                KeySym.Exclamation => (Key.D1, "!"),
                KeySym.Quote => (Key.D2, "\""),
                KeySym.NumberSign => (Key.D3, "#"),
                KeySym.Dollar => (Key.D4, "$"),
                KeySym.Percent => (Key.D5, "%"),
                KeySym.Ampersand => (Key.D7, "&"),
                KeySym.Apostrophe => (Key.Oem3, "'"),
                KeySym.ParenthesisLeft => (Key.D9, "("),
                KeySym.ParenthesisRight => (Key.D0, ")"),
                KeySym.Asterisk => (Key.D8, "*"),
                KeySym.Plus => (Key.OemPlus, "+"),
                KeySym.Comma => (Key.OemComma, ","),
                KeySym.Minus => (Key.OemMinus, "-"),
                KeySym.Period => (Key.OemPeriod, "."),
                KeySym.Slash => (Key.OemQuestion, "/"),
                KeySym.D0 => (Key.D0, "0"),
                KeySym.D1 => (Key.D1, "1"),
                KeySym.D2 => (Key.D2, "2"),
                KeySym.D3 => (Key.D3, "3"),
                KeySym.D4 => (Key.D4, "4"),
                KeySym.D5 => (Key.D5, "5"),
                KeySym.D6 => (Key.D6, "6"),
                KeySym.D7 => (Key.D7, "7"),
                KeySym.D8 => (Key.D8, "8"),
                KeySym.D9 => (Key.D9, "9"),
                KeySym.Colon => (Key.OemSemicolon, ":"),
                KeySym.Semicolon => (Key.OemSemicolon, ";"),
                KeySym.Less => (Key.OemComma, "<"),
                KeySym.Equal => (Key.OemPlus, "="),
                KeySym.Greater => (Key.OemPeriod, ">"),
                KeySym.Question => (Key.OemQuestion, "?"),
                KeySym.At => (Key.Oem3, "@"),
                KeySym.A => (Key.A, "A"),
                KeySym.B => (Key.B, "B"),
                KeySym.C => (Key.C, "C"),
                KeySym.D => (Key.D, "D"),
                KeySym.E => (Key.E, "E"),
                KeySym.F => (Key.F, "F"),
                KeySym.G => (Key.G, "G"),
                KeySym.H => (Key.H, "H"),
                KeySym.I => (Key.I, "I"),
                KeySym.J => (Key.J, "J"),
                KeySym.K => (Key.K, "K"),
                KeySym.L => (Key.L, "L"),
                KeySym.M => (Key.M, "M"),
                KeySym.N => (Key.N, "N"),
                KeySym.O => (Key.O, "O"),
                KeySym.P => (Key.P, "P"),
                KeySym.Q => (Key.Q, "Q"),
                KeySym.R => (Key.R, "R"),
                KeySym.S => (Key.S, "S"),
                KeySym.T => (Key.T, "T"),
                KeySym.U => (Key.U, "U"),
                KeySym.V => (Key.V, "V"),
                KeySym.W => (Key.W, "W"),
                KeySym.X => (Key.X, "X"),
                KeySym.Y => (Key.Y, "Y"),
                KeySym.Z => (Key.Z, "Z"),
                KeySym.BracketLeft => (Key.OemOpenBrackets, "["),
                KeySym.Backslash => (Key.OemPipe, "\\"),
                KeySym.Bracketright => (Key.OemCloseBrackets, "]"),
                KeySym.Underscore => (Key.OemMinus, "_"),
                KeySym.Grave => (Key.Oem8, "`"),
                KeySym.a => (Key.A, "a"),
                KeySym.b => (Key.B, "b"),
                KeySym.c => (Key.C, "c"),
                KeySym.d => (Key.D, "d"),
                KeySym.e => (Key.E, "e"),
                KeySym.f => (Key.F, "f"),
                KeySym.g => (Key.G, "g"),
                KeySym.h => (Key.H, "h"),
                KeySym.i => (Key.I, "i"),
                KeySym.j => (Key.J, "j"),
                KeySym.k => (Key.K, "k"),
                KeySym.l => (Key.L, "l"),
                KeySym.m => (Key.M, "m"),
                KeySym.n => (Key.M, "n"),
                KeySym.o => (Key.O, "o"),
                KeySym.p => (Key.P, "p"),
                KeySym.q => (Key.Q, "q"),
                KeySym.r => (Key.R, "r"),
                KeySym.s => (Key.S, "s"),
                KeySym.t => (Key.T, "t"),
                KeySym.u => (Key.U, "u"),
                KeySym.v => (Key.V, "v"),
                KeySym.w => (Key.W, "w"),
                KeySym.x => (Key.X, "x"),
                KeySym.y => (Key.Y, "y"),
                KeySym.z => (Key.Z, "z"),
                KeySym.BraceLeft => (Key.OemOpenBrackets, "{"),
                KeySym.Bar => (Key.OemPipe, "|"),
                KeySym.BraceRight => (Key.OemCloseBrackets, "}"),
                KeySym.AsciiTilde => (Key.OemTilde, "~"),
                _ => (Key.None, null)
            };


        [Flags]
        enum VncButton
        {
            Left = 1,
            Middle = 2,
            Right = 4,
            ScrollUp = 8,
            ScrollDown = 16
        }
        

        private static VncButton[] CheckedButtons = new[] {VncButton.Left, VncButton.Middle, VncButton.Right}; 

        public unsafe VncFramebuffer Capture()
        {
            lock (_lock)
            {
                using (var bmpRef = Window.GetLastRenderedFrame())
                {
                    if (bmpRef == null)
                        return _framebuffer;
                    var bmp = bmpRef;
                    if (bmp.PixelSize.Width != _framebuffer.Width || bmp.PixelSize.Height != _framebuffer.Height)
                    {
                        _framebuffer = new VncFramebuffer("Avalonia", bmp.PixelSize.Width, bmp.PixelSize.Height,
                            VncPixelFormat.RGB32);
                    }

                    var buffer = _framebuffer.GetBuffer();
                    fixed (byte* bufferPtr = buffer)
                    {
                        bmp.CopyPixels(new PixelRect(default, bmp.PixelSize), (IntPtr)bufferPtr, buffer.Length, _framebuffer.Stride);
                    }
                }
            }

            return _framebuffer;
        }
    }
}
