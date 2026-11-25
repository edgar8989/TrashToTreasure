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

    static GUIStyle frameStyle, panelStyle, headerStyle, sectionStyle, bodyStyle;
    static GUIStyle rowStyle, selectedRowStyle, buttonStyle, activeTabStyle;
    static GUIStyle primaryButtonStyle, closeButtonStyle, mutedStyle;
    static Texture2D frameTexture, panelTexture, headerTexture, rowTexture, selectedRowTexture, buttonTexture, activeTexture, primaryTexture, closeTexture;
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

    void EnsureCargoStyles()
    {
        if (frameStyle != null) return;

        frameTexture = MakeTexture(new Color(0.18f, 0.40f, 0.74f, 1f));
        panelTexture = MakeTexture(new Color(0.98f, 0.93f, 0.84f, 1f));
        headerTexture = MakeTexture(new Color(0.17f, 0.48f, 0.86f, 1f));
        rowTexture = MakeTexture(new Color(0.99f, 0.97f, 0.90f, 1f));
        selectedRowTexture = MakeTexture(new Color(0.85f, 0.94f, 0.77f, 1f));
        buttonTexture = MakeTexture(new Color(0.27f, 0.55f, 0.84f, 1f));
        activeTexture = MakeTexture(new Color(0.40f, 0.77f, 0.14f, 1f));
        primaryTexture = MakeTexture(new Color(0.46f, 0.82f, 0.16f, 1f));
        closeTexture = MakeTexture(new Color(0.87f, 0.17f, 0.15f, 1f));

        frameStyle = MakeStyle(frameTexture, Color.white, 14, FontStyle.Normal, TextAnchor.MiddleCenter);
        panelStyle = MakeStyle(panelTexture, new Color(0.20f, 0.16f, 0.12f), 14, FontStyle.Normal, TextAnchor.UpperLeft);
        headerStyle = MakeStyle(headerTexture, Color.white, 24, FontStyle.Bold, TextAnchor.MiddleCenter);
        sectionStyle = MakeStyle(null, new Color(0.27f, 0.20f, 0.12f), 14, FontStyle.Bold, TextAnchor.MiddleCenter);
        bodyStyle = MakeStyle(null, new Color(0.22f, 0.16f, 0.12f), 14, FontStyle.Normal, TextAnchor.MiddleLeft);
        rowStyle = MakeStyle(rowTexture, new Color(0.20f, 0.16f, 0.12f), 14, FontStyle.Normal, TextAnchor.MiddleLeft);
        selectedRowStyle = MakeStyle(selectedRowTexture, new Color(0.14f, 0.25f, 0.10f), 14, FontStyle.Bold, TextAnchor.MiddleLeft);
        buttonStyle = MakeStyle(buttonTexture, Color.white, 13, FontStyle.Bold, TextAnchor.MiddleCenter);
        activeTabStyle = MakeStyle(activeTexture, Color.white, 13, FontStyle.Bold, TextAnchor.MiddleCenter);
        primaryButtonStyle = MakeStyle(primaryTexture, Color.white, 16, FontStyle.Bold, TextAnchor.MiddleCenter);
        closeButtonStyle = MakeStyle(closeTexture, Color.white, 15, FontStyle.Bold, TextAnchor.MiddleCenter);
        mutedStyle = MakeStyle(null, new Color(0.42f, 0.32f, 0.18f), 12, FontStyle.Italic, TextAnchor.MiddleLeft);
    }

    Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    GUIStyle MakeStyle(Texture2D background, Color textColor, int fontSize, FontStyle fontStyle, TextAnchor alignment)
    {
        GUIStyle style = new GUIStyle(background == null ? GUI.skin.label : GUI.skin.button);
        style.fontSize = fontSize;
        style.fontStyle = fontStyle;
        style.alignment = alignment;
        style.wordWrap = true;
        style.padding = new RectOffset(12, 12, 7, 7);
        style.margin = new RectOffset(2, 2, 2, 2);
        style.normal.textColor = textColor;
        style.hover.textColor = textColor;
        style.active.textColor = textColor;
        if (background != null)
        {
            style.normal.background = background;
            style.hover.background = background;
            style.active.background = background;
        }
        return style;
    }

    void OnGUI()
    {
        if (!CargoMenuOpen) return;
        GameManager gm = GameManager.I;
        if (gm == null) return;

        EnsureCargoStyles();

        float width = Mathf.Min(680f, Screen.width - 28f);
        float height = Mathf.Min(610f, Screen.height - 28f);
        float x = (Screen.width - width) * 0.5f;
        float y = (Screen.height - height) * 0.5f;

        GUI.Box(new Rect(x, y + 14, width, height), GUIContent.none, frameStyle);
        GUI.Box(new Rect(x + 5, y + 19, width - 10, height - 10), GUIContent.none, panelStyle);

        float ribbonWidth = Mathf.Min(width - 110f, 410f);
        float ribbonX = x + (width - ribbonWidth) * 0.5f;
        GUI.Box(new Rect(ribbonX, y, ribbonWidth, 50f), GUIContent.none, headerStyle);
        GUI.Label(new Rect(ribbonX, y + 2, ribbonWidth, 44f), "BAGASI MOBIL", headerStyle);

        if (GUI.Button(new Rect(x + width - 62f, y + 8f, 40f, 40f), "X", closeButtonStyle))
        {
            CloseCargo();
            return;
        }

        GUILayout.BeginArea(new Rect(x + 20, y + 64, width - 40, height - 76));
        GUILayout.BeginVertical();

        GUILayout.Label("Isi bagasi: " + TotalCargo() + "/" + cargoCapacity + "    |    Isi tas: " + gm.TotalInventoryCount + "/" + gm.inventoryCapacity, sectionStyle);
        GUILayout.Space(8);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(cargoModePut ? "MASUKKAN KE BAGASI" : "Masukkan ke bagasi", cargoModePut ? activeTabStyle : buttonStyle, GUILayout.Height(38))) { cargoModePut = true; hasSelection = false; }
        if (GUILayout.Button(!cargoModePut ? "AMBIL KE TAS" : "Ambil ke tas", !cargoModePut ? activeTabStyle : buttonStyle, GUILayout.Height(38))) { cargoModePut = false; hasSelection = false; }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(!cargoProductMode ? "SAMPAH" : "Sampah", !cargoProductMode ? activeTabStyle : buttonStyle, GUILayout.Height(34))) { cargoProductMode = false; hasSelection = false; }
        if (GUILayout.Button(cargoProductMode ? "PRODUK" : "Produk", cargoProductMode ? activeTabStyle : buttonStyle, GUILayout.Height(34))) { cargoProductMode = true; hasSelection = false; }
        GUILayout.EndHorizontal();
        GUILayout.Space(8);

        if (!cargoProductMode)
        {
            foreach (TrashType t in GameManager.AllTrashTypes)
            {
                int idx = (int)t;
                int stock = cargoModePut ? gm.CountCarried(t) : cargoTrash[idx];
                bool isSelected = hasSelection && selectedTrash == idx;
                GUILayout.BeginHorizontal(isSelected ? selectedRowStyle : rowStyle, GUILayout.Height(38));
                GUILayout.Label(GameManager.GetTrashName(t) + " (" + stock + ")", bodyStyle, GUILayout.ExpandWidth(true));
                GUI.enabled = stock > 0;
                if (GUILayout.Button(isSelected ? "DIPILIH" : "PILIH", isSelected ? activeTabStyle : buttonStyle, GUILayout.Width(112), GUILayout.Height(30)))
                { selectedTrash = idx; hasSelection = true; selectedAmount = 1; }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.Space(3);
            }
        }
        else
        {
            for (int i = 0; i < GameManager.ProductCount; i++)
            {
                int stock = cargoModePut ? gm.products[i] : cargoProducts[i];
                bool isSelected = hasSelection && selectedProduct == i;
                GUILayout.BeginHorizontal(isSelected ? selectedRowStyle : rowStyle, GUILayout.Height(38));
                GUILayout.Label(GameManager.ProductNames[i] + " (" + stock + ")", bodyStyle, GUILayout.ExpandWidth(true));
                GUI.enabled = stock > 0;
                if (GUILayout.Button(isSelected ? "DIPILIH" : "PILIH", isSelected ? activeTabStyle : buttonStyle, GUILayout.Width(112), GUILayout.Height(30)))
                { selectedProduct = i; hasSelection = true; selectedAmount = 1; }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
                GUILayout.Space(3);
            }
        }

        if (hasSelection)
        {
            int available = cargoProductMode
                ? (cargoModePut ? gm.products[selectedProduct] : cargoProducts[selectedProduct])
                : (cargoModePut ? gm.CountCarried((TrashType)selectedTrash) : cargoTrash[selectedTrash]);
            int room = cargoModePut ? cargoCapacity - TotalCargo() : gm.FreeInventorySpace;
            int max = Mathf.Min(available, room);
            GUILayout.Space(8);
            if (max > 0)
            {
                selectedAmount = Mathf.Clamp(selectedAmount, 1, max);
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("-", buttonStyle, GUILayout.Width(48), GUILayout.Height(34))) selectedAmount = Mathf.Max(1, selectedAmount - 1);
                GUILayout.Label("JUMLAH: " + selectedAmount + " / " + max, sectionStyle, GUILayout.Width(170), GUILayout.Height(34));
                if (GUILayout.Button("+", buttonStyle, GUILayout.Width(48), GUILayout.Height(34))) selectedAmount = Mathf.Min(max, selectedAmount + 1);
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                if (GUILayout.Button(cargoModePut ? "SIMPAN KE BAGASI" : "PINDAHKAN KE TAS", primaryButtonStyle, GUILayout.Height(42)))
                    TransferCargo(selectedAmount);
            }
            else GUILayout.Label("Kapasitas penuh atau stok kosong.", mutedStyle);
        }

        if (!string.IsNullOrEmpty(cargoMessage)) GUILayout.Label(cargoMessage, mutedStyle);

        GUILayout.FlexibleSpace();
        if (GUILayout.Button("TUTUP BAGASI", buttonStyle, GUILayout.Height(40))) CloseCargo();
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
