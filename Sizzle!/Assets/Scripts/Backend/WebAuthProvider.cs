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
    }
}
