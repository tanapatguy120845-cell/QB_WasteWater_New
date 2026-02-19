using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AdaptivePerformance;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PGroup
{
    public class NewObjectPlacement : MonoBehaviour
    {
        [Header("Placement Settings")]
        private GameObject currentPreview;
        private GameObject currentPrefab;
        public float gridSize = 0.1f;
        public LayerMask tankLayer;
        public LayerMask ignoreRaycastLayers; // Layers to ignore when clicking (e.g., WaterBounds) 

        [Header("Pipe Snap Settings")]
        public float breakDistanceMultiplier = 1.5f;

        [Header("Selection & Delete Settings")]
        public GameObject selectedObject;
        public GameObject settingPopupPrefab; // [NEW] Prefab for Delete UI
        public Color highlightColor = Color.red;
        private Color originalColor;
        private SpriteRenderer selectedRenderer;

        [Header("Prefab Selector UI")]
        public GameObject prefabSelectMachinePrefab; // assign PrefabSelectMachine UI prefab in Inspector (optional)
        private GameObject activePrefabSelector; // current instantiated selector UI
        private GameObject prefabSelectorTarget; // which GameObject the selector is for (so clicks on same object don't close it)

        private float lastClickTime;
        private float doubleClickThreshold = 0.3f;

        [Header("Move State")]
        private GameObject movingObject;
        private Vector3 originalPosition;
        private Transform originalParent;

        [Header("Selection Placements")]
        [SerializeField] private LayerMask placementLayer;

        //PGroup
        private Transform placeChecker = null;

        public static Action<string> OnShowHighlight;
        private void Start()
        {
            //Turn off all HL
            OnShowHighlight?.Invoke("");
        }

        void Update()
        {
            if (currentPreview != null || movingObject != null)
            {
                HandlePlacement();
            }
            else
            {
                HandleSelection();
            }
        }

        // --- 1. ระบบการวางและ Snap ---
        void HandlePlacement()
        {
            Vector2 mousePixelPos = Mouse.current.position.ReadValue();
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(mousePixelPos);
            mousePos.z = 0;

            GameObject activeObj = movingObject != null ? movingObject : currentPreview;
            if (activeObj == null) return;

            Vector3 targetPos = mousePos;
            bool canPlace = true; // ตัวแปรเช็คว่าตำแหน่งนี้วางได้หรือไม่

            // เช็คประเภทวัตถุจาก Component และ Tag
            IPipeConnector previewConn = activeObj.GetComponent<IPipeConnector>();
            bool isDeviceOrFloat = activeObj.CompareTag("Device") || activeObj.CompareTag("Float");

            // ตรรกะการ Snap
            if (previewConn != null)
            {
                // ถ้าเป็นท่อ ให้ Snap เข้าหาจุดเชื่อมท่อ
                targetPos = CalculatePipeSnap(previewConn, mousePos);
            }
            else if (isDeviceOrFloat)
            {
                // ถ้าเป็น Device/Float ให้ดูดเข้าหาขอบ Tank หรือ Marker
                targetPos = CalculateTankSnap(mousePos);

                if (activeObj.GetComponent<PlacementChecker>())
                {
                    Debug.Log(activeObj.GetComponent<PlacementChecker>().placementName);
                    OnShowHighlight?.Invoke(activeObj.GetComponent<PlacementChecker>().placementName);

                    // [แก้ไข] เช็คว่าถ้ามัน Snap เข้าหา Marker (TankFloat) ได้ เราจะยอมให้วางได้ทันที
                    // โดยไม่ต้องเช็คว่ามี Tank Collider รองรับไหม เพราะถังบางรุ่น Collider มันเล็ก (มีแค่ที่พื้น)
                    Collider2D hitMarker = Physics2D.OverlapCircle(targetPos, 0.05f, placementLayer);
                    if (hitMarker != null && hitMarker.name.Contains(activeObj.GetComponent<PlacementChecker>().placementName))
                    {
                        if (hitMarker.transform.localScale.x < 0 && activeObj.transform.localScale.x > 0)
                        {
                            activeObj.transform.localScale = new Vector3(-activeObj.transform.localScale.x, activeObj.transform.localScale.y, activeObj.transform.localScale.z);
                        }
                        else if (hitMarker.transform.localScale.x > 0 && activeObj.transform.localScale.x < 0)
                        {
                            activeObj.transform.localScale = new Vector3(-activeObj.transform.localScale.x, activeObj.transform.localScale.y, activeObj.transform.localScale.z);
                        }
                        placeChecker = hitMarker.transform;
                        canPlace = true;
                    }
                    else
                    {
                        placeChecker = null;
                        canPlace = false;
                        // ถ้าไม่ได้ทับ Marker ให้เช็คว่าทับตัวถังจริงๆ ไหม
                        Collider2D hitTank = Physics2D.OverlapCircle(targetPos, 0.05f, placementLayer);
                        if (hitTank == null)
                        {
                            canPlace = false; // ถ้าไม่มี Tank อยู่ข้างหลัง จะวางไม่ได้
                        }
                    }
                }
            }
            else
            {
                // วัตถุทั่วไป ใช้ Grid Snap
                float snappedX = Mathf.Round(mousePos.x / gridSize) * gridSize;
                float snappedY = Mathf.Round(mousePos.y / gridSize) * gridSize;
                targetPos = new Vector3(snappedX, snappedY, 0);
            }

            // อัปเดตตำแหน่ง Preview
            activeObj.transform.position = targetPos;

            // ระบบเปลี่ยนสี Visual Feedback
            UpdatePreviewVisual(activeObj, canPlace);

            // ยืนยันการวาง
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (canPlace)
                {
                    // 🌟 เพิ่มการเช็ค Capacity ก่อนวางจริง
                    if (IsTankFullAtPosition(targetPos))
                    {
                        Debug.LogWarning("ถังนี้เต็มแล้ว! ไม่สามารถวางเพิ่มได้");
                        // คุณอาจจะสั่งเปิด UI Popup แจ้งเตือนตรงนี้ก็ได้
                    }
                    else
                    {
                        Debug.Log($"Place : {placeChecker}");
                        Debug.Log($"Target : {targetPos}");
                        PlaceObject(targetPos, placeChecker);
                    }
                }
                else
                {
                    Debug.LogWarning("ตำแหน่งนี้วางไม่ได้!");
                }
            }

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (movingObject != null)
                {
                    CancelMoving();
                }
                else
                {
                    CancelPlacement();
                }
            }
        }

        // ระบบดูดท่อเข้าหากัน (Magnetic Pipe Snap)
        Vector3 CalculatePipeSnap(IPipeConnector previewConn, Vector3 mousePos)
        {
            foreach (Transform myPoint in previewConn.GetAllPoints())
            {
                if (myPoint == null) continue;
                Collider2D[] hitColliders = Physics2D.OverlapCircleAll(myPoint.position, previewConn.snapRange);

                foreach (var hit in hitColliders)
                {
                    IPipeConnector otherConn = hit.GetComponentInParent<IPipeConnector>();
                    GameObject activeObj = movingObject != null ? movingObject : currentPreview;
                    if (otherConn != null && hit.transform.parent != activeObj.transform)
                    {
                        foreach (Transform otherPoint in otherConn.GetAllPoints())
                        {
                            if (Vector2.Distance(myPoint.position, otherPoint.position) <= previewConn.snapRange)
                            {
                                Vector3 offset = myPoint.position - currentPreview.transform.position;
                                Vector3 snappedPos = otherPoint.position - offset;

                                // เช็คระยะสะบัดหลุด (Break Distance)
                                if (Vector2.Distance(mousePos, snappedPos) < previewConn.snapRange * breakDistanceMultiplier)
                                    return snappedPos;
                            }
                        }
                    }
                }
            }
            return mousePos;
        }

        // ระบบดูดเข้าหาขอบ Tank (Closest Point Snap)
        Vector3 CalculateTankSnap(Vector3 mousePos)
        {
            // 1. ลองหา Marker (TankFloat) ในรัศมีใกล้ๆ ก่อนเพื่อให้ Snap เข้าจุดระดับน้ำได้ง่าย
            Collider2D[] hits = Physics2D.OverlapCircleAll(mousePos, 0.5f, tankLayer);
            Collider2D bestHit = null;

            foreach (var hit in hits)
            {
                if (hit.name.Contains("TankFloat"))
                {
                    bestHit = hit;
                    return hit.transform.position; // เจอจุดระดับน้ำ ให้ดูดเข้าหาจุดนั้นทันที
                }
                if (bestHit == null) bestHit = hit;
            }

            if (bestHit != null)
            {
                Vector3 closestPoint = bestHit.ClosestPoint(mousePos);
                if (Vector2.Distance(mousePos, closestPoint) < 0.5f * breakDistanceMultiplier)
                    return closestPoint;
            }
            return mousePos;
        }

        // เปลี่ยนสี Preview ตามสถานะ canPlace
        void UpdatePreviewVisual(GameObject obj, bool canPlace)
        {
            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // ถ้าวางได้ให้เป็นสีขาวใส (0.5f) ถ้าวางไม่ได้ให้เป็นสีแดงใส
                sr.color = canPlace ? new Color(1, 1, 1, 0.5f) : new Color(1, 0, 0, 0.5f);
            }
        }

        void PlaceObject(Vector3 position,Transform newParent)
        {
            OnShowHighlight?.Invoke("");

            GameObject newObj;
            if (movingObject != null)
            {
                newObj = movingObject;
                newObj.transform.position = position;
                newObj.transform.parent = originalParent;
                // เปิด Collider คืน
                foreach (Collider2D col in newObj.GetComponentsInChildren<Collider2D>()) col.enabled = true;

                // คืนค่าสีตัวเลือก (Alpha = 1)
                SpriteRenderer sr = newObj.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = Color.white;

                movingObject = null;
            }
            else
            {
                newObj = Instantiate(currentPrefab, position, currentPreview.transform.rotation);
                newObj.transform.parent = GameplayController.instance.pool;
            }

            // ใช้ OverlapCircle ตรวจจับ Tank/Marker ใกล้จุดวาง
            Collider2D[] placementHits = Physics2D.OverlapCircleAll(position, 0.2f, tankLayer);
            Collider2D bestPlacementHit = null;

            foreach (var hit in placementHits)
            {
                if (hit.name.Contains("TankFloat"))
                {
                    bestPlacementHit = hit;
                    newObj.transform.position = hit.transform.position; // Snap ทับจุดระดับน้ำเป๊ะๆ
                    break;
                }
                if (bestPlacementHit == null) bestPlacementHit = hit;
            }

            if (bestPlacementHit != null)
            {
                TankData tankData = bestPlacementHit.GetComponentInParent<TankData>();
                Transform parentTransform = (tankData != null) ? tankData.transform : bestPlacementHit.transform;

                if (placeChecker != null && !newObj.CompareTag("Tank"))
                {
                    Vector3 getScale = newObj.transform.localScale;
                    newObj.transform.SetParent(newParent);
                    newObj.transform.localPosition = Vector3.zero;
                    newObj.transform.localEulerAngles = Vector3.zero;
                    newObj.transform.localScale = getScale;
                    if (newObj.GetComponent<SpriteRenderer>())
                    {
                        newObj.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, .8f);
                    }
                    Debug.Log(newObj);
                }

                if (newObj.GetComponent<Rigidbody2D>() && parentTransform.GetComponent<Rigidbody2D>())
                {
                    FixedJoint2D joint = newObj.GetComponent<FixedJoint2D>();
                    if (joint == null) joint = newObj.AddComponent<FixedJoint2D>();
                    joint.connectedBody = parentTransform.GetComponent<Rigidbody2D>();
                }
            }

            // ล้าง preview หลังวาง
            CancelPlacement();
        }

        public void StartMoving(GameObject obj)
        {
            DeselectCurrent();
            movingObject = obj;
            originalPosition = obj.transform.position;
            originalParent = obj.transform.parent;

            // ดึงออกจาก Parent ชั่วคราวเพื่อให้ไม่ติด Snap ตัวเอง
            obj.transform.SetParent(null);

            // ปิด Collider ของตัวที่จะย้าย
            foreach (Collider2D col in obj.GetComponentsInChildren<Collider2D>()) col.enabled = false;

            // ลบ Joint เดิมถ้ามี
            FixedJoint2D joint = obj.GetComponent<FixedJoint2D>();
            if (joint != null) Destroy(joint);
        }

        void CancelMoving()
        {
            if (movingObject != null)
            {
                movingObject.transform.position = originalPosition;
                movingObject.transform.SetParent(originalParent);

                // เปิด Collider คืน
                foreach (Collider2D col in movingObject.GetComponentsInChildren<Collider2D>()) col.enabled = true;

                // คืนค่า Joint ถ้ามี Rigidbody
                if (movingObject.GetComponent<Rigidbody2D>() && originalParent != null && originalParent.GetComponent<Rigidbody2D>())
                {
                    FixedJoint2D joint = movingObject.AddComponent<FixedJoint2D>();
                    joint.connectedBody = originalParent.GetComponent<Rigidbody2D>();
                }

                // คืนค่าสี
                SpriteRenderer sr = movingObject.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = Color.white;

                movingObject = null;
            }
        }

        /// <summary>
        /// เช็คว่า GameObject นี้เป็น Device ที่อยู่ใน Tank หรือไม่
        /// </summary>
        bool IsDeviceInTank(GameObject obj)
        {
            if (obj == null) return false;

            // เช็คว่าเป็น Device หรือ Float
            bool isDevice = obj.CompareTag("Device") || obj.CompareTag("Float");
            if (!isDevice) return false;

            // เช็คว่า parent เป็น Tank หรือไม่
            Transform parent = obj.transform.parent;
            if (parent != null)
            {
                // เช็คจาก TankData component หรือ Layer
                if (parent.GetComponent<TankData>() != null) return true;
                if (((1 << parent.gameObject.layer) & tankLayer) != 0) return true;
            }

            return false;
        }

        /// <summary>
        /// เปิด SelectMachine popup เหนืออุปกรณ์ (เหมือน Bangpla_Anim)
        /// </summary>
        void OpenSelectMachineForDevice(GameObject deviceObj)
        {
            // ปิด popup เก่าถ้ามี
            if (activePrefabSelector != null)
            {
                Destroy(activePrefabSelector);
                activePrefabSelector = null;
                prefabSelectorTarget = null;
            }

            // หา Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("OpenSelectMachineForDevice: Canvas not found!");
                return;
            }

            // ดึงชื่อเครื่องจาก DeviceComponent.displayName (ถ้ามี) - จะได้ชื่อที่โหลดจาก API
            DeviceComponent deviceComp = deviceObj.GetComponent<DeviceComponent>();
            string machineName = (deviceComp != null && !string.IsNullOrEmpty(deviceComp.displayName))
                ? deviceComp.displayName
                : deviceObj.name.Replace("(Clone)", "").Trim();
            string tankName = "unnamed tank";

            Transform parent = deviceObj.transform.parent;
            if (parent != null)
            {
                TankData tankData = parent.GetComponent<TankData>();
                if (tankData != null)
                    tankName = tankData.displayName;
                else
                    tankName = parent.name;
            }

            // ใช้ PopupManager ถ้ามี
            if (PopupManager.Instance != null && PopupManager.Instance.selectMachinePrefab != null)
            {
                activePrefabSelector = Instantiate(PopupManager.Instance.selectMachinePrefab, canvas.transform);
            }
            else if (prefabSelectMachinePrefab != null)
            {
                activePrefabSelector = Instantiate(prefabSelectMachinePrefab, canvas.transform);
            }
            else
            {
                Debug.LogWarning("OpenSelectMachineForDevice: No SelectMachine prefab assigned!");
                return;
            }

            prefabSelectorTarget = deviceObj;

            // ตั้งตำแหน่ง popup เหนืออุปกรณ์
            SelectMachine selectMachine = activePrefabSelector.GetComponent<SelectMachine>();
            if (selectMachine != null && selectMachine.panel != null)
            {
                // แปลง World Position เป็น Screen Position
                Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, deviceObj.transform.position);

                // แปลง Screen Position เป็น Local Position ใน Canvas
                RectTransform canvasRect = canvas.transform as RectTransform;
                RectTransform panelRect = selectMachine.panel.GetComponent<RectTransform>();

                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, canvas.worldCamera, out localPoint);

                // ปรับให้ popup อยู่เหนืออุปกรณ์
                localPoint.y += 280f;

                // ตั้งค่า anchor ให้เป็น center ก่อน
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = localPoint;

                // ตั้งค่า popup พร้อม DeviceComponent
                selectMachine.SetPanel(machineName, tankName, deviceComp,
                    (id, tankId) => { Debug.Log($"Selected: {id} in {tankId}"); },
                    (id, tankId) => {
                        Debug.Log($"Cancelled: {id}");
                        activePrefabSelector = null;
                        prefabSelectorTarget = null;
                    }
                );
            }
        }

        /// <summary>
        /// เปิด Delete Popup เหนืออุปกรณ์ (Edit Mode)
        /// </summary>
        void OpenDeletePopup(GameObject targetObj)
        {
            // ปิด popup เก่าถ้ามี
            if (activePrefabSelector != null)
            {
                Destroy(activePrefabSelector);
                activePrefabSelector = null;
                prefabSelectorTarget = null;
            }

            if (settingPopupPrefab == null)
            {
                Debug.LogWarning("OpenDeletePopup: No DeletePopup prefab assigned!");
                return;
            }

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;

            // Instantiate
            activePrefabSelector = Instantiate(settingPopupPrefab, canvas.transform);
            prefabSelectorTarget = targetObj;

            //WaterLevelController waterLevelController = prefabSelectorTarget.GetComponent<TankData>().waterLevelScript;
            //activePrefabSelector.GetComponent<SettingPopup>().SetupValue(waterLevelController.GetBaseWaterLevel(), waterLevelController.GetDirtyWaterLevel());

            // Position Logic (เหมือน SelectMachine)
            /*SettingPopup settingPopup = activePrefabSelector.GetComponent<SettingPopup>();
            if (settingPopup != null)
            {
                RectTransform panelRect = activePrefabSelector.GetComponent<RectTransform>(); // สมมติว่าตัว root มี RectTransform

                // แปลง World Position เป็น Screen Position
                Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, targetObj.transform.position);

                // แปลง Screen Position เป็น Local Position ใน Canvas
                RectTransform canvasRect = canvas.transform as RectTransform;
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, canvas.worldCamera, out localPoint);

                localPoint.y += 170f; //สำหรับกันบังเมาส์

                if (panelRect != null)
                {
                    panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                    panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                    panelRect.anchoredPosition = localPoint;
                }

                Debug.Log(targetObj);
                if (targetObj.GetComponent<TankData>() != null)
                {
                    Debug.Log("Setup Tank");
                    settingPopup.SetupValue(targetObj.GetComponent<TankData>().waterLevelScript.GetBaseWaterLevel(), targetObj.GetComponent<TankData>().waterLevelScript.GetDirtyWaterLevel());
                    settingPopup.SelectTypeSetting("Tank");
                    settingPopup.SetupTank(
                    onDelete: () => {
                        DeleteSelectedObject();
                    },
                    onMove: () => {
                        StartMoving(targetObj);
                    },
                    onCancel: () => {
                        activePrefabSelector = null;
                        prefabSelectorTarget = null;
                    },
                    onWaterPlus: () => {
                        prefabSelectorTarget.GetComponent<TankData>().waterLevelScript.SetupBaseWater(true);
                    },
                    onWaterMinus: () => {
                        prefabSelectorTarget.GetComponent<TankData>().waterLevelScript.SetupBaseWater(false);
                    },
                    onDirtyWaterPlus: () => {
                        prefabSelectorTarget.GetComponent<TankData>().waterLevelScript.SetupDirtyWater(true);
                    },
                    onDirtyWaterMinus: () => {
                        prefabSelectorTarget.GetComponent<TankData>().waterLevelScript.SetupDirtyWater(false);
                    }
                    );
                }
                else if (targetObj.GetComponent<PGroup.PipeController>() != null)
                {
                    Debug.Log("Setup Pipe");
                    settingPopup.SelectTypeSetting("Pipe");
                    settingPopup.SetupPipe(
                        onPipeToggleWater: () => {
                            prefabSelectorTarget.GetComponent<PipeController>().SetWater();
                        },
                        onDelete: () => {
                            DeleteSelectedObject();
                        },
                        onChangeWaterColor: () => {
                            prefabSelectorTarget.GetComponent<PipeController>().ChangeWater();
                        }
                    );
                }
            }*/
        }

        // --- ระบบเลือกและลบ ---
        void HandleSelection()
        {
            // ถ้าเมาส์กำลังชี้อยู่บน UI (ปุ่ม Level ต่างๆ) ไม่ต้องทำการ Select/Deselect วัตถุในฉาก
            if (EventSystem.current.IsPointerOverGameObject()) return;

            if (Mouse.current.leftButton.wasPressedThisFrame) SelectObjectAtMouse();
            if (Keyboard.current.deleteKey.wasPressedThisFrame && selectedObject != null) DeleteSelectedObject();
        }

        void SelectObjectAtMouse()
        {
            if (GameplayController.instance.isOnBuildPipe) return;
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            // Skip WaterBounds and other ignored layers
            int layerMask = ~ignoreRaycastLayers.value;
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, layerMask);

            // If we currently have an active selector UI, and the click is on empty space or on a different object, destroy it
            if (activePrefabSelector != null)
            {
                if (hit.collider == null || hit.collider.gameObject != prefabSelectorTarget)
                {
                    Destroy(activePrefabSelector);
                    activePrefabSelector = null;
                    prefabSelectorTarget = null;
                }
            }

            if (hit.collider != null)
            {
                DeselectCurrent(); // เคลียร์ตัวเก่าก่อนเลือกตัวใหม่
                selectedObject = hit.collider.gameObject;
                if (selectedObject.GetComponent<PipeController>())
                {
                    GameplayController.instance.GetSelectedObject(selectedObject.transform.parent.gameObject);
                }
                else
                {
                    GameplayController.instance.GetSelectedObject(selectedObject);
                }

                TankData tankData = selectedObject.GetComponent<TankData>();
                DeviceComponent deviceData = selectedObject.GetComponent<DeviceComponent>();

                // เช็คว่าอยู่ใน Edit Mode หรือไม่
                bool isEditMode = SaveManager.Instance != null && SaveManager.Instance.IsEditMode;

                if (isEditMode)
                {
                    // === EDIT MODE ===
                    if (Time.time - lastClickTime < doubleClickThreshold)
                    {
                        // Double Click: Rename
                        if (activePrefabSelector != null)
                        {
                            Destroy(activePrefabSelector);
                            activePrefabSelector = null;
                            prefabSelectorTarget = null;
                        }

                        if (tankData != null && TankManager.Instance.renamePanel != null)
                            TankManager.Instance.renamePanel.OpenRenamePanel(tankData);
                        else if (deviceData != null && TankManager.Instance.deviceRenamePanel != null)
                            TankManager.Instance.deviceRenamePanel.OpenRenamePanel(deviceData);
                    }
                    else
                    {
                        // Single Click: Open Delete Popup
                        //OpenDeletePopup(selectedObject);
                    }
                }
                else
                {
                    // === VIEW MODE: Single Click = SelectMachine popup, ไม่เปิด Rename ===
                    if (IsDeviceInTank(selectedObject))
                    {
                        OpenSelectMachineForDevice(selectedObject);
                    }
                }

                if (tankData != null) TankManager.Instance.SelectTank(tankData);

                lastClickTime = Time.time;


                selectedRenderer = selectedObject.GetComponent<SpriteRenderer>();
                if (selectedRenderer != null)
                {
                    if (selectedRenderer.color == Color.white || selectedRenderer.color == new Color(1, 1, 1, .8f))
                    {
                        originalColor = selectedRenderer.color;
                        selectedRenderer.color = highlightColor;
                    }
                }
                // ทำ Highlight (ยกเว้น Device ที่มี DeviceComponent - ให้ DeviceComponent จัดการสีเอง)
                //DeviceComponent deviceComp = selectedObject.GetComponent<DeviceComponent>();
                /*if (deviceComp == null)
                {
                    selectedRenderer = selectedObject.GetComponent<SpriteRenderer>();
                    if (selectedRenderer != null)
                    {
                        originalColor = selectedRenderer.color;
                        selectedRenderer.color = highlightColor;
                    }
                }
                else
                {
                    // Device มี DeviceComponent - ไม่ยุ่งกับสี
                    selectedRenderer = null;
                }*/
            }
            else
            {
                // 🌟 ถ้าคลิกไม่โดนอะไรเลย (คลิกที่ว่าง) ให้ปิด popup และปล่อยตัวที่เลือก
                if (activePrefabSelector != null)
                {
                    Destroy(activePrefabSelector);
                    activePrefabSelector = null;
                    prefabSelectorTarget = null;
                }

                DeselectCurrent();
                if (TankManager.Instance != null) TankManager.Instance.selectedTank = null;
            }
        }

        void DeselectCurrent()
        {
            // Reset สีเฉพาะ object ที่ไม่ใช่ Device (selectedRenderer จะเป็น null สำหรับ Device)
            if (selectedObject != null && selectedRenderer != null)
            {
                if (selectedRenderer.color == Color.red)
                    selectedRenderer.color = originalColor;
            }
            selectedObject = null;
            selectedRenderer = null;
        }

        public void DeleteSelectedObject()
        {
            if (selectedObject != null)
            {
                GameObject objToDestroy = selectedObject;

                // ปิด Popup ถ้ามีค้างอยู่ (แก้ปัญหา Delete Key แล้ว Popup ไม่หาย)
                if (activePrefabSelector != null)
                {
                    Destroy(activePrefabSelector);
                    activePrefabSelector = null;
                    prefabSelectorTarget = null;
                }

                if (TankManager.Instance != null)
                {
                    if (TankManager.Instance.renamePanel != null) TankManager.Instance.renamePanel.gameObject.SetActive(false);
                    if (TankManager.Instance.deviceRenamePanel != null) TankManager.Instance.deviceRenamePanel.gameObject.SetActive(false);
                }

                DeselectCurrent();
                Destroy(objToDestroy);
            }
        }

        public void StartPlacement(GameObject prefab)
        {
            DeselectCurrent();
            if (currentPreview != null) Destroy(currentPreview);
            currentPrefab = prefab;
            currentPreview = Instantiate(prefab);

            // ปิด Collider ของตัว Preview และลูกๆ ทั้งหมด
            foreach (Collider2D col in currentPreview.GetComponentsInChildren<Collider2D>()) col.enabled = false;
        }

        void CancelPlacement()
        {
            if (currentPreview != null) Destroy(currentPreview);
            currentPreview = null;
            currentPrefab = null;

            OnShowHighlight?.Invoke("");
        }

        private void OnDrawGizmos()
        {
            if (currentPreview != null)
            {
                IPipeConnector connScript = currentPreview.GetComponent<IPipeConnector>();
                if (connScript != null)
                {
                    Gizmos.color = Color.yellow;
                    foreach (Transform p in connScript.GetAllPoints())
                        if (p != null) Gizmos.DrawWireSphere(p.position, connScript.snapRange);
                }
            }
        }

        bool IsTankFullAtPosition(Vector3 position)
        {
            GameObject activeObj = movingObject != null ? movingObject : currentPreview;
            if (activeObj == null) return false;

            Collider2D tankHit = Physics2D.OverlapCircle(position, 0.1f, tankLayer);

            if (tankHit != null)
            {
                TankData tankData = tankHit.GetComponent<TankData>();
                if (tankData != null)
                {
                    bool isDeviceOrFloat = activeObj.CompareTag("Device") || activeObj.CompareTag("Float");

                    if (isDeviceOrFloat)
                    {
                        return !tankData.CanAddDevice();
                    }
                }
            }
            return false;
        }
    }
}
