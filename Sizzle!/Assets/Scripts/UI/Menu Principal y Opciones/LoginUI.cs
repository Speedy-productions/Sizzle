// -----------------------------------------------
// LoginUI.cs
// Resumen:
// - Lee usuario/email + contraseña y llama a AuthService (que usa HTTPS).
// - Si ok, navega al menú principal.
// -----------------------------------------------
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
    public string registerMenuName = "Registrarse";

    void Awake() { AuthService.Init(this); }

    public void OnClickLogin()
    {
        var user = emailInput ? emailInput.text.Trim() : "";
        var pass = passwordInput ? passwordInput.text : "";

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass)) return;

        AuthService.ValidateCredentials(user, pass, (ok, err) =>
        {
            if (!ok) return;
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
