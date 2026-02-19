using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UICategoryManager : MonoBehaviour
{
    [System.Serializable]
    public class Category
    {
        public string name;
        public Button headerButton; // ˹á
        public GameObject panel;    // ˹ҵҧ·Դ
        public Button backButton;   // ͹Ѻ˹ҵҧ
    }

    [Header("UI Root References")]
    public GameObject categoryButtonsRoot; // ˹ѡ
    public GameObject saveButtonObject;    // ปุ่ม Save
    public GameObject pipeButtonObject;    // ปุ่ม Pipeѡ
    public List<Category> categories = new List<Category>();

    private Category currentExpanded;

    private void Start()
    {
        foreach (var cat in categories)
        {
            if (cat.panel != null)
                cat.panel.SetActive(false); // �Դ˹�����·������͹�����

            if (cat.backButton != null)
            {
                cat.backButton.gameObject.SetActive(false);
                cat.backButton.onClick.AddListener(CloseCurrentPanel);
            }

            if (cat.headerButton != null)
            {
                Category captured = cat;
                cat.headerButton.onClick.AddListener(() => OpenCategory(captured));
            }
        }

    }

    public void OpenCategory(Category category)
    {
        // 1. �Դ˹��������ѡ
        if (categoryButtonsRoot != null) categoryButtonsRoot.SetActive(false);
        if (saveButtonObject != null) saveButtonObject.SetActive(false);
        if (pipeButtonObject != null) pipeButtonObject.SetActive(false);

        // 2. �Դ˹����Ǵ������蹷���Ҩ���Դ��ҧ���� (�����)
        if (currentExpanded != null && currentExpanded.panel != null)
            currentExpanded.panel.SetActive(false);

        // 3. �Դ˹����Ǵ���������͡
        if (category.panel != null)
            category.panel.SetActive(true);

        if (category.backButton != null)
            category.backButton.gameObject.SetActive(true);

        currentExpanded = category;
    }

    public void CloseCurrentPanel()
    {
        // 1. �Դ˹�����·���Դ����
        if (currentExpanded != null)
        {
            if (currentExpanded.panel != null)
                currentExpanded.panel.SetActive(false);

            if (currentExpanded.backButton != null)
                currentExpanded.backButton.gameObject.SetActive(false);
        }

        currentExpanded = null;

        // 2. �Դ˹��������ѡ��Ѻ�׹��
        if (categoryButtonsRoot != null) categoryButtonsRoot.SetActive(true);
        if (saveButtonObject != null) saveButtonObject.SetActive(true);
        if (pipeButtonObject != null) pipeButtonObject.SetActive(true);
    }
}