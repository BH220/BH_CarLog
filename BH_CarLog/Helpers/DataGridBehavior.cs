using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace BH_CarLog.Helpers
{
    /// <summary>
    /// DataGrid 첨부 동작.
    /// - RowDoubleClickCommand : 행 더블클릭 시 명령 실행 (헤더/빈 영역 더블클릭은 무시)
    /// - SelectRowOnRightClick : 우클릭한 행을 먼저 선택 (컨텍스트 메뉴의 수정/삭제가 우클릭한 행을 대상으로 하도록)
    /// - ScrollSelectionIntoView : 선택된 행이 보이도록 스크롤 (목록이 날짜 오름차순이라 최신 자료가 맨 아래에 있다)
    /// </summary>
    public static class DataGridBehavior
    {
        #region RowDoubleClickCommand
        public static readonly DependencyProperty RowDoubleClickCommandProperty =
            DependencyProperty.RegisterAttached("RowDoubleClickCommand", typeof(ICommand), typeof(DataGridBehavior),
                new PropertyMetadata(null, OnRowDoubleClickCommandChanged));

        public static ICommand? GetRowDoubleClickCommand(DependencyObject obj) => (ICommand?)obj.GetValue(RowDoubleClickCommandProperty);
        public static void SetRowDoubleClickCommand(DependencyObject obj, ICommand? value) => obj.SetValue(RowDoubleClickCommandProperty, value);

        private static void OnRowDoubleClickCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGrid grid)
                return;

            grid.MouseDoubleClick -= OnMouseDoubleClick;
            if (e.NewValue != null)
                grid.MouseDoubleClick += OnMouseDoubleClick;
        }

        private static void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid grid || e.OriginalSource is not DependencyObject source)
                return;

            if (FindRow(grid, source) is not DataGridRow row)
                return;

            var command = GetRowDoubleClickCommand(grid);
            if (command?.CanExecute(row.Item) == true)
            {
                command.Execute(row.Item);
                e.Handled = true;
            }
        }
        #endregion

        #region SelectRowOnRightClick
        public static readonly DependencyProperty SelectRowOnRightClickProperty =
            DependencyProperty.RegisterAttached("SelectRowOnRightClick", typeof(bool), typeof(DataGridBehavior),
                new PropertyMetadata(false, OnSelectRowOnRightClickChanged));

        public static bool GetSelectRowOnRightClick(DependencyObject obj) => (bool)obj.GetValue(SelectRowOnRightClickProperty);
        public static void SetSelectRowOnRightClick(DependencyObject obj, bool value) => obj.SetValue(SelectRowOnRightClickProperty, value);

        private static void OnSelectRowOnRightClickChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGrid grid)
                return;

            grid.PreviewMouseRightButtonDown -= OnPreviewMouseRightButtonDown;
            if ((bool)e.NewValue)
                grid.PreviewMouseRightButtonDown += OnPreviewMouseRightButtonDown;
        }

        private static void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid grid || e.OriginalSource is not DependencyObject source)
                return;

            if (FindRow(grid, source) is not DataGridRow row)
                return;

            // 우클릭한 행을 선택 상태로 만든다. (컨텍스트 메뉴는 SelectedItem 을 대상으로 동작)
            if (row.IsSelected == false)
            {
                grid.SelectedItem = row.Item;
                row.IsSelected = true;
            }
            row.Focus();
        }
        #endregion

        #region ScrollSelectionIntoView
        /// <summary>
        /// 선택된 행이 보이도록 스크롤한다.
        /// 목록은 날짜 오름차순이라 최신 자료가 맨 아래에 있고, 화면을 처음 열면 ViewModel 이 마지막 행을 고른다.
        /// 이 값을 켜 두면 그 행까지 자동으로 내려가서 최신 자료가 바로 보인다.
        /// </summary>
        public static readonly DependencyProperty ScrollSelectionIntoViewProperty =
            DependencyProperty.RegisterAttached("ScrollSelectionIntoView", typeof(bool), typeof(DataGridBehavior),
                new PropertyMetadata(false, OnScrollSelectionIntoViewChanged));

        public static bool GetScrollSelectionIntoView(DependencyObject obj) => (bool)obj.GetValue(ScrollSelectionIntoViewProperty);
        public static void SetScrollSelectionIntoView(DependencyObject obj, bool value) => obj.SetValue(ScrollSelectionIntoViewProperty, value);

        private static void OnScrollSelectionIntoViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGrid grid)
                return;

            grid.SelectionChanged -= OnSelectionChangedScroll;
            grid.Loaded -= OnLoadedScroll;

            if ((bool)e.NewValue == false)
                return;

            grid.SelectionChanged += OnSelectionChangedScroll;
            grid.Loaded += OnLoadedScroll;
        }

        private static void OnSelectionChangedScroll(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid grid)
                ScrollToSelection(grid);
        }

        private static void OnLoadedScroll(object sender, RoutedEventArgs e)
        {
            if (sender is DataGrid grid)
                ScrollToSelection(grid);
        }

        /// <summary>행 컨테이너가 아직 없을 수 있어 레이아웃이 끝난 뒤로 미뤄 호출한다.</summary>
        private static void ScrollToSelection(DataGrid grid)
        {
            if (grid.SelectedItem == null)
                return;

            grid.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                object? selected = grid.SelectedItem;
                if (selected == null)
                    return;
                try
                {
                    grid.ScrollIntoView(selected);
                }
                catch
                {
                    // 목록이 그 사이 비었거나 바뀐 경우. 스크롤은 화면 편의일 뿐이라 무시한다.
                }
            }));
        }
        #endregion

        private static DataGridRow? FindRow(DataGrid grid, DependencyObject source)
            => ItemsControl.ContainerFromElement(grid, source) as DataGridRow;
    }
}
