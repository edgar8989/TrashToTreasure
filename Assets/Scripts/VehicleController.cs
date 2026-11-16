
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : Interactable
{
    [Header("Referensi Pemain")]
    public PlayerController playerController;
    public PlayerInteractor playerInteractor;
    public CharacterController playerCharacterController;
    public Transform driverSeat;
    public Transform exitPoint;
    public Transform driverCamera;

    [Header("Wheel Colliders")]
    public WheelCollider frontLeft;
    public WheelCollider frontRight;
    public WheelCollider rearLeft;
    public WheelCollider rearRight;

    [Header("Wheel Visuals")]
    public Transform wheelVisualFL;
    public Transform wheelVisualFR;
    public Transform wheelVisualRL;
    public Transform wheelVisualRR;

    [Header("Pengaturan Kendaraan")]
    public float motorTorque = 1800f;
    public float maxSpeed = 60f;
    public float reverseSpeed = 15f;
    public float maxSteerAngle = 30f;

    [Range(0.1f, 1f)]
    public float steeringAtMaxSpeed = 0.45f;

    public float brakeTorque = 3000f;
    public float handbrakeTorque = 5000f;
    public float downforce = 50f;

    [Header("Kamera Third Person")]
    public Vector3 drivingCameraPosition =
        new Vector3(0f, 3.5f, -7f);

    public float drivingCameraPitch = 18f;
    public float mouseSensitivity = 2f;
    public float maxLookUp = 55f;
    public float maxLookDown = 45f;

    [Header("Status Kendaraan")]
    public static bool IsPlayerDriving { get; private set; }

    public bool IsOccupied => driverRoot != null;

    private Rigidbody rb;

    private Transform driverRoot;

    private float horizontalInput;
    private float verticalInput;

    private bool handbrake;

    private float cameraPitch;
    private float cameraYaw;

    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;

    private bool cameraOriginalSaved;

    // ==========================================
    // INITIALIZATION
    // ==========================================

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.mass = 1200f;
        rb.linearDamping = 0.05f;
        rb.angularDamping = 0.5f;

        rb.interpolation =
            RigidbodyInterpolation.Interpolate;

        rb.collisionDetectionMode =
            CollisionDetectionMode.ContinuousDynamic;

        rb.centerOfMass =
            new Vector3(0f, -0.35f, 0f);

        // Cari Player otomatis jika belum diisi.

        if (playerController == null)
        {
            playerController =
                FindFirstObjectByType<PlayerController>();
        }

        if (playerController != null)
        {
            if (playerCharacterController == null)
            {
                playerCharacterController =
                    playerController.GetComponent<CharacterController>();
            }

            if (playerInteractor == null)
            {
                playerInteractor =
                    playerController.GetComponentInChildren<PlayerInteractor>();
            }

            if (driverCamera == null)
            {
                driverCamera = playerController.cam;
            }
        }

        SaveOriginalCamera();

        prompt = "[E] Masuk mobil";
    }

    void SaveOriginalCamera()
    {
        if (driverCamera == null || cameraOriginalSaved)
            return;

        originalCameraLocalPosition =
            driverCamera.localPosition;

        originalCameraLocalRotation =
            driverCamera.localRotation;

        cameraOriginalSaved = true;
    }

    // ==========================================
    // INTERACTION
    // ==========================================

    public override void Interact()
    {
        if (IsOccupied)
        {
            ExitVehicle();
            return;
        }

        if (SortingBin.MenuOpen ||
            Machine.MenuOpen ||
            SellPoint.MenuOpen ||
            VehicleCargo.MenuOpen)
        {
            return;
        }

        EnterVehicle();
    }

    // ==========================================
    // ENTER VEHICLE
    // ==========================================

    void EnterVehicle()
    {
        GameManager gm = GameManager.I;
        if (gm != null && gm.CarriedCount > 0)
        {
            gm.Notify(
                "Tidak bisa masuk mobil! Tas masih berisi " +
                gm.CarriedCount + " sampah. Simpan semua sampah ke bagasi " +
                "mobil atau buang terlebih dahulu dengan tombol G."
            );
            return;
        }

        if (playerController == null ||
            driverSeat == null ||
            exitPoint == null)
        {
            Debug.LogError(
                "VehicleController: Player Controller, " +
                "Driver Seat, atau Exit Point belum diisi!"
            );

            return;
        }

        if (driverCamera == null)
            driverCamera = playerController.cam;

        if (driverCamera == null)
        {
            Debug.LogError(
                "VehicleController: Driver Camera belum diisi!"
            );

            return;
        }

        SaveOriginalCamera();

        if (VehicleCargo.MenuOpen)
            return;

        driverRoot = playerController.transform;

        if (playerCharacterController == null)
        {
            playerCharacterController =
                driverRoot.GetComponent<CharacterController>();
        }

        if (playerInteractor == null)
        {
            playerInteractor =
                driverRoot.GetComponentInChildren<PlayerInteractor>();
        }

        // Nonaktifkan kontrol berjalan.

        if (playerCharacterController != null)
            playerCharacterController.enabled = false;

        if (playerController != null)
            playerController.enabled = false;

        if (playerInteractor != null)
            playerInteractor.enabled = false;

        // Pindahkan pemain ke kursi pengemudi.

        driverRoot.SetParent(driverSeat, false);

        driverRoot.localPosition = Vector3.zero;
        driverRoot.localRotation = Quaternion.identity;

        // Atur kamera third-person.

        cameraYaw = 0f;
        cameraPitch = drivingCameraPitch;

        driverCamera.localPosition =
            drivingCameraPosition;

        driverCamera.localRotation =
            Quaternion.Euler(cameraPitch, 0f, 0f);

        // Aktifkan status mengemudi.

        IsPlayerDriving = true;

        horizontalInput = 0f;
        verticalInput = 0f;
        handbrake = false;

        prompt = "[E] Keluar mobil";

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (GameManager.I != null)
        {
            GameManager.I.prompt = "[E] Keluar mobil";

            GameManager.I.Notify(
                "Kamu masuk ke mobil. " +
                "WASD untuk mengemudi, " +
                "Space untuk rem tangan, E untuk keluar."
            );
        }

        Debug.Log("Player berhasil masuk mobil.");
    }

    // ==========================================
    // EXIT VEHICLE
    // ==========================================

    void ExitVehicle()
    {
        if (driverRoot == null)
            return;

        Transform exitingPlayer = driverRoot;

        // Lepaskan pemain dari kursi mobil.

        exitingPlayer.SetParent(null, true);

        // Pindahkan pemain ke titik keluar.

        exitingPlayer.position = exitPoint.position;
        exitingPlayer.rotation = exitPoint.rotation;

        // Kembalikan posisi dan rotasi kamera.

        if (driverCamera != null && cameraOriginalSaved)
        {
            driverCamera.localPosition =
                originalCameraLocalPosition;

            driverCamera.localRotation =
                originalCameraLocalRotation;
        }

        // Aktifkan kembali kontrol pemain.

        if (playerCharacterController != null)
            playerCharacterController.enabled = true;

        if (playerController != null)
            playerController.enabled = true;

        if (playerInteractor != null)
            playerInteractor.enabled = true;

        // Reset status kendaraan.

        driverRoot = null;

        IsPlayerDriving = false;

        horizontalInput = 0f;
        verticalInput = 0f;
        handbrake = true;

        prompt = "";

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (GameManager.I != null)
        {
            GameManager.I.prompt = "";

            GameManager.I.Notify(
                "Kamu keluar dari mobil."
            );
        }

        Debug.Log("Player berhasil keluar mobil.");
    }

    // ==========================================
    // UPDATE
    // ==========================================

    void Update()
    {
        if (!IsOccupied)
            return;

        // Tombol keluar mobil.

        if (Input.GetKeyDown(KeyCode.E))
        {
            ExitVehicle();
            return;
        }

        ReadDrivingInput();

        HandleDrivingCamera();
    }

    // ==========================================
    // DRIVING INPUT
    // ==========================================

    void ReadDrivingInput()
    {
        horizontalInput =
            Input.GetAxisRaw("Horizontal");

        verticalInput =
            Input.GetAxisRaw("Vertical");

        handbrake =
            Input.GetKey(KeyCode.Space);
    }

    // ==========================================
    // THIRD PERSON CAMERA
    // ==========================================

    void HandleDrivingCamera()
    {
        if (driverRoot == null ||
            driverCamera == null)
        {
            return;
        }

        // Putar kamera menggunakan mouse.

        cameraYaw +=
            Input.GetAxis("Mouse X") * mouseSensitivity;

        cameraPitch -=
            Input.GetAxis("Mouse Y") * mouseSensitivity;

        cameraPitch = Mathf.Clamp(
            cameraPitch,
            -maxLookUp,
            maxLookDown
        );

        // Kamera berputar mengelilingi mobil.

        driverRoot.localRotation =
            Quaternion.Euler(0f, cameraYaw, 0f);

        driverCamera.localRotation =
            Quaternion.Euler(cameraPitch, 0f, 0f);
    }

    // ==========================================
    // PHYSICS UPDATE
    // ==========================================

    void FixedUpdate()
    {
        if (!IsOccupied)
        {
            ApplyIdleBrakes();
            UpdateWheelVisuals();
            return;
        }

        HandleSteering();

        HandleMotor();

        HandleBraking();

        ApplyDownforce();

        UpdateWheelVisuals();
    }

    // ==========================================
    // IDLE BRAKES
    // ==========================================

    void ApplyIdleBrakes()
    {
        SetBrake(frontLeft, handbrakeTorque);
        SetBrake(frontRight, handbrakeTorque);
        SetBrake(rearLeft, handbrakeTorque);
        SetBrake(rearRight, handbrakeTorque);

        SetMotor(rearLeft, 0f);
        SetMotor(rearRight, 0f);

        SetSteer(frontLeft, 0f);
        SetSteer(frontRight, 0f);
    }

    // ==========================================
    // STEERING
    // ==========================================

    void HandleSteering()
    {
        float speed = rb.linearVelocity.magnitude;

        float speedFactor = Mathf.Clamp01(
            speed / Mathf.Max(1f, maxSpeed / 3.6f)
        );

        float steerFactor = Mathf.Lerp(
            1f,
            steeringAtMaxSpeed,
            speedFactor
        );

        float angle =
            horizontalInput * maxSteerAngle * steerFactor;

        SetSteer(frontLeft, angle);
        SetSteer(frontRight, angle);
    }

    // ==========================================
    // MOTOR
    // ==========================================

    void HandleMotor()
    {
        float speed = Vector3.Dot(
            rb.linearVelocity,
            transform.forward
        );

        float torque = 0f;

        // Maju.

        if (verticalInput > 0.05f && speed >= -0.5f)
        {
            if (speed < maxSpeed / 3.6f)
            {
                torque = verticalInput * motorTorque;
            }
        }

        // Mundur.

        else if (verticalInput < -0.05f && speed <= 0.5f)
        {
            if (speed > -(reverseSpeed / 3.6f))
            {
                torque =
                    verticalInput * motorTorque * 0.6f;
            }
        }

        SetMotor(rearLeft, torque);
        SetMotor(rearRight, torque);
    }

    // ==========================================
    // BRAKING
    // ==========================================

    void HandleBraking()
    {
        float speed = Vector3.Dot(
            rb.linearVelocity,
            transform.forward
        );

        float brake = 0f;

        if (verticalInput < -0.05f && speed > 0.5f)
            brake = brakeTorque;

        if (verticalInput > 0.05f && speed < -0.5f)
            brake = brakeTorque;

        SetBrake(frontLeft, brake);
        SetBrake(frontRight, brake);

        SetBrake(
            rearLeft,
            handbrake ? handbrakeTorque : brake
        );

        SetBrake(
            rearRight,
            handbrake ? handbrakeTorque : brake
        );
    }

    // ==========================================
    // DOWNFORCE
    // ==========================================

    void ApplyDownforce()
    {
        rb.AddForce(
            -transform.up *
            downforce *
            rb.linearVelocity.magnitude,
            ForceMode.Force
        );
    }

    // ==========================================
    // WHEEL VISUALS
    // ==========================================

    void UpdateWheelVisuals()
    {
        UpdateWheel(frontLeft, wheelVisualFL);
        UpdateWheel(frontRight, wheelVisualFR);
        UpdateWheel(rearLeft, wheelVisualRL);
        UpdateWheel(rearRight, wheelVisualRR);
    }

    void UpdateWheel(
        WheelCollider wheel,
        Transform visual
    )
    {
        if (wheel == null || visual == null)
            return;

        wheel.GetWorldPose(
            out Vector3 position,
            out Quaternion rotation
        );

        visual.position = position;
        visual.rotation = rotation;
    }

    // ==========================================
    // SAFE WHEEL METHODS
    // ==========================================

    void SetMotor(WheelCollider wheel, float value)
    {
        if (wheel != null)
            wheel.motorTorque = value;
    }

    void SetBrake(WheelCollider wheel, float value)
    {
        if (wheel != null)
            wheel.brakeTorque = value;
    }

    void SetSteer(WheelCollider wheel, float value)
    {
        if (wheel != null)
            wheel.steerAngle = value;
    }

    // ==========================================
    // CLEANUP
    // ==========================================

    void OnDisable()
    {
        if (IsOccupied)
            ExitVehicle();
    }
}
