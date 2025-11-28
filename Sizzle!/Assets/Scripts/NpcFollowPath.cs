
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

[RequireComponent(typeof(Rigidbody))]
public class NpcFollowPath : MonoBehaviourPunCallbacks, IPunObservable, IPunInstantiateMagicCallback
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

    [Header("Lifetime")]
[Tooltip("Tiempo máximo que el NPC permanece vivo antes de destruirse (solo Master cuenta).")]
public float aliveTime = 90f;
[Tooltip("Retardo al terminar por ruta feliz antes de destruir (decorativo).")]
public float destroyGraceAfterHappy = 2f;

private float aliveLeft;

    [Header("Path Settings")]
    public List<Transform> normalPathPoints;
    public List<Transform> happyPathPoints;
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float reachDistance = 0.4f;
    public float waitTimeAfterPopup = 10f;
    [Tooltip("Índice del punto del camino normal donde se detiene a pedir.")]
    public int popupAtPointIndex = 4;

    [Header("Animation")]
    public Animator anim;
    public Transform npcModel;

    [Header("Popup")]
    public PopupChar popupChar;

    [Header("NPC Tray Hold")]
    [Tooltip("Dónde se parenteará la bandeja final cuando la acepte el NPC.")]
    public Transform trayHoldTransform;

    // --- Runtime ---
    private Rigidbody rb;
    private NpcState npcState = NpcState.WalkingNormal;
    private float stateTimer = 0f;
    private bool usingHappyPath = false;
    private int currentPoint = 0;
    private List<Transform> activePath;

    // Orden del NPC
    private Order npcAssignedOrder = null;
    private bool orderCompleted = false; // hamburguesa correcta
    private bool friesCompleted = false; // papas presentes (si la pide)
    private bool npcWantsFries = false;

    // Sync movimiento
    private Vector3 netPos;
    private Quaternion netRot;
    private float netSpeed;
    private bool firstSync = true;
    private bool syncWaiting;

    // --- Wait spot (punto exclusivo para esperar en la caja) ---
    private int waitIndex = -1;
    private Transform waitSpot;
    private bool headingToWaitSpot = false;
    private bool _despawned = false;

    private bool appliedAltWaitYaw = false;
private const float ALT_WAIT_YAW = 90f;


    // =====================================================================
    //   INSTANTIACIÓN (lee waitIndex del Spawner)
    // =====================================================================
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] data = info.photonView?.InstantiationData;
        if (data != null && data.Length > 0 && data[0] is int idx)
        {
            waitIndex = idx;
        }
        else
        {
            // Fallback: si no llegó índice y somos Master, intentamos reservar uno
            if (PhotonNetwork.IsMasterClient)
                waitIndex = WaitSpotRegistry.MasterTryReserveAny();
        }

        waitSpot = WaitSpotRegistry.Get(waitIndex);
    }

    // =====================================================================
    //   API de BANDEJA
    // =====================================================================
    public void TryAcceptTray(BandejaFinal tray)
    {
        if (tray == null) return;

        bool mp = PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode;

        if (!mp)
        {
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

        PhotonView trayPV = tray.GetComponent<PhotonView>();
        if (!trayPV)
        {
            Debug.LogError("[NPC] La bandeja no tiene PhotonView en MULTIPLAYER.");
            return;
        }

        if (!PhotonNetwork.IsMasterClient)
            photonView.RPC(nameof(RPC_TryAcceptTray_Master), RpcTarget.MasterClient, trayPV.ViewID);
        else
            RPC_TryAcceptTray_Master(trayPV.ViewID);
    }

    private (bool ok, string message) ValidateTray(BandejaFinal tray)
    {
        if (npcAssignedOrder == null || npcAssignedOrder.ingredients == null || npcAssignedOrder.ingredients.Length == 0)
            return (false, "¡No he pedido nada aún!");

        var trayBurger = tray.ingredientesHamburguesa ?? new List<string>();
        bool burgerOk = CompareBurgerToOrder(trayBurger, npcAssignedOrder);
        if (!burgerOk) return (false, "¡que es esta &$%!");

        if (npcWantsFries && !tray.contienePapas)
            return (false, "¡donde estan mis papas!");

        return (true, "OK");
    }

    private bool CompareBurgerToOrder(List<string> burger, Order order)
    {
        if (order == null || order.ingredients == null) return false;

        var a = burger.Select(NormalizarNombre).OrderBy(x => x).ToList();
        var b = order.ingredients.Select(NormalizarNombre).OrderBy(x => x).ToList();

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
            photonView.RPC(nameof(RPC_TrayResult), RpcTarget.All, false, msg);
            return;
        }

        photonView.RPC(nameof(RPC_AttachTrayToNPC), RpcTarget.AllBuffered, trayViewID);

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
        t.SetParent(null);

        Transform hold = trayHoldTransform != null ? trayHoldTransform : npcModel;
        t.SetParent(hold);
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;

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
            GoToHappyAfterDelay(4f);
        }
        else
        {
            popupChar?.MostrarCaraMolesta(msg);
        }
    }

    // =====================================================================
    //   ESTADOS / MOVIMIENTO
    // =====================================================================
    private void GoToHappyAfterDelay(float seconds)
    {
        npcState = NpcState.WaitingBeforeHappyPath;
        stateTimer = seconds;

        ResetAltWaitYawIfNeeded();
        // Al abandonar la cola: liberar el wait spot (solo Master)
        if (PhotonNetwork.IsMasterClient && waitIndex >= 0)
        {
            WaitSpotRegistry.MasterRelease(waitIndex);
            waitIndex = -1;
            waitSpot = null;
            headingToWaitSpot = false;
        }
    }

    public bool IsWaitingForPlayer()
    {
        if (!PhotonNetwork.IsConnected || PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
        {
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

    [PunRPC]
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

        // === Cargar puntos desde la escena si el prefab viene vacío ===
        if ((normalPathPoints == null || normalPathPoints.Count == 0) && PathPointRegistry.Instance != null)
            normalPathPoints = new List<Transform>(PathPointRegistry.Instance.GetNormal());

        if ((happyPathPoints == null || happyPathPoints.Count == 0) && PathPointRegistry.Instance != null)
            happyPathPoints = new List<Transform>(PathPointRegistry.Instance.GetHappy());

        if (normalPathPoints == null || normalPathPoints.Count == 0)
            Debug.LogError("[NPC] normalPathPoints vacío. Asigna puntos en PathPointRegistry de la escena.");

        if (happyPathPoints == null || happyPathPoints.Count == 0)
            Debug.LogWarning("[NPC] happyPathPoints vacío. La ruta feliz no funcionará.");

        if (normalPathPoints != null && normalPathPoints.Count > 0)
            popupAtPointIndex = Mathf.Clamp(popupAtPointIndex, 0, normalPathPoints.Count - 1);

        activePath = (normalPathPoints != null) ? new List<Transform>(normalPathPoints) : new List<Transform>();

        if (popupChar != null)
            popupChar.npcFollowPath = this;

        // En MP, sólo el Master usa física/mueve
        if (PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode)
        {
            bool iAmMaster = PhotonNetwork.IsMasterClient;
            rb.isKinematic = !iAmMaster;
            Debug.Log($"[NPC] Start() - IAmMaster={iAmMaster}, normals={normalPathPoints?.Count}, happy={happyPathPoints?.Count}, waitIndex={waitIndex}, waitSpot={(waitSpot ? waitSpot.name : "null")}");
        }
        else
        {
            rb.isKinematic = false; // offline / singleplayer
        }

        netPos = transform.position;
        netRot = npcModel ? npcModel.rotation : transform.rotation;

        aliveLeft = Mathf.Max(0.1f, aliveTime);
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

            if (npcModel)
            {
                npcModel.rotation = Quaternion.Slerp(
                    npcModel.rotation,
                    netRot,
                    rotationSpeed * Time.fixedDeltaTime * 1.25f
                );
            }

            anim.SetFloat("Speed", netSpeed);
        }
    }

public bool WantsFries() => npcWantsFries;
public int GetWaitIndex() => waitIndex;           // -1 si no tiene spot
public bool IsWaitingNow() => IsWaitingForPlayer();


    private void TickLogic(float dt)
    {
        if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
{
    // Countdown de vida del NPC
    aliveLeft -= dt;
    if (aliveLeft <= 0f && npcState != NpcState.Finished)
    {
        Despawn("timeout");
        return; // este NPC ya se va a destruir
    }
}
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
        if (npcState == NpcState.Finished) return;

        // Si estamos yendo al waitSpot, ignorar la ruta normal hasta llegar
        if (headingToWaitSpot && waitSpot != null)
        {
            MoveTowards(waitSpot.position, dt);
            if (FlatDist(transform.position, waitSpot.position) < reachDistance)
            {
                headingToWaitSpot = false;
                npcState = NpcState.WaitingForPlayer;
                anim.SetFloat("Speed", 0f);
                netSpeed = 0f;

                // 🔄 si el spot NO es el 0, darle yaw +90°
                ApplyAltWaitYawIfNeeded();
                return;
            }
            return;
        }


        if (activePath == null || activePath.Count == 0) return;

        Transform target = activePath[currentPoint];
        MoveTowards(target.position, dt);

        // Llegada al punto objetivo
        if (FlatDist(transform.position, target.position) < reachDistance)
        {
            // Sólo Master decide estados
            bool mp = PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode;
            if (mp && !PhotonNetwork.IsMasterClient) return;

            if (!usingHappyPath && currentPoint == popupAtPointIndex)
            {
                // Redirigir hacia el waitSpot si lo tenemos y no estamos ya ahí
                if (waitSpot != null && FlatDist(transform.position, waitSpot.position) >= reachDistance)
                {
                    headingToWaitSpot = true;
                    return;
                }

                // Ya en el waitSpot → esperar a jugador
                npcState = NpcState.WaitingForPlayer;
                anim.SetFloat("Speed", 0f);
                netSpeed = 0f;
            }
            else
            {
                AdvancePoint();
            }
        }
    }

    private void MoveTowards(Vector3 worldTarget, float dt)
    {
        Vector3 nextPos = Vector3.MoveTowards(transform.position, worldTarget, moveSpeed * dt);
        if (rb != null) rb.MovePosition(nextPos);
        else transform.position = nextPos;

        Vector3 flatDir = new Vector3(worldTarget.x - transform.position.x, 0, worldTarget.z - transform.position.z).normalized;
        if (flatDir.sqrMagnitude > 0.001f && npcModel)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatDir);
            npcModel.rotation = Quaternion.Slerp(npcModel.rotation, targetRot, rotationSpeed * dt);
        }

        float speed = (nextPos - transform.position).magnitude / dt;
        anim.SetFloat("Speed", speed);
        netSpeed = speed;

        // actualizar buffers de red en master
        if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
        {
            netPos = transform.position;
            netRot = npcModel ? npcModel.rotation : transform.rotation;
        }
    }

    private float FlatDist(Vector3 a, Vector3 b)
        => Vector3.Distance(new Vector3(a.x, 0, a.z), new Vector3(b.x, 0, b.z));

    private void AdvancePoint()
{
    currentPoint++;

    if (currentPoint >= (activePath?.Count ?? 0))
    {
        if (usingHappyPath)
        {
            npcState = NpcState.Finished;
            anim.SetFloat("Speed", 0f);
            netSpeed = 0f;

            // OPCIONAL: destruir tras un pequeño grace
            if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
                Invoke(nameof(_DestroyAfterHappy), destroyGraceAfterHappy);

            return;
        }
        currentPoint = 0;
    }
}

    // =====================================================================
    //   POPUP / PEDIDO
    // =====================================================================
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

        foreach (Collider c in trayGO.GetComponentsInChildren<Collider>(true))
            c.isTrigger = true;

        var final = trayGO.GetComponent<BandejaFinal>();
        orderCompleted = true;
        friesCompleted = !npcWantsFries || (final != null && final.contienePapas);
    }

    // =====================================================================
    //   MASTER SWITCH & CLEANUP
    // =====================================================================
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

    void ApplyAltWaitYawIfNeeded()
{
    if (appliedAltWaitYaw) return;
    if (waitIndex <= 0) return;                  // solo spots alternos (índice > 0)
    if (!npcModel) return;

    // gira +90° en Y (world)
    npcModel.Rotate(0f, ALT_WAIT_YAW, 0f, Space.World);
    appliedAltWaitYaw = true;
}

void ResetAltWaitYawIfNeeded()
{
    if (!appliedAltWaitYaw) return;
    if (!npcModel) return;

    // deshacer el giro aplicado
    npcModel.Rotate(0f, -ALT_WAIT_YAW, 0f, Space.World);
    appliedAltWaitYaw = false;
}


    private int FindNextPointInDirection()
    {
        if (activePath == null || activePath.Count == 0) return 0;

        int bestIndex = 0;
        float bestAngle = 999f;

        Vector3 pos = transform.position;
        Vector3 fwd = npcModel ? npcModel.forward : transform.forward;

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

    public void OnPopupClosed()
    {
        if (npcState == NpcState.ShowingPopup)
        {
            npcState = NpcState.WaitingAfterPopup;
            stateTimer = waitTimeAfterPopup;
            ResetAltWaitYawIfNeeded();
        }
    }



    // =====================================================================
    //   PUN OBSERVE (sync movimiento)
    // =====================================================================
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        bool mp = PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode;
        if (!mp) return;

        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(npcModel ? npcModel.rotation : transform.rotation);
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
                if (npcModel) npcModel.rotation = netRot;
            }
        }
    }

    private void _DestroyAfterHappy()
{
    if (!PhotonNetwork.IsMasterClient) return;
    Despawn("happy");
}
private void Despawn(string reason)
{
    if (_despawned) return;
    _despawned = true;

    // Liberar wait spot si aún está reservado (solo Master)
    if (PhotonNetwork.IsMasterClient && waitIndex >= 0)
    {
        WaitSpotRegistry.MasterRelease(waitIndex);
        waitIndex = -1;
        waitSpot = null;
        headingToWaitSpot = false;
    }

    // Notificar al spawner (solo Master)
    if (PhotonNetwork.IsMasterClient && NpcSpawner.Instance != null)
        NpcSpawner.Instance.NotifyNpcDespawn();

    // Destruir por red (solo Master)
    if (PhotonNetwork.IsMasterClient)
        PhotonNetwork.Destroy(photonView);
}

// ===== ajusta OnDestroy() para redundancia segura =====
void OnDestroy()
{
    // Liberar el wait spot si el NPC muere/desaparece
    if (PhotonNetwork.IsMasterClient && waitIndex >= 0)
    {
        WaitSpotRegistry.MasterRelease(waitIndex);
        waitIndex = -1;
        waitSpot = null;
    }

    // Notificar al spawner solo en Master (en clientes OnDestroy también corre)
    if (PhotonNetwork.IsMasterClient && NpcSpawner.Instance != null)
        NpcSpawner.Instance.NotifyNpcDespawn();
}
}


