using UnityEngine;
using UnityEngine.UIElements;

public class ResourceItem
{
    public VisualElement Root { get; private set; }
    public VisualElement ResourceImage { get; private set; }
    public Label ResourceNameAndAmount { get; private set; }

    public ResourceItem(VisualTreeAsset template, string resourceName)
    {
        // ������ ������
        var UIItem = template.CloneTree();

        // ��������� �������� ������� �� �������
        Root = UIItem.Q<VisualElement>($"ResourceItemRoot");
        Root.name = $"ResourceItemRoot_{resourceName}";

        // ������� �������� ����������
        ResourceImage = Root.Q<VisualElement>("ResourceImage");
        ResourceNameAndAmount = Root.Q<Label>("ResourceNameAndAmount");
    }

    // ������ ��� ��������� ����������
    public void SetIcon(Texture2D texture)
    {
        ResourceImage.style.backgroundImage = texture;
    }

    public void SetText(string text)
    {
        ResourceNameAndAmount.text = text;
    }
}