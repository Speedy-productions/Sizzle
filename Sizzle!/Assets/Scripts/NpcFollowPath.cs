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
        if (!PhotonNetwork.IsConnected || PhotonNetwork.IsMasterClient)
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
            if (PhotonNetwork.IsConnected && !PhotonNetwork.IsMasterClient)
                return;

            if (!usingHappyPath && currentPoint == popupAtPointIndex)
            {
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
    // POPUP & INTERACCIÓN
    // -------------------------------------------------------------------

    public void OnPlayerInteracted()
    {
        GetComponent<NpcAudio>()?.PlayTalk();

        if (!IsWaitingForPlayer())
            return;

        // Single player
        if (!PhotonNetwork.IsConnected || PhotonNetwork.OfflineMode)
        {
            popupChar?.ShowPopup();
            npcState = NpcState.ShowingPopup;
            stateTimer = popupChar != null ? popupChar.popupDuration : waitTimeAfterPopup;
            return;
        }

        // Multiplayer
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_StartPopup), RpcTarget.All);
        }
        else
        {
            photonView.RPC(nameof(RPC_RequestPopup), RpcTarget.MasterClient);
        }
    }

    [PunRPC]
    private void RPC_RequestPopup()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC(nameof(RPC_StartPopup), RpcTarget.All);
    }

    [PunRPC]
    private void RPC_StartPopup()
    {
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

    // Se llama SIEMPRE desde Interact.TransferirHamburguesaAlNpc
    public void SetHamburguesaEnMano(Hamburguesa hamb)
    {
        if (hamb == null) return;

        bool mp = PhotonNetwork.IsConnected && !PhotonNetwork.OfflineMode;
        bool isMaster = PhotonNetwork.IsMasterClient;

        if (mp && !isMaster)
        {
            // Cliente no host:
            // 1) pedir al Master que ejecute la lógica real
            PhotonView hambPV = hamb.GetComponent<PhotonView>();
            if (hambPV != null)
            {
                photonView.RPC(
                    nameof(RPC_SetHamburguesaEnMano_Master),
                    RpcTarget.MasterClient,
                    hambPV.ViewID
                );
            }
            else
            {
                Debug.LogWarning("[NpcFollowPath] Hamburguesa sin PhotonView, no se puede sincronizar por RPC. Solo se moverá local en este cliente.");
            }

            // 2) mover localmente la hamburguesa para que este jugador la vea en la mano del NPC
            AttachHamburguesaLocal(hamb);
            // NO cambiamos estado ni mostramos caras aquí, eso lo decide el host
            return;
        }

        // --------- SINGLE PLAYER o MASTER ---------
        AttachHamburguesaLocal(hamb);
        orderCompleted = true;

        npcState = NpcState.WaitingBeforeHappyPath;
        stateTimer = 4f;

        if (!mp)
        {
            popupChar?.MostrarCaraFeliz("¡Bien hecho!");
        }
        else
        {
            photonView.RPC(nameof(RPC_ShowHappyFace), RpcTarget.All, "¡Bien hecho!");
        }
    }

    [PunRPC]
    private void RPC_SetHamburguesaEnMano_Master(int hamburgerViewID)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView pv = PhotonView.Find(hamburgerViewID);
        if (pv == null) return;

        Hamburguesa hamb = pv.GetComponent<Hamburguesa>();
        if (hamb == null) return;

        AttachHamburguesaLocal(hamb);
        orderCompleted = true;

        npcState = NpcState.WaitingBeforeHappyPath;
        stateTimer = 4f;

        photonView.RPC(nameof(RPC_ShowHappyFace), RpcTarget.All, "¡Bien hecho!");
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
