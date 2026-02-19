using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TankRename : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameInput;
    [SerializeField] private GameObject panel; 
    public Button saveButton;          
    public Button cancelButton;        

    private TankData currentTank;

    void Start()
    {
        panel.SetActive(false);
        saveButton.onClick.AddListener(SaveName);
        cancelButton.onClick.AddListener(ClosePanel);
    }

    public void OpenRenamePanel(TankData tank)
    {
        currentTank = tank;
        nameInput.text = tank.displayName;
        panel.SetActive(true);
    }

    private void SaveName()
    {
        if (currentTank != null)
            currentTank.SetName(nameInput.text);

        ClosePanel();
    }

    private void ClosePanel()
    {
        panel.SetActive(false);
        currentTank = null;
    }
}
