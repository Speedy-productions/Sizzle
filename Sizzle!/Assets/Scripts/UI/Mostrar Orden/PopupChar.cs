using UnityEngine;
using System.Collections;
using Photon.Pun;

[System.Serializable]
public class FoodGroup
{
    public string name;
    public Sprite[] variants;
}

[System.Serializable]
public class BubbleGroup
{
    public string name;
    public Sprite[] variants;
}

public class PopupChar : MonoBehaviourPun
{
    [Header("Prefabs y Sprites")]
    public Popup popupPrefab;
    public FoodGroup[] foodGroups;
    public BubbleGroup[] bubbleGroups;

    [Header("Caras del NPC")]
    public Sprite smileyFace;
    public Sprite angryFace;

    [Header("Configuración")]
    public float switchInterval = 1f;
    public float popupDuration = 5f;

    [HideInInspector] public NpcFollowPath npcFollowPath;

    private Popup currentPopup;
    private Popup facePopup;

    private bool notifyCoroutineRunning = false;

    // ORDEN FIJA DEL NPC (se sincroniza en multiplayer)
    private Order npcOrder;


    // ============================================================
    //  GENERACIÓN / SINCRONIZACIÓN DE ORDEN
    // ============================================================

    private Order GenerateNpcOrderLocal()
    {
        // Si ya hay una orden previa en npcFollowPath ? usarla
        if (npcFollowPath != null && npcFollowPath.GetAssignedOrder() != null)
        {
            npcOrder = npcFollowPath.GetAssignedOrder();
            if (OrderManager.Instance != null)
                OrderManager.Instance.SetCurrentOrder(npcOrder);
            return npcOrder;
        }

        // Crear nueva orden
        Order newOrder =
            OrderManager.Instance != null ?
            OrderManager.Instance.GenerateHamburgerOrder() :
            null;

        npcOrder = newOrder;

        if (npcFollowPath != null && newOrder != null)
            npcFollowPath.AssignNpcOrder(newOrder);

        if (OrderManager.Instance != null)
            OrderManager.Instance.SetCurrentOrder(newOrder);

        return newOrder;
    }


    // ============================================================
    //  MOSTRAR POPUP PRINCIPAL
    // ============================================================

    public void ShowPopup()
    {
        if (popupPrefab == null) return;

        if (currentPopup != null)
            Destroy(currentPopup.gameObject);

        currentPopup = Instantiate(
            popupPrefab,
            transform.position + Vector3.up * 2f,
            Quaternion.identity
        );

        Destroy(currentPopup.gameObject, popupDuration);

        bool mp = PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode;

        if (!mp)
        {
            // SINGLE PLAYER
            GenerateNpcOrderLocal();
            StartCoroutine(AlternateSpritesLimitedTime(popupDuration));
        }
        else
        {
            // MULTIPLAYER
            if (PhotonNetwork.IsMasterClient)
            {
                // Host genera la orden
                Order o = GenerateNpcOrderLocal();

                if (o != null)
                {
                    photonView.RPC(nameof(RPC_ReceiveOrder),
                        RpcTarget.Others,
                        o.ingredients);
                }

                StartCoroutine(AlternateSpritesLimitedTime(popupDuration));
            }
            else
            {
                // Clientes esperan al RPC_ReceiveOrder para iniciar popup
            }
        }

        if (!notifyCoroutineRunning)
            StartCoroutine(NotifyNpcAfterPopup());
    }

    [PunRPC]
    private void RPC_ReceiveOrder(string[] ingredients)
    {
        // Crear orden local en cliente
        npcOrder = new Order(ingredients);

        // Guardarla en npcFollowPath
        if (npcFollowPath != null)
            npcFollowPath.AssignNpcOrder(npcOrder);

        // Actualizar OrderManager en cliente
        if (OrderManager.Instance != null)
            OrderManager.Instance.SetCurrentOrder(npcOrder);

        // Si no existe popup, instanciarlo
        if (currentPopup == null && popupPrefab != null)
        {
            currentPopup = Instantiate(
                popupPrefab,
                transform.position + Vector3.up * 2f,
                Quaternion.identity
            );

            Destroy(currentPopup.gameObject, popupDuration);
        }

        StartCoroutine(AlternateSpritesLimitedTime(popupDuration));
    }


    // ============================================================
    //  NOTIFICAR CUANDO TERMINA EL POPUP
    // ============================================================

    private IEnumerator NotifyNpcAfterPopup()
    {
        notifyCoroutineRunning = true;
        yield return new WaitForSeconds(popupDuration);

        npcFollowPath?.OnPopupClosed();
        notifyCoroutineRunning = false;
    }


    // ============================================================
    //  ALTERNAR SPRITES (PEDIDO)
    // ============================================================

    private IEnumerator AlternateSpritesLimitedTime(float duration)
    {
        if (currentPopup == null)
            yield break;

        float end = Time.time + duration;

        // asegurar que npcOrder está seteada
        if (npcOrder == null && npcFollowPath != null)
            npcOrder = npcFollowPath.GetAssignedOrder();

        while (Time.time < end)
        {
            if (npcOrder == null) yield break;

            Sprite bubble = GetRandomBubbleAnyGroup();

            foreach (var ingName in npcOrder.ingredients)
            {
                if (Time.time >= end) break;

                Sprite food = GetSpriteByName(ingName);
                currentPopup.Show(transform, food, bubble, null);

                yield return new WaitForSeconds(switchInterval);
            }
        }

        currentPopup = null;
    }


    // ============================================================
    //  CARAS (Feliz / Enojado)
    // ============================================================

    public void MostrarCaraFeliz(string msg = "¡Bien hecho!")
    {
        Sprite bubble = GetBubbleByGroup("Calmado");
        MostrarCara(smileyFace, bubble, msg, 32f);
    }

    public void MostrarCaraMolesta(string msg = "¿Qué es esta $#*!?")
    {
        Sprite bubble = GetBubbleByGroup("Grosero");
        MostrarCara(angryFace, bubble, msg, 26f);
    }

    private void MostrarCara(Sprite face, Sprite bubble, string texto, float size)
    {
        if (popupPrefab == null || face == null) return;

        if (facePopup != null)
            Destroy(facePopup.gameObject);

        facePopup = Instantiate(
            popupPrefab,
            transform.position + Vector3.up * 2.5f,
            Quaternion.identity
        );

        facePopup.Show(transform, null, bubble, face, texto, size);
        StartCoroutine(HideFace());
    }

    private IEnumerator HideFace()
    {
        yield return new WaitForSeconds(4f);
        if (facePopup != null)
        {
            Destroy(facePopup.gameObject);
            facePopup = null;
        }
    }


    // ============================================================
    //  HELPERS
    // ============================================================

    private Sprite GetSpriteByName(string name)
    {
        foreach (var g in foodGroups)
        {
            if (!g.name.Equals(name)) continue;
            if (g.variants.Length > 0)
                return g.variants[Random.Range(0, g.variants.Length)];
        }
        return null;
    }

    private Sprite GetRandomBubbleAnyGroup()
    {
        if (bubbleGroups.Length == 0) return null;

        var g = bubbleGroups[Random.Range(0, bubbleGroups.Length)];
        if (g.variants.Length == 0) return null;

        return g.variants[Random.Range(0, g.variants.Length)];
    }

    private Sprite GetBubbleByGroup(string groupName)
    {
        foreach (var g in bubbleGroups)
        {
            if (!g.name.Equals(groupName)) continue;
            if (g.variants.Length > 0)
                return g.variants[Random.Range(0, g.variants.Length)];
        }
        return null;
    }
}
