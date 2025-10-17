// -----------------------------------------------
// RegisterUI.cs
// Resumen:
// - Recoge username, email y contraseña, valida básico en cliente,
//   y llama a AuthService.Register (HTTPS).
// - Si ok, limpia inputs y vuelve a la pantalla de login.
// -----------------------------------------------
using UnityEngine;
using TMPro;
using Sizzle.Auth;

public class RegisterUI : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] TMP_InputField usernameInput;
    [SerializeField] TMP_InputField emailInput;
    [SerializeField] TMP_InputField passwordInput;
    [SerializeField] TMP_InputField repeatPasswordInput;

    [Header("Navegación")]
    [SerializeField] MenuManager menuManager;
    [SerializeField] string loginMenuName = "Login";

    void OnEnable() => ClearAll();

    void ClearAll()
    {
        usernameInput?.SetTextWithoutNotify("");
        emailInput?.SetTextWithoutNotify("");
        passwordInput?.SetTextWithoutNotify("");
        repeatPasswordInput?.SetTextWithoutNotify("");
    }

    public void OnClickGoLogin()
    {
        ClearAll();
        if (menuManager && !string.IsNullOrEmpty(loginMenuName))
            menuManager.ShowMenu(loginMenuName);
    }

    public void OnClickRegister()
    {
        if (!usernameInput || !emailInput || !passwordInput || !repeatPasswordInput)
            return;

        var user = usernameInput.text?.Trim() ?? "";
        var mail = emailInput.text?.Trim() ?? "";
        var pass = passwordInput.text ?? "";
        var rep = repeatPasswordInput.text ?? "";

        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(mail) ||
            string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(rep)) return;

        if (pass != rep) return;

        AuthService.Register(user, mail, pass, (ok, _err) =>
        {
            if (!ok) return;
            ClearAll();
            OnClickGoLogin();
        });
    }
}
