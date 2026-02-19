using System;
using UnityEngine;

namespace PGroup
{
    public class HighlightController : MonoBehaviour
    {
        [SerializeField] private string highlightName;
        private SpriteRenderer spriteRenderer;
        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            spriteRenderer.color = Color.clear;

            NewObjectPlacement.OnShowHighlight += HandleShowHighlight;
        }
        private void OnDestroy()
        {
            NewObjectPlacement.OnShowHighlight -= HandleShowHighlight;
        }

        private void HandleShowHighlight(string value)
        {
            if (transform.parent.childCount > 1) return;
            spriteRenderer = GetComponent<SpriteRenderer>();

            if (highlightName == value)
            {
                if (GetComponentInParent<Collider2D>()) GetComponentInParent<Collider2D>().enabled = true;
                spriteRenderer.color = new Color(0.5f, 1, 0.5f, 0.4f);
            }
            else
            {
                if (GetComponentInParent<Collider2D>()) GetComponentInParent<Collider2D>().enabled = false;
                spriteRenderer.color = Color.clear;
            }
        }
    }
}
