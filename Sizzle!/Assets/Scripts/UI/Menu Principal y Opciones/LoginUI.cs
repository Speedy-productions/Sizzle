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

    void Start()
    {
        // Asegura que los inputs estén vacíos al iniciar el juego
        ClearAll();
    }

    void OnEnable()
    {
        // Asegura que si se activa el objeto, también se limpien
        ClearAll();
    }

    void ClearAll()
    {
        if (emailInput != null)
            emailInput.text = "";
        if (passwordInput != null)
            passwordInput.text = "";
    }

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
        //  Limpia al cambiar de menú también
        ClearAll();

        if (menuManager && !string.IsNullOrEmpty(registerMenuName))
            menuManager.ShowMenu(registerMenuName);
    }

    public void OnClickLoginWithGoogle()
    {
        AuthService.StartGoogleLogin((ok, err) =>
        {
            if (!ok)
            {
                Debug.LogError("[Login] Google: " + err);
                return;
            }

            if (menuManager && !string.IsNullOrEmpty(mainMenuName))
                menuManager.ShowMenu(mainMenuName);
        });
    }
}
