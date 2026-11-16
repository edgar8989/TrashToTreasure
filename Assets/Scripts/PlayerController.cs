using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Kamera")]
    public Transform cam;
    public float mouseSensitivity = 2f;

    [Header("Kecepatan Pemain")]
    public float walkSpeed = 4f;
    public float runSpeed = 7f;

    [Header("Lompatan")]
    public float jumpHeight = 2f;

    [Header("Gravitasi")]
    public float gravity = -20f;

    private CharacterController cc;
    private float pitch;
    private float velY;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (IsMenuOpen())
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        HandleCamera();
        HandleMovement();
    }

    bool IsMenuOpen()
    {
        return SortingBin.MenuOpen ||
               Machine.MenuOpen ||
               SellPoint.MenuOpen ||
               VehicleCargo.MenuOpen;
    }

    void HandleCamera()
    {
        float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
        float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(0f, mx, 0f);
        pitch = Mathf.Clamp(pitch - my, -80f, 80f);

        if (cam != null)
            cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    void HandleMovement()
    {
        bool running = Input.GetKey(KeyCode.LeftShift);
        float speed = running ? runSpeed : walkSpeed;

        if (GameManager.I != null && GameManager.I.hunger < 20f)
            speed *= 0.6f;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 move = transform.right * horizontal +
                       transform.forward * vertical;

        move = Vector3.ClampMagnitude(move, 1f);

        if (cc.isGrounded && velY < 0f)
            velY = -2f;

        if (cc.isGrounded && Input.GetKeyDown(KeyCode.Space))
            velY = Mathf.Sqrt(jumpHeight * -2f * gravity);

        velY += gravity * Time.deltaTime;

        Vector3 velocity = move * speed + Vector3.up * velY;
        cc.Move(velocity * Time.deltaTime);
    }
}
