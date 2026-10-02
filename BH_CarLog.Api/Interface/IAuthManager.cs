using BH_CarLog.Api.Model.Response;

namespace BH_CarLog.Api.Interface
{
    public interface IAuthManager
    {
        /// <summary>
        /// 로그인. 성공 시 SessionManager 에 서버가 발급한 세션(uuid = Bearer 토큰)이 생성된다.
        /// force = true 면 다른 곳의 세션을 끊고 로그인한다.
        /// </summary>
        Task<LoginResult> LoginAsync(string userId, string password, bool force = false);

        /// <summary>중복 로그인 안내에서 사용자가 로그인을 포기한 경우 서버에 알린다.</summary>
        Task AbortLoginAsync(string userId);

        /// <summary>로그아웃. 세션을 정리한다.</summary>
        Task LogoutAsync();
    }
}
