using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PGroup
{
    public class SettingPopup : MonoBehaviour
    {
        [Header("UI References")]
        public Button deleteButton;
        public Button moveButton; // [NEW] Button to start moving the object
        public Button cancelButton; // Optional: can be a background button or "X" button
        public Button waterButtonPlus;
        public Button waterButtonMinus;
        public Button dirtyWaterButtonPlus;
        public Button dirtyWaterButtonMinus;
        public Button pipeWaterButton;
        public Button changeWaterButton;
        public TextMeshProUGUI waterLevelText;
        public TextMeshProUGUI dirtyWaterLevelText;

        [SerializeField] private GameObject panelTank;
        [SerializeField] private GameObject panelPipe;

        private int waterLevel;
        private int dirtyWaterLevel;
        private bool pipeWater;

        private UnityAction onDelete;
        private UnityAction onMove;
        private UnityAction onCancel;
        private UnityAction onWaterPlus;
        private UnityAction onWaterMinus;
        private UnityAction onDirtyWaterPlus;
        private UnityAction onDirtyWaterMinus;

        private UnityAction onPipeToggleWater;
        private UnityAction onChangeWaterColor;

        public void SelectTypeSetting(string type)
        {
            panelTank.SetActive(false);
            panelPipe.SetActive(false);
            switch (type)
            {
                case "Tank":
                    panelTank.SetActive(true);
                    break;
                case "Pipe":
                    panelPipe.SetActive(true);
                    break;
            }
        }
        public void SetupPipe(UnityAction onPipeToggleWater, UnityAction onDelete, UnityAction onChangeWaterColor)
        {
            this.onPipeToggleWater = onPipeToggleWater;
            this.onDelete = onDelete;
            this.onChangeWaterColor = onChangeWaterColor;
        }
        public void SetupTank(UnityAction onDelete, UnityAction onMove, UnityAction onCancel, UnityAction onWaterPlus, UnityAction onWaterMinus, UnityAction onDirtyWaterPlus, UnityAction onDirtyWaterMinus)
        {
            this.onDelete = onDelete;
            this.onMove = onMove;
            this.onCancel = onCancel;
            this.onWaterPlus = onWaterPlus;
            this.onWaterMinus = onWaterMinus;
            this.onDirtyWaterPlus = onDirtyWaterPlus;
            this.onDirtyWaterMinus = onDirtyWaterMinus;
        }
        public void SetupValue(int baseWater,int dirtyWater)
        {
            waterLevel = baseWater;
            dirtyWaterLevel = dirtyWater;
            waterLevelText.text = baseWater.ToString();
            dirtyWaterLevelText.text = dirtyWater.ToString();
            //Debug.Log($"SetWaterLevelValue : {waterLevel} , {dirtyWaterLevel}");
        }

        void Start()
        {
            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(() =>
                {
                    onDelete?.Invoke();
                    Destroy(gameObject);
                });
            }

            if (moveButton != null)
            {
                moveButton.onClick.AddListener(() =>
                {
                    onMove?.Invoke();
                    Destroy(gameObject);
                });
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(() =>
                {
                    onCancel?.Invoke();
                    Destroy(gameObject);
                });
            }

            if (waterButtonPlus != null)
            {
                waterButtonPlus.onClick.AddListener(() =>
                {
                    if (waterLevel < 5)
                    {
                        waterLevel++;
                        waterLevelText.text = "" + waterLevel;
                        onWaterPlus?.Invoke();
                    }
                });
            }

            if (waterButtonMinus != null)
            {
                waterButtonMinus.onClick.AddListener(() =>
                {
                    if (waterLevel > 0)
                    {
                        waterLevel--;
                        waterLevelText.text = "" + waterLevel;
                        onWaterMinus?.Invoke();
                    }
                });
            }

            if (dirtyWaterButtonPlus != null)
            {
                dirtyWaterButtonPlus.onClick.AddListener(() =>
                {
                    if (dirtyWaterLevel < 5)
                    {
                        dirtyWaterLevel++;
                        dirtyWaterLevelText.text = "" + dirtyWaterLevel;
                        onDirtyWaterPlus?.Invoke();
                    }
                });
            }

            if (dirtyWaterButtonMinus != null)
            {
                dirtyWaterButtonMinus.onClick.AddListener(() =>
                {
                    if (dirtyWaterLevel > 0)
                    {
                        dirtyWaterLevel--;
                        dirtyWaterLevelText.text = "" + dirtyWaterLevel;
                        onDirtyWaterMinus?.Invoke();
                    }
                });
            }

            if (pipeWaterButton != null)
            {
                pipeWaterButton.onClick.AddListener(() =>
                {
                    pipeWater = !pipeWater;
                    onPipeToggleWater?.Invoke();
                });
            }

            if (changeWaterButton != null)
            {
                changeWaterButton.onClick.AddListener(() =>
                {
                    onChangeWaterColor?.Invoke();
                });
            }
        }
    }
}
