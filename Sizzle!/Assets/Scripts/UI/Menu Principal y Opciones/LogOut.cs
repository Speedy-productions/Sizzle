using UnityEngine;

public class LogoutButton : MonoBehaviour
{
    public MenuManager menuManager;
    public string loginMenuName = "Login";

    public void OnClickLogout()
    {
        PlayerPrefs.DeleteKey("jwt_token");
        PlayerPrefs.DeleteKey("user_id");
        PlayerPrefs.DeleteKey("user_name");
        PlayerPrefs.DeleteKey("user_email");
        PlayerPrefs.Save();
        AuthEvents.RaiseSessionChanged();
        if (menuManager && !string.IsNullOrEmpty(loginMenuName))
            menuManager.ShowMenu(loginMenuName);
    }
}
