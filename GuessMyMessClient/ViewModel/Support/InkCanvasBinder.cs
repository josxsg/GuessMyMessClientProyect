using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;

namespace GuessMyMessClient.ViewModel.Support
{
    public static class InkCanvasBinder
    {
        // ==========================================
        // 1. LÓGICA DEL BORRADOR (EraserSize)
        // ==========================================

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

        // ==========================================
        // 2. LÓGICA DE EVENTOS DEL MOUSE (Comandos)
        // ==========================================

        // --- Comienzo del Click (MouseDown) ---
        public static readonly DependencyProperty MouseStartCommandProperty =
            DependencyProperty.RegisterAttached(
                "MouseStartCommand",
                typeof(ICommand),
                typeof(InkCanvasBinder),
                new PropertyMetadata(null, OnMouseCommandChanged));

        public static ICommand GetMouseStartCommand(DependencyObject obj) => (ICommand)obj.GetValue(MouseStartCommandProperty);
        public static void SetMouseStartCommand(DependencyObject obj, ICommand value) => obj.SetValue(MouseStartCommandProperty, value);

        // --- Movimiento (MouseMove) ---
        public static readonly DependencyProperty MouseMoveCommandProperty =
            DependencyProperty.RegisterAttached(
                "MouseMoveCommand",
                typeof(ICommand),
                typeof(InkCanvasBinder),
                new PropertyMetadata(null, OnMouseCommandChanged));

        public static ICommand GetMouseMoveCommand(DependencyObject obj) => (ICommand)obj.GetValue(MouseMoveCommandProperty);
        public static void SetMouseMoveCommand(DependencyObject obj, ICommand value) => obj.SetValue(MouseMoveCommandProperty, value);

        // --- Soltar Click (MouseUp) ---
        public static readonly DependencyProperty MouseEndCommandProperty =
            DependencyProperty.RegisterAttached(
                "MouseEndCommand",
                typeof(ICommand),
                typeof(InkCanvasBinder),
                new PropertyMetadata(null, OnMouseCommandChanged));

        public static ICommand GetMouseEndCommand(DependencyObject obj) => (ICommand)obj.GetValue(MouseEndCommandProperty);
        public static void SetMouseEndCommand(DependencyObject obj, ICommand value) => obj.SetValue(MouseEndCommandProperty, value);


        // --- Manejador de Suscripción a Eventos ---
        // Este método se llama cuando asignas cualquier comando en el XAML
        private static void OnMouseCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is InkCanvas inkCanvas)
            {
                // Primero desuscribimos para evitar duplicados si el comando cambia dinámicamente
                inkCanvas.PreviewMouseLeftButtonDown -= InkCanvas_MouseDown;
                inkCanvas.PreviewMouseMove -= InkCanvas_MouseMove;
                inkCanvas.PreviewMouseLeftButtonUp -= InkCanvas_MouseUp;

                // Nos volvemos a suscribir a los eventos del control
                inkCanvas.PreviewMouseLeftButtonDown += InkCanvas_MouseDown;
                inkCanvas.PreviewMouseMove += InkCanvas_MouseMove;
                inkCanvas.PreviewMouseLeftButtonUp += InkCanvas_MouseUp;
            }
        }

        // --- Ejecución de los Comandos ---

        private static void InkCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is InkCanvas inkCanvas)
            {
                var command = GetMouseStartCommand(inkCanvas);
                // Ejecutamos el comando enviando la posición del mouse (Point)
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
                // Solo enviamos si el botón izquierdo está presionado (arrastrando)
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
                // Al soltar, no necesitamos posición, solo avisar que terminó
                if (command != null && command.CanExecute(null))
                {
                    command.Execute(null);
                }
            }
        }
    }
}