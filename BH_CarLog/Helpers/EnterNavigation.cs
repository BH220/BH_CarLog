using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace BH_CarLog.Helpers
{
    /// <summary>
    /// Enter 로 다음 입력란으로 넘어가기 (기존 EnterKeyFocuseHelper.FocuseNextControl).
    /// Tab 과 달리 인덱스가 아니라 화면 좌표로 정한다.
    ///   1. 지금 컨트롤과 같은 줄(세로 범위가 겹치는 것)에서 오른쪽에 있는 것 중 가장 가까운 것
    ///   2. 없으면 아랫줄 중 가장 위에 있는 줄의 맨 왼쪽 것
    /// 대상은 입력 컨트롤(TextBox·PasswordBox·ComboBox·DatePicker·CheckBox)뿐이고, 비활성·숨김·읽기 전용은 건너뛴다.
    /// 여러 줄 TextBox 안에서는 Enter 가 줄바꿈이므로 넘기지 않고, 열린 콤보/달력에서는 선택이 우선이다.
    /// 창(Window) 에 IsEnabled="True" 를 붙인다. 다음 컨트롤이 없으면 아무 일도 하지 않고 Enter 를 다른 처리(KeyBinding 등)에 넘긴다.
    /// </summary>
    public static class EnterNavigation
    {
        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(EnterNavigation),
                new PropertyMetadata(false, OnIsEnabledChanged));

        public static bool GetIsEnabled(DependencyObject obj) => (bool)obj.GetValue(IsEnabledProperty);
        public static void SetIsEnabled(DependencyObject obj, bool value) => obj.SetValue(IsEnabledProperty, value);

        /// <summary>
        /// true 면 이 컨트롤의 Enter 는 여기서 건드리지 않는다.
        /// Enter 를 직접 처리한 뒤 <see cref="MoveToNext"/> 를 부르는 컨트롤(가게 입력란)이 쓴다.
        /// </summary>
        public static readonly DependencyProperty SkipProperty =
            DependencyProperty.RegisterAttached("Skip", typeof(bool), typeof(EnterNavigation), new PropertyMetadata(false));

        public static bool GetSkip(DependencyObject obj) => (bool)obj.GetValue(SkipProperty);
        public static void SetSkip(DependencyObject obj, bool value) => obj.SetValue(SkipProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element)
                return;
            element.PreviewKeyDown -= OnPreviewKeyDown;
            if ((bool)e.NewValue)
                element.PreviewKeyDown += OnPreviewKeyDown;
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || Keyboard.Modifiers != ModifierKeys.None)
                return;
            if (e.OriginalSource is not DependencyObject source)
                return;

            var current = FindInputControl(source);
            if (current == null || GetSkip(current))
                return;
            if (current is TextBox { AcceptsReturn: true })
                return;
            if (current is ComboBox { IsDropDownOpen: true } || current is DatePicker { IsDropDownOpen: true })
                return;

            if (MoveToNext(current))
                e.Handled = true;
        }

        /// <summary>
        /// 포커스가 있던 요소에서 입력 컨트롤 단위로 올라간다.
        /// DatePicker·ComboBox 안의 텍스트박스는 그 부모 컨트롤로 치고, DataGrid 안에서는 끼어들지 않는다.
        /// </summary>
        private static Control? FindInputControl(DependencyObject source)
        {
            Control? found = null;
            for (DependencyObject? d = source; d != null && d is not Window; d = GetParent(d))
            {
                if (d is DataGrid)
                    return null;
                if (d is DatePicker || d is ComboBox)
                    return (Control)d;
                if (found == null && (d is TextBox || d is PasswordBox || d is CheckBox))
                    found = (Control)d;
            }
            return found;
        }

        /// <summary>지금 컨트롤 기준으로 좌표상 다음 입력란에 포커스를 준다. 없으면 false.</summary>
        public static bool MoveToNext(FrameworkElement current)
        {
            FrameworkElement? root = Window.GetWindow(current);
            if (root == null)
                return false;

            Rect cur = BoundsIn(current, root);
            if (cur.IsEmpty)
                return false;

            var inputs = new List<(FrameworkElement Element, Rect Bounds)>();
            Collect(root, root, inputs);
            inputs.RemoveAll(x => ReferenceEquals(x.Element, current) || IsInside(x.Element, current));

            // 1. 같은 줄 오른쪽: 세로 중심이 지금 컨트롤의 세로 범위 안에 있고 왼쪽 끝이 더 오른쪽인 것 중 가장 가까운 것
            var right = inputs.Where(x => x.Bounds.Left > cur.Left + 1 && CenterY(x.Bounds) >= cur.Top && CenterY(x.Bounds) <= cur.Bottom)
                              .OrderBy(x => x.Bounds.Left)
                              .Select(x => x.Element)
                              .FirstOrDefault();
            if (right != null)
                return Focus(right);

            // 2. 아랫줄: 세로 중심이 지금 컨트롤보다 아래인 것 중, 가장 위에 있는 줄(첫 컨트롤의 세로 범위와 겹치는 것들)의 맨 왼쪽
            var below = inputs.Where(x => CenterY(x.Bounds) > cur.Bottom).ToList();
            if (below.Count == 0)
                return false;
            Rect firstRow = below.OrderBy(x => x.Bounds.Top).First().Bounds;
            var next = below.Where(x => CenterY(x.Bounds) >= firstRow.Top && CenterY(x.Bounds) <= firstRow.Bottom)
                            .OrderBy(x => x.Bounds.Left)
                            .First().Element;
            return Focus(next);
        }

        /// <summary>
        /// 시각 트리를 훑어 입력 컨트롤을 모은다. DatePicker·ComboBox 는 통째로 하나이고 안으로 들어가지 않는다.
        /// DataGrid 안은 폼 입력이 아니므로 건너뛴다.
        /// </summary>
        private static void Collect(DependencyObject node, FrameworkElement root, List<(FrameworkElement, Rect)> inputs)
        {
            if (node is DataGrid)
                return;

            if (node is DatePicker || node is ComboBox || node is TextBox || node is PasswordBox || node is CheckBox)
            {
                if (node is FrameworkElement input && IsEditable(input))
                {
                    Rect bounds = BoundsIn(input, root);
                    if (bounds.IsEmpty == false)
                        inputs.Add((input, bounds));
                }
                return;
            }

            int count = VisualTreeHelper.GetChildrenCount(node);
            for (int i = 0; i < count; i++)
                Collect(VisualTreeHelper.GetChild(node, i), root, inputs);
        }

        /// <summary>원본 ControlInfo.GetEditable: 보이고, 쓸 수 있고, 읽기 전용이 아닌 것. (읽기 전용 콤보는 IsHitTestVisible=false 로 표시된다)</summary>
        private static bool IsEditable(FrameworkElement element)
        {
            if (element.IsVisible == false || element.IsEnabled == false || element.IsHitTestVisible == false)
                return false;
            if (element.ActualWidth <= 0 || element.ActualHeight <= 0)
                return false;
            if (element is TextBox { IsReadOnly: true })
                return false;
            if (element is Control { Focusable: false } && element is not DatePicker)
                return false;
            return true;
        }

        private static Rect BoundsIn(FrameworkElement element, FrameworkElement root)
        {
            if (element.IsVisible == false || ReferenceEquals(element, root))
                return Rect.Empty;
            try
            {
                return element.TransformToAncestor(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
            }
            catch (InvalidOperationException)
            {
                // 아직 시각 트리에 붙지 않았거나 다른 창의 요소
                return Rect.Empty;
            }
        }

        private static bool Focus(FrameworkElement target)
        {
            switch (target)
            {
                case DatePicker picker:
                    // DatePicker 자체가 아니라 안의 텍스트박스가 입력을 받는다.
                    if (picker.Template?.FindName("PART_TextBox", picker) is DatePickerTextBox pickerBox)
                    {
                        bool ok = pickerBox.Focus();
                        pickerBox.SelectAll();
                        return ok;
                    }
                    return picker.Focus();
                case TextBox textBox:
                    {
                        bool ok = textBox.Focus();
                        textBox.SelectAll();
                        return ok;
                    }
                case PasswordBox passwordBox:
                    {
                        bool ok = passwordBox.Focus();
                        passwordBox.SelectAll();
                        return ok;
                    }
                default:
                    return target.Focus();
            }
        }

        private static double CenterY(Rect r) => r.Top + r.Height / 2;

        private static bool IsInside(DependencyObject element, DependencyObject container)
        {
            for (DependencyObject? d = GetParent(element); d != null; d = GetParent(d))
            {
                if (ReferenceEquals(d, container))
                    return true;
            }
            return false;
        }

        private static DependencyObject? GetParent(DependencyObject d)
            => d is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(d) : LogicalTreeHelper.GetParent(d);
    }
}
