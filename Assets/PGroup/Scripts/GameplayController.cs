using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace PGroup
{
    public class GameplayController : MonoBehaviour
    {
        public static GameplayController instance;

        public Transform pool;
        public bool isOnBuildPipe;
        public bool isEditMode;

        [Header("Scripts")]
        [SerializeField] private NewObjectPlacement newObjectPlacement;
        [SerializeField] private PipeInputManager pipeInputManager;
        [SerializeField] private NewPipeBuilder newPipeBuilder;

        [Header("EditMode")]
        [SerializeField] private GameObject highlightEditMode;

        [Header("Item Container")]
        [SerializeField] private GameObject itemContrainer;
        [SerializeField] private GameObject[] panelContents;
        [SerializeField] private TextMeshProUGUI itemContentText;
        [SerializeField] private GameObject[] highlightButton;
        [SerializeField] private RectTransform rectContent;

        [Header("Object")]
        public GameObject selectedObject;

        [Header("Pipe Control")]
        [SerializeField] private GameObject highlightPipe;
        [SerializeField] private GameObject popupConnectPipe;
        [SerializeField] private GameObject straightPipePrefab;
        private List<GameObject> connectPipe;
        private Transform currentPipeGroup;

        [Header("Grid")]
        [SerializeField] private GameObject grid;

        [Header("Buttons")]
        [SerializeField] private Button[] editButtons;

        [Header("Water Setting")]
        [SerializeField] private GameObject waterSettingPanel;
        private WaterLevelController waterLevelController;
        [SerializeField] private Slider[] sliderWater;

        [Header("DetailPanel")]
        [SerializeField] private Image detailImage;
        [SerializeField] private Button activeButton;
        [SerializeField] private GameObject detailPanel;
        [SerializeField] private TextMeshProUGUI deviceName;
        [SerializeField] private GameObject renamePopup;
        private Color oldColor;
        private bool getActive;


        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            else
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }
        private void Start()
        {
            isEditMode = true;
            connectPipe = new List<GameObject>();
            ButtonEditMode();

            activeButton.onClick.AddListener(() => ButtonActiveDevice());
            oldColor = activeButton.image.color;
        }
        public void GetSelectedObject(GameObject gameObject)
        {
            selectedObject = gameObject;
            if(isOnBuildPipe)
            {
                pipeInputManager.ToggleBuildingMode();
            }
            if (selectedObject.GetComponent<TankData>())
            {
                SetupWaterSettingPanel();
            }
            if (selectedObject.GetComponent<PipeGroupPath>())
            {
                TriggerWater();
            }
            if (gameObject.GetComponent<DeviceDataController>())
            {
                DisplayDetailDevice();
            }
        }
        public void ButtonClear()
        {
            if (isOnBuildPipe) pipeInputManager.ToggleBuildingMode();
            if (pool.childCount > 0)
            {
                for (int i = 0; i < pool.childCount; i++)
                {
                    Destroy(pool.GetChild(i).gameObject);
                }
            }

            newPipeBuilder.ClearAllPipes();
        }
        public void ButtonToggleGrid()
        {
            grid.SetActive(!grid.activeSelf);
        }
        public void ButtonPipeBuild(bool value)
        {
            isOnBuildPipe = value;
            highlightPipe.SetActive(isOnBuildPipe);
        }
        public void ButtonSelectObject(int num)
        {
            if (isOnBuildPipe) pipeInputManager.ToggleBuildingMode();
            if (!panelContents[num].activeSelf)
            {
                itemContrainer.SetActive(false);
                for (int i = 0; i < panelContents.Length; i++)
                {
                    panelContents[i].SetActive(false);
                    highlightButton[i].SetActive(false);
                }
                switch (num)
                {
                    case 0:
                        itemContentText.text = "Device";
                        break;
                    case 1:
                        itemContentText.text = "Tank";
                        break;
                    case 2:
                        itemContentText.text = "Decorator";
                        break;
                }
                panelContents[num].SetActive(true);
                itemContrainer.SetActive(true);
                highlightButton[num].SetActive(true);
                rectContent.sizeDelta = panelContents[num].GetComponent<RectTransform>().sizeDelta;
            }
            else
            {
                panelContents[num].SetActive(false);
                highlightButton[num].SetActive(false);
                itemContrainer.SetActive(false);
            }
        }
        public void ButtonDelete()
        {
            if (selectedObject == null) return;
            newObjectPlacement.selectedObject = selectedObject;
            newObjectPlacement.DeleteSelectedObject();
            if (selectedObject.GetComponent<PipeGroupPath>())
            {
                newPipeBuilder.RemovePipeGroup(selectedObject);
            }
        }
        public void ButtonMove()
        {
            if (selectedObject == null) return;
            if (selectedObject.GetComponent<PipeGroupPath>()) return;
            if (selectedObject.GetComponentInParent<PipeGroupPath>()) return;

            Debug.Log(selectedObject);
            newObjectPlacement.StartMoving(selectedObject);
        }
        public void ButtonRename()
        {
            if (selectedObject == null) return;
            if (selectedObject.GetComponent<DeviceDataController>())
            {
                renamePopup.SetActive(true);
            }
        }
        public void RenameDevice(TMP_InputField value)
        {
            selectedObject.GetComponent<DeviceDataController>().deviceData.name = value.text;
            renamePopup.SetActive(false);
            deviceName.text = value.text;
        }
        public void ButtonFlip()
        {
            if (selectedObject.GetComponent<PipeGroupPath>())
            {
                selectedObject.GetComponent<PipeGroupPath>().FlipDirectionPipe();
            }
        }
        public void ButtonEditMode()
        {
            isEditMode = !isEditMode;
            highlightEditMode.SetActive(isEditMode);
            if (!isEditMode)
            {
                if (itemContrainer.activeSelf) itemContrainer.SetActive(false);
                if (isOnBuildPipe) pipeInputManager.ToggleBuildingMode();
                if (waterSettingPanel.activeSelf) waterSettingPanel.SetActive(false);
            }
            for (int i = 0; i < editButtons.Length; i++)
            {
                editButtons[i].enabled = isEditMode;
            }
            for (int i = 0; i < highlightButton.Length; i++)
            {
                if (highlightButton[i].activeSelf) highlightButton[i].SetActive(isEditMode);
            }
        }
        public void ButtonZoomIn()
        {
            if (Camera.main.orthographicSize <= 2) return;
            Camera.main.orthographicSize -= 1;
        }
        public void ButtonZoomOut()
        {
            if (Camera.main.orthographicSize >= 20) return;
            Camera.main.orthographicSize += 1;
        }
        public void SetupWaterSettingPanel()
        {
            waterSettingPanel.SetActive(true);
            waterLevelController = selectedObject.GetComponentInChildren<WaterLevelController>();
            sliderWater[0].value = waterLevelController.GetBaseWaterLevel();
            sliderWater[1].value = waterLevelController.GetDirtyWaterLevel();
            sliderWater[2].value = waterLevelController.GetGreenWaterLevel();
        }
        public void SetBaseWater(float value)
        {
            waterLevelController.SetBaseWater(value);
        }
        public void SetDirtyWater(float value)
        {
            waterLevelController.SetDirtyWater(value);
        }
        public void SetGreenWater(float value)
        {
            waterLevelController.SetGreenWater(value);
        }
        public void GetPipeConnection(Transform currentGroup,bool isFour)
        {
            isOnBuildPipe = false;
            currentPipeGroup = currentGroup;
            GameObject obj = GameObject.FindGameObjectWithTag("Intersection");
            if (obj != null) connectPipe.Add(obj);

            if (!isFour)
            {
                popupConnectPipe.SetActive(true);
            }
            else
            {
                ButtonCancelConectPipe();
            }
        }
        public void ButtonSubmitConectPipe()
        {
            isOnBuildPipe = true;
            popupConnectPipe.SetActive(false);
            for (int i = 0; i < connectPipe.Count; i++)
            {
                connectPipe[i].tag = "Untagged";
            }
            connectPipe.Clear();
            Debug.Log(connectPipe.Count);
        }
        public void ButtonCancelConectPipe()
        {
            isOnBuildPipe = true;
            popupConnectPipe.SetActive(false);
            if (connectPipe.Count > 0)
            {
                for (int i = 0; i < connectPipe.Count; i++)
                {
                    GameObject obj1 = Instantiate(straightPipePrefab, connectPipe[i].transform.position, Quaternion.Euler(0, 0, 0), connectPipe[i].transform.parent);
                    GameObject obj2 = Instantiate(straightPipePrefab, connectPipe[i].transform.position, Quaternion.Euler(0, 0, 90), currentPipeGroup);
                    Debug.Log(connectPipe[i]);
                    Destroy(connectPipe[i]);
                }
                connectPipe.Clear();
            }
        }
        public void TriggerWater()
        {
            ReorderPipe();
        }
        private void ReorderPipe()
        {
            List<Transform> targets = new List<Transform>();
            for (int i = 0; i < selectedObject.transform.childCount; i++)
            {
                targets.Add(selectedObject.transform.GetChild(i));
                if (i > 0 && selectedObject.transform.GetChild(i).GetComponent<PipeController>())
                {
                    if (selectedObject.transform.GetChild(i).position.y == selectedObject.transform.GetChild(0).position.y)
                    {
                        selectedObject.transform.GetChild(i).localEulerAngles = Vector3.zero;
                    }
                    else
                    {
                        selectedObject.transform.GetChild(i).localEulerAngles = new Vector3(0, 0, 90);
                    }
                }
            }
            if (targets[0].transform.position.x == targets[1].transform.position.x)
            {
                targets.Sort((a, b) => a.position.y.CompareTo(b.position.y));
            }
            else
            {
                targets.Sort((a, b) => a.position.x.CompareTo(b.position.x));
            }
            for (int i = 0; i < targets.Count; i++)
            {
                targets[i].SetSiblingIndex(i);
            }
            OpenWater();
        }
        private void OpenWater()
        {
            selectedObject.GetComponent<PipeGroupPath>().StartWaterFlow();
        }
        private void DisplayDetailDevice()
        {
            detailImage.sprite = selectedObject.GetComponent<SpriteRenderer>().sprite;
            detailPanel.SetActive(true);
            activeButton.image.color = selectedObject.GetComponent<DeviceDataController>().deviceData.is_active ? Color.green : oldColor;
            getActive = selectedObject.GetComponent<DeviceDataController>().deviceData.is_active;

            deviceName.text = selectedObject.GetComponent<DeviceDataController>().deviceData.name;

            if (string.IsNullOrEmpty(deviceName.text))
            {
                deviceName.text = "Unnamed";
            }
        }
        private void ButtonActiveDevice()
        {
            getActive = !getActive;
            selectedObject.GetComponent<DeviceDataController>().deviceData.is_active = getActive;
            activeButton.image.color = getActive ? Color.green : oldColor;

            selectedObject.GetComponent<SpriteRenderer>().color = getActive ? Color.green : Color.white;
        }
    }
}
