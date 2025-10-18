// -----------------------------------------------
// WebAuthProvider.cs
// Resumen :
// - Cliente HTTP de Unity. Envía JSON sobre HTTPS a /auth/login y /auth/register.
// - La confidencialidad e integridad la aporta TLS (HTTPS). No envia contraseñas en claro “legibles” en la red.
// - El servidor valida hash bcrypt y responde { ok, user|error }.
// -----------------------------------------------
using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Sizzle.Auth
{
    [Serializable] class ApiOk { public bool ok; public string error; }

    public class WebAuthProvider : IAuthProvider
    {
        private readonly string _baseUrl;
        private readonly MonoBehaviour _runner;

        public WebAuthProvider(string baseUrl, MonoBehaviour runner)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _runner = runner;
        }

        public void ValidateCredentials(string emailOrUser, string password, Action<bool, string> onResult)
            => _runner.StartCoroutine(PostJson("/auth/login",
                 $"{{\"emailOrUser\":\"{Esc(emailOrUser)}\",\"password\":\"{Esc(password)}\"}}",
                 onResult));

        public void Register(string username, string email, string password, Action<bool, string> onResult)
            => _runner.StartCoroutine(PostJson("/auth/register",
                 $"{{\"username\":\"{Esc(username)}\",\"email\":\"{Esc(email)}\",\"password\":\"{Esc(password)}\"}}",
                 onResult));

        System.Collections.IEnumerator PostJson(string path, string json, Action<bool, string> cb)
        {
            var url = _baseUrl + path;
            var payload = Encoding.UTF8.GetBytes(json);

            using (var req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(payload);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
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
                    cb(resp != null && resp.ok, resp?.error);
                }
            }
        }

        static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

        public void StartGoogleLogin(Action<bool, string> onResult)
        {
            // genera estado único (puede ser Guid)
            var state = System.Guid.NewGuid().ToString("N");
            // abre el navegador del sistema: /auth/google/start?state=...
            Application.OpenURL(_baseUrl + "/auth/google/start?state=" + state);
            _runner.StartCoroutine(PollGoogleTx(state, onResult));
        }

        private System.Collections.IEnumerator PollGoogleTx(string state, Action<bool, string> cb)
        {
            var url = _baseUrl + "/auth/google/tx/" + state;
            var deadline = Time.realtimeSinceStartup + 90f; // 90s timeout

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
                        // estructura esperada: { status:'pending'|'ok'|'error', data?, error? }
                        if (json.Contains("\"status\":\"ok\""))
                        {
                            cb(true, null);
                            yield break;
                        }
                        if (json.Contains("\"status\":\"error\""))
                        {
                            cb(false, "Google auth error");
                            yield break;
                        }
                    }
                }
                yield return new WaitForSeconds(2f); // polling cada 2s
            }
            cb(false, "Timeout esperando Google");
        }

    }
}
