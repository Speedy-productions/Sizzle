using UnityEngine;
using Photon.Pun;

public class PlayerMovement : MonoBehaviourPun
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float groundDrag = 4f;

    [Header("Ground Check")]
    public float playerHeight = 2f;
    public LayerMask whatIsGround;
    bool grounded;

    [Header("Slope Handling")]
    public float maxSlopeAngle = 30f;
    RaycastHit slopeHit;

    [Header("References")]
    public Transform playerModel;
    public Transform orientation;
    public Animator anim;

    private PhotonView view;
    private Interact interactScript;
    private HeadLook headLookScript;

    Rigidbody rb;

    float horizontalInput;
    float verticalInput;

    Vector3 moveDirection;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        view = GetComponent<PhotonView>();

        interactScript = GetComponent<Interact>();
        headLookScript = GetComponent<HeadLook>();

        // ================================================================
        //     JUGADOR REMOTO (NO LOCAL)
        // ================================================================
        if (!view.IsMine)
        {
            // 🔸 Rigidbodies remotos NO deben simular física
            rb.isKinematic = true;
            rb.useGravity = false;

            // 🔸 Colliders desactivados para evitar empujes
            foreach (Collider c in GetComponentsInChildren<Collider>())
                c.enabled = false;

            // 🔸 Scripts solo del jugador local se desactivan
            if (interactScript) interactScript.enabled = false;
            if (headLookScript) headLookScript.enabled = false;

            // 🔹 IMPORTANTE:
            // NO desactivamos este script, porque controla animaciones del local
            return;
        }

        // ================================================================
        //     JUGADOR LOCAL — DESACTIVAMOS NETWORK SYNC DEL ROOT
        // ================================================================
        var sync = GetComponent<PlayerNetworkSync>();
        if (sync) sync.enabled = false;
    }

    void Update()
    {
        // ================================================================
        //     SOLO EL JUGADOR LOCAL MUEVE EL PERSONAJE
        // ================================================================
        if (!view.IsMine)
            return;   // animación remota la controla PhotonAnimatorView

        grounded = Physics.SphereCast(
            transform.position,
            0.3f,
            Vector3.down,
            out slopeHit,
            playerHeight * 0.5f + 0.3f,
            whatIsGround
        );

        MyInput();

        rb.linearDamping = grounded ? groundDrag : 0;

        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        anim.SetFloat("Speed", flatVel.magnitude);  // PhotonAnimatorView lo envía a los remotos

        // Rotación del modelo del jugador
        Quaternion targetRot = Quaternion.Euler(0, orientation.eulerAngles.y, 0);
        playerModel.rotation = Quaternion.Slerp(playerModel.rotation, targetRot, Time.deltaTime * 10f);
    }

   void FixedUpdate()
{
    if (!view.IsMine) return;

    // Lógica de pausa (de HEAD)
    if (PauseMenu.GameIsPaused)
    {
        rb.linearVelocity = Vector3.zero;
        return;
    }

    // Lógica de detener movimiento cuando está en el suelo y casi no hay input (de feature)
    if (grounded && Mathf.Abs(horizontalInput) < 0.1f && Mathf.Abs(verticalInput) < 0.1f)
    {
        Vector3 v = rb.linearVelocity;
        v.x = 0;
        v.z = 0;
        rb.linearVelocity = v;
        return;
    }

    MovePlayer();
    SpeedControl();
}


    void MyInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");
    }

    void MovePlayer()
    {
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        if (OnSlope())
        {
            Vector3 slopeDir = Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
            rb.AddForce(slopeDir * moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            rb.linearVelocity = new Vector3(
                moveDirection.x * moveSpeed,
                rb.linearVelocity.y,
                moveDirection.z * moveSpeed
            );
        }
    }

    void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limited = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(
                limited.x,
                rb.linearVelocity.y,
                limited.z
            );
        }
    }

    bool OnSlope()
    {
        if (!grounded) return false;

        float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
        return angle < maxSlopeAngle && angle != 0;
    }
}
