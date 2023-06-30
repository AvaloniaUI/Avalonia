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

                Key? key = TranslateKey(args.Keysym);
                if (key == null)
                    return;

                //we only care about text input on key up if not using Ctrl or Alt
                string? inputText = args.Pressed || _keyState.HasFlag(RawInputModifiers.Control) || _keyState.HasFlag(RawInputModifiers.Alt)
                    ? null 
                    : KeyToText(args.Keysym);

                Dispatcher.UIThread.Post(() =>
                {
                    if (args.Pressed)
                        Window?.KeyPress(key.Value, _keyState);
                    else
                        Window?.KeyRelease(key.Value, _keyState);

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

        private static string? KeyToText(KeySym key)
        {
            int keyCode = (int)key;
            if (key >= KeySym.Space && key <= KeySym.AsciiTilde)
                return new string((char)key, 1);

            //handle as normal text chars 0-9
            if (key >= KeySym.NumPad0 && key <= KeySym.NumPad9)
                return new string((char)(key - 65408), 1);

            switch (key)
            {
                case KeySym.NumPadAdd: return "+";
                case KeySym.NumPadSubtract: return "-";
                case KeySym.NumPadMultiply: return "*";
                case KeySym.NumPadDivide: return "/";
                case KeySym.NumPadSeparator: return NumberFormatInfo.CurrentInfo.NumberDecimalSeparator;
            }

            return null;
        }

        private static Key? TranslateKey(KeySym key) =>
            key switch
            {
                KeySym.Backspace => Key.Back,
                KeySym.Tab => Key.Tab,
                KeySym.LineFeed => Key.LineFeed,
                KeySym.Clear => Key.Clear,
                KeySym.Return => Key.Return,
                KeySym.Pause => Key.Pause,
                KeySym.Escape => Key.Escape,
                KeySym.Delete => Key.Delete,
                KeySym.Home => Key.Home,
                KeySym.Left => Key.Left,
                KeySym.Up => Key.Up,
                KeySym.Right => Key.Right,
                KeySym.Down => Key.Down,
                KeySym.PageUp => Key.PageUp,
                KeySym.PageDown => Key.PageDown,
                KeySym.End => Key.End,
                KeySym.Begin => Key.Home,
                KeySym.Select => Key.Select,
                KeySym.Print => Key.Print,
                KeySym.Execute => Key.Execute,
                KeySym.Insert => Key.Insert,
                KeySym.Cancel => Key.Cancel,
                KeySym.Help => Key.Help,
                KeySym.Break => Key.Pause,
                KeySym.Num_Lock => Key.NumLock,
                KeySym.NumPadSpace => Key.Space,
                KeySym.NumPadTab => Key.Tab,
                KeySym.NumPadEnter => Key.Enter,
                KeySym.NumPadF1 => Key.F1,
                KeySym.NumPadF2 => Key.F2,
                KeySym.NumPadF3 => Key.F3,
                KeySym.NumPadF4 => Key.F4,
                KeySym.NumPadHome => Key.Home,
                KeySym.NumPadLeft => Key.Left,
                KeySym.NumPadUp => Key.Up,
                KeySym.NumPadRight => Key.Right,
                KeySym.NumPadDown => Key.Down,
                KeySym.NumPadPageUp => Key.PageUp,
                KeySym.NumPadPageDown => Key.PageDown,
                KeySym.NumPadEnd => Key.End,
                KeySym.NumPadBegin => Key.Home,
                KeySym.NumPadInsert => Key.Insert,
                KeySym.NumPadDelete => Key.Delete,
                KeySym.NumPadEqual => Key.Return,
                KeySym.NumPadMultiply => Key.Multiply,
                KeySym.NumPadAdd => Key.Add,
                KeySym.NumPadSeparator => Key.Separator,
                KeySym.NumPadSubtract => Key.Subtract,
                KeySym.NumPadDecimal => Key.Decimal,
                KeySym.NumPadDivide => Key.Divide,
                KeySym.NumPad0 => Key.NumPad0,
                KeySym.NumPad1 => Key.NumPad1,
                KeySym.NumPad2 => Key.NumPad2,
                KeySym.NumPad3 => Key.NumPad3,
                KeySym.NumPad4 => Key.NumPad4,
                KeySym.NumPad5 => Key.NumPad5,
                KeySym.NumPad6 => Key.NumPad6,
                KeySym.NumPad7 => Key.NumPad7,
                KeySym.NumPad8 => Key.NumPad8,
                KeySym.NumPad9 => Key.NumPad9,
                KeySym.F1 => Key.F1,
                KeySym.F2 => Key.F2,
                KeySym.F3 => Key.F3,
                KeySym.F4 => Key.F4,
                KeySym.F5 => Key.F5,
                KeySym.F6 => Key.F6,
                KeySym.F7 => Key.F7,
                KeySym.F8 => Key.F8,
                KeySym.F9 => Key.F9,
                KeySym.F10 => Key.F10,
                KeySym.F11 => Key.F11,
                KeySym.F12 => Key.F12,
                KeySym.F13 => Key.F13,
                KeySym.F14 => Key.F14,
                KeySym.F15 => Key.F15,
                KeySym.F16 => Key.F16,
                KeySym.F17 => Key.F17,
                KeySym.F18 => Key.F18,
                KeySym.F19 => Key.F19,
                KeySym.F20 => Key.F20,
                KeySym.F21 => Key.F21,
                KeySym.F22 => Key.F22,
                KeySym.F23 => Key.F23,
                KeySym.F24 => Key.F24,
                KeySym.ShiftLeft => Key.LeftShift,
                KeySym.ShiftRight => Key.RightShift,
                KeySym.ControlLeft => Key.LeftCtrl,
                KeySym.ControlRight => Key.RightCtrl,
                KeySym.CapsLock => Key.CapsLock,
                KeySym.AltLeft => Key.LeftAlt,
                KeySym.AltRight => Key.RightAlt,
                KeySym.Space => Key.Space,
                KeySym.Exclamation => Key.D1,
                KeySym.Quote => Key.D2,
                KeySym.NumberSign => Key.D3,
                KeySym.Dollar => Key.D4,
                KeySym.Percent => Key.D5,
                KeySym.Ampersand => Key.D7,
                KeySym.Apostrophe => Key.Oem3,
                KeySym.ParenthesisLeft => Key.D9,
                KeySym.ParenthesisRight => Key.D0,
                KeySym.Asterisk => Key.D8,
                KeySym.Plus => Key.OemPlus,
                KeySym.Comma => Key.OemComma,
                KeySym.Minus => Key.OemMinus,
                KeySym.Period => Key.OemPeriod,
                KeySym.Slash => Key.OemQuestion,
                KeySym.D0 => Key.D0,
                KeySym.D1 => Key.D1,
                KeySym.D2 => Key.D2,
                KeySym.D3 => Key.D3,
                KeySym.D4 => Key.D4,
                KeySym.D5 => Key.D5,
                KeySym.D6 => Key.D6,
                KeySym.D7 => Key.D7,
                KeySym.D8 => Key.D8,
                KeySym.D9 => Key.D9,
                KeySym.Colon => Key.OemSemicolon,
                KeySym.Semicolon => Key.OemSemicolon,
                KeySym.Less => Key.OemComma,
                KeySym.Equal => Key.OemPlus,
                KeySym.Greater => Key.OemPeriod,
                KeySym.Question => Key.OemQuestion,
                KeySym.At => Key.Oem3,
                KeySym.A => Key.A,
                KeySym.B => Key.B,
                KeySym.C => Key.C,
                KeySym.D => Key.D,
                KeySym.E => Key.E,
                KeySym.F => Key.F,
                KeySym.G => Key.G,
                KeySym.H => Key.H,
                KeySym.I => Key.I,
                KeySym.J => Key.J,
                KeySym.K => Key.K,
                KeySym.L => Key.L,
                KeySym.M => Key.M,
                KeySym.N => Key.N,
                KeySym.O => Key.O,
                KeySym.P => Key.P,
                KeySym.Q => Key.Q,
                KeySym.R => Key.R,
                KeySym.S => Key.S,
                KeySym.T => Key.T,
                KeySym.U => Key.U,
                KeySym.V => Key.V,
                KeySym.W => Key.W,
                KeySym.X => Key.X,
                KeySym.Y => Key.Y,
                KeySym.Z => Key.Z,
                KeySym.BracketLeft => Key.OemOpenBrackets,
                KeySym.Backslash => Key.OemPipe,
                KeySym.Bracketright => Key.OemCloseBrackets,
                KeySym.Underscore => Key.OemMinus,
                KeySym.Grave => Key.Oem8,
                KeySym.a => Key.A,
                KeySym.b => Key.B,
                KeySym.c => Key.C,
                KeySym.d => Key.D,
                KeySym.e => Key.E,
                KeySym.f => Key.F,
                KeySym.g => Key.G,
                KeySym.h => Key.H,
                KeySym.i => Key.I,
                KeySym.j => Key.J,
                KeySym.k => Key.K,
                KeySym.l => Key.L,
                KeySym.m => Key.M,
                KeySym.n => Key.M,
                KeySym.o => Key.O,
                KeySym.p => Key.P,
                KeySym.q => Key.Q,
                KeySym.r => Key.R,
                KeySym.s => Key.S,
                KeySym.t => Key.T,
                KeySym.u => Key.U,
                KeySym.v => Key.V,
                KeySym.w => Key.W,
                KeySym.x => Key.X,
                KeySym.y => Key.Y,
                KeySym.z => Key.Z,
                KeySym.BraceLeft => Key.OemOpenBrackets,
                KeySym.Bar => Key.OemPipe,
                KeySym.BraceRight => Key.OemCloseBrackets,
                KeySym.AsciiTilde => Key.OemTilde,
                _ => null
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
