using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcFollowPath : MonoBehaviour
{
    public bool IsWaitingForPlayer() => isWaitingForPlayer;

    [Header("Path Settings")]
    public List<Transform> pathPoints;
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float reachDistance = 0.5f;
    public float waitTimeAtPopup = 10f;

    [Header("Animation")]
    public Animator anim;
    public Transform NpcModel;

    [Header("Popup")]
    public PopupChar popupChar;
    public int popupAtPointIndex = 2;

    private int currentPointIndex = 0;
    private Rigidbody rb;
    private Vector3 lastPosition;

    private bool isWaitingForPlayer = false;
    private bool hasShownPopup = false;
    private bool waitingCoroutineRunning = false;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        if (popupChar != null)
            popupChar.npcFollowPath = this;
    }

    private void FixedUpdate()
    {
        if (pathPoints.Count == 0) return;
        if (isWaitingForPlayer || hasShownPopup) return;

        Transform targetPoint = pathPoints[currentPointIndex];
        Vector3 direction = (targetPoint.position - transform.position).normalized;
        direction.y = 0;

        Vector3 nextPos = Vector3.MoveTowards(transform.position, targetPoint.position, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(nextPos);

        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            NpcModel.rotation = Quaternion.Slerp(NpcModel.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }

        float speed = ((transform.position - lastPosition).magnitude) / Time.fixedDeltaTime;
        anim.SetFloat("Speed", speed);
        lastPosition = transform.position;

        Vector3 flatNPC = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 flatTarget = new Vector3(targetPoint.position.x, 0, targetPoint.position.z);

        if (Vector3.Distance(flatNPC, flatTarget) < reachDistance)
        {
            if (currentPointIndex == popupAtPointIndex && !hasShownPopup)
            {
                isWaitingForPlayer = true;
                anim.SetFloat("Speed", 0f);
                Debug.Log("[NPC] Waiting for player interaction...");
            }
            else
            {
                MoveToNextPoint();
            }
        }
    }

    public void OnPopupClosed()
    {
        if (waitingCoroutineRunning)
            return;
        StartCoroutine(WaitAfterPopup());
    }

    public void OnPlayerInteracted()
    {
        if (isWaitingForPlayer && !hasShownPopup)
        {
            ShowPopup();
        }
    }

    private void ShowPopup()
    {
        if (popupChar != null)
        {
            popupChar.npcFollowPath = this;
            popupChar.ShowPopup();
            Debug.Log("[NPC] Popup shown!");
        }
        else
        {
            Debug.LogWarning("[NPC] PopupChar reference not set in NPC");
        }

        hasShownPopup = true;
        isWaitingForPlayer = false;
        anim.SetFloat("Speed", 0f);
    }

    private IEnumerator WaitAfterPopup()
    {
        waitingCoroutineRunning = true;
        anim.SetFloat("Speed", 0f);
        Debug.Log("[NPC] Popup closed -> waiting extra " + waitTimeAtPopup + " seconds.");
        yield return new WaitForSeconds(waitTimeAtPopup);
        MoveToNextPoint();
        hasShownPopup = false;
        waitingCoroutineRunning = false;
    }

    private void MoveToNextPoint()
    {
        currentPointIndex++;
        if (currentPointIndex >= pathPoints.Count)
        {
            currentPointIndex = 0;
        }
    }
}
