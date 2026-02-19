using UnityEngine;

public class WaterSwitch : MonoBehaviour
{
    void OnMouseDown() // ทำงานเมื่อคลิกที่ตัว Object (ต้องมี Collider)
    {
        PipeConnector pc = GetComponent<PipeConnector>();
        WaterSystemManager manager = FindObjectOfType<WaterSystemManager>();

        if (pc != null && manager != null)
        {
            manager.ToggleWaterSource(pc);
            Debug.Log("Toggle Water Source: " + pc.isWaterSource);
        }
    }
}