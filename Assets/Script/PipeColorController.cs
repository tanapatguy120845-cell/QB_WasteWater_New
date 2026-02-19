using UnityEngine;

public class PipeColorController : MonoBehaviour
{
    public Renderer pipeRenderer;

    [Header("4 Pipe Colors")]
    public Color whiteColor = Color.white;
    public Color normalColor = new Color(0.5f, 0.8f, 1f);
    public Color dirtyColor = new Color(0.45f, 0.3f, 0.15f);
    public Color chemicalColor = new Color(0.6f, 0.85f, 0.6f);

    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    void Set(Color c)
    {
        if (pipeRenderer == null)
        {
            Debug.LogError("PipeColorController: pipeRenderer NOT SET");
            return;
        }

        pipeRenderer.material.SetColor(BaseColorID, c);
    }

    public void SetWhite() => Set(whiteColor);
    public void SetNormal() => Set(normalColor);
    public void SetDirty() => Set(dirtyColor);
    public void SetChemical() => Set(chemicalColor);
}
