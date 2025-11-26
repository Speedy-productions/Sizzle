using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

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

    [Header("NPC Hand")]
    public Transform handTransform;

    private Rigidbody rb;

    private NpcState npcState = NpcState.WalkingNormal;
    private float stateTimer = 0f;
    private bool usingHappyPath = false;
    private int currentPoint = 0;
    private List<Transform> activePath;

    // ORDEN PROPIA DE ESTE NPC
    private Order npcAssignedOrder = null;
    private bool orderCompleted = false;

    // Sync movimiento
    private Vector3 netPos;
    private Quaternion netRot;
    private float netSpeed;
    private bool firstSync = true;

    // Sync de "esperando jugador"
    private bool syncWaiting;

    // -------------------------------------------------------------------
    // API externa
    // -------------------------------------------------------------------

    public bool IsWaitingForPlayer()
    {
        // host / single player usan su propio estado
        if (!PhotonNetwork.IsConnected || PhotonNetwork.OfflineMode || PhotonNetwork.IsMasterClient)
            return npcState == NpcState.WaitingForPlayer && !orderCompleted;

        // clientes usan valor sincronizado
        return syncWaiting;
    }

    public void AssignNpcOrder(Order order)
    {
        if (order == null) return;
        npcAssignedOrder = order;
    }

    public Order GetAssignedOrder() => npcAssignedOrder;

    // RPC: setear orden remotamente cuando otro jugador la genera
    [PunRPC]
    void RPC_SetNpcOrder(string[] ingredients)
    {
        npcAssignedOrder = new Order(ingredients);
    }

    // -------------------------------------------------------------------
    // Unity
    // -------------------------------------------------------------------

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        activePath = new List<Transform>(normalPathPoints);

        if (popupChar != null)
            popupChar.npcFollowPath = this;

        // En multiplayer, solo el Master usa f�sica
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
            // Interpolaci�n en clientes
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

    // -------------------------------------------------------------------
    // STATE MACHINE
    // -------------------------------------------------------------------

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

        // Sincronizar flag "esperando al jugador"
        syncWaiting = (npcState == NpcState.WaitingForPlayer && !orderCompleted);
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

                // Aqu� NO generamos la orden a�n, eso lo hace el jugador que interact�e primero.
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

    // -------------------------------------------------------------------
    // POPUP & INTERACCI�N
    // -------------------------------------------------------------------

    public void OnPlayerInteracted()
    {
        GetComponent<NpcAudio>()?.PlayTalk();

        if (!IsWaitingForPlayer())
            return;

        bool mp = PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode;

        // --- AVENTURA / OFFLINE ---
        if (!mp)
        {
            if (npcAssignedOrder == null && OrderManager.Instance != null)
            {
                npcAssignedOrder = OrderManager.Instance.GenerateHamburgerOrder();
            }

            popupChar?.ShowPopup();
            npcState = NpcState.ShowingPopup;
            stateTimer = popupChar != null ? popupChar.popupDuration : waitTimeAfterPopup;
            return;
        }

        // --- MULTIJUGADOR ---
        // Cualquier jugador que interact�e primero genera la orden (si no existe)
        if (npcAssignedOrder == null && OrderManager.Instance != null)
        {
            npcAssignedOrder = OrderManager.Instance.GenerateHamburgerOrder();
        }

        if (npcAssignedOrder == null)
        {
            Debug.LogError("[NpcFollowPath] No se pudo generar la orden del NPC al interactuar.");
            return;
        }

        // Este jugador (cliente o host) dispara el popup y la orden para TODOS
        photonView.RPC(
            nameof(RPC_StartPopupWithOrder),
            RpcTarget.AllBuffered,
            npcAssignedOrder.ingredients
        );
    }

    [PunRPC]
    private void RPC_StartPopupWithOrder(string[] ingredients)
    {
        // Fijar la orden en todos los clientes
        npcAssignedOrder = new Order(ingredients);

        // Mostrar popup localmente
        popupChar?.ShowPopup();

        npcState = NpcState.ShowingPopup;
        stateTimer = popupChar != null ? popupChar.popupDuration : waitTimeAfterPopup;
    }

    public void OnPopupClosed()
    {
        if (npcState == NpcState.ShowingPopup)
        {
            npcState = NpcState.WaitingAfterPopup;
            stateTimer = waitTimeAfterPopup;
        }
    }

    // -------------------------------------------------------------------
    // ENTREGA DE HAMBURGUESA
    // -------------------------------------------------------------------

   public void SetHamburguesaEnMano(Hamburguesa hamb)
{
    if (hamb == null) return;

    bool mp = PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode;

    // ------------------------------
    // 🟢 SINGLE PLAYER (NO PHOTON)
    // ------------------------------
    if (!mp)
    {
        // mover la hamburguesa localmente al NPC
        AttachHamburguesaLocal(hamb);

        orderCompleted = true;
        npcState = NpcState.WaitingBeforeHappyPath;
        stateTimer = 4f;

        popupChar?.MostrarCaraFeliz("¡Bien hecho!");
        return;
    }

    // ------------------------------
    // 🔵 MULTIJUGADOR
    // ------------------------------
    PhotonView hambPV = hamb.GetComponent<PhotonView>();
    if (!hambPV)
    {
        Debug.LogError("[NPC] Hamburguesa sin PhotonView en MULTIPLAYER.");
        return;
    }

    // Cliente → pedir al host procesar
    if (!PhotonNetwork.IsMasterClient)
    {
        photonView.RPC(nameof(RPC_SetHamburguesaEnMano_Master), RpcTarget.MasterClient, hambPV.ViewID);
        return;
    }

    // Host / Master → aplicar de una vez
    AssignHamburgerToNpcForAll(hambPV.ViewID);
}



    [PunRPC]
private void RPC_SetHamburguesaEnMano_Master(int hamburgerViewID)
{
    if (!PhotonNetwork.IsMasterClient) return;
    AssignHamburgerToNpcForAll(hamburgerViewID);
}

private void AssignHamburgerToNpcForAll(int hamburgerViewID)
{
    photonView.RPC(nameof(RPC_AttachHamburgerToNPC), RpcTarget.AllBuffered, hamburgerViewID);

    orderCompleted = true;
    npcState = NpcState.WaitingBeforeHappyPath;
    stateTimer = 4f;

    photonView.RPC(nameof(RPC_ShowHappyFace), RpcTarget.All, "�Bien hecho!");
}


[PunRPC]
private void RPC_AttachHamburgerToNPC(int hamburgerViewID)
{
    PhotonView hambPV = PhotonView.Find(hamburgerViewID);
    if (!hambPV) return;

    Transform hambT = hambPV.transform;

    // 1) Quitarlo de la mano del jugador
    hambT.SetParent(null);

    // 2) Reparentarlo al NPC
    if (handTransform != null)
    {
        hambT.SetParent(handTransform);
        hambT.localPosition = Vector3.zero;
        hambT.localRotation = Quaternion.identity;
    }

    // 3) F�sicas correctas
    Rigidbody rb = hambT.GetComponent<Rigidbody>();
    if (rb)
    {
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    // 4) Colliders correctos
    foreach (Collider c in hambT.GetComponentsInChildren<Collider>())
        c.isTrigger = true;
}



    private void AttachHamburguesaLocal(Hamburguesa hamb)
    {
        if (handTransform != null)
        {
            hamb.transform.SetParent(handTransform);
            hamb.transform.localPosition = Vector3.zero;
            hamb.transform.localRotation = Quaternion.identity;
        }
    }

    [PunRPC]
    public void RPC_ShowHappyFace(string msg)
    {
        popupChar?.MostrarCaraFeliz(msg);
    }

    [PunRPC]
    public void RPC_ShowAngryFace(string msg)
    {
        popupChar?.MostrarCaraMolesta(msg);
    }

    // -------------------------------------------------------------------
    // MASTER SWITCH
    // -------------------------------------------------------------------

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

    // -------------------------------------------------------------------
    // PHOTON SYNC
    // -------------------------------------------------------------------

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
