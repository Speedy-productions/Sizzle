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

    void Awake()
    {
        Debug.Log($"[RegisterUI:{GetInstanceID()}] Awake. " +
                  $"username={Has(usernameInput)} email={Has(emailInput)} " +
                  $"pass={Has(passwordInput)} rep={Has(repeatPasswordInput)}");
    }

    string Has(UnityEngine.Object o) => o ? "OK" : "NULL";

    [ContextMenu("Dump bindings")]
    void DumpBindings()
    {
        Debug.Log($"[RegisterUI:{GetInstanceID()}] username={usernameInput?.name} " +
                  $"email={emailInput?.name} pass={passwordInput?.name} " +
                  $"rep={repeatPasswordInput?.name} menu={menuManager?.name}");
    }

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
        else
            Debug.LogWarning("[RegisterUI] MenuManager o loginMenuName no configurados.");
    }

    public void OnClickRegister()
    {
        if (!usernameInput || !emailInput || !passwordInput || !repeatPasswordInput)
        {
            Debug.LogError($"[RegisterUI:{GetInstanceID()}] Referencias NULL. ¿El botón apunta a este objeto?");
            DumpBindings();
            return;
        }

        var user = usernameInput.text?.Trim() ?? "";
        var mail = emailInput.text?.Trim() ?? "";
        var pass = passwordInput.text ?? "";
        var rep = repeatPasswordInput.text ?? "";

        if (string.IsNullOrWhiteSpace(user) ||
            string.IsNullOrWhiteSpace(mail) ||
            string.IsNullOrEmpty(pass) ||
            string.IsNullOrEmpty(rep))
        {
            Debug.LogWarning("[RegisterUI] Faltan campos");
            return;
        }
        if (pass != rep)
        {
            Debug.LogWarning("[RegisterUI] Las contraseñas no coinciden");
            return;
        }

        AuthService.Register(user, mail, pass, (ok, err) =>
        {
            if (!ok) { Debug.LogError("[RegisterUI] " + err); return; }
            ClearAll();
            OnClickGoLogin();
        });
    }
}
