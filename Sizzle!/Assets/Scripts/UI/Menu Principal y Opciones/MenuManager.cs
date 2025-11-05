using System;
using UnityEngine;

public class MenuManager : MonoBehaviour
{
    [Header("Lista de menús (GameObjects raíz de cada pantalla)")]
    public GameObject[] menus;

    [Header("Inicio")]
    [Tooltip("Menú que se mostrará si al iniciar ninguno está activo. Normalmente 'Titulo'")]
    [SerializeField] private string defaultMenuName = "Titulo";

    [Tooltip("Si true, al iniciar activará defaultMenuName SOLO si ninguno está activo")]
    [SerializeField] private bool autoSelectDefaultIfNoneActive = true;

    void Start()
    {
        if (!autoSelectDefaultIfNoneActive) return;

        // ¿Hay alguno activo ya (porque lo dejaste activo en el editor o SessionManager lo activó)?
        bool anyActive = false;
        foreach (var m in menus)
        {
            if (m != null && m.activeSelf) { anyActive = true; break; }
        }

        // Si no hay ninguno activo, mostramos el "Titulo"
        if (!anyActive && !string.IsNullOrWhiteSpace(defaultMenuName))
        {
            ShowMenu(defaultMenuName);
        }
    }

    public void ShowMenu(string menuName)
    {
        if (menus == null || menus.Length == 0) return;
        if (string.IsNullOrWhiteSpace(menuName)) return;

        bool found = false;
        foreach (GameObject menu in menus)
        {
            if (menu == null) continue;

            bool isTarget = string.Equals(menu.name, menuName, StringComparison.OrdinalIgnoreCase);
            menu.SetActive(isTarget);
            if (isTarget) found = true;
        }

        if (!found)
        {
            Debug.LogWarning($"[MenuManager] Menú '{menuName}' no encontrado. Revisa el nombre en el inspector/escena.");
        }
    }

    public void HideAllMenus()
    {
        if (menus == null) return;
        foreach (GameObject menu in menus)
        {
            if (menu != null) menu.SetActive(false);
        }
    }
}
