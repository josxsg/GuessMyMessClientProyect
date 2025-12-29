using System.Windows;
using System.Windows.Controls;

namespace GuessMyMessClient.ViewModel.Support
{
    public static class TextBoxHelper
    {
        // Propiedad para activar/desactivar el contador desde el XAML
        public static readonly DependencyProperty ShowCounterProperty =
            DependencyProperty.RegisterAttached("ShowCounter", typeof(bool), typeof(TextBoxHelper), new PropertyMetadata(false, OnShowCounterChanged));

        public static bool GetShowCounter(DependencyObject obj) => (bool)obj.GetValue(ShowCounterProperty);
        public static void SetShowCounter(DependencyObject obj, bool value) => obj.SetValue(ShowCounterProperty, value);

        // Propiedad que guarda la longitud actual del texto (solo lectura para el XAML)
        private static readonly DependencyPropertyKey CurrentLengthPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly("CurrentLength", typeof(int), typeof(TextBoxHelper), new PropertyMetadata(0));

        public static readonly DependencyProperty CurrentLengthProperty = CurrentLengthPropertyKey.DependencyProperty;

        public static int GetCurrentLength(DependencyObject obj) => (int)obj.GetValue(CurrentLengthProperty);

        // Cuando se activa ShowCounter, nos "suscribimos" a los cambios de texto
        private static void OnShowCounterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                textBox.TextChanged -= TextBox_TextChanged;
                if ((bool)e.NewValue)
                {
                    textBox.TextChanged += TextBox_TextChanged;
                    UpdateLength(textBox);
                }
            }
        }

        private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateLength(sender as TextBox);
        }

        private static void UpdateLength(TextBox textBox)
        {
            if (textBox != null)
            {
                textBox.SetValue(CurrentLengthPropertyKey, textBox.Text.Length);
            }
        }
    }
}