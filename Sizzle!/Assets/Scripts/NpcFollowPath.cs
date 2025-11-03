using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcFollowPath : MonoBehaviour
{
    public bool IsWaitingForPlayer() => isWaitingForPlayer && !orderCompleted;

    [Header("Path Settings")]
    public List<Transform> normalPathPoints;      // puntos del camino normal
    public List<Transform> happyPathPoints;       // puntos del camino cuando está feliz
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float reachDistance = 0.5f;
    public float waitTimeAtPopup = 10f;

    [Header("Animation")]
    public Animator anim;
    public Transform NpcModel;

    [Header("Popup")]
    public PopupChar popupChar;
    public int popupAtPointIndex = 4; // donde espera al jugador

    [Header("NPC Hand")]
    public Transform handTransform;

    private Rigidbody rb;
    private Vector3 lastPosition;

    private Hamburguesa npcHamburguesa;
    private Order npcAssignedOrder = null;

    private bool isWaitingForPlayer = false;
    private bool hasShownPopup = false;
    private bool waitingCoroutineRunning = false;
    private bool orderCompleted = false;
    private bool usingHappyPath = false;

    private int currentPointIndex = 0;
    private List<Transform> activePath;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null) rb.freezeRotation = true;

        activePath = new List<Transform>(normalPathPoints);

        if (popupChar != null)
            popupChar.npcFollowPath = this;
    }

    private void FixedUpdate()
    {
        if (activePath == null || activePath.Count == 0) return;
        if (isWaitingForPlayer || hasShownPopup) return;

        Transform targetPoint = activePath[currentPointIndex];
        Vector3 direction = (targetPoint.position - transform.position).normalized;
        direction.y = 0f;

        Vector3 nextPos = Vector3.MoveTowards(transform.position, targetPoint.position, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(nextPos);

        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            NpcModel.rotation = Quaternion.Slerp(NpcModel.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }

        float speed = ((transform.position - lastPosition).magnitude) / Time.fixedDeltaTime;
        if (anim != null) anim.SetFloat("Speed", speed);
        lastPosition = transform.position;

        Vector3 flatNPC = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 flatTarget = new Vector3(targetPoint.position.x, 0, targetPoint.position.z);

        if (Vector3.Distance(flatNPC, flatTarget) < reachDistance)
        {
            if (!usingHappyPath && currentPointIndex == popupAtPointIndex && !hasShownPopup)
            {
                isWaitingForPlayer = true;
                if (anim != null) anim.SetFloat("Speed", 0f);
                Debug.Log("[NPC] Esperando al jugador en la caja...");
            }
            else
            {
                MoveToNextPoint();
            }
        }
    }

    // ===================== INTERACCIÓN =====================
    public void SetHamburguesaEnMano(Hamburguesa hamburguesa)
    {
        if (handTransform == null || hamburguesa == null) return;

        hamburguesa.transform.SetParent(handTransform);
        hamburguesa.transform.localPosition = Vector3.zero;
        hamburguesa.transform.localRotation = Quaternion.identity;

        npcHamburguesa = hamburguesa;
        Debug.Log("[NPC] Hamburguesa recibida y colocada en la mano.");

        if (npcAssignedOrder != null)
        {
            orderCompleted = true;
            isWaitingForPlayer = false;
            hasShownPopup = false;

            // ? Mostrar popup feliz y luego esperar a que se cierre antes de moverse
            if (popupChar != null)
            {
                popupChar.MostrarCaraFeliz("¡Bien hecho!");
                StartCoroutine(WaitAndSwitchToHappyPath(4f)); // espera 4 segundos antes de moverse
            }

            Debug.Log("[NPC] Pedido completado, esperando a que desaparezca el popup feliz para cambiar al camino 'feliz'.");
        }
    }

    // ? Nueva corrutina: espera al popup feliz antes de moverse
    private IEnumerator WaitAndSwitchToHappyPath(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        SwitchToHappyPath();
    }

    private void SwitchToHappyPath()
    {
        if (happyPathPoints == null || happyPathPoints.Count == 0)
        {
            Debug.LogWarning("[NPC] No hay puntos configurados para el camino feliz.");
            return;
        }

        activePath = new List<Transform>(happyPathPoints);
        currentPointIndex = 0;
        usingHappyPath = true;

        StopAllCoroutines();
        waitingCoroutineRunning = false;
        hasShownPopup = false;
        isWaitingForPlayer = false;

        Debug.Log("[NPC] Iniciando camino feliz...");
    }

    // ===================== EVENTOS DEL POPUP =====================
    public void OnPopupClosed()
    {
        if (waitingCoroutineRunning) return;
        StartCoroutine(WaitAfterPopup());
    }

    public void OnPlayerInteracted()
    {
        if (isWaitingForPlayer && !hasShownPopup)
            ShowPopup();
    }

    public void CambiarEstadoNpcGrosero()
    {
        popupChar?.CambiarEstadoNpcGrosero();
    }

    // ===================== POPUP =====================
    private void ShowPopup()
    {
        if (popupChar != null)
        {
            popupChar.npcFollowPath = this;
            popupChar.ShowPopup();
            Debug.Log("[NPC] Popup mostrado.");
        }
        else
        {
            Debug.LogWarning("[NPC] Falta referencia al PopupChar.");
        }

        hasShownPopup = true;
        isWaitingForPlayer = false;
        if (anim != null) anim.SetFloat("Speed", 0f);
    }

    private IEnumerator WaitAfterPopup()
    {
        waitingCoroutineRunning = true;
        if (anim != null) anim.SetFloat("Speed", 0f);
        Debug.Log("[NPC] Esperando " + waitTimeAtPopup + "s tras cerrar popup.");
        yield return new WaitForSeconds(waitTimeAtPopup);
        MoveToNextPoint();
        hasShownPopup = false;
        waitingCoroutineRunning = false;
    }

    private void MoveToNextPoint()
    {
        currentPointIndex++;
        if (currentPointIndex >= activePath.Count)
        {
            if (usingHappyPath)
            {
                if (anim != null) anim.SetFloat("Speed", 0f);
                Debug.Log("[NPC] Llegó a su destino final feliz. Se queda aquí.");
                enabled = false;
                return;
            }

            currentPointIndex = 0; // bucle normal
        }
    }

    // ===================== PEDIDO =====================
    public void AssignNpcOrder(Order order)
    {
        if (npcAssignedOrder == null)
            npcAssignedOrder = order;
    }

    public Order GetAssignedOrder()
    {
        return npcAssignedOrder;
    }
}
