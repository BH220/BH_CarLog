namespace BH_CarLog.Api
{
    /// <summary>
    /// 아직 API 연동이 구현되지 않은 기능을 호출했을 때 던진다.
    /// 화면에서는 오류가 아닌 "구현 필요" 안내로 표시되어, 버튼을 눌러 보면서 남은 작업을 확인할 수 있다.
    /// </summary>
    public class ApiNotImplementedException : NotImplementedException
    {
        /// <summary>기능 이름 (예: 주유기록 목록 조회)</summary>
        public string Feature { get; }

        /// <summary>연동할 엔드포인트와 응답 모델 (예: POST /api/carlog/fuel/list → ResFuelList)</summary>
        public string Endpoint { get; }

        public ApiNotImplementedException(string feature, string endpoint)
            : base($"[구현 필요] {feature}\r\nAPI: {endpoint}")
        {
            Feature = feature;
            Endpoint = endpoint;
        }
    }
}
