using UnityEngine;
using System.Collections;

public class WaterLevel2D : MonoBehaviour
{
    public SpriteRenderer waterSprite; 
    public float growSpeed = 1.5f;
    public float[] levelHeights; // สำหรับระบบกดปุ่มเดิม

    [Header("Debug Settings")]
    public bool debugMode = false; // เปิดเฉพาะตอนต้องการดู Debug Log

    [Header("Auto Float Settings")]
    public bool useFloatAutoLevel = true; // ติ๊กถูกถ้าต้องการให้ตามลูกลอยอัตโนมัติ
    public float maxScaleY = 0.32f;       // สเกลสูงสุดที่อนุญาต
    public bool useDiscreteFloatLevels = true; // ใช้ระดับแบบขั้นตาม Level Heights
    public BoxCollider2D waterBoundsCollider; // ถ้าตั้งไว้ จะใช้ขอบนี้แทน Collider ถัง
    public LayerMask floatLevelLayer; // ถ้าตั้งไว้ จะนับเฉพาะ Float ที่อยู่ในเลเยอร์นี้
    public bool useEventBasedUpdate = true; // ใช้ระบบ event-based (แนะนำ) แทน polling

    private int currentLevel = 0;
    private Coroutine routine;
    private float currentTargetScale = 0f;
    private bool isAnimating = false;

    void Update()
    {
        // Animate น้ำให้ค่อยๆ เปลี่ยนไปที่ target
        if (isAnimating)
        {
            UpdateWaterLevel(currentTargetScale);
        }
    }

    /// <summary>
    /// เรียกจาก FloatSensor เมื่อมี Float เปลี่ยนแปลง (Event-based)
    /// </summary>
    public void RecalculateWaterLevel()
    {
        if (!useFloatAutoLevel) return;

        Transform highestFloat = FindHighestFloat();
        float targetScale = 0f;

        if (highestFloat != null)
        {
            targetScale = CalculateTargetScale(highestFloat.position.y);
            if (debugMode)
                Debug.Log("[WaterLevel2D] Recalculate: Highest Float=" + highestFloat.name + " at Y=" + highestFloat.position.y.ToString("F2") + ", Target=" + targetScale.ToString("F3"));
        }
        else
        {
            if (debugMode)
                Debug.Log("[WaterLevel2D] Recalculate: No Float found - draining to 0");
        }

        currentTargetScale = targetScale;
        isAnimating = true;
    }

    // --- ระบบเดิม: เปลี่ยนระดับผ่านปุ่ม UI ---
    public void ChangeLevel(int targetLevel)
    {
        useFloatAutoLevel = false; // ถ้ากดปุ่มสั่ง ให้ปิดระบบ Auto ชั่วคราวเพื่อให้ผลลัพธ์จากปุ่มทำงานได้
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(StepRoutine(targetLevel));
    }

    IEnumerator StepRoutine(int targetLevel)
    {
        targetLevel = Mathf.Clamp(targetLevel, 0, levelHeights.Length - 1);
        int dir = targetLevel > currentLevel ? 1 : -1;

        while (currentLevel != targetLevel)
        {
            int next = currentLevel + dir;
            yield return StartCoroutine(AnimateToHeight(levelHeights[next]));
            currentLevel = next;
        }
    }

    IEnumerator AnimateToHeight(float targetHeight)
    {
        float currentHeight = waterSprite.transform.localScale.y;
        while (Mathf.Abs(currentHeight - targetHeight) > 0.001f)
        {
            currentHeight = Mathf.MoveTowards(currentHeight, targetHeight, growSpeed * Time.deltaTime);
            waterSprite.transform.localScale = new Vector3(waterSprite.transform.localScale.x, currentHeight, 1f);
            yield return null;
        }
    }

    // --- ระบบใหม่: ตรวจจับลูกลอย (Float) ---
    Transform FindHighestFloat()
    {
        // ค้นหาในระดับถัง (Grandparent) เพราะ WaterRoot เป็นแค่วงศ์วานลูก
        Transform tankTransform = transform.parent != null ? transform.parent.parent : null;
        if (tankTransform == null) 
        {
            if (debugMode)
                Debug.LogWarning("[WaterLevel2D] Tank transform not found!");
            return null;
        }

        Transform highest = null;
        float maxY = float.MinValue;
        int foundCount = 0;
        int filteredCount = 0;

        foreach (Transform child in tankTransform)
        {
            bool isFloat = child.CompareTag("Float");
            if (!isFloat) continue;
            
            foundCount++;
            
            // เช็คว่า Float ยัง active อยู่หรือเปล่า (สำหรับกรณีที่กำลังจะถูกลบ)
            FloatSensor sensor = child.GetComponent<FloatSensor>();
            if (sensor != null && !sensor.isActive)
            {
                if (debugMode)
                    Debug.Log("[WaterLevel2D] Skipping " + child.name + " - Float is being destroyed");
                continue;
            }
            
            string layerName = LayerMask.LayerToName(child.gameObject.layer);

            // ถ้าตั้ง Float Level Layer ให้กรองเฉพาะ Layer นั้น ถ้าไม่ตั้งให้ผ่านทุกตัว
            if (floatLevelLayer.value != 0)
            {
                bool isInAllowedLayer = ((floatLevelLayer.value & (1 << child.gameObject.layer)) != 0);
                if (!isInAllowedLayer)
                {
                    if (debugMode)
                        Debug.Log("[WaterLevel2D] Skipping " + child.name + " (Layer: " + layerName + ") - not in allowed layer");
                    continue; // ข้ามตัวที่ไม่อยู่ใน Layer ที่กำหนด
                }
            }
            
            filteredCount++;
            if (debugMode)
                Debug.Log("[WaterLevel2D] Found Float: " + child.name + " (Layer: " + layerName + ") at Y=" + child.position.y.ToString("F2"));

            if (child.position.y > maxY)
            {
                maxY = child.position.y;
                highest = child;
            }
        }
        
        if (debugMode)
            Debug.Log("[WaterLevel2D] Total Float objects: " + foundCount + ", After layer filter: " + filteredCount);
        return highest;
    }

    float CalculateTargetScale(float floatWorldY)
    {
        // ดึงข้อมูล Collider จากตัว Tank (Grandparent)
        Transform tankTransform = transform.parent != null ? transform.parent.parent : null;
        if (tankTransform == null) return 0;

        BoxCollider2D tankCollider = tankTransform.GetComponent<BoxCollider2D>();
        BoxCollider2D boundsCollider = waterBoundsCollider != null ? waterBoundsCollider : tankCollider;
        
        if (boundsCollider != null)
        {
            // หาจุดสูงสุดและต่ำสุดของ Collider ถังในโลกความจริง
            float tankBottomY = boundsCollider.bounds.min.y;
            float tankTopY = boundsCollider.bounds.max.y;

            // 2. เทียบตำแหน่ง Y ของ Float กับขอบถัง
            float t = Mathf.InverseLerp(tankBottomY, tankTopY, floatWorldY);
            t = Mathf.Clamp01(t);
            
            // ส่งค่ากลับไปเป็น Scale (0 ถึง maxScaleY)
            float continuousScale = Mathf.Lerp(0, maxScaleY, t);
            if (useDiscreteFloatLevels && levelHeights != null && levelHeights.Length > 0)
            {
                return SnapToNearestLevel(continuousScale);
            }
            return continuousScale;
        }
        
        return 0; // ถ้าหา Collider ไม่เจอให้น้ำอยู่ที่พื้นถัง
    }

    float SnapToNearestLevel(float value)
    {
        float nearest = levelHeights[0];
        float minDist = Mathf.Abs(value - nearest);
        for (int i = 1; i < levelHeights.Length; i++)
        {
            float dist = Mathf.Abs(value - levelHeights[i]);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = levelHeights[i];
            }
        }
        return nearest;
    }

    void UpdateWaterLevel(float targetHeight)
    {
        float currentHeight = waterSprite.transform.localScale.y;
        if (Mathf.Abs(currentHeight - targetHeight) > 0.001f)
        {
            float nextHeight = Mathf.MoveTowards(currentHeight, targetHeight, growSpeed * Time.deltaTime);
            waterSprite.transform.localScale = new Vector3(waterSprite.transform.localScale.x, nextHeight, 1f);
        }
        else
        {
            // Snap to exact target when very close to prevent floating point residue
            waterSprite.transform.localScale = new Vector3(waterSprite.transform.localScale.x, targetHeight, 1f);
            isAnimating = false; // หยุด animate เมื่อถึง target แล้ว
        }
    }
}