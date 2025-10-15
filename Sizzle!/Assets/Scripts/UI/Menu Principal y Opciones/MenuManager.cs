using UnityEngine;

public class MenuManager : MonoBehaviour
{
    public GameObject[] menus;

    public void ShowMenu(string menuName)
    {
        foreach (GameObject menu in menus)
        {
            if (menu != null)
                menu.SetActive(menu.name == menuName);
        }
    }

    public void HideAllMenus()
    {
        foreach (GameObject menu in menus)
        {
            if (menu != null)
                menu.SetActive(false);
        }
    }
}
