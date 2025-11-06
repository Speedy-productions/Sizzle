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
        PlayerPrefs.DeleteKey("jwt_token");
        PlayerPrefs.DeleteKey("user_id");
        PlayerPrefs.DeleteKey("user_name");
        PlayerPrefs.DeleteKey("user_email");
        PlayerPrefs.Save();
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
        PlayerPrefs.DeleteKey("jwt_token");
        PlayerPrefs.DeleteKey("user_id");
        PlayerPrefs.DeleteKey("user_name");
        PlayerPrefs.DeleteKey("user_email");
        PlayerPrefs.Save();
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
