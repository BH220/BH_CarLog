using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using BH_CarLog.Controls;

namespace BH_CarLog.Helpers
{
    /// <summary>
    /// 캘린더 템플릿(Controls.xaml 의 CalendarItem) 안에 놓인 버튼의 동작. <c>helpers:CalendarBehavior.Action</c> 로 지정한다.
    /// <list type="bullet">
    /// <item><b>YearMode</b>: 헤더 연도 칩 → 연도 선택 화면(Decade)</item>
    /// <item><b>MonthMode</b>: 헤더 월 칩 → 월 선택 화면(Year)</item>
    /// <item><b>Today</b>: 오늘 날짜(월 선택기는 이번 달) 선택 후 팝업 닫기</item>
    /// <item><b>Clear</b>: 선택 해제 후 팝업 닫기</item>
    /// </list>
    /// 캘린더가 DatePicker / MonthPicker 팝업 안에 있으면 그 컨트롤의 값과 열림 상태를 함께 처리한다.
    /// </summary>
    public static class CalendarBehavior
    {
        public const string YearMode = "YearMode";
        public const string MonthMode = "MonthMode";
        public const string Today = "Today";
        public const string Clear = "Clear";

        public static readonly DependencyProperty ActionProperty =
            DependencyProperty.RegisterAttached("Action", typeof(string), typeof(CalendarBehavior),
                new PropertyMetadata(null, OnActionChanged));

        public static string? GetAction(DependencyObject obj) => (string?)obj.GetValue(ActionProperty);
        public static void SetAction(DependencyObject obj, string? value) => obj.SetValue(ActionProperty, value);

        private static void OnActionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Button button)
                return;

            button.Click -= OnClick;
            if (string.IsNullOrEmpty(e.NewValue as string) == false)
                button.Click += OnClick;
        }

        private static void OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is not Button button)
                return;
            var calendar = FindAncestor<Calendar>(button);
            if (calendar == null)
                return;

            switch (GetAction(button))
            {
                case YearMode:
                    calendar.DisplayMode = CalendarMode.Decade;
                    break;
                case MonthMode:
                    calendar.DisplayMode = CalendarMode.Year;
                    break;
                case Today:
                    SelectDate(calendar, DateTime.Today);
                    break;
                case Clear:
                    SelectDate(calendar, null);
                    break;
            }
            e.Handled = true;
        }

        private static void SelectDate(Calendar calendar, DateTime? date)
        {
            var owner = FindOwner(calendar);
            switch (owner)
            {
                case MonthPicker monthPicker:
                    // 월 선택기: 해당 월의 1일. 팝업은 월 타일 화면을 유지한다.
                    monthPicker.SelectAndClose(date);
                    return;

                case DatePicker datePicker:
                    datePicker.SelectedDate = date;
                    if (date.HasValue)
                        datePicker.DisplayDate = date.Value;
                    datePicker.IsDropDownOpen = false;
                    break;

                default:
                    calendar.SelectedDate = date;
                    if (date.HasValue)
                        calendar.DisplayDate = date.Value;
                    break;
            }
            calendar.DisplayMode = CalendarMode.Month;
        }

        /// <summary>
        /// 캘린더를 품고 있는 컨트롤(DatePicker / MonthPicker). 둘 다 캘린더를 템플릿의 Popup 자식으로 두므로
        /// 논리 트리를 따라 올라가 Popup 의 TemplatedParent 를 본다.
        /// </summary>
        private static FrameworkElement? FindOwner(Calendar calendar)
        {
            DependencyObject? node = calendar;
            while (node != null)
            {
                if (node is DatePicker or MonthPicker)
                    return (FrameworkElement)node;
                if (node is Popup popup && popup.TemplatedParent is FrameworkElement owner && owner is DatePicker or MonthPicker)
                    return owner;
                node = LogicalTreeHelper.GetParent(node) ?? VisualTreeHelper.GetParent(node);
            }
            return null;
        }

        private static T? FindAncestor<T>(DependencyObject start) where T : DependencyObject
        {
            DependencyObject? node = VisualTreeHelper.GetParent(start);
            while (node != null)
            {
                if (node is T found)
                    return found;
                node = VisualTreeHelper.GetParent(node);
            }
            return null;
        }
    }
}
