using UnityEngine;

/// <summary>
/// ติดกับ Float prefab เพื่อแจ้ง Tank เมื่อมีการเปลี่ยนแปลง
/// </summary>
public class FloatSensor : MonoBehaviour
{
    private TankData parentTank;
    private Vector3 lastPosition;
    private float positionCheckInterval = 0.1f;
    private float nextCheckTime = 0f;
    
    [HideInInspector]
    public bool isActive = true; // ใช้เช็คว่า Float ยัง active อยู่หรือถูกลบแล้ว

    void Start()
    {
        // หา parent tank ตอน spawn
        FindParentTank();
        lastPosition = transform.position;
        
        // แจ้ง tank ว่ามี float เพิ่มเข้ามา
        NotifyTankChanged();
    }

    void OnEnable()
    {
        FindParentTank();
        NotifyTankChanged();
    }

    void OnDisable()
    {
        isActive = false;
        NotifyTankChanged();
    }

    void OnDestroy()
    {
        isActive = false;
        NotifyTankChanged();
    }

    void FixedUpdate()
    {
        // เช็คว่า float เคลื่อนที่หรือเปล่า (สำหรับกรณีที่มี physics)
        if (Time.time >= nextCheckTime)
        {
            if (Vector3.Distance(transform.position, lastPosition) > 0.01f)
            {
                NotifyTankChanged();
                lastPosition = transform.position;
            }
            nextCheckTime = Time.time + positionCheckInterval;
        }
    }

    void FindParentTank()
    {
        if (parentTank == null)
        {
            Transform parent = transform.parent;
            if (parent != null)
            {
                parentTank = parent.GetComponent<TankData>();
            }
        }
    }

    void NotifyTankChanged()
    {
        if (parentTank != null && parentTank.waterLevelScript != null)
        {
            //parentTank.waterLevelScript.RecalculateWaterLevel();
        }
    }
    public void ShowFloating(bool isFloat)
    {
        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (isFloat)
        {
            spriteRenderer.enabled = false;
            transform.GetChild(0).gameObject.SetActive(true);
        }
        else
        {
            transform.GetChild(0).gameObject.SetActive(false);
            spriteRenderer.enabled = true;
        }
    }
}
