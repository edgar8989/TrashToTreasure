using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : Interactable
{
    [Header("Pemain")]
    public PlayerController playerController;
    public CharacterController playerCharacterController;
    public Transform driverSeat;
    public Transform exitPoint;
    public Transform cargoPoint;
    public Transform driverCamera;

    [Header("Bagasi")]
    [Min(1)] public int cargoCapacity = 50;
    public int[] cargoTrash = new int[GameManager.TrashTypeCount];
    public int[] cargoProducts = new int[GameManager.ProductCount];

    [Header("Wheel Colliders")]
    public WheelCollider frontLeft, frontRight, rearLeft, rearRight;
    [Header("Wheel Visuals")]
    public Transform wheelVisualFL, wheelVisualFR, wheelVisualRL, wheelVisualRR;

    [Header("Kendaraan")]
    public float motorTorque = 1800f, maxSpeed = 60f, reverseSpeed = 15f;
    public float maxSteerAngle = 30f;
    [Range(0.1f, 1f)] public float steeringAtMaxSpeed = 0.45f;
    public float brakeTorque = 3000f, handbrakeTorque = 5000f, downforce = 50f;

    [Header("Kamera Mengemudi")]
    public Vector3 drivingCameraPosition = new Vector3(0, 3.5f, -7);
    public float drivingCameraPitch = 18f, mouseSensitivity = 2f;
    public float maxLookUp = 55f, maxLookDown = 45f;

    public static bool IsPlayerDriving { get; private set; }
    public static bool CargoMenuOpen { get; private set; }

    Rigidbody rb;
    Transform driverRoot;
    Vector3 oldCameraPosition;
    Quaternion oldCameraRotation;
    float horizontal, vertical, cameraYaw, cameraPitch;
    bool handbrake, cameraSaved, canExit;
    Renderer[] playerRenderers;
    bool[] playerRendererStates;
    bool cargoModePut = true, cargoProductMode, hasSelection;
    int selectedTrash, selectedProduct, selectedAmount = 1;
    string cargoMessage = "";
    Vector2 cargoScroll;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.mass = 1200;
        rb.linearDamping = 0.05f;
        rb.angularDamping = 0.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.centerOfMass = new Vector3(0, -0.35f, 0);
        if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            if (playerCharacterController == null) playerCharacterController = playerController.GetComponent<CharacterController>();
            if (driverCamera == null) driverCamera = playerController.cam;
        }
        if (cargoTrash == null) cargoTrash = new int[GameManager.TrashTypeCount];
        if (cargoProducts == null) cargoProducts = new int[GameManager.ProductCount];
        System.Array.Resize(ref cargoTrash, GameManager.TrashTypeCount);
        System.Array.Resize(ref cargoProducts, GameManager.ProductCount);
        prompt = "[E] Masuk mobil";
    }

    public override void Interact()
    {
        if (IsOccupied) { ExitVehicle(); return; }
        if (WorldObject.MenuOpen || CargoMenuOpen) return;

        Transform player = playerController != null ? playerController.transform : null;
        if (player != null && cargoPoint != null &&
            Vector3.Distance(player.position, cargoPoint.position) < 2.5f)
        {
            OpenCargo();
            return;
        }
        EnterVehicle();
    }

    bool IsOccupied => driverRoot != null;

    void EnterVehicle()
    {
        GameManager gm = GameManager.I;
        if (gm != null && gm.TotalInventoryCount > 0)
        {
            gm.Notify("Simpan sampah/produk ke bagasi terlebih dahulu. Roti boleh tetap dibawa.");
            return;
        }
        if (playerController == null || driverSeat == null || exitPoint == null)
        {
            Debug.LogError("Isi Player Controller, Driver Seat, dan Exit Point pada VehicleController.");
            return;
        }
        if (driverCamera == null) driverCamera = playerController.cam;
        if (driverCamera == null) { Debug.LogError("Driver Camera belum diisi."); return; }

        if (!cameraSaved)
        {
            oldCameraPosition = driverCamera.localPosition;
            oldCameraRotation = driverCamera.localRotation;
            cameraSaved = true;
        }

        driverRoot = playerController.transform;
        canExit = false;
        if (playerCharacterController == null) playerCharacterController = driverRoot.GetComponent<CharacterController>();
        if (playerCharacterController != null) playerCharacterController.enabled = false;
        playerController.enabled = false;

        // Sembunyikan model karakter selama mengemudi.
        SetPlayerVisibility(false);

        driverRoot.SetParent(driverSeat, false);
        driverRoot.localPosition = Vector3.zero;
        driverRoot.localRotation = Quaternion.identity;
        cameraYaw = 0;
        cameraPitch = drivingCameraPitch;
        driverCamera.localPosition = drivingCameraPosition;
        driverCamera.localRotation = Quaternion.Euler(cameraPitch, 0, 0);

        IsPlayerDriving = true;
        horizontal = vertical = 0;
        handbrake = false;
        prompt = "[E] Keluar mobil";
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (gm != null) gm.Notify("Masuk mobil. WASD mengemudi, Space rem tangan, E keluar.");
    }

    void ExitVehicle()
    {
        if (driverRoot == null) return;
        driverRoot.SetParent(null, true);
        driverRoot.position = exitPoint.position;
        driverRoot.rotation = exitPoint.rotation;
        if (driverCamera != null && cameraSaved)
        {
            driverCamera.localPosition = oldCameraPosition;
            driverCamera.localRotation = oldCameraRotation;
        }
        if (playerCharacterController != null) playerCharacterController.enabled = true;
        if (playerController != null) playerController.enabled = true;

        // Tampilkan kembali model karakter setelah keluar mobil.
        SetPlayerVisibility(true);

        driverRoot = null;
        IsPlayerDriving = false;
        canExit = false;
        horizontal = vertical = 0;
        prompt = "";
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (GameManager.I != null) GameManager.I.Notify("Kamu keluar dari mobil.");
    }

    // =========================================
    // PLAYER VISIBILITY
    // =========================================

    void SetPlayerVisibility(bool visible)
    {
        if (playerController == null) return;

        if (!visible)
        {
            // Simpan kondisi awal setiap Renderer karakter.
            playerRenderers =
                playerController.GetComponentsInChildren<Renderer>(true);

            playerRendererStates = new bool[playerRenderers.Length];

            for (int i = 0; i < playerRenderers.Length; i++)
            {
                if (playerRenderers[i] == null) continue;

                playerRendererStates[i] = playerRenderers[i].enabled;
                playerRenderers[i].enabled = false;
            }
        }
        else
        {
            if (playerRenderers == null || playerRendererStates == null)
                return;

            // Pulihkan kondisi Renderer seperti semula.
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                if (playerRenderers[i] != null)
                    playerRenderers[i].enabled = playerRendererStates[i];
            }

            playerRenderers = null;
            playerRendererStates = null;
        }
    }

    void Update()
    {     if (!IsOccupied && cargoPoint != null && playerController != null)
{
    float distance = Vector3.Distance(
        playerController.transform.position,
        cargoPoint.position
    );

    prompt = distance < 2.5f
        ? "[E] Buka bagasi mobil"
        : "[E] Masuk mobil";
}
        if (CargoMenuOpen)
        {
            if (Input.GetKeyDown(KeyCode.Escape)) CloseCargo();
            return;
        }

        if (IsOccupied)
        {
            if (!canExit)
            {
                if (!Input.GetKey(KeyCode.E)) canExit = true;
                return;
            }
            if (Input.GetKeyDown(KeyCode.E)) { ExitVehicle(); return; }
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
            handbrake = Input.GetKey(KeyCode.Space);
            if (driverRoot != null && driverCamera != null)
            {
                cameraYaw += Input.GetAxis("Mouse X") * mouseSensitivity;
                cameraPitch = Mathf.Clamp(cameraPitch - Input.GetAxis("Mouse Y") * mouseSensitivity, -maxLookUp, maxLookDown);
                driverRoot.localRotation = Quaternion.Euler(0, cameraYaw, 0);
                driverCamera.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
            }
        }
    }

    void OpenCargo()
    {
        if (GameManager.I == null) return;
        CargoMenuOpen = true;
        cargoModePut = true;
        cargoProductMode = false;
        hasSelection = false;
        selectedAmount = 1;
        cargoMessage = "";
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    int TotalCargo()
    {
        int total = 0;
        foreach (int n in cargoTrash) total += Mathf.Max(0, n);
        foreach (int n in cargoProducts) total += Mathf.Max(0, n);
        return total;
    }

    void OnGUI()
    {
        if (!CargoMenuOpen) return;
        GameManager gm = GameManager.I;
        if (gm == null) return;
        float w = Mathf.Min(540, Screen.width - 30), h = Mathf.Min(570, Screen.height - 30);
        GUILayout.BeginArea(new Rect((Screen.width-w)/2, (Screen.height-h)/2, w, h), GUI.skin.window);
        GUILayout.BeginVertical();
        GUILayout.Label("BAGASI MOBIL");
        GUILayout.Label("Bagasi: " + TotalCargo() + "/" + cargoCapacity +
            " | Tas: " + gm.TotalInventoryCount + "/" + gm.inventoryCapacity);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(cargoModePut ? "Masukkan ke bagasi ✓" : "Masukkan ke bagasi")) { cargoModePut = true; hasSelection = false; }
        if (GUILayout.Button(!cargoModePut ? "Ambil ke tas ✓" : "Ambil ke tas")) { cargoModePut = false; hasSelection = false; }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(!cargoProductMode ? "Sampah ✓" : "Sampah")) { cargoProductMode = false; hasSelection = false; }
        if (GUILayout.Button(cargoProductMode ? "Produk ✓" : "Produk")) { cargoProductMode = true; hasSelection = false; }
        GUILayout.EndHorizontal();

        cargoScroll = GUILayout.BeginScrollView(cargoScroll, GUILayout.ExpandHeight(true));
        if (!cargoProductMode)
        {
            foreach (TrashType t in GameManager.AllTrashTypes)
            {
                int idx = (int)t;
                int stock = cargoModePut ? gm.CountCarried(t) : cargoTrash[idx];
                GUILayout.BeginHorizontal();
                GUILayout.Label(GameManager.GetTrashName(t) + " (" + stock + ")");
                GUI.enabled = stock > 0;
                if (GUILayout.Button(hasSelection && selectedTrash == idx ? "Dipilih" : "Pilih", GUILayout.Width(80)))
                { selectedTrash = idx; hasSelection = true; selectedAmount = 1; }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }
        else
        {
            for (int i = 0; i < GameManager.ProductCount; i++)
            {
                int stock = cargoModePut ? gm.products[i] : cargoProducts[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label(GameManager.ProductNames[i] + " (" + stock + ")");
                GUI.enabled = stock > 0;
                if (GUILayout.Button(hasSelection && selectedProduct == i ? "Dipilih" : "Pilih", GUILayout.Width(80)))
                { selectedProduct = i; hasSelection = true; selectedAmount = 1; }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }

        if (hasSelection)
        {
            int available = cargoProductMode
                ? (cargoModePut ? gm.products[selectedProduct] : cargoProducts[selectedProduct])
                : (cargoModePut ? gm.CountCarried((TrashType)selectedTrash) : cargoTrash[selectedTrash]);
            int room = cargoModePut ? cargoCapacity - TotalCargo() : gm.FreeInventorySpace;
            int max = Mathf.Min(available, room);
            if (max > 0)
            {
                selectedAmount = Mathf.Clamp(selectedAmount, 1, max);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("-", GUILayout.Width(45))) selectedAmount = Mathf.Max(1, selectedAmount - 1);
                GUILayout.Label("Jumlah: " + selectedAmount);
                if (GUILayout.Button("+", GUILayout.Width(45))) selectedAmount = Mathf.Min(max, selectedAmount + 1);
                GUILayout.EndHorizontal();
                if (GUILayout.Button(cargoModePut ? "Simpan ke bagasi" : "Pindahkan ke tas")) TransferCargo(selectedAmount);
            }
            else GUILayout.Label("Kapasitas penuh atau stok kosong.");
        }
        if (!string.IsNullOrEmpty(cargoMessage)) GUILayout.Label(cargoMessage);
        GUILayout.EndScrollView();
        if (GUILayout.Button("TUTUP BAGASI", GUILayout.Height(34))) CloseCargo();
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    void TransferCargo(int n)
    {
        GameManager gm = GameManager.I;
        if (cargoProductMode)
        {
            if (cargoModePut)
            {
                gm.products[selectedProduct] -= n;
                cargoProducts[selectedProduct] += n;
            }
            else
            {
                cargoProducts[selectedProduct] -= n;
                gm.products[selectedProduct] += n;
            }
        }
        else
        {
            TrashType t = (TrashType)selectedTrash;
            if (cargoModePut)
            {
                int removed = 0;
                for (int i = gm.carried.Count - 1; i >= 0 && removed < n; i--)
                    if (gm.carried[i] == t) { gm.carried.RemoveAt(i); removed++; }
                cargoTrash[selectedTrash] += removed;
            }
            else if (gm.TryAddCarried(t, n)) cargoTrash[selectedTrash] -= n;
            else { cargoMessage = "Tas tidak cukup."; return; }
        }
        hasSelection = false;
        cargoMessage = "Pemindahan berhasil.";
    }

    void CloseCargo()
    {
        CargoMenuOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void FixedUpdate()
    {
        if (!IsOccupied) { ApplyIdleBrakes(); UpdateWheels(); return; }
        float speed = Vector3.Dot(rb.linearVelocity, transform.forward);
        float factor = Mathf.Clamp01(rb.linearVelocity.magnitude / Mathf.Max(1, maxSpeed / 3.6f));
        float steer = horizontal * maxSteerAngle * Mathf.Lerp(1, steeringAtMaxSpeed, factor);
        SetSteer(frontLeft, steer); SetSteer(frontRight, steer);

        float torque = 0;
        if (vertical > 0.05f && speed >= -0.5f && speed < maxSpeed / 3.6f) torque = vertical * motorTorque;
        else if (vertical < -0.05f && speed <= 0.5f && speed > -reverseSpeed / 3.6f) torque = vertical * motorTorque * 0.6f;
        SetMotor(rearLeft, torque); SetMotor(rearRight, torque);

        float brake = ((vertical < -0.05f && speed > 0.5f) || (vertical > 0.05f && speed < -0.5f)) ? brakeTorque : 0;
        SetBrake(frontLeft, brake); SetBrake(frontRight, brake);
        SetBrake(rearLeft, handbrake ? handbrakeTorque : brake);
        SetBrake(rearRight, handbrake ? handbrakeTorque : brake);
        rb.AddForce(-transform.up * downforce * rb.linearVelocity.magnitude);
        UpdateWheels();
    }

    void ApplyIdleBrakes()
    {
        SetBrake(frontLeft, handbrakeTorque); SetBrake(frontRight, handbrakeTorque);
        SetBrake(rearLeft, handbrakeTorque); SetBrake(rearRight, handbrakeTorque);
        SetMotor(rearLeft, 0); SetMotor(rearRight, 0);
        SetSteer(frontLeft, 0); SetSteer(frontRight, 0);
    }

    void UpdateWheels()
    {
        UpdateWheel(frontLeft, wheelVisualFL); UpdateWheel(frontRight, wheelVisualFR);
        UpdateWheel(rearLeft, wheelVisualRL); UpdateWheel(rearRight, wheelVisualRR);
    }
    void UpdateWheel(WheelCollider w, Transform v)
    {
        if (w == null || v == null) return;
        w.GetWorldPose(out Vector3 p, out Quaternion r); v.position = p; v.rotation = r;
    }
    void SetMotor(WheelCollider w, float x) { if (w != null) w.motorTorque = x; }
    void SetBrake(WheelCollider w, float x) { if (w != null) w.brakeTorque = x; }
    void SetSteer(WheelCollider w, float x) { if (w != null) w.steerAngle = x; }

    void OnDisable()
    {
        if (IsOccupied) ExitVehicle();
        if (CargoMenuOpen) CloseCargo();
    }
}
