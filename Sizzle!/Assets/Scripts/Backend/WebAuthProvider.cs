using System;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Sizzle.Auth
{
    [Serializable] class ApiUser { public int id; public string nombre; public string email; }
    [Serializable] class ApiOk { public bool ok; public string error; public string token; public ApiUser user; }

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

                // Si hay token guardado, lo manda
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
                        if (!string.IsNullOrEmpty(resp.token))
                        {
                            PlayerPrefs.SetString("jwt_token", resp.token);
                            PlayerPrefs.SetString("user_name", resp.user.nombre);
                            PlayerPrefs.SetString("user_email", resp.user.email);
                            PlayerPrefs.Save();
                        }
                        cb(true, null);
                    }
                    else
                        cb(false, resp?.error ?? "Respuesta inválida");
                }
            }
        }

        static string Esc(string s) => (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");

        public void StartGoogleLogin(Action<bool, string> onResult)
        {
            var state = System.Guid.NewGuid().ToString("N");
            Application.OpenURL(_baseUrl + "/auth/google/start?state=" + state);
            _runner.StartCoroutine(PollGoogleTx(state, onResult));
        }

        private System.Collections.IEnumerator PollGoogleTx(string state, Action<bool, string> cb)
        {
            var url = _baseUrl + "/auth/google/tx/" + state;
            var deadline = Time.realtimeSinceStartup + 90f; // 90 segundos máximo

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

                        // buscamos si ya se completó
                        if (json.Contains("\"status\":\"ok\""))
                        {
                            try
                            {
                                // intenta deserializar los datos correctamente
                                ApiOk resp = JsonUtility.FromJson<ApiOk>(json.Replace("\"status\":\"ok\",", "").Trim());
                                if (resp != null && resp.user != null && !string.IsNullOrEmpty(resp.token))
                                {
                                    PlayerPrefs.SetString("jwt_token", resp.token);
                                    PlayerPrefs.SetString("user_name", resp.user.nombre);
                                    PlayerPrefs.SetString("user_email", resp.user.email);
                                    PlayerPrefs.Save();
                                }
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogWarning("[GoogleLogin] Error leyendo respuesta JSON: " + ex.Message);
                            }

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
                yield return new WaitForSeconds(2f); // vuelve a consultar cada 2 segundos
            }

            cb(false, "Timeout esperando Google");
        }


    }
}
