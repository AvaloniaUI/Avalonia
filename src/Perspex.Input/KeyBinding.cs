﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Perspex.Input
{
    public class KeyBinding : PerspexObject
    {
        public static readonly StyledProperty<ICommand> CommandProperty =
            PerspexProperty.Register<KeyBinding, ICommand>("Command");

        public ICommand Command
        {
            get { return GetValue(CommandProperty); }
            set { SetValue(CommandProperty, value); }
        }

        public static readonly StyledProperty<object> CommandParameterProperty =
            PerspexProperty.Register<KeyBinding, object>("CommandParameter");

        public object CommandParameter
        {
            get { return GetValue(CommandParameterProperty); }
            set { SetValue(CommandParameterProperty, value); }
        }

        public static readonly StyledProperty<KeyGesture> GestureProperty =
            PerspexProperty.Register<KeyBinding, KeyGesture>("Gesture");

        public KeyGesture Gesture
        {
            get { return GetValue(GestureProperty); }
            set { SetValue(GestureProperty, value); }
        }

        public void TryHandle(KeyEventArgs args)
        {
            if (Gesture?.Matches(args) == true)
            {
                args.Handled = true;
                if (Command?.CanExecute(CommandParameter) == true)
                    Command.Execute(CommandParameter);
            }
        }
    }
}
