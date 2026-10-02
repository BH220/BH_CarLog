namespace BH_CarLog.Api.Model.Response
{
    public class ResBase
    {
        /// <summary>
        /// 서버 자체가 살아 있는지 여부. 1: 살아 있음, 0: 죽어 있거나 찾지 못함
        /// </summary>
        public int is_alive { get; set; } = 0;

        /// <summary>
        /// API 요청 처리 결과. 1: 성공, 0: 실패
        /// </summary>
        public int result { get; set; } = 0;

        public string msg { get; set; } = "";

        /// <summary>
        /// 인증 실패(401) 여부. 로그아웃/세션 만료로 서버가 요청을 거부한 경우 true.
        /// true 인 경우 사용자에게 오류 팝업을 띄우지 않는다.
        /// </summary>
        public bool unauthorized { get; set; } = false;
    }
}
