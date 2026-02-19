using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaterSystemManager : MonoBehaviour
{
    // ฟังก์ชันนี้เรียกใช้จากปุ่ม หรือจาก Device ที่เราไปกด
    public void ToggleWaterSource(PipeConnector source)
    {
        source.isWaterSource = !source.isWaterSource; // สลับเปิด/ปิด
        RefreshSystem();
    }

    public void RefreshSystem()
    {
        PipeConnector[] allNodes = FindObjectsOfType<PipeConnector>();
        foreach (var node in allNodes) 
        {
            node.hasWater = false;
            node.UpdateWaterVisual();
        }

        // เริ่มไหลใหม่จากจุดกำเนิดน้ำทั้งหมดที่เปิดอยู่
        foreach (var node in allNodes)
        {
            if (node.isWaterSource)
            {
                StartCoroutine(FlowRoutine(node));
            }
        }
    }

    IEnumerator FlowRoutine(PipeConnector currentNode)
    {
        if (currentNode.hasWater) yield break;
        
        currentNode.hasWater = true;
        currentNode.UpdateWaterVisual();

        // หน่วงเวลานิดนึงให้น้ำดูเหมือนค่อยๆ ไหลไปท่อถัดไป (เช่น 0.2 วินาที)
        yield return new WaitForSeconds(0.2f);

        foreach (PipeConnector neighbor in currentNode.GetNeighbors())
        {
            StartCoroutine(FlowRoutine(neighbor));
        }
    }
}