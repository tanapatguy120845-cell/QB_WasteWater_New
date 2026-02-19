using UnityEngine;

public class TankManager : MonoBehaviour
{
    public static TankManager Instance;
    public WaterLevel2D selectedTank;
    public TankRename renamePanel;
    public DeviceRename deviceRenamePanel;

    void Awake() 
    { 
        if (Instance == null) Instance = this; 
        else Destroy(gameObject);
    }

    public void SelectTank(TankData tank) 
    {
        if (tank != null) 
        {
            selectedTank = tank.GetComponentInChildren<WaterLevel2D>();
            Debug.Log("Selected: " + tank.displayName);
        }
    }

    public void SetSelectedTankLevel(int level)
    {
        if (selectedTank != null)
        {
            selectedTank.ChangeLevel(level);
        }
    }

    public void ClearAllRenamePanels()
    {
        if (renamePanel != null) renamePanel.gameObject.SetActive(false);
        if (deviceRenamePanel != null) deviceRenamePanel.gameObject.SetActive(false);
    }
}