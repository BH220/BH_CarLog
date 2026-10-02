using System.Windows;
using System.Windows.Controls;

namespace BH_CarLog.Helpers
{
    /// <summary>
    /// PasswordBox.Password 는 DependencyProperty 가 아니어서 바인딩이 불가능하다.
    /// BindPassword="True" 로 이벤트를 연결하고 BoundPassword 로 ViewModel 과 양방향 바인딩한다.
    /// </summary>
    public static class PasswordBoxHelper
    {
        public static readonly DependencyProperty BindPasswordProperty =
            DependencyProperty.RegisterAttached("BindPassword", typeof(bool), typeof(PasswordBoxHelper),
                new PropertyMetadata(false, OnBindPasswordChanged));

        public static readonly DependencyProperty BoundPasswordProperty =
            DependencyProperty.RegisterAttached("BoundPassword", typeof(string), typeof(PasswordBoxHelper),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnBoundPasswordChanged));

        private static readonly DependencyProperty IsUpdatingProperty =
            DependencyProperty.RegisterAttached("IsUpdating", typeof(bool), typeof(PasswordBoxHelper));

        public static bool GetBindPassword(DependencyObject obj) => (bool)obj.GetValue(BindPasswordProperty);
        public static void SetBindPassword(DependencyObject obj, bool value) => obj.SetValue(BindPasswordProperty, value);

        public static string? GetBoundPassword(DependencyObject obj) => (string?)obj.GetValue(BoundPasswordProperty);
        public static void SetBoundPassword(DependencyObject obj, string? value) => obj.SetValue(BoundPasswordProperty, value);

        private static void OnBindPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PasswordBox box)
                return;

            box.PasswordChanged -= OnPasswordChanged;
            if ((bool)e.NewValue)
                box.PasswordChanged += OnPasswordChanged;
        }

        private static void OnBoundPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not PasswordBox box)
                return;
            if ((bool)box.GetValue(IsUpdatingProperty))
                return;

            // ViewModel 쪽 값이 바뀐 경우 (초기화, 로그아웃 후 비우기 등) PasswordBox 에 반영
            string newValue = e.NewValue as string ?? string.Empty;
            if (box.Password != newValue)
            {
                box.PasswordChanged -= OnPasswordChanged;
                box.Password = newValue;
                if (GetBindPassword(box))
                    box.PasswordChanged += OnPasswordChanged;
            }
        }

        private static void OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            var box = (PasswordBox)sender;
            box.SetValue(IsUpdatingProperty, true);
            SetBoundPassword(box, box.Password);
            box.SetValue(IsUpdatingProperty, false);
        }
    }
}
