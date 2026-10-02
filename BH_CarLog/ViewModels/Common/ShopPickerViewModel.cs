using BH_CarLog.Api.Model.Response;
using BH_CarLog.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BH_CarLog.ViewModels.Common
{
    /// <summary>
    /// 가게 입력 (기존 BhSearchItem + BHS_Shops). 콤보가 아니라 가게명 일부를 쳐서 고른다.
    /// Enter 를 누르면 후보 중 가게명에 그 글자가 들어가는 것을 찾아
    ///   - 딱 하나면 그 가게로 확정하고 전체 이름을 보여 준다
    ///   - 여럿이면 그 글자로 걸러진 가게 선택 창(<see cref="ShopSelectViewModel"/>)을 띄운다
    ///   - 빈 채로 Enter 면 전체 목록이 뜬다
    /// 확정 뒤 글자를 고치면 선택이 풀리고 다시 찾아야 한다. 글자와 선택이 어긋난 채 저장되는 일을 막기 위해서다.
    /// </summary>
    public partial class ShopPickerViewModel : ObservableObject
    {
        private readonly IDialogService _dialog;
        private List<CarShopInfo> _candidates = new();

        /// <summary>입력 중인 글자. 확정되면 가게 전체 이름이 들어간다.</summary>
        [ObservableProperty] private string _text = "";

        /// <summary>확정된 가게. null 이면 아직 고르지 않았다.</summary>
        [ObservableProperty] private CarShopInfo? _selected;

        [ObservableProperty] private bool _isReadOnly;
        [ObservableProperty] private bool _isFocused;

        public ShopPickerViewModel(IDialogService dialog)
        {
            _dialog = dialog;
        }

        /// <summary>고를 수 있는 가게. 화면마다 다르다 (주유: 주유소·혼합, 유지보수: 전부).</summary>
        public void SetCandidates(IEnumerable<CarShopInfo> shops)
            => _candidates = shops.OrderBy(x => x.name).ToList();

        /// <summary>가게를 바로 확정한다 (수정 모드에서 저장된 가게를 보여 줄 때). null 이면 비운다.</summary>
        public void Set(CarShopInfo? shop)
        {
            Selected = shop;
            Text = shop?.name ?? "";
        }

        partial void OnTextChanged(string value)
        {
            if (Selected != null && string.Equals(value, Selected.name, StringComparison.Ordinal) == false)
                Selected = null;
        }

        /// <summary>Enter. 입력한 글자로 가게를 찾는다. 하나면 확정, 아니면 선택 창.</summary>
        [RelayCommand]
        private void Resolve() => TryResolve();

        /// <summary>
        /// 저장 직전 검증용. 이미 확정됐으면 true. 아니면 Enter 를 누른 것과 같은 절차(하나면 확정, 아니면 선택 창)를 거친다.
        /// Enter 없이 바로 저장을 눌러도 원본처럼 동작하게 하기 위해서다.
        /// </summary>
        public bool EnsureResolved() => TryResolve();

        private bool TryResolve()
        {
            if (IsReadOnly)
                return Selected != null;
            if (Selected != null && Text == Selected.name)
                return true;

            string keyword = (Text ?? "").Trim();
            // 원본(BhSearchForm.GetResultOneRow)과 같이 빈 글자는 하나로 치지 않고 전체 목록을 연다.
            var matches = Match(keyword);
            if (keyword.Length > 0 && matches.Count == 1)
            {
                Set(matches[0]);
                return true;
            }
            return OpenPicker(keyword);
        }

        /// <summary>가게명·대표자명에 글자가 들어가는 가게. (주소·전화까지 보지 않는다. 선택 창도 같은 기준)</summary>
        private List<CarShopInfo> Match(string keyword)
        {
            if (keyword.Length == 0)
                return _candidates;
            return _candidates.Where(x => ShopSelectViewModel.Matches(x, keyword)).ToList();
        }

        /// <summary>선택 창. 입력한 글자가 검색어로 들어가 걸러진 채 뜬다.</summary>
        private bool OpenPicker(string keyword)
        {
            if (_candidates.Count == 0)
            {
                _dialog.ShowWarning("고를 수 있는 가게가 없습니다. 가게 화면에서 먼저 등록하세요.");
                return false;
            }

            var vm = new ShopSelectViewModel(_candidates, keyword);
            if (_dialog.ShowDialog(vm) == true && vm.Result != null)
            {
                Set(vm.Result);
                return true;
            }
            return false;
        }
    }
}
