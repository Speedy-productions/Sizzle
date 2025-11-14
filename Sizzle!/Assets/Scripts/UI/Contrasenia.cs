using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class Contrasenia : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField correoInput;     // assign in Inspector
    public GameObject popupExito;      // success popup (Canvas)
    public GameObject popupError;      // error popup (Canvas)

    private string apiUrl = "https://serversizzle.onrender.com/contrasenia/send-mail";

    // Called when the "Restablecer Contraseña" button is pressed
    public void RestablecerContraseña()
    {
        string correo = correoInput.text.Trim().ToLower(); ;
        Debug.Log($"Correo ingresado: '{correo}' (length: {correo.Length})");

        if (string.IsNullOrEmpty(correo))
        {
            Debug.LogWarning("Email field is empty.");
            MostrarPopupError("Por favor, introduce un correo válido.");
            return;
        }

        StartCoroutine(EnviarSolicitudCorreo(correo));
    }

    private IEnumerator EnviarSolicitudCorreo(string correo)
    {
        // Create JSON body
        string jsonBody = "{\"email\":\"" + correo + "\"}";

        Debug.Log("📤 Sending JSON: " + jsonBody);

        // Prepare request
        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Send the request
        yield return request.SendWebRequest();

        // Handle response
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Email sent successfully: " + request.downloadHandler.text);
            MostrarPopupExito("Correo enviado para restablecer contraseña.");
        }
        else
        {
            Debug.LogError($"❌ Error: {request.responseCode} - {request.error}\n{request.downloadHandler.text}");

            // Handle backend message
            if (request.downloadHandler.text.Contains("no encontrado") || request.responseCode == 404)
            {
                MostrarPopupError("Correo no encontrado. Intenta nuevamente.");
            }
            else
            {
                MostrarPopupError("Ocurrió un error al enviar el correo.");
            }
        }
    }

    private void MostrarPopupExito(string mensaje)
    {
        popupExito.SetActive(true);
        popupError.SetActive(false);
        Debug.Log(mensaje);
    }

    private void MostrarPopupError(string mensaje)
    {
        popupError.SetActive(true);
        popupExito.SetActive(false);
        Debug.Log(mensaje);
    }

    [System.Serializable]
    private class CorreoRequest
    {
        public string correo;
    }
}
