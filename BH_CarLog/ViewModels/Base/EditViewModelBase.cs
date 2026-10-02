using BH_CarLog.Api;
using BH_CarLog.Api.Interface;
using BH_CarLog.Core.Helper;
using BH_CarLog.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BH_CarLog.ViewModels.Base
{
    /// <summary>
    /// 입력/수정 창 공통 ViewModel. (기존 frmXxxInfo : BhSaveForm)
    /// 저장 / 저장 후 계속 / 닫기 버튼을 공통 제공한다.
    /// </summary>
    public abstract partial class EditViewModelBase : DialogViewModelBase
    {
        protected readonly IDialogService Dialog;
        protected readonly ICodeManager Codes;

        [ObservableProperty]
        private bool _isNew = true;

        /// <summary>
        /// 상세 보기 전용인지 여부 (기존 frmXxxInfo.JustViewer). true 면 입력을 막고 저장 버튼을 감춘다.
        /// 다른 화면(교체이력 창 등)에서 기록 내용만 확인할 때 쓴다.
        /// </summary>
        [ObservableProperty]
        private bool _isReadOnly;

        /// <summary>한 번이라도 저장에 성공했는지 여부. 닫을 때 목록 갱신 여부로 사용된다.</summary>
        protected bool IsSaved { get; set; }

        protected EditViewModelBase(IDialogService dialog, ICodeManager codes)
        {
            Dialog = dialog;
            Codes = codes;
        }

        /// <summary>
        /// 상세 조회 공통 처리. 서버 실패(<see cref="ApiException"/>)면 서버 메시지를 보여 주고 null 을 돌려준다.
        /// </summary>
        /// <param name="what">메시지에 쓸 대상 이름 (예: 차량정보)</param>
        protected async Task<TInfo?> LoadDetailAsync<TInfo>(Func<Task<TInfo?>> load, string what) where TInfo : class
        {
            try
            {
                var info = await load();
                if (info == null)
                    Dialog.ShowError($"{what} 조회에 실패했습니다.");
                return info;
            }
            catch (ApiException ex)
            {
                Dialog.ShowError($"{what} 조회에 실패했습니다.\r\n{ex.Message}");
                return null;
            }
        }

        /// <summary>필수값 검증. 실패 시 error 에 메시지를 담고 false 반환</summary>
        protected abstract bool Validate(out string error);

        protected abstract Task<(bool Success, string Message)> SaveAsync();

        /// <summary>"저장 후 계속" 이후 새 입력을 위해 화면을 초기화한다.</summary>
        protected virtual void ResetForContinue() { }

        private async Task<bool> TrySaveAsync()
        {
            // 상세 보기 창은 저장 버튼이 없지만 단축키 등으로 들어와도 저장하지 않는다.
            if (IsReadOnly)
                return false;

            if (Validate(out string error) == false)
            {
                Dialog.ShowWarning(error);
                return false;
            }

            IsBusy = true;
            try
            {
                var (success, message) = await SaveAsync();
                if (success == false)
                {
                    Dialog.ShowError(string.IsNullOrEmpty(message) ? "저장에 실패하였습니다." : message);
                    return false;
                }
                IsSaved = true;
                // 성공인데 메시지가 있으면 "본문은 저장됐지만 일부 첨부 처리 실패" 같은 경고다. (서버 성공 응답의 msg 는 비어 있다)
                if (string.IsNullOrEmpty(message) == false)
                    Dialog.ShowWarning(message);
                return true;
            }
            catch (ApiNotImplementedException ex)
            {
                // API 연동 전: 오류가 아닌 "구현 필요" 안내
                Dialog.ShowInfo(ex.Message, "구현 필요");
                return false;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"{GetType().Name} Save failed");
                Dialog.ShowError($"저장 중 오류가 발생했습니다.\r\n{ex.Message}");
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        /// <summary>저장 후 창 닫기</summary>
        [RelayCommand]
        private async Task Save()
        {
            if (await TrySaveAsync())
                DialogResult = true;
        }

        /// <summary>저장 후 계속 입력</summary>
        [RelayCommand]
        private async Task SaveContinue()
        {
            if (await TrySaveAsync())
            {
                IsNew = true;
                ResetForContinue();
            }
        }

        protected override void OnClose() => DialogResult = IsSaved;
    }
}
