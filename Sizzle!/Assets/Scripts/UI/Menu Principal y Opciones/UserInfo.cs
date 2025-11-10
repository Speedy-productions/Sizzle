using UnityEngine;
using TMPro;

public class UserInfoUI : MonoBehaviour
{
    public TMP_Text userText;
    public string fallbackName = "Invitado";

    void Reset()
    {
        if (!userText) userText = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        Refresh();
        // Se suscribe al evento que se dispara al cambiar de sesión
        AuthEvents.OnSessionChanged += Refresh;
    }

    void OnDisable()
    {
        // Limpia la suscripción para evitar memory leaks
        AuthEvents.OnSessionChanged -= Refresh;
    }

    // Actualiza el texto con el usuario actual
    public void Refresh()
    {
        string name = PlayerPrefs.GetString("user_name", fallbackName);
        if (userText) userText.text = name;
    }
}
