using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Sizzle.Auth
{
    [Serializable] class ApiUser { public int id; public string nombre; public string email; }
    [Serializable] class ApiOk { public bool ok; public string error; public string token; public ApiUser user; }

    [Serializable] class GoogleTxData { public string status; public GoogleTxPayload data; public string error; }
    [Serializable] class GoogleTxPayload { public ApiUser user; public string token; }

    public class WebAuthProvider : IAuthProvider
    {
        private readonly string _baseUrl;
        private readonly MonoBehaviour _runner;

        public WebAuthProvider(string baseUrl, MonoBehaviour runner)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _runner = runner;
        }

        // LOGIN NORMAL
        public void ValidateCredentials(string emailOrUser, string password, Action<bool, string> onResult)
            => _runner.StartCoroutine(PostJson("/auth/login",
                 $"{{\"emailOrUser\":\"{Esc(emailOrUser)}\",\"password\":\"{Esc(password)}\"}}",
                 onResult));

        // REGISTRO NORMAL
        public void Register(string username, string email, string password, Action<bool, string> onResult)
            => _runner.StartCoroutine(PostJson("/user/registrar",
                 $"{{\"username\":\"{Esc(username)}\",\"email\":\"{Esc(email)}\",\"password\":\"{Esc(password)}\"}}",
                 onResult));

        // ENVÍO DE JSON
        System.Collections.IEnumerator PostJson(string path, string json, Action<bool, string> cb)
        {
            var url = _baseUrl + path;
            var payload = Encoding.UTF8.GetBytes(json);

            using (var req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(payload);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                var token = PlayerPrefs.GetString("jwt_token", "");
                if (!string.IsNullOrEmpty(token))
                    req.SetRequestHeader("Authorization", "Bearer " + token);

                yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                bool error = req.result != UnityWebRequest.Result.Success;
#else
                bool error = req.isNetworkError || req.isHttpError;
#endif
                if (error)
                {
                    string msg = !string.IsNullOrEmpty(req.downloadHandler.text) ? req.downloadHandler.text : req.error;
                    cb(false, $"HTTP {req.responseCode}: {msg}");
                }
                else
                {
                    ApiOk resp = null;
                    try { resp = JsonUtility.FromJson<ApiOk>(req.downloadHandler.text); } catch { }
                    if (resp != null && resp.ok)
                    {
                        SaveSession(resp.user, resp.token);
                        cb(true, null);
                    }
                    else cb(false, resp?.error ?? "Respuesta inválida");
                }
            }
        }

        static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

        // === LOGIN CON GOOGLE ===
        public void StartGoogleLogin(Action<bool, string> onResult)
        {
            var state = System.Guid.NewGuid().ToString("N");
            Application.OpenURL(_baseUrl + "/auth/google/start?state=" + state + "&prompt=select_account");

            _runner.StartCoroutine(PollGoogleTx(state, onResult));
        }

        private System.Collections.IEnumerator PollGoogleTx(string state, Action<bool, string> cb)
        {
            var url = _baseUrl + "/auth/google/tx/" + state;
            var deadline = Time.realtimeSinceStartup + 90f;

            while (Time.realtimeSinceStartup < deadline)
            {
                using (var req = UnityWebRequest.Get(url))
                {
                    yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                    bool error = req.result != UnityWebRequest.Result.Success;
#else
                    bool error = req.isNetworkError || req.isHttpError;
#endif
                    if (!error)
                    {
                        var json = req.downloadHandler.text;
                        GoogleTxData resp = null;
                        try { resp = JsonUtility.FromJson<GoogleTxData>(json); } catch { }

                        if (resp != null)
                        {
                            if (resp.status == "ok" && resp.data != null)
                            {
                                SaveSession(resp.data.user, resp.data.token);
                                cb(true, null);
                                yield break;
                            }
                            if (resp.status == "error")
                            {
                                cb(false, "Google auth error");
                                yield break;
                            }
                        }
                    }
                }
                yield return new WaitForSeconds(2f);
            }
            cb(false, "Timeout esperando Google");
        }

        // === GUARDA TOKEN Y DATOS DEL USUARIO ===
        private void SaveSession(ApiUser user, string token)
        {
            if (!string.IsNullOrEmpty(token))
            {
                PlayerPrefs.SetString("jwt_token", token);
                PlayerPrefs.SetInt("user_id", user.id);
                PlayerPrefs.SetString("user_name", user.nombre);
                PlayerPrefs.SetString("user_email", user.email);
                PlayerPrefs.Save();
                AuthEvents.RaiseSessionChanged();
            }
        }
    }
}
