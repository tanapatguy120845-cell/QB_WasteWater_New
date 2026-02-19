using UnityEngine;

public class ColorController : MonoBehaviour
{
    public Renderer pipeRenderer;

    [Header("4 Colors")]
    public Color blueColor = Color.blue;
    public Color grayColor = new Color(0.5f, 0.8f, 1f);
    public Color greenColor = new Color(0.45f, 0.3f, 0.15f);
    public Color orangeColor = new Color(0.6f, 0.85f, 0.6f);
    public Color redColor = new Color(0.6f, 0.85f, 0.6f);

    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    void Set(Color c)
    {
        if (pipeRenderer == null)
        {
            Debug.LogError("ColorController: ColorRenderer NOT SET");
            return;
        }

        pipeRenderer.material.SetColor(BaseColorID, c);
    }

    public void SetWhite() => Set(blueColor);
    public void SetNormal() => Set(grayColor);
    public void SetDirty() => Set(greenColor);
    public void SetChemical() => Set(orangeColor);
    public void Setred() => Set(redColor);
}
