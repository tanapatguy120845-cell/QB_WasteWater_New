using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SensorRename : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private GameObject panel;
    public Button saveButton;
    public Button cancelButton;

    private SensorComponent currentSensor;

    void Start()
    {
        panel.SetActive(false);
        saveButton.onClick.AddListener(SaveName);
        cancelButton.onClick.AddListener(ClosePanel);
    }

    private void Update()
    {
        if (currentSensor == null)
        {
            if (panel.activeSelf) ClosePanel();
            return;
        }
    }

    public void OpenRenamePanel(SensorComponent sensor)
    {
        currentSensor = sensor;
        nameInput.text = sensor.displayName;
        panel.SetActive(true);
    }

    private void SaveName()
    {
        if (currentSensor != null && !string.IsNullOrEmpty(nameInput.text))
        {
            currentSensor.SetDisplayName(nameInput.text);
        }

        ClosePanel();
    }

    private void ClosePanel()
    {
        panel.SetActive(false);
        currentSensor = null;
    }
}
