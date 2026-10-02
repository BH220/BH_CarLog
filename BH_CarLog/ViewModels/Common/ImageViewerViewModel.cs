using BH_CarLog.Api.Model.Response;
using BH_CarLog.Helpers;
using BH_CarLog.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BH_CarLog.ViewModels.Common
{
    /// <summary>
    /// 이미지 보기 팝업. 확대/축소/회전/창 맞춤/실제 크기를 제공한다.
    /// 뷰는 Image 의 LayoutTransform(Scale × Rotate)에 <see cref="Zoom"/>, <see cref="Angle"/> 을 바인딩한다.
    /// </summary>
    public partial class ImageViewerViewModel : DialogViewModelBase
    {
        private const double ZoomStep = 1.25;
        private const double MinZoom = 0.05;
        private const double MaxZoom = 16.0;

        private double _viewportWidth;
        private double _viewportHeight;

        public ImageInfo Image { get; }

        /// <summary>원본 픽셀 크기. Zoom 1.0 = 이미지 1픽셀을 화면 1픽셀로 표시</summary>
        public int PixelWidth { get; }
        public int PixelHeight { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ZoomText))]
        private double _zoom = 1.0;

        /// <summary>회전 각도 (0 / 90 / 180 / 270)</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ZoomText))]
        private int _angle;

        /// <summary>true 면 표시 영역 크기에 맞춰 배율을 자동 계산한다. 확대/축소/실제 크기를 누르면 해제된다.</summary>
        [ObservableProperty]
        private bool _isFit = true;

        public string ZoomText => Angle == 0 ? $"{Zoom * 100:0}%" : $"{Zoom * 100:0}% · {Angle}°";

        public ImageViewerViewModel(ImageInfo image)
        {
            Image = image;
            var (width, height) = ImageHelper.ReadPixelSize(image.data);
            PixelWidth = width > 0 ? width : Math.Max(image.width, 1);
            PixelHeight = height > 0 ? height : Math.Max(image.height, 1);
            Title = $"{image.FileName} ({PixelWidth}x{PixelHeight}, {image.SizeText})";
        }

        /// <summary>뷰가 표시 영역(ScrollViewer viewport) 크기를 알려준다. 맞춤 모드면 배율을 다시 계산한다.</summary>
        public void SetViewport(double width, double height)
        {
            _viewportWidth = width;
            _viewportHeight = height;
            if (IsFit)
                ApplyFit();
        }

        private void ApplyFit()
        {
            if (_viewportWidth <= 0 || _viewportHeight <= 0)
                return;
            bool rotated = Angle % 180 != 0;
            double imageWidth = rotated ? PixelHeight : PixelWidth;
            double imageHeight = rotated ? PixelWidth : PixelHeight;
            Zoom = Clamp(Math.Min(_viewportWidth / imageWidth, _viewportHeight / imageHeight));
        }

        private static double Clamp(double zoom) => Math.Max(MinZoom, Math.Min(MaxZoom, zoom));

        [RelayCommand]
        private void ZoomIn()
        {
            IsFit = false;
            Zoom = Clamp(Zoom * ZoomStep);
        }

        [RelayCommand]
        private void ZoomOut()
        {
            IsFit = false;
            Zoom = Clamp(Zoom / ZoomStep);
        }

        /// <summary>창 크기에 맞춤</summary>
        [RelayCommand]
        private void Fit()
        {
            IsFit = true;
            ApplyFit();
        }

        /// <summary>실제 크기 (100%)</summary>
        [RelayCommand]
        private void ActualSize()
        {
            IsFit = false;
            Zoom = 1.0;
        }

        [RelayCommand]
        private void RotateLeft() => Rotate(-90);

        [RelayCommand]
        private void RotateRight() => Rotate(90);

        private void Rotate(int delta)
        {
            Angle = ((Angle + delta) % 360 + 360) % 360;
            if (IsFit)
                ApplyFit();
        }
    }
}
