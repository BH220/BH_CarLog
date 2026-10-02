using System.Collections.ObjectModel;
using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace BH_CarLog.Services
{
    /// <summary>
    /// 상단 차량 선택 상태. (기존 frmMain 의 carSelect 콤보)
    /// 주유·유지보수·소모품 화면이 모두 "지금 보고 있는 차" 를 기준으로 동작하므로 한 곳에서 들고 있는다.
    /// </summary>
    public interface ICarContext
    {
        ObservableCollection<CarInfo> Cars { get; }
        CarInfo? SelectedCar { get; set; }
        int SelectedCarNum { get; }

        /// <summary>
        /// 차량 목록을 다시 읽는다. 이미 고른 차가 목록에 남아 있으면 선택을 유지하고,
        /// 없으면 마지막에 등록된 차를 고른다.
        /// 조회에 실패하면 <see cref="Api.ApiException"/> 을 던지고 기존 목록은 그대로 둔다. (빈 목록과 구분하기 위해)
        /// </summary>
        Task ReloadAsync();

        /// <summary>car_num 으로 차량 표시명을 찾는다. 없으면 빈 문자열.</summary>
        string GetCarName(int carNum);
    }

    public partial class CarContext : ObservableObject, ICarContext
    {
        private readonly ICarManager _cars;
        private readonly IMessenger _messenger;

        public ObservableCollection<CarInfo> Cars { get; } = new();

        [ObservableProperty]
        private CarInfo? _selectedCar;

        public int SelectedCarNum => SelectedCar?.car_num ?? 0;

        public CarContext(ICarManager cars, IMessenger messenger)
        {
            _cars = cars;
            _messenger = messenger;
        }

        partial void OnSelectedCarChanged(CarInfo? value)
        {
            OnPropertyChanged(nameof(SelectedCarNum));
            _messenger.Send(new CarChangedMessage(value));
        }

        public async Task ReloadAsync()
        {
            int preNum = SelectedCarNum;
            var list = await _cars.GetListAsync();

            Cars.Clear();
            // car_num 은 auto_increment 라 큰 값이 나중에 등록된 차다. 최근 등록 차를 위에 둔다.
            foreach (var car in list.OrderByDescending(x => x.car_num))
                Cars.Add(car);

            // 고른 차가 남아 있으면 유지, 없으면 마지막에 등록된 차(= 목록 첫 번째)
            SelectedCar = Cars.FirstOrDefault(x => x.car_num == preNum) ?? Cars.FirstOrDefault();
        }

        public string GetCarName(int carNum)
            => Cars.FirstOrDefault(x => x.car_num == carNum)?.DisplayName ?? "";
    }
}
