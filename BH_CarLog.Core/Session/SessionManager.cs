namespace BH_CarLog.Core.Session
{
    /// <summary>
    /// 로그인 세션. UUID 가 이후 API 요청의 Bearer 토큰이다.
    /// 로그인 전에는 빈 세션(IsLive = false)이 들어 있다. 비밀번호는 보관하지 않는다.
    /// </summary>
    public class SessionManager
    {
        public bool IsLive { get; private set; }
        public string UUID { get; private set; } = "";
        public ulong UserNo { get; private set; }
        public string ID { get; private set; } = "";
        public string Name { get; private set; } = "";

        /// <summary>세션 유지 시간(분). 0 이면 서버가 만료시키지 않는다.</summary>
        public int Timeout { get; private set; }
        public UserType UserType { get; private set; } = UserType.User;
        public StatusTypes Status { get; private set; } = StatusTypes.Disable;

        private static SessionManager _instance = new();

        public static SessionManager Instance => _instance;

        /// <summary>로그인 응답(user_session_data)으로 세션을 만든다.</summary>
        public static void MakeSession(UserSessionData data)
        {
            _instance = new SessionManager
            {
                UUID = data.uuid ?? "",
                UserNo = data.user_no,
                ID = data.user_id ?? "",
                Name = data.user_name ?? "",
                Status = data.status,
                Timeout = data.timeout,
                UserType = data.user_type,
                IsLive = true,
            };
        }

        /// <summary>로그아웃/세션 만료. 표시용 ID, Name 은 남기고 토큰과 상태만 지운다.</summary>
        public static void Clear()
        {
            _instance.IsLive = false;
            _instance.UUID = "";
            _instance.UserNo = 0;
            _instance.Status = StatusTypes.Disable;
        }
    }
}
