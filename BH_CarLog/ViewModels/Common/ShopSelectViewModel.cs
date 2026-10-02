using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using BH_CarLog.Api.Model.Response;
using BH_CarLog.ViewModels.Base;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BH_CarLog.ViewModels.Common
{
    /// <summary>
    /// 가게 선택 창 (기존 BhSearchForm). 검색어로 거른 목록에서 하나를 고른다.
    /// 검색 기준은 가게명과 대표자명뿐이다. 가게 입력란(ShopPickerViewModel)의 자동 확정도 같은 기준을 쓴다.
    /// 열릴 때 포커스는 그리드에 있다 (뷰가 맡는다). 검색어 상자에서 ↓ 를 누르면 그리드로 내려간다.
    /// </summary>
    public partial class ShopSelectViewModel : DialogViewModelBase
    {
        public ObservableCollection<CarShopInfo> Items { get; }
        public ICollectionView ItemsView { get; }

        /// <summary>검색어. 가게 입력란에서 친 글자가 그대로 들어와 목록이 걸러진 채 열린다.</summary>
        [ObservableProperty] private string _keyword = "";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SelectCommand))]
        private CarShopInfo? _selectedItem;

        /// <summary>고른 가게. 닫기(Esc)면 null</summary>
        public CarShopInfo? Result { get; private set; }

        public ShopSelectViewModel(IEnumerable<CarShopInfo> shops, string keyword)
        {
            Title = "가게 선택";
            Items = new ObservableCollection<CarShopInfo>(shops);
            ItemsView = CollectionViewSource.GetDefaultView(Items);
            ItemsView.Filter = o => o is CarShopInfo shop && Matches(shop, Keyword);
            Keyword = keyword ?? "";
            SelectFirst();
        }

        partial void OnKeywordChanged(string value)
        {
            ItemsView.Refresh();
            SelectFirst();
        }

        /// <summary>걸러진 첫 행을 골라 두어 Enter 만 쳐도 바로 선택되게 한다.</summary>
        private void SelectFirst() => SelectedItem = ItemsView.Cast<CarShopInfo>().FirstOrDefault();

        /// <summary>가게명 또는 대표자명에 글자가 들어가면 맞는 것으로 본다. 빈 글자는 전부.</summary>
        public static bool Matches(CarShopInfo shop, string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return true;
            string k = keyword.Trim();
            return Has(shop.name, k) || Has(shop.ceo, k);
        }

        private static bool Has(string? text, string keyword)
            => string.IsNullOrEmpty(text) == false && text.Contains(keyword, StringComparison.OrdinalIgnoreCase);

        private bool CanSelect() => SelectedItem != null;

        [RelayCommand(CanExecute = nameof(CanSelect))]
        private void Select()
        {
            Result = SelectedItem;
            DialogResult = true;
        }
    }
}
