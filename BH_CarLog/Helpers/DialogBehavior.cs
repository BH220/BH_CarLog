using System.Windows;

namespace BH_CarLog.Helpers
{
    /// <summary>
    /// ViewModel 의 DialogResult(bool?) 를 Window 에 바인딩하여 코드비하인드 없이 창을 닫는다.
    /// </summary>
    public static class DialogBehavior
    {
        public static readonly DependencyProperty DialogResultProperty =
            DependencyProperty.RegisterAttached("DialogResult", typeof(bool?), typeof(DialogBehavior),
                new PropertyMetadata(null, OnDialogResultChanged));

        public static bool? GetDialogResult(DependencyObject obj) => (bool?)obj.GetValue(DialogResultProperty);
        public static void SetDialogResult(DependencyObject obj, bool? value) => obj.SetValue(DialogResultProperty, value);

        private static void OnDialogResultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Window window || e.NewValue == null)
                return;

            try
            {
                window.DialogResult = (bool?)e.NewValue;
            }
            catch (InvalidOperationException)
            {
                // ShowDialog 로 열리지 않은 창은 DialogResult 설정이 불가능하므로 그냥 닫는다.
                window.Close();
            }
        }
    }
}
