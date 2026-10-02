using System.Windows.Controls;
using System.Windows.Input;
using BH_CarLog.Helpers;
using BH_CarLog.ViewModels.Common;

namespace BH_CarLog.Views.Common
{
    public partial class ShopPickerControl : UserControl
    {
        public ShopPickerControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Enter: 가게를 찾고, 확정되면 좌표상 다음 입력란으로 넘어간다. (기존 EnterKeyFocuseHelper.FocuseNextControl)
        /// 선택 창이 떠서 고른 경우도 확정이므로 똑같이 넘어간다. 취소했으면 그대로 남아 다시 칠 수 있게 한다.
        /// </summary>
        private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || DataContext is not ShopPickerViewModel vm)
                return;

            e.Handled = true;
            if (vm.IsReadOnly)
                return;

            vm.ResolveCommand.Execute(null);
            if (vm.Selected != null)
                EnterNavigation.MoveToNext(InputBox);
        }
    }
}
