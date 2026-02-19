using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class DeletePopup : MonoBehaviour
{
    [Header("UI References")]
    public Button deleteButton;
    public Button moveButton; // [NEW] Button to start moving the object
    public Button cancelButton; // Optional: can be a background button or "X" button

    private UnityAction onDelete;
    private UnityAction onMove;
    private UnityAction onCancel;

    public void Setup(UnityAction onDelete, UnityAction onMove, UnityAction onCancel)
    {
        this.onDelete = onDelete;
        this.onMove = onMove;
        this.onCancel = onCancel;
    }

    void Start()
    {
        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(() =>
            {
                onDelete?.Invoke();
                Destroy(gameObject);
            });
        }

        if (moveButton != null)
        {
            moveButton.onClick.AddListener(() =>
            {
                onMove?.Invoke();
                Destroy(gameObject);
            });
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(() =>
            {
                onCancel?.Invoke();
                Destroy(gameObject);
            });
        }
    }
}
