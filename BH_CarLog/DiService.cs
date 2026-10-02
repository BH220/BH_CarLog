using BH_CarLog.Api.Interface;
using BH_CarLog.Api.Manager;
using BH_CarLog.Services;
using BH_CarLog.ViewModels;
using BH_CarLog.ViewModels.Car;
using BH_CarLog.ViewModels.Consumable;
using BH_CarLog.ViewModels.Fuel;
using BH_CarLog.ViewModels.Maintenance;
using BH_CarLog.ViewModels.NewMessage;
using BH_CarLog.ViewModels.Shop;
using BH_CarLog.ViewModels.Term;
using BH_CarLog.Views;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace BH_CarLog
{
    public static class DiService
    {
        public static ServiceProvider ServicesRegister()
        {
            var services = new ServiceCollection();

            // 서비스
            services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IClipboardService, ClipboardService>();
            services.AddSingleton<ICarContext, CarContext>();

            // API 매니저
            services.AddSingleton<IAuthManager, AuthManager>();
            services.AddSingleton<ICodeManager, CodeManager>();
            services.AddSingleton<ICarManager, CarManager>();
            services.AddSingleton<ICarShopManager, CarShopManager>();
            services.AddSingleton<IFuelManager, FuelManager>();
            services.AddSingleton<IMaintenanceManager, MaintenanceManager>();
            services.AddSingleton<IConsumableManager, ConsumableManager>();
            services.AddSingleton<INewMessageManager, NewMessageManager>();
            services.AddSingleton<IImageManager, ImageManager>(); // 첨부 이미지 원본 내려받기 (공용)
            services.AddSingleton<ITermManager, TermManager>();   // 교환주기 계산 (소모품 + 교체이력 + 주행거리 합성)

            // ViewModel
            services.AddSingleton<MainViewModel>();
            services.AddTransient<LoginViewModel>();

            services.AddTransient<NewMessageListViewModel>();
            services.AddTransient<NewMessageDetailViewModel>();
            services.AddTransient<FuelListViewModel>();
            services.AddTransient<FuelEditViewModel>();
            services.AddTransient<MaintenanceListViewModel>();
            services.AddTransient<MaintenanceEditViewModel>();
            services.AddTransient<ConsumableListViewModel>();
            services.AddTransient<ConsumableEditViewModel>();
            services.AddTransient<ConsumableHistoryViewModel>();
            services.AddTransient<TermListViewModel>();
            services.AddTransient<CarListViewModel>();
            services.AddTransient<CarEditViewModel>();
            services.AddTransient<CarShopListViewModel>();
            services.AddTransient<CarShopEditViewModel>();

            services.AddSingleton<MainWindow>();

            return services.BuildServiceProvider();
        }
    }
}
