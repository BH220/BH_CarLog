using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace BH_CarLog.Views.Common
{
    public partial class ShopSelectWindow : Window
    {
        public ShopSelectWindow()
        {
            InitializeComponent();
        }

        /// <summary>열리자마자 그리드의 골라진 행에 포커스를 둔다. 검색어 상자가 아니다. (기존 BhSearchForm 과 동일)</summary>
        private void Window_Loaded(object sender, RoutedEventArgs e) => FocusSelectedRow();

        /// <summary>검색어 상자에서 ↓ 를 누르면 그리드로 내려간다. (기존 bhSearchBox1_KeyDown)</summary>
        private void KeywordBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Down)
                return;
            e.Handled = true;
            FocusSelectedRow();
        }

        /// <summary>
        /// 골라진 행의 첫 셀에 키보드 포커스를 준다. 행 컨테이너는 레이아웃이 끝나야 생기므로 한 박자 뒤에 한다.
        /// 행이 없으면 그리드 자체에 포커스를 두어 Esc·Enter 바인딩이 살아 있게 한다.
        /// </summary>
        private void FocusSelectedRow()
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
            {
                if (ResultGrid.Items.Count == 0)
                {
                    ResultGrid.Focus();
                    return;
                }
                if (ResultGrid.SelectedIndex < 0)
                    ResultGrid.SelectedIndex = 0;

                object item = ResultGrid.SelectedItem;
                ResultGrid.ScrollIntoView(item);
                ResultGrid.UpdateLayout();
                if (ResultGrid.Columns.Count > 0)
                    ResultGrid.CurrentCell = new DataGridCellInfo(item, ResultGrid.Columns[0]);

                bool focused = ResultGrid.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow row
                               && row.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
                if (focused == false)
                    ResultGrid.Focus();
            }));
        }
    }
}
