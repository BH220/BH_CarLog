using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace BH_CarLog.Helpers
{
    /// <summary>
    /// ViewModel 에서 bool 속성으로 컨트롤 포커스를 제어하기 위한 첨부 속성.
    /// IsFocused=true 가 되면 포커스를 주고, 포커스를 잃으면 false 로 되돌린다.
    /// </summary>
    public static class FocusExtension
    {
        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.RegisterAttached("IsFocused", typeof(bool), typeof(FocusExtension),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsFocusedChanged));

        public static bool GetIsFocused(DependencyObject obj) => (bool)obj.GetValue(IsFocusedProperty);
        public static void SetIsFocused(DependencyObject obj, bool value) => obj.SetValue(IsFocusedProperty, value);

        private static void OnIsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element)
                return;

            element.LostFocus -= OnLostFocus;
            element.LostFocus += OnLostFocus;

            if ((bool)e.NewValue)
            {
                element.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
                {
                    element.Focus();
                    Keyboard.Focus(element);
                    if (element is TextBox textBox)
                        textBox.SelectAll();
                    else if (element is PasswordBox passwordBox)
                        passwordBox.SelectAll();
                }));
            }
        }

        private static void OnLostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is DependencyObject d && GetIsFocused(d))
                SetIsFocused(d, false);
        }
    }
}
