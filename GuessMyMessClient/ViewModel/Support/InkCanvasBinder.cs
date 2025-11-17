using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace GuessMyMessClient.ViewModel.Support
{
    public static class InkCanvasBinder
    {
        public static readonly DependencyProperty EraserSizeProperty =
            DependencyProperty.RegisterAttached(
                "EraserSize",
                typeof(double),
                typeof(InkCanvasBinder),
                new PropertyMetadata(5.0, OnEraserSizeChanged));

        public static double GetEraserSize(DependencyObject obj)
        {
            return (double)obj.GetValue(EraserSizeProperty);
        }

        public static void SetEraserSize(DependencyObject obj, double value)
        {
            obj.SetValue(EraserSizeProperty, value);
        }

        private static void OnEraserSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InkCanvas inkCanvas && (double)e.NewValue > 0)
            {
                double newSize = (double)e.NewValue;
                inkCanvas.EraserShape = new EllipseStylusShape(newSize, newSize);
            }
        }

        public static readonly DependencyProperty MouseStartCommandProperty =
            DependencyProperty.RegisterAttached(
                "MouseStartCommand",
                typeof(ICommand),
                typeof(InkCanvasBinder),
                new PropertyMetadata(null, OnMouseCommandChanged));

        public static ICommand GetMouseStartCommand(DependencyObject obj) => (ICommand)obj.GetValue(MouseStartCommandProperty);
        public static void SetMouseStartCommand(DependencyObject obj, ICommand value) => obj.SetValue(MouseStartCommandProperty, value);

        public static readonly DependencyProperty MouseMoveCommandProperty =
            DependencyProperty.RegisterAttached(
                "MouseMoveCommand",
                typeof(ICommand),
                typeof(InkCanvasBinder),
                new PropertyMetadata(null, OnMouseCommandChanged));

        public static ICommand GetMouseMoveCommand(DependencyObject obj) => (ICommand)obj.GetValue(MouseMoveCommandProperty);
        public static void SetMouseMoveCommand(DependencyObject obj, ICommand value) => obj.SetValue(MouseMoveCommandProperty, value);

        public static readonly DependencyProperty MouseEndCommandProperty =
            DependencyProperty.RegisterAttached(
                "MouseEndCommand",
                typeof(ICommand),
                typeof(InkCanvasBinder),
                new PropertyMetadata(null, OnMouseCommandChanged));

        public static ICommand GetMouseEndCommand(DependencyObject obj) => (ICommand)obj.GetValue(MouseEndCommandProperty);
        public static void SetMouseEndCommand(DependencyObject obj, ICommand value) => obj.SetValue(MouseEndCommandProperty, value);

        private static void OnMouseCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InkCanvas inkCanvas)
            {
                inkCanvas.PreviewMouseLeftButtonDown -= InkCanvas_MouseDown;
                inkCanvas.PreviewMouseMove -= InkCanvas_MouseMove;
                inkCanvas.PreviewMouseLeftButtonUp -= InkCanvas_MouseUp;

                inkCanvas.PreviewMouseLeftButtonDown += InkCanvas_MouseDown;
                inkCanvas.PreviewMouseMove += InkCanvas_MouseMove;
                inkCanvas.PreviewMouseLeftButtonUp += InkCanvas_MouseUp;
            }
        }

        private static void InkCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is InkCanvas inkCanvas)
            {
                var command = GetMouseStartCommand(inkCanvas);
                if (command != null && command.CanExecute(null))
                {
                    command.Execute(e.GetPosition(inkCanvas));
                }
            }
        }

        private static void InkCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is InkCanvas inkCanvas)
            {
                var command = GetMouseMoveCommand(inkCanvas);
                if (command != null && e.LeftButton == MouseButtonState.Pressed && command.CanExecute(null))
                {
                    command.Execute(e.GetPosition(inkCanvas));
                }
            }
        }

        private static void InkCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is InkCanvas inkCanvas)
            {
                var command = GetMouseEndCommand(inkCanvas);
                if (command != null && command.CanExecute(null))
                {
                    command.Execute(null);
                }
            }
        }
    }
}
