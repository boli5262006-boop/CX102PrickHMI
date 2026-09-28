using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CX102PrickHMI.Behaviors
{

    public static class MomentaryButtonBehavior
    {
        /// <summary>Identifies the Command attached property (ICommand, receives true/false).</summary>
        public static readonly DependencyProperty CommandProperty =
            DependencyProperty.RegisterAttached(
                "Command",
                typeof(ICommand),
                typeof(MomentaryButtonBehavior),
                new PropertyMetadata(OnCommandChanged));


        private static readonly DependencyProperty IsPressActiveProperty =
            DependencyProperty.RegisterAttached(
                "IsPressActive",
                typeof(bool),
                typeof(MomentaryButtonBehavior),
                new PropertyMetadata(false));

        /// <summary>Gets the momentary command attached to the specified button.</summary>
        public static ICommand GetCommand(DependencyObject obj)
        {
            return (ICommand)obj.GetValue(CommandProperty);
        }

        /// <summary>Sets the momentary command attached to the specified button.</summary>
        public static void SetCommand(DependencyObject obj, ICommand value)
        {
            obj.SetValue(CommandProperty, value);
        }

        private static bool GetIsPressActive(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsPressActiveProperty);
        }

        private static void SetIsPressActive(DependencyObject obj, bool value)
        {
            obj.SetValue(IsPressActiveProperty, value);
        }

        private static Button GetButton(DependencyObject dobj)
        {
            return dobj as Button;
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = GetButton(d);
            if (button == null)
            {
                return;
            }

            if (e.OldValue != null)
            {
                button.PreviewMouseLeftButtonDown -= OnPreviewMouseLeftButtonDown;
                button.PreviewMouseLeftButtonUp -= OnPreviewMouseLeftButtonUp;
                button.LostMouseCapture -= OnLostMouseCapture;
            }

            if (e.NewValue != null)
            {
                button.PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
                button.PreviewMouseLeftButtonUp += OnPreviewMouseLeftButtonUp;
                button.LostMouseCapture += OnLostMouseCapture;
            }

            SetIsPressActive(button, false);
        }

        private static void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var button = GetButton(sender as DependencyObject);
            if (button == null || !button.IsEnabled)
            {
                return;
            }

            var command = GetCommand(button);
            if (command == null || !command.CanExecute(true))
            {
                return;
            }

            SetIsPressActive(button, true);
            command.Execute(true);
        }

        private static void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Release(sender as DependencyObject);
        }

        private static void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            Release(sender as DependencyObject);
        }

        /// <summary>
        /// Sends the release signal ("false") once per press, whatever ended it.
        /// ButtonBase captures the mouse itself, so no explicit CaptureMouse here.
        /// </summary>
        private static void Release(DependencyObject dobj)
        {
            var button = GetButton(dobj);
            if (button == null || !GetIsPressActive(button))
            {
                return;
            }

            SetIsPressActive(button, false);

            var command = GetCommand(button);
            if (command != null && command.CanExecute(false))
            {
                command.Execute(false);
            }
        }
    }
}
