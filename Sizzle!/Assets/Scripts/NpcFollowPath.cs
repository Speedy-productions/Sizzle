using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Linq;

[RequireComponent(typeof(Rigidbody))]
public class NpcFollowPath : MonoBehaviourPunCallbacks, IPunObservable
{
    public enum NpcState
    {
        WalkingNormal,
        WaitingForPlayer,
        ShowingPopup,
        WaitingAfterPopup,
        WaitingBeforeHappyPath,
        WalkingHappy,
        Finished
    }

    [Header("Path Settings")]
    public List<Transform> normalPathPoints;
    public List<Transform> happyPathPoints;
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float reachDistance = 0.4f;
    public float waitTimeAfterPopup = 10f;

    [Header("Animation")]
    public Animator anim;
    public Transform npcModel;

    [Header("Popup")]
    public PopupChar popupChar;
    public int popupAtPointIndex = 4;

    [Header("NPC Tray Hold")]
    [Tooltip("Dónde se parenteará la bandeja final cuando la acepte el NPC.")]
    public Transform trayHoldTransform;

    private Rigidbody rb;

    private NpcState npcState = NpcState.WalkingNormal;
    private float stateTimer = 0f;
    private bool usingHappyPath = false;
    private int currentPoint = 0;
    private List<Transform> activePath;

    // ORDEN PROPIA DE ESTE NPC (para hamburguesa)
    private Order npcAssignedOrder = null;

    // Flags de finalización
    private bool orderCompleted = false;   // hamburguesa correcta
    private bool friesCompleted = false;   // papas presentes (si el pedido las requiere)
    private bool npcWantsFries = false;

    // Sync movimiento
    private Vector3 netPos;
    private Quaternion netRot;
    private float netSpeed;
    private bool firstSync = true;

    // Sync de "esperando jugador"
    private bool syncWaiting;

    // =====================================================================
    // API pública usada por Interact (nuevo flujo con Bandeja)
    // =====================================================================

    /// <summary>
    /// Intenta entregar una BandejaFinal a este NPC.
    /// En SP se valida local; en MP el cliente pide al Master validar.
    /// </summary>
    public void TryAcceptTray(BandejaFinal tray)
    {
        if (tray == null) return;

        bool mp = PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode;

        if (!mp)
        {
            // Singleplayer: valida y resuelve local
            var (ok, msg) = ValidateTray(tray);
            if (ok)
            {
                AttachTrayLocal(tray.gameObject);
                popupChar?.MostrarCaraFeliz("¡Pedido completo!");
                GoToHappyAfterDelay(4f);
            }
            else
            {
                popupChar?.MostrarCaraMolesta(msg);
            }
            return;
        }

        // Multiplayer: el Master decide
        PhotonView trayPV = tray.GetComponent<PhotonView>();
        if (!trayPV)
        {
            Debug.LogError("[NPC] La bandeja no tiene PhotonView en MULTIPLAYER.");
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
        {
            // Cliente pide al Master validar/adjuntar
            photonView.RPC(nameof(RPC_TryAcceptTray_Master), RpcTarget.MasterClient, trayPV.ViewID);
        }
        else
        {
            // El propio Master valida directamente
            RPC_TryAcceptTray_Master(trayPV.ViewID);
        }
    }

    // =====================================================================
    // VALIDACIÓN DE LA BANDEJA
    // =====================================================================

    private (bool ok, string message) ValidateTray(BandejaFinal tray)
    {
        // 1) Debe tener una orden asignada (para validar hamburguesa)
        if (npcAssignedOrder == null || npcAssignedOrder.ingredients == null || npcAssignedOrder.ingredients.Length == 0)
        {
            // Si no hay orden, no aceptamos nada (seguridad)
            return (false, "¡No he pedido nada aún!");
        }

        // 2) Validar hamburguesa
        var trayBurger = tray.ingredientesHamburguesa ?? new List<string>();
        bool burgerOk = CompareBurgerToOrder(trayBurger, npcAssignedOrder);

        if (!burgerOk)
        {
            return (false, "¡que es esta &$%!");
        }

        // 3) Validar papas si el NPC las quiere
        if (npcWantsFries && !tray.contienePapas)
        {
            return (false, "¡donde estan mis papas!");
        }

        return (true, "OK");
    }

    private bool CompareBurgerToOrder(List<string> burger, Order order)
    {
        if (order == null || order.ingredients == null) return false;

        // Normalizar, ordenar y comparar
        var a = burger.Select(NormalizarNombre).OrderBy(x => x).ToList();
        var b = order.ingredients.Select(NormalizarNombre).OrderBy(x => x).ToList();

        // Debug opcional
        // Debug.Log($"[NPC] TrayBurger: {string.Join(",", a)} | Order: {string.Join(",", b)}");

        if (a.Count != b.Count) return false;
        for (int i = 0; i < a.Count; i++)
            if (a[i] != b[i]) return false;

        return true;
    }

    private string NormalizarNombre(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        name = name.Replace("Sliced", "").Replace("Slice", "").Replace("Cortado", "");
        return name.Trim();
    }

    // =====================================================================
    // RPCs BANDEJA (MP)
    // =====================================================================

    [PunRPC]
    private void RPC_TryAcceptTray_Master(int trayViewID)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView trayPV = PhotonView.Find(trayViewID);
        if (!trayPV) return;

        BandejaFinal tray = trayPV.GetComponent<BandejaFinal>();
        if (!tray) return;

        var (ok, msg) = ValidateTray(tray);

        if (!ok)
        {
            // Notificar a todos el rechazo (cara molesta) con motivo
            photonView.RPC(nameof(RPC_TrayResult), RpcTarget.All, false, msg);
            return;
        }

        // Aceptado: adjuntar visualmente la bandeja en todos
        photonView.RPC(nameof(RPC_AttachTrayToNPC), RpcTarget.AllBuffered, trayViewID);

        // Marcar completado en Master y decidir final feliz
        orderCompleted = true;
        friesCompleted = !npcWantsFries || tray.contienePapas;
        photonView.RPC(nameof(RPC_TrayResult), RpcTarget.All, true, "¡Pedido completo!");
    }

    [PunRPC]
    private void RPC_AttachTrayToNPC(int trayViewID)
    {
        PhotonView trayPV = PhotonView.Find(trayViewID);
        if (!trayPV) return;

        Transform t = trayPV.transform;

        // Desparentar de mano del jugador (si aplica) y parentear al NPC
        t.SetParent(null);

        Transform hold = trayHoldTransform != null ? trayHoldTransform : npcModel;
        t.SetParent(hold);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

        // Físicas seguras
        Rigidbody rbT = t.GetComponent<Rigidbody>();
        if (rbT)
        {
            rbT.isKinematic = true;
            rbT.useGravity = false;
            rbT.linearVelocity = Vector3.zero;
            rbT.angularVelocity = Vector3.zero;
        }

        foreach (Collider c in t.GetComponentsInChildren<Collider>(true))
            c.isTrigger = true;
    }

    [PunRPC]
    private void RPC_TrayResult(bool ok, string msg)
    {
        if (ok)
        {
            popupChar?.MostrarCaraFeliz(msg);
            // Entrar a la ruta feliz tras un pequeño delay
            GoToHappyAfterDelay(4f);
        }
        else
        {
            popupChar?.MostrarCaraMolesta(msg);
        }
    }

    // =====================================================================
    // LÓGICA DE MOVIMIENTO / POPUP (igual que antes)
    // =====================================================================

    private void GoToHappyAfterDelay(float seconds)
    {
        npcState = NpcState.WaitingBeforeHappyPath;
        stateTimer = seconds;
    }

    public bool IsWaitingForPlayer()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
        {
            // Con bandeja, seguimos usando esta lógica de “esperando”
            if (npcWantsFries)
                return npcState == NpcState.WaitingForPlayer && !(orderCompleted && friesCompleted);
            else
                return npcState == NpcState.WaitingForPlayer && !orderCompleted;
        }

        return syncWaiting;
    }

    public void AssignNpcOrder(Order order)
    {
        if (order == null) return;
        npcAssignedOrder = order;
        orderCompleted = false;
        friesCompleted = false;
    }

    public Order GetAssignedOrder() => npcAssignedOrder;

    [PunRPC] // (lo dejamos por compat con quien genere la orden)
    void RPC_SetNpcOrder(string[] ingredients)
    {
        npcAssignedOrder = new Order(ingredients);
        orderCompleted = false;
        friesCompleted = false;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        activePath = new List<Transform>(normalPathPoints);

        if (popupChar != null)
            popupChar.npcFollowPath = this;

        // En multiplayer, solo el Master usa física
        if (PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode)
        {
            if (!PhotonNetwork.IsMasterClient)
                rb.isKinematic = true;
        }

        netPos = transform.position;
        netRot = npcModel.rotation;
    }

    private void FixedUpdate()
    {
        bool mp = PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode;

        if (!mp)
        {
            TickLogic(Time.fixedDeltaTime);
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            TickLogic(Time.fixedDeltaTime);
        }
        else
        {
            // Interpolación en clientes
            transform.position = Vector3.MoveTowards(
                transform.position,
                netPos,
                moveSpeed * Time.fixedDeltaTime * 1.25f
            );

            npcModel.rotation = Quaternion.Slerp(
                npcModel.rotation,
                netRot,
                rotationSpeed * Time.fixedDeltaTime * 1.25f
            );

            anim.SetFloat("Speed", netSpeed);
        }
    }

    private void TickLogic(float dt)
    {
        switch (npcState)
        {
            case NpcState.WalkingNormal:
            case NpcState.WalkingHappy:
                TickWalking(dt);
                break;

            case NpcState.WaitingForPlayer:
                anim.SetFloat("Speed", 0f);
                netSpeed = 0f;
                break;

            case NpcState.ShowingPopup:
                stateTimer -= dt;
                if (stateTimer <= 0f)
                {
                    npcState = NpcState.WaitingAfterPopup;
                    stateTimer = waitTimeAfterPopup;
                }
                break;

            case NpcState.WaitingAfterPopup:
                stateTimer -= dt;
                if (stateTimer <= 0f)
                {
                    AdvancePoint();
                    npcState = usingHappyPath ? NpcState.WalkingHappy : NpcState.WalkingNormal;
                }
                break;

            case NpcState.WaitingBeforeHappyPath:
                stateTimer -= dt;
                if (stateTimer <= 0f)
                {
                    usingHappyPath = true;
                    activePath = new List<Transform>(happyPathPoints);
                    currentPoint = 0;
                    npcState = NpcState.WalkingHappy;
                }
                break;

            case NpcState.Finished:
                anim.SetFloat("Speed", 0f);
                netSpeed = 0f;
                break;
        }

        // “Esperando” = aún no cumple (si quiere papas, ambas condiciones)
        syncWaiting = (npcState == NpcState.WaitingForPlayer && !(orderCompleted && (npcWantsFries ? friesCompleted : true)));
    }

    private void TickWalking(float dt)
    {
        if (activePath == null || activePath.Count == 0) return;
        if (npcState == NpcState.Finished) return;

        Transform target = activePath[currentPoint];

        Vector3 direction = target.position - transform.position;
        Vector3 flatDir = new Vector3(direction.x, 0, direction.z).normalized;

        Vector3 nextPos = Vector3.MoveTowards(
            transform.position,
            target.position,
            moveSpeed * dt
        );

        if (rb != null)
            rb.MovePosition(nextPos);
        else
            transform.position = nextPos;

        if (flatDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatDir);
            npcModel.rotation = Quaternion.Slerp(
                npcModel.rotation,
                targetRot,
                rotationSpeed * dt
            );
        }

        float speed = (nextPos - transform.position).magnitude / dt;
        anim.SetFloat("Speed", speed);
        netSpeed = speed;

        Vector3 flatA = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 flatB = new Vector3(target.position.x, 0, target.position.z);

        if (Vector3.Distance(flatA, flatB) < reachDistance)
        {
            // Solo el Master decide cambios de estado
            bool mp = PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode;
            if (mp && !PhotonNetwork.IsMasterClient)
                return;

            if (!usingHappyPath && currentPoint == popupAtPointIndex)
            {
                npcState = NpcState.WaitingForPlayer;
                anim.SetFloat("Speed", 0f);
                netSpeed = 0f;
                // La orden se genera al interactuar por primera vez (como antes).
            }
            else
            {
                AdvancePoint();
            }
        }
    }

    private void AdvancePoint()
    {
        currentPoint++;

        if (currentPoint >= activePath.Count)
        {
            if (usingHappyPath)
            {
                npcState = NpcState.Finished;
                anim.SetFloat("Speed", 0f);
                netSpeed = 0f;
                return;
            }

            currentPoint = 0;
        }
    }

    // POPUP & INTERACCIÓN (idéntico para mostrar pedido)
    public void OnPlayerInteracted()
    {
        GetComponent<NpcAudio>()?.PlayTalk();

        if (!IsWaitingForPlayer())
            return;

        bool mp = PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode;

        if (!mp)
        {
            if (npcAssignedOrder == null && OrderManager.Instance != null)
            {
                npcAssignedOrder = OrderManager.Instance.GenerateHamburgerOrder();
                npcWantsFries = OrderManager.Instance.CurrentFriesOrder != null;
            }

            popupChar?.ShowPopup();
            npcState = NpcState.ShowingPopup;
            stateTimer = popupChar != null ? popupChar.popupDuration : waitTimeAfterPopup;
            return;
        }

        if (npcAssignedOrder == null && OrderManager.Instance != null)
        {
            npcAssignedOrder = OrderManager.Instance.GenerateHamburgerOrder();
            npcWantsFries = OrderManager.Instance.CurrentFriesOrder != null;
        }

        if (npcAssignedOrder == null)
        {
            Debug.LogError("[NpcFollowPath] No se pudo generar la orden del NPC al interactuar.");
            return;
        }

        photonView.RPC(
            nameof(RPC_StartPopupWithOrder),
            RpcTarget.AllBuffered,
            npcAssignedOrder.ingredients,
            npcWantsFries
        );
    }

    [PunRPC]
    private void RPC_StartPopupWithOrder(string[] ingredients, bool wantsFries)
    {
        npcAssignedOrder = new Order(ingredients);
        npcWantsFries = wantsFries;

        popupChar?.ShowPopup();

        npcState = NpcState.ShowingPopup;
        stateTimer = popupChar != null ? popupChar.popupDuration : waitTimeAfterPopup;
    }

    // Adjuntar local en SP
    private void AttachTrayLocal(GameObject trayGO)
    {
        if (!trayGO) return;

        Transform hold = trayHoldTransform != null ? trayHoldTransform : npcModel;

        trayGO.transform.SetParent(hold);
        trayGO.transform.localPosition = Vector3.zero;
        trayGO.transform.localRotation = Quaternion.identity;

        var rb = trayGO.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        foreach (var c in trayGO.GetComponentsInChildren<Collider>(true))
            c.isTrigger = true;

        // Marcar completado según bandeja
        var final = trayGO.GetComponent<BandejaFinal>();
        orderCompleted = true;
        friesCompleted = !npcWantsFries || (final != null && final.contienePapas);
    }

    // MASTER SWITCH y SYNC de movimiento (igual que antes)
    public override void OnMasterClientSwitched(Player newMaster)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        rb.isKinematic = false;

        currentPoint = FindNextPointInDirection();

        if (npcState != NpcState.Finished)
        {
            npcState = usingHappyPath ? NpcState.WalkingHappy : NpcState.WalkingNormal;
            stateTimer = 0f;
        }
    }

    private int FindNextPointInDirection()
    {
        int bestIndex = 0;
        float bestAngle = 999f;

        Vector3 pos = transform.position;
        Vector3 fwd = npcModel.forward;

        for (int i = 0; i < activePath.Count; i++)
        {
            Vector3 dir = (activePath[i].position - pos).normalized;
            float angle = Vector3.Angle(fwd, dir);

            if (angle < bestAngle)
            {
                bestAngle = angle;
                bestIndex = i;
            }
        }

        return bestIndex;
    }
// Lo llama PopupChar cuando termina de mostrar el popup de la orden
public void OnPopupClosed()
{
    if (npcState == NpcState.ShowingPopup)
    {
        npcState = NpcState.WaitingAfterPopup;
        stateTimer = waitTimeAfterPopup;
    }
}
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        bool mp = PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode;
        if (!mp) return;

        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(npcModel.rotation);
            stream.SendNext(netSpeed);
            stream.SendNext(syncWaiting);
        }
        else
        {
            netPos = (Vector3)stream.ReceiveNext();
            netRot = (Quaternion)stream.ReceiveNext();
            netSpeed = (float)stream.ReceiveNext();
            syncWaiting = (bool)stream.ReceiveNext();

            if (firstSync)
            {
                firstSync = false;
                transform.position = netPos;
                npcModel.rotation = netRot;
            }
        }
    }
}
