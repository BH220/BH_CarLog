using BH_CarLog.Api.Model.Response;
using BH_CarLog.Core.Configurations;
using BH_CarLog.Core.Configurations.Setting;
using BH_CarLog.Core.Helper;
using BH_CarLog.Core.Session;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net;

namespace BH_CarLog.Api
{
    /// <summary>
    /// 차계부 API 클라이언트. (BH_SecurityCode.Api.ScApi 와 같은 구조)
    /// 서버는 같고 경로만 /api/carlog/* 를 쓴다. 인증 토큰은 <see cref="SessionManager"/> 의 UUID 다.
    /// </summary>
    public class CarLogApi
    {
        private static CarLogApi? _instance;

        public static CarLogApi Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CarLogApi();
                    _instance.SetUrl(Config.ServerSetting.Address);
                }
                return _instance;
            }
        }

        /// <summary>로그인 요청 시 서버에 전달하는 프로그램 번호 (BH 차계부 = 3)</summary>
        public const string ProgramNum = "3";

        private string _baseUrl;

        public CarLogApi()
        {
            _baseUrl = "https://www.bhsoft.com";
        }

        /// <summary>API 기본 주소를 설정한다. 스킴 포함 (예: https://www.bhsoft.com). 스킴이 없으면 https:// 로 간주한다.</summary>
        public void SetUrl(string url)
        {
            string address = ServerSetting.Normalize(url);
            _baseUrl = address;
            Config.ServerSetting.Address = address;
        }

        public bool IsAlive() => IsAlive(Config.ServerSetting.Address);

        /// <summary>지정한 주소(스킴 포함)의 서버가 응답하는지 확인한다. (설정 저장 전 연결 테스트용)</summary>
        public bool IsAlive(string address)
        {
            address = ServerSetting.Normalize(address);
            if (string.IsNullOrEmpty(address))
                return false;
            try
            {
                using var client = CreateClient(address);
                client.Timeout = TimeSpan.FromSeconds(3);
                Uri url = new Uri($"{address}/health");
                var response = client.GetAsync(url).GetAwaiter().GetResult();
                if (response.StatusCode == HttpStatusCode.OK)
                    return true;
            }
            catch (Exception ex)
            {
                Log.Exception(ex, "IsAlive check failed");
            }
            return false;
        }

        private HttpClient HttpClientEx() => CreateClient(_baseUrl);

        /// <summary>
        /// 인증서 검증은 개발용 로컬 서버(localhost / 127.0.0.1)에서만 끈다. 그 외 주소는 정상 검증한다.
        /// 연결 확인(IsAlive)과 실제 요청이 같은 기준을 쓴다.
        /// </summary>
        private static HttpClient CreateClient(string address)
        {
            bool isLocal = address.Contains("://localhost", StringComparison.OrdinalIgnoreCase)
                        || address.Contains("://127.0.0.1", StringComparison.OrdinalIgnoreCase);
            if (isLocal == false)
                return new HttpClient();

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            return new HttpClient(handler);
        }

        public async Task<T> PostResAsync<T>(string endpoint, Dictionary<string, string> dictionary) where T : ResBase, new()
        {
            FormUrlEncodedContent content = new FormUrlEncodedContent(dictionary);
            return await PostResponseAsync<T>(endpoint, content);
        }

        public async Task<T> PostResponseAsync<T>(string endpoint, HttpContent? content = null) where T : ResBase, new()
        {
            // 인증 필요 여부는 클라이언트가 판단하지 않는다. (서버 라우터의 auth 선언이 판단)
            bool wasLive = SessionManager.Instance.IsLive;
            try
            {
                using var client = HttpClientEx();
                string apiUrl = $"{_baseUrl}{endpoint}";

                if (string.IsNullOrEmpty(SessionManager.Instance.UUID) == false)
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.Instance.UUID);

                HttpResponseMessage response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<T>(result) ?? new T { msg = "응답을 해석할 수 없습니다." };
                }
                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    SessionManager.Clear();
                    if (wasLive)
                    {
                        // 사용 중 서버에서 세션이 만료/종료됨 → 세션 정리 후, 사용자에게 명확한 메시지를 전달한다.
                        Log.ErrorLog($"인증 실패(Unauthorized) - EndPoint: {endpoint}");
                        return new T { result = 0, unauthorized = true, msg = "Session expired. Please login again." };
                    }
                    // 로그아웃/미로그인 상태에서 보낸 요청의 401 은 조용히 실패 처리한다.
                    return new T { result = 0, unauthorized = true, msg = "" };
                }

                // 서버는 입력값 문제를 400 + {result:0, msg:"..."} 로 알려준다.
                // 그 msg 가 사용자가 고칠 수 있는 유일한 단서이므로 예외로 버리지 않고 그대로 돌려준다.
                string body = await response.Content.ReadAsStringAsync();
                Log.ErrorLog($"Post 요청 실패: {response.StatusCode} {Environment.NewLine} EndPoint: {endpoint} {Environment.NewLine} Body: {body}");

                if (string.IsNullOrWhiteSpace(body) == false)
                {
                    try
                    {
                        T? failed = JsonConvert.DeserializeObject<T>(body);
                        if (failed != null && string.IsNullOrWhiteSpace(failed.msg) == false)
                        {
                            failed.result = 0;
                            return failed;
                        }
                    }
                    catch (JsonException)
                    {
                        // JSON 이 아니면 아래 예외로 넘어간다.
                    }
                }

                throw new Exception($"Post 요청 실패: {response.StatusCode}");
            }
            catch (Exception ex)
            {
                return new T { msg = ex.Message };
            }
        }

        /// <summary>
        /// 인증이 필요한 바이너리(첨부 이미지 원본 등)를 GET 으로 받는다.
        /// endpoint 는 "/api/carlog/image/1" 같은 상대 경로 또는 절대 URL. (서버 라우트: GET /api/carlog/image/{image_num})
        /// </summary>
        public async Task<(bool Success, string Message, byte[]? Data)> GetBytesAsync(string endpoint)
        {
            bool wasLive = SessionManager.Instance.IsLive;
            try
            {
                using var client = HttpClientEx();
                bool isAbsolute = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                               || endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
                string apiUrl = isAbsolute ? endpoint : $"{_baseUrl}{(endpoint.StartsWith("/") ? "" : "/")}{endpoint}";

                if (string.IsNullOrEmpty(SessionManager.Instance.UUID) == false)
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SessionManager.Instance.UUID);

                using HttpResponseMessage response = await client.GetAsync(apiUrl);

                if (response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    SessionManager.Clear();
                    if (wasLive)
                        Log.ErrorLog($"인증 실패(Unauthorized) - EndPoint: {endpoint}");
                    return (false, wasLive ? "Session expired. Please login again." : "", null);
                }

                // 오류는 JSON 으로 온다.
                string mediaType = response.Content.Headers.ContentType?.MediaType ?? "";
                if (mediaType.Contains("json", StringComparison.OrdinalIgnoreCase))
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var res = JsonConvert.DeserializeObject<ResBase>(json);
                    string msg = string.IsNullOrEmpty(res?.msg) ? $"요청 실패: {response.StatusCode}" : res!.msg;
                    Log.ErrorLog($"GET 요청 실패: {response.StatusCode} {msg} EndPoint: {endpoint}");
                    return (false, msg, null);
                }

                if (response.IsSuccessStatusCode == false)
                {
                    Log.ErrorLog($"GET 요청 실패: {response.StatusCode} EndPoint: {endpoint}");
                    return (false, $"요청 실패: {response.StatusCode}", null);
                }

                byte[] data = await response.Content.ReadAsByteArrayAsync();
                return (true, "", data);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, $"GetBytesAsync failed: {endpoint}");
                return (false, ex.Message, null);
            }
        }

        /// <summary>
        /// 파일 업로드 (multipart/form-data, Bearer 인증). fields 는 일반 폼 값, 파일은 fileField 이름(이미지 등록 API 는 "image_data")으로 붙인다.
        /// 서버(PHP)는 $_FILES[fileField] 로 받으므로 파트에 filename 이 반드시 있어야 한다.
        /// </summary>
        public async Task<T> PostMultipartAsync<T>(string endpoint, IDictionary<string, string> fields, string fileField, string fileName, byte[] fileData, string? contentType = null) where T : ResBase, new()
        {
            using var content = new MultipartFormDataContent();
            foreach (var field in fields)
                content.Add(new StringContent(field.Value ?? ""), field.Key);

            var file = new ByteArrayContent(fileData);
            file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType ?? GuessImageContentType(fileName));
            content.Add(file, fileField, fileName);

            return await PostResponseAsync<T>(endpoint, content);
        }

        /// <summary>파일명 확장자로 이미지 Content-Type 을 정한다. 모르면 application/octet-stream.</summary>
        private static string GuessImageContentType(string fileName)
        {
            string ext = Path.GetExtension(fileName ?? "").TrimStart('.').ToLowerInvariant();
            return ext switch
            {
                "jpg" or "jpeg" => "image/jpeg",
                "png" => "image/png",
                "gif" => "image/gif",
                "bmp" => "image/bmp",
                "webp" => "image/webp",
                _ => "application/octet-stream",
            };
        }

        /// <summary>
        /// 신규 첨부 이미지를 한 장씩 올린다. 한 장이 실패해도 나머지는 계속 올리고 실패 목록을 msg 로 돌려준다.
        /// </summary>
        public async Task<ResBase> AddImages<T>(string endpoint, string ownerField, int ownerNum, IEnumerable<ImageInfo>? images) where T : ResImageAddBase, new()
        {
            var result = new ResBase { result = 1, is_alive = 1, msg = "" };
            if (images == null)
                return result;

            var errors = new List<string>();

            foreach (var image in images.Where(x => x.IsNew).ToList())
            {
                if (image.data == null || image.data.Length == 0)
                {
                    errors.Add($"{image.FileName}: 업로드할 이미지 데이터가 없습니다.");
                    continue;
                }

                var fields = new Dictionary<string, string>
                {
                    [ownerField] = ownerNum.ToString(),
                    ["image_name"] = image.FileName,
                };
                T res = await PostMultipartAsync<T>(endpoint, fields, "image_data", image.FileName, image.data);

                if (res.unauthorized)
                    return res; // 세션 만료: 더 진행하지 않는다.

                if (res.result != 1 || res.LinkImageNum <= 0)
                {
                    Log.Warn($"이미지 등록 실패 {endpoint} {ownerField}={ownerNum} {image.FileName}: {res.msg}");
                    errors.Add($"{image.FileName}: {(string.IsNullOrEmpty(res.msg) ? "이미지 등록 실패" : res.msg)}");
                    continue;
                }

                // 서버가 JPEG 로 압축해 저장하므로 확장자/크기/치수는 응답 값으로 바꾼다.
                image.LinkImageNum = res.LinkImageNum;
                image.image_num = res.image_num;
                if (string.IsNullOrEmpty(res.name) == false) image.name = res.name;
                if (string.IsNullOrEmpty(res.extension) == false) image.extension = res.extension;
                image.size = res.size;
                image.width = res.width;
                image.height = res.height;
                image.url = res.url ?? "";
            }

            if (errors.Count > 0)
                result.msg = "일부 이미지 등록에 실패했습니다.\r\n" + string.Join("\r\n", errors);
            return result;
        }

        /// <summary>로그인. 다른 곳에 세션이 있으면 409 / msg = DUPLICATE_SESSION 으로 응답한다. (<see cref="ResLogin.IsDuplicateSession"/>)</summary>
        public Task<ResLogin> GetTokenAsync(string userId, string password)
            => LoginInternalAsync("/api/auth/login", userId, password);

        /// <summary>기존 세션을 끊고 로그인한다.</summary>
        public Task<ResLogin> GetTokenForceAsync(string userId, string password)
            => LoginInternalAsync("/api/auth/login/force", userId, password);

        private async Task<ResLogin> LoginInternalAsync(string endpoint, string userId, string password)
        {
            ResLogin result = new ResLogin();
            if (IsAlive() == false)
            {
                result.is_alive = 0;
                result.msg = "서버에 연결할 수 없습니다.";
                return result;
            }
            try
            {
                using var client = HttpClientEx();
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("program_num", ProgramNum),
                    new KeyValuePair<string, string>("id", userId),
                    new KeyValuePair<string, string>("pw", password),
                });

                var response = await client.PostAsync($"{_baseUrl}{endpoint}", content);
                result.is_alive = 1;

                string body = await response.Content.ReadAsStringAsync();
                JObject? json = null;
                try { json = string.IsNullOrWhiteSpace(body) ? null : JObject.Parse(body); } catch { /* JSON 이 아닌 응답 */ }

                if (response.IsSuccessStatusCode)
                {
                    var session = json?["user_session_data"]?.ToObject<UserSessionData>();
                    if (session == null)
                    {
                        result.msg = "서버 응답에 세션 정보가 없습니다.";
                        Log.ErrorLog($"로그인 응답 형식 오류 - EndPoint: {endpoint}, Body: {body}");
                        return result;
                    }
                    result.result = 1;
                    result.user_session_data = session;
                    SessionManager.MakeSession(session);
                }
                else
                {
                    result.msg = json?["msg"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(result.msg))
                        result.msg = $"로그인 실패 ({(int)response.StatusCode} {response.StatusCode})";
                    result.unauthorized = response.StatusCode == HttpStatusCode.Unauthorized;

                    var existing = json?["existing_session"] ?? json?["existingSession"];
                    if (existing != null)
                    {
                        try { result.existingSession = existing.ToObject<ExistingSessionData>(); } catch { }
                    }
                    Log.Warn($"로그인 실패 - EndPoint: {endpoint}, Status: {(int)response.StatusCode}, Msg: {result.msg}");
                }
            }
            catch (HttpRequestException ex)
            {
                result.is_alive = 0;
                result.msg = ex.Message;
                Log.Exception(ex, $"로그인 요청 실패 - EndPoint: {endpoint}");
            }
            catch (Exception ex)
            {
                result.msg = ex.Message;
                Log.Exception(ex, $"로그인 처리 오류 - EndPoint: {endpoint}");
            }
            return result;
        }

        /// <summary>중복 로그인 안내에서 사용자가 로그인을 포기한 경우 서버에 알린다.</summary>
        public async Task<ResBase> SetLoginAbortAsync(string userId)
        {
            ResBase result = new ResBase();
            if (IsAlive() == false)
                return result;
            try
            {
                using var client = HttpClientEx();
                var content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("program_num", ProgramNum),
                    new KeyValuePair<string, string>("id", userId),
                });

                var response = await client.PostAsync($"{_baseUrl}/api/auth/login/abort", content);
                if (response.IsSuccessStatusCode)
                    result.result = 1;
                else
                    result.msg = response.StatusCode.ToString();
            }
            catch (Exception ex)
            {
                result.msg = ex.Message;
            }
            return result;
        }
    }
}
