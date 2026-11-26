
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Kamera")]
    public Transform cam;
    public float mouseSensitivity = 2f;

    [Header("Gerakan")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;
    public float jumpHeight = 2f;
    public float gravity = -20f;
    public float interactionRange = 3f;

    [Header("Animasi Karakter")]
    public Animator characterAnimator;
    public float animationDampTime = 0.12f;

    // Animator Parameters
    private static readonly int HorHash =
        Animator.StringToHash("Hor");

    private static readonly int VertHash =
        Animator.StringToHash("Vert");

    private static readonly int StateHash =
        Animator.StringToHash("State");

    private static readonly int JumpHash =
        Animator.StringToHash("IsJump");

    private CharacterController cc;

    private float pitch;
    private float velocityY;

    void Start()
    {
        cc = GetComponent<CharacterController>();

        // Cari Animator pada child jika belum diisi Inspector
        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>();
        }

        // FPS Camera
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Animator tidak boleh menggerakkan Player FPS
        if (characterAnimator != null)
        {
            characterAnimator.applyRootMotion = false;
        }
        else
        {
            Debug.LogWarning(
                "Character Animator belum ditemukan!"
            );
        }
    }

    void Update()
    {
        // Jangan proses input pemain ketika Pause Menu aktif.
        if (PauseMenuUI.IsPaused)
        {
            ResetCharacterAnimation();
            return;
        }

        GameManager gm = GameManager.I;

        if (gm == null) return;

        // Dialog NPC menghentikan kontrol pemain sampai dialog ditutup.
        if (FriendlyNPC.DialogOpen)
        {
            gm.prompt = "";
            ResetCharacterAnimation();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        // Ketika menu terbuka
        if (WorldObject.MenuOpen ||
            VehicleController.CargoMenuOpen)
        {
            gm.prompt = "";

            ResetCharacterAnimation();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            return;
        }

        // Ketika sedang mengendarai kendaraan
        if (VehicleController.IsPlayerDriving)
        {
            ResetCharacterAnimation();
            return;
        }

        // Mengunci kembali cursor
        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            return;
        }

        // Membuka cursor dengan Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            ResetCharacterAnimation();

            return;
        }

        // Interaksi objek
        HandleInteraction();

        // Pastikan controller masih aktif
        if (!enabled ||
            cc == null ||
            !cc.enabled ||
            VehicleController.IsPlayerDriving)
        {
            ResetCharacterAnimation();
            return;
        }

        // Kamera FPS
        HandleCamera();

        // Gerakan dan animasi
        HandleMovement();
    }

    // =========================================
    // INTERACTION
    // =========================================

    void HandleInteraction()
    {
        GameManager gm = GameManager.I;
        if (gm == null) return;

        gm.prompt = "";

        Transform cameraTransform = cam != null ? cam : transform;
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        Interactable target = null;

        // Prioritaskan raycast tepat di tengah layar seperti sistem semula.
        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            interactionRange,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Collide))
        {
            target = hit.collider.GetComponentInParent<Interactable>();
        }

        // Fallback: beberapa NPC memakai collider/rig bertingkat sehingga
        // raycast dapat mengenai bagian tubuh yang bukan child Interactable,
        // atau collider NPC tidak ikut terdeteksi. Cari NPC terdekat di depan kamera.
        if (target == null)
        {
            Interactable[] candidates = FindObjectsByType<Interactable>(FindObjectsSortMode.None);
            float bestDistance = float.MaxValue;

            foreach (Interactable candidate in candidates)
            {
                if (candidate == null || !candidate.isActiveAndEnabled)
                    continue;

                Vector3 aimPoint = candidate.transform.position + Vector3.up * 1.2f;
                float playerDistance = Vector3.Distance(transform.position, candidate.transform.position);
                if (playerDistance > interactionRange + 0.75f)
                    continue;

                Vector3 direction = (aimPoint - cameraTransform.position).normalized;
                float facing = Vector3.Dot(cameraTransform.forward, direction);
                if (facing < 0.55f) // sekitar 57 derajat dari arah kamera
                    continue;

                float cameraDistance = Vector3.Distance(cameraTransform.position, aimPoint);
                if (cameraDistance < bestDistance)
                {
                    bestDistance = cameraDistance;
                    target = candidate;
                }
            }
        }

        if (target != null)
        {
            gm.prompt = target.prompt;

            if (Input.GetKeyDown(KeyCode.E))
                target.Interact();
        }
    }

    // =========================================
    // FPS CAMERA
    // =========================================

    void HandleCamera()
    {
        float mx =
            Input.GetAxis("Mouse X") * mouseSensitivity;

        float my =
            Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(0f, mx, 0f);

        pitch = Mathf.Clamp(
            pitch - my,
            -80f,
            80f
        );

        if (cam != null)
        {
            cam.localRotation =
                Quaternion.Euler(pitch, 0f, 0f);
        }
    }

    // =========================================
    // PLAYER MOVEMENT
    // =========================================

    void HandleMovement()
    {
        bool running =
            Input.GetKey(KeyCode.LeftShift);

        float speed = running ? runSpeed : walkSpeed;

        // Hunger system
        if (GameManager.I.hunger < 20f)
        {
            speed *= 0.6f;
        }

        float horizontal =
            Input.GetAxisRaw("Horizontal");

        float vertical =
            Input.GetAxisRaw("Vertical");

        Vector3 move =
            transform.right * horizontal +
            transform.forward * vertical;

        move = Vector3.ClampMagnitude(move, 1f);

        // Ground check
        if (cc.isGrounded && velocityY < 0f)
        {
            velocityY = -2f;
        }

        // Jump
        if (cc.isGrounded &&
            Input.GetKeyDown(KeyCode.Space))
        {
            velocityY =
                Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Gravity
        velocityY += gravity * Time.deltaTime;

        // Apply movement
        cc.Move(
            (move * speed + Vector3.up * velocityY)
            * Time.deltaTime
        );

        // Update character animation
        UpdateCharacterAnimation(
            new Vector2(horizontal, vertical),
            running
        );
    }

    // =========================================
    // CHARACTER ANIMATION
    // =========================================

    void UpdateCharacterAnimation(
        Vector2 movement,
        bool running)
    {
        if (characterAnimator == null)
            return;

        // Normalize movement
        movement = Vector2.ClampMagnitude(
            movement,
            1f
        );

        // Check if player is moving
        bool isMoving =
            movement.sqrMagnitude > 0.01f;

        // Run state only when moving
        float state = (running && isMoving) ? 1f : 0f;

        // Horizontal animation
        characterAnimator.SetFloat(
            HorHash,
            movement.x,
            animationDampTime,
            Time.deltaTime
        );

        // Vertical animation
        characterAnimator.SetFloat(
            VertHash,
            movement.y,
            animationDampTime,
            Time.deltaTime
        );

        // Walking / Running state
        characterAnimator.SetFloat(
            StateHash,
            state,
            animationDampTime,
            Time.deltaTime
        );

        // Jump / Falling animation
        characterAnimator.SetBool(
            JumpHash,
            !cc.isGrounded
        );
    }

    // =========================================
    // RESET ANIMATION
    // =========================================

    void ResetCharacterAnimation()
    {
        if (characterAnimator == null)
            return;

        characterAnimator.SetFloat(
            HorHash,
            0f,
            animationDampTime,
            Time.deltaTime
        );

        characterAnimator.SetFloat(
            VertHash,
            0f,
            animationDampTime,
            Time.deltaTime
        );

        characterAnimator.SetFloat(
            StateHash,
            0f,
            animationDampTime,
            Time.deltaTime
        );

        characterAnimator.SetBool(
            JumpHash,
            false
        );
    }
}
