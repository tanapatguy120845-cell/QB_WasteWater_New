using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DeviceRename : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private GameObject panel;
    public Button saveButton;
    public Button cancelButton;

    private DeviceComponent currentDevice;

    void Start()
    {
        panel.SetActive(false);
        saveButton.onClick.AddListener(SaveName);
        cancelButton.onClick.AddListener(ClosePanel);
    }

    private void Update() 
    {
        if (currentDevice == null) 
        {
            if (panel.activeSelf) ClosePanel();
            return;
        }
    }

    public void OpenRenamePanel(DeviceComponent device)
    {
        currentDevice = device;
        nameInput.text = device.displayName; 
        panel.SetActive(true);
    }

   private void SaveName()
    {
        if (currentDevice != null && !string.IsNullOrEmpty(nameInput.text))
        {
            currentDevice.SetDisplayName(nameInput.text);
        }

        ClosePanel();
    }

    private void ClosePanel()
    {
        panel.SetActive(false);
        currentDevice = null;
    }
}