using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace BH_CarLog.Controls
{
    /// <summary>
    /// 연·월만 고르는 입력 (카드 유효기간 등). 모양은 DatePicker 와 같고, 팝업 캘린더를 월 선택 화면(Year 모드)으로만 쓴다.
    /// 월 타일을 누르면 그 달의 1일이 <see cref="SelectedMonth"/> 에 들어가고 팝업이 닫힌다. 표시는 <see cref="Format"/> (기본 yyyy-MM).
    /// 스타일/템플릿은 Themes/Controls.xaml 의 <c>controls:MonthPicker</c> 에 있다.
    /// </summary>
    [TemplatePart(Name = PartButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartPopup, Type = typeof(Popup))]
    [TemplatePart(Name = PartCalendar, Type = typeof(Calendar))]
    public class MonthPicker : Control
    {
        public const string PartButton = "PART_Button";
        public const string PartPopup = "PART_Popup";
        public const string PartCalendar = "PART_Calendar";

        private ButtonBase? _button;
        private Popup? _popup;
        private Calendar? _calendar;

        static MonthPicker()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(MonthPicker), new FrameworkPropertyMetadata(typeof(MonthPicker)));
        }

        #region 속성
        /// <summary>선택한 월. 항상 그 달의 1일로 보정된다.</summary>
        public static readonly DependencyProperty SelectedMonthProperty =
            DependencyProperty.Register(nameof(SelectedMonth), typeof(DateTime?), typeof(MonthPicker),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedMonthChanged, CoerceSelectedMonth));

        public DateTime? SelectedMonth
        {
            get => (DateTime?)GetValue(SelectedMonthProperty);
            set => SetValue(SelectedMonthProperty, value);
        }

        public static readonly DependencyProperty IsDropDownOpenProperty =
            DependencyProperty.Register(nameof(IsDropDownOpen), typeof(bool), typeof(MonthPicker),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsDropDownOpenChanged));

        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        /// <summary>표시 형식 (기본 yyyy-MM)</summary>
        public static readonly DependencyProperty FormatProperty =
            DependencyProperty.Register(nameof(Format), typeof(string), typeof(MonthPicker),
                new PropertyMetadata("yyyy-MM", (d, _) => ((MonthPicker)d).UpdateText()));

        public string Format
        {
            get => (string)GetValue(FormatProperty);
            set => SetValue(FormatProperty, value);
        }

        /// <summary>비어 있을 때 안내 문구</summary>
        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.Register(nameof(Watermark), typeof(string), typeof(MonthPicker), new PropertyMetadata("년-월 선택"));

        public string Watermark
        {
            get => (string)GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        /// <summary>화면 표시용 문자열 (SelectedMonth 를 Format 으로)</summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(nameof(Text), typeof(string), typeof(MonthPicker), new PropertyMetadata(""));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            private set => SetValue(TextProperty, value);
        }
        #endregion

        public override void OnApplyTemplate()
        {
            if (_button != null) _button.Click -= OnButtonClick;
            if (_popup != null) _popup.Opened -= OnPopupOpened;
            if (_calendar != null) _calendar.DisplayModeChanged -= OnCalendarDisplayModeChanged;

            base.OnApplyTemplate();

            _button = GetTemplateChild(PartButton) as ButtonBase;
            _popup = GetTemplateChild(PartPopup) as Popup;
            _calendar = GetTemplateChild(PartCalendar) as Calendar;

            if (_button != null) _button.Click += OnButtonClick;
            if (_popup != null)
            {
                _popup.Opened += OnPopupOpened;
                _popup.IsOpen = IsDropDownOpen;
            }
            if (_calendar != null) _calendar.DisplayModeChanged += OnCalendarDisplayModeChanged;

            UpdateText();
            SyncCalendar();
        }

        /// <summary>오늘 / 지우기 버튼(CalendarBehavior)이 호출한다. 월을 정하고 팝업을 닫는다.</summary>
        public void SelectAndClose(DateTime? month)
        {
            SelectedMonth = month;
            IsDropDownOpen = false;
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            // 아이콘 버튼은 자기가 처리하므로 여기는 값 영역 클릭만 온다.
            Focus();
            IsDropDownOpen = true;
            e.Handled = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape && IsDropDownOpen)
            {
                IsDropDownOpen = false;
                e.Handled = true;
            }
            else if ((e.Key == Key.Down || e.Key == Key.Space || e.Key == Key.Enter) && IsDropDownOpen == false)
            {
                IsDropDownOpen = true;
                e.Handled = true;
            }
            else if (e.Key == Key.Delete || e.Key == Key.Back)
            {
                SelectedMonth = null;
                e.Handled = true;
            }
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            Focus();
            IsDropDownOpen = !IsDropDownOpen;
        }

        private void OnPopupOpened(object? sender, EventArgs e)
        {
            SyncCalendar();
            if (_calendar != null)
                _calendar.DisplayMode = CalendarMode.Year; // 항상 월 타일부터
        }

        /// <summary>
        /// 월 타일을 누르면 Calendar 가 DisplayDate 를 그 달로 바꾼 뒤 Month 모드로 들어온다. 그 순간을 "월 선택" 으로 본다.
        /// (Decade → Year 전환은 연도 선택이므로 무시)
        /// </summary>
        private void OnCalendarDisplayModeChanged(object? sender, CalendarModeChangedEventArgs e)
        {
            if (_calendar == null || e.NewMode != CalendarMode.Month)
                return;

            var picked = new DateTime(_calendar.DisplayDate.Year, _calendar.DisplayDate.Month, 1);
            // 이벤트 안에서 DisplayMode 를 되돌리면 재진입하므로 한 틱 뒤에 처리한다.
            Dispatcher.BeginInvoke(() =>
            {
                SelectedMonth = picked;
                IsDropDownOpen = false;
                _calendar.DisplayMode = CalendarMode.Year;
            });
        }

        private void SyncCalendar()
        {
            if (_calendar == null)
                return;
            // SelectedDate 를 1일로 두면 월 타일에 HasSelectedDays 표시가 붙는다.
            _calendar.SelectedDate = SelectedMonth;
            _calendar.DisplayDate = SelectedMonth ?? DateTime.Today;
        }

        private void UpdateText()
        {
            Text = SelectedMonth.HasValue ? SelectedMonth.Value.ToString(Format) : "";
        }

        private static object? CoerceSelectedMonth(DependencyObject d, object? value)
            => value is DateTime date ? new DateTime(date.Year, date.Month, 1) : null;

        private static void OnSelectedMonthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = (MonthPicker)d;
            picker.UpdateText();
            picker.SyncCalendar();
        }

        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var picker = (MonthPicker)d;
            if (picker._popup != null)
                picker._popup.IsOpen = (bool)e.NewValue;
        }
    }
}
