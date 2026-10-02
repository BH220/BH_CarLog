using BH_CarLog.Api.Model.Response;

namespace BH_CarLog.Api
{
    /// <summary>
    /// 서버가 실패(result != 1)로 응답했거나 요청 자체가 실패한 경우.
    /// 빈 목록과 "조회 실패"를 화면이 구분하고, 서버가 준 메시지를 그대로 보여 줄 수 있게 전달한다.
    /// </summary>
    public class ApiException : Exception
    {
        /// <summary>401. 세션 만료/로그아웃 상태. 화면은 오류 팝업 대신 상태 표시로 알린다.</summary>
        public bool Unauthorized { get; }

        public ApiException(string message, bool unauthorized = false) : base(message)
        {
            Unauthorized = unauthorized;
        }
    }

    public static class ResBaseExtensions
    {
        /// <summary>
        /// 실패 응답이면 <see cref="ApiException"/> 을 던진다. 성공이면 응답을 그대로 돌려준다.
        /// </summary>
        /// <param name="feature">서버 메시지가 비어 있을 때 쓸 기능 이름 (예: 차량 목록)</param>
        public static T EnsureSuccess<T>(this T res, string feature) where T : ResBase
        {
            if (res.result == 1)
                return res;

            string message;
            if (res.unauthorized)
                message = string.IsNullOrEmpty(res.msg) ? "로그인이 필요합니다. 다시 로그인하세요." : res.msg;
            else
                message = string.IsNullOrEmpty(res.msg) ? $"{feature} 요청이 실패했습니다." : res.msg;

            throw new ApiException(message, res.unauthorized);
        }
    }
}
