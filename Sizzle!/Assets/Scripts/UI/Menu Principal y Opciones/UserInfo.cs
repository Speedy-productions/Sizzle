using UnityEngine;
using TMPro;

public class UserInfoUI : MonoBehaviour
{
    [Header("Referencia al texto donde se muestra el nombre")]
    public TMP_Text userText;

    [Tooltip("Texto por defecto si no hay nombre guardado")]
    public string fallbackName = "Invitado";

    void Reset()
    {
        if (!userText) userText = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        string name = PlayerPrefs.GetString("user_name", fallbackName);
        if (userText) userText.text = name;
    }
}
