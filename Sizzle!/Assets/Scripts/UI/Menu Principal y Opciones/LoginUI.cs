// Assets/Scripts/UI/LoginUI.cs
using UnityEngine;
using TMPro;
using Sizzle.Auth;

public class LoginUI : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField emailInput;
    public TMP_InputField passwordInput;

    [Header("Menus")]
    public MenuManager menuManager;
    public string mainMenuName = "MenuPrincipal";
    public string registerMenuName = "Registrarse"; // <- nombre del panel/menú de tu UI

    void Awake()
    {
        AuthService.Init(this);
    }

    public void OnClickLogin()
    {
        var user = emailInput ? emailInput.text.Trim() : "";
        var pass = passwordInput ? passwordInput.text : "";

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            Debug.LogWarning("[Login] Faltan datos");
            return;
        }

        AuthService.ValidateCredentials(user, pass, (ok, err) =>
        {
            if (!ok) { Debug.LogError("[Login] " + err); return; }
            if (menuManager && !string.IsNullOrEmpty(mainMenuName))
                menuManager.ShowMenu(mainMenuName);
        });
    }

    public void OnClickGoToRegister()
    {
        if (menuManager && !string.IsNullOrEmpty(registerMenuName))
            menuManager.ShowMenu(registerMenuName);
    }
}
