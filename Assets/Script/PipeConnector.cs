using UnityEngine;
using System.Collections.Generic;

public class PipeConnector : MonoBehaviour, IPipeConnector
{
    [Header("Connection Points")]
    public List<Transform> connectPoints = new List<Transform>();
    public float snapRange = 0.3f;
    float IPipeConnector.snapRange => snapRange;

    [Header("Water Logic")]
    public bool isWaterSource = false; // ติ๊กอันนี้เฉพาะที่ "ถังน้ำ" หรือ "ปั๊ม"
    public bool hasWater = false;

    [Header("Water Visual")]
    public GameObject waterVisual;
    
    // ใช้ส่งค่าหาจุดเชื่อม
    public Transform[] GetAllPoints() => connectPoints.ToArray();

    // --- ฟังก์ชันหัวใจ: ค้นหาเพื่อนบ้านที่ "จุดเขียว" แตะกัน ---
    public List<PipeConnector> GetNeighbors()
    {
        List<PipeConnector> neighbors = new List<PipeConnector>();

        foreach (Transform myPoint in connectPoints)
        {
            // เช็ครัศมีเล็กๆ รอบจุดเขียวของตัวเอง
            Collider2D[] hits = Physics2D.OverlapCircleAll(myPoint.position, 0.1f);
            foreach (var hit in hits)
            {
                // มองหา PipeConnector ที่ไม่ใช่ตัวเอง
                PipeConnector other = hit.GetComponentInParent<PipeConnector>();
                if (other != null && other != this)
                {
                    neighbors.Add(other);
                }
            }
        }
        return neighbors;
    }

    public void UpdateWaterVisual()
    {
        if (waterVisual != null)
        {
            Animator anim = waterVisual.GetComponent<Animator>();
            
            if (hasWater)
            {
                // ถ้าพึ่งจะเริ่มมีน้ำ
                if (!waterVisual.activeSelf)
                {
                    waterVisual.SetActive(true);
                    // Animator จะเริ่มเล่น Flow_Start อัตโนมัติจาก Entry
                }
            }
            else
            {
                if (waterVisual.activeSelf && anim != null)
                {
                    // สั่งให้เล่นท่าจบ (Flow_End)
                    anim.SetTrigger("Stop");
                    // ใช้การ Invoke เพื่อปิด Object หลังจากอนิเมชั่นจบ (เช่น 0.5 วินาที)
                    Invoke("DisableWaterObject", 0.5f);
                }
            }
        }
    }

    void DisableWaterObject()
    {
        if (!hasWater) waterVisual.SetActive(false);
    }

}