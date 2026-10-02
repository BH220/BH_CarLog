using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace BH_CarLog.Helpers
{
    /// <summary>
    /// 코드 입력 칸(TextBox) 첨부 동작.
    /// <list type="bullet">
    /// <item><b>IsNumericOnly</b>: 숫자(0~9)만 입력. 키 입력·붙여넣기·드롭·한글 IME 를 모두 차단한다.</item>
    /// <item><b>AutoAdvance</b>: MaxLength 만큼 다 채우면 같은 <b>IsAdvanceScope</b> 안에서 TabIndex 가 바로 다음인 칸으로 포커스를 옮긴다.
    /// 칸의 TabIndex 에 코드 번호(No)를 넣어 두면 화면 배치(가로/세로)와 무관하게 "다음 번호" 칸으로 이동한다.</item>
    /// <item><b>IsAdvanceScope</b>: 다음 칸을 찾는 범위(격자 루트)에 표시한다.</item>
    /// </list>
    /// </summary>
    public static class NumericTextBoxBehavior
    {
        #region IsNumericOnly
        public static readonly DependencyProperty IsNumericOnlyProperty =
            DependencyProperty.RegisterAttached("IsNumericOnly", typeof(bool), typeof(NumericTextBoxBehavior),
                new PropertyMetadata(false, OnIsNumericOnlyChanged));

        public static bool GetIsNumericOnly(DependencyObject obj) => (bool)obj.GetValue(IsNumericOnlyProperty);
        public static void SetIsNumericOnly(DependencyObject obj, bool value) => obj.SetValue(IsNumericOnlyProperty, value);

        private static void OnIsNumericOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox textBox)
                return;

            textBox.PreviewTextInput -= OnPreviewTextInput;
            textBox.PreviewKeyDown -= OnPreviewKeyDown;
            DataObject.RemovePastingHandler(textBox, OnPasting);

            if ((bool)e.NewValue)
            {
                textBox.PreviewTextInput += OnPreviewTextInput;
                textBox.PreviewKeyDown += OnPreviewKeyDown;
                DataObject.AddPastingHandler(textBox, OnPasting);
                InputMethod.SetIsInputMethodEnabled(textBox, false); // 한글 등 IME 조합 입력 차단
                textBox.AllowDrop = false;                            // 드롭으로 문자 들어오는 것 차단
            }
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // 숫자가 아닌 문자가 하나라도 있으면 입력 자체를 막는다.
            e.Handled = IsAllDigits(e.Text) == false;
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Space 는 TextInput 이 아닌 키 처리로 들어오므로 따로 막는다. IME 처리 키도 차단.
            if (e.Key == Key.Space || e.Key == Key.ImeProcessed)
                e.Handled = true;
        }

        private static void OnPasting(object sender, DataObjectPastingEventArgs e)
        {
            string text = e.DataObject.GetDataPresent(DataFormats.Text) ? (string)e.DataObject.GetData(DataFormats.Text) : "";
            if (IsAllDigits(text) == false)
                e.CancelCommand();
        }

        private static bool IsAllDigits(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return false;
            foreach (char c in text)
            {
                if (c < '0' || c > '9')
                    return false;
            }
            return true;
        }
        #endregion

        #region AutoAdvance / IsAdvanceScope
        public static readonly DependencyProperty AutoAdvanceProperty =
            DependencyProperty.RegisterAttached("AutoAdvance", typeof(bool), typeof(NumericTextBoxBehavior),
                new PropertyMetadata(false, OnAutoAdvanceChanged));

        public static bool GetAutoAdvance(DependencyObject obj) => (bool)obj.GetValue(AutoAdvanceProperty);
        public static void SetAutoAdvance(DependencyObject obj, bool value) => obj.SetValue(AutoAdvanceProperty, value);

        public static readonly DependencyProperty IsAdvanceScopeProperty =
            DependencyProperty.RegisterAttached("IsAdvanceScope", typeof(bool), typeof(NumericTextBoxBehavior),
                new PropertyMetadata(false));

        public static bool GetIsAdvanceScope(DependencyObject obj) => (bool)obj.GetValue(IsAdvanceScopeProperty);
        public static void SetIsAdvanceScope(DependencyObject obj, bool value) => obj.SetValue(IsAdvanceScopeProperty, value);

        private static void OnAutoAdvanceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox textBox)
                return;

            textBox.TextChanged -= OnTextChanged;
            if ((bool)e.NewValue)
                textBox.TextChanged += OnTextChanged;
        }

        private static void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            // 바인딩으로 값이 채워질 때(불러오기)가 아니라 사용자가 직접 입력하고 있을 때만 이동한다.
            if (textBox.IsKeyboardFocusWithin == false)
                return;
            if (textBox.MaxLength <= 0 || textBox.Text.Length < textBox.MaxLength)
                return;
            // 가운데를 고쳐 쓰는 중(커서가 끝이 아닐 때)이면 이동하지 않는다.
            if (textBox.CaretIndex < textBox.Text.Length)
                return;

            var next = FindNext(textBox);
            if (next == null)
                return;

            // TextChanged 처리 중 포커스를 바꾸면 재진입이 생기므로 한 틱 뒤에 옮긴다.
            textBox.Dispatcher.BeginInvoke(DispatcherPriority.Input, () =>
            {
                next.Focus();
                next.SelectAll();
            });
        }

        /// <summary>같은 범위(IsAdvanceScope) 안에서 TabIndex 가 현재보다 크면서 가장 작은 칸</summary>
        private static TextBox? FindNext(TextBox current)
        {
            DependencyObject scope = FindScope(current);
            TextBox? best = null;
            foreach (var candidate in Descendants(scope))
            {
                if (candidate is not TextBox textBox || ReferenceEquals(textBox, current))
                    continue;
                if (GetAutoAdvance(textBox) == false || textBox.IsVisible == false || textBox.IsEnabled == false)
                    continue;
                if (textBox.TabIndex <= current.TabIndex)
                    continue;
                if (best == null || textBox.TabIndex < best.TabIndex)
                    best = textBox;
            }
            return best;
        }

        private static DependencyObject FindScope(DependencyObject start)
        {
            DependencyObject? node = start;
            DependencyObject last = start;
            while (node != null)
            {
                if (GetIsAdvanceScope(node))
                    return node;
                last = node;
                node = VisualTreeHelper.GetParent(node);
            }
            return last; // 범위 표시가 없으면 시각 트리 루트(창) 전체
        }

        private static IEnumerable<DependencyObject> Descendants(DependencyObject root)
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                yield return child;
                foreach (var grandChild in Descendants(child))
                    yield return grandChild;
            }
        }
        #endregion
    }
}
