using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Sizzle.Auth;

/// <summary>
/// Comprueba si existe un JWT guardado y si es válido.
/// Si es válido, pasa directamente al menú principal.
/// Si no, muestra el menú de login.
/// </summary>
public class SessionManager : MonoBehaviour
{
    [Header("UI / Menús")]
    public MenuManager menuManager;
    public string mainMenuName = "MenuPrincipal";
    public string loginMenuName = "Login";

    [Header("Servidor")]
    [Tooltip("Usa el mismo endpoint que ServerConfig.BaseUrl")]
    public string baseUrl = "https://serversizzle.onrender.com";

    [Header("Opcional")]
    public TMP_Text statusText;

    void Start()
    {
        // Lanza la verificación del token al iniciar
        StartCoroutine(ValidateSession());
    }

    private System.Collections.IEnumerator ValidateSession()
    {
        var token = PlayerPrefs.GetString("jwt_token", "");
        if (string.IsNullOrEmpty(token))
        {
            ShowLogin("Sin sesión guardada.");
            yield break;
        }

        string url = baseUrl.TrimEnd('/') + "/auth/validate";
        using (var req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Authorization", "Bearer " + token);
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool error = req.result != UnityWebRequest.Result.Success;
#else
            bool error = req.isNetworkError || req.isHttpError;
#endif
            if (error)
            {
                PlayerPrefs.DeleteKey("jwt_token");
                PlayerPrefs.DeleteKey("user_name");
                PlayerPrefs.DeleteKey("user_email");
                PlayerPrefs.Save();
                ShowLogin("Token inválido o expirado.");
                yield break;
            }

            var json = req.downloadHandler.text;
            if (json.Contains("\"ok\":true"))
            {
                ShowMain("Sesión restaurada correctamente.");
            }
            else
            {
                PlayerPrefs.DeleteKey("jwt_token");
                PlayerPrefs.Save();
                ShowLogin("Token inválido o expirado.");
            }
        }
    }

    private void ShowLogin(string msg)
    {
        if (statusText) statusText.text = msg;
        if (menuManager && !string.IsNullOrEmpty(loginMenuName))
            menuManager.ShowMenu(loginMenuName);
    }

    private void ShowMain(string msg)
    {
        if (statusText) statusText.text = msg;
        if (menuManager && !string.IsNullOrEmpty(mainMenuName))
            menuManager.ShowMenu(mainMenuName);
    }
}
