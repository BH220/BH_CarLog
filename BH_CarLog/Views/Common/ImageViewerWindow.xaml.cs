using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BH_CarLog.ViewModels.Common;

namespace BH_CarLog.Views.Common
{
    /// <summary>
    /// 이미지 보기 팝업. 확대/축소/회전 로직은 <see cref="ImageViewerViewModel"/> 에 있고,
    /// 여기서는 뷰만 알 수 있는 표시 영역 크기와 마우스 휠을 ViewModel 에 전달한다.
    /// </summary>
    public partial class ImageViewerWindow : Window
    {
        public ImageViewerWindow()
        {
            InitializeComponent();
        }

        private ImageViewerViewModel? ViewModel => DataContext as ImageViewerViewModel;

        private void Viewer_SizeChanged(object sender, SizeChangedEventArgs e) => UpdateViewport();

        private void Viewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // 스크롤바가 생기거나 사라져 표시 영역이 바뀐 경우 (창 맞춤 재계산)
            if (e.ViewportWidthChange != 0 || e.ViewportHeightChange != 0)
                UpdateViewport();
        }

        private void UpdateViewport()
        {
            if (ViewModel == null || Viewer.ViewportWidth <= 0 || Viewer.ViewportHeight <= 0)
                return;
            // 반올림으로 스크롤바가 깜빡이지 않도록 약간 여유를 둔다.
            ViewModel.SetViewport(Viewer.ViewportWidth - 2, Viewer.ViewportHeight - 2);
        }

        /// <summary>마우스 휠: 위로 확대, 아래로 축소 (스크롤 대신 배율 조정)</summary>
        private void Viewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ViewModel == null)
                return;
            if (e.Delta > 0)
                ViewModel.ZoomInCommand.Execute(null);
            else if (e.Delta < 0)
                ViewModel.ZoomOutCommand.Execute(null);
            e.Handled = true;
        }
    }
}
