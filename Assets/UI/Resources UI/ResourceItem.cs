using UnityEngine;
using UnityEngine.UIElements;

public class ResourceItem
{
    public VisualElement Root { get; private set; }
    public VisualElement ResourceImage { get; private set; }
    public Label ResourceNameAndAmount { get; private set; }

    public ResourceItem(VisualTreeAsset template, string resourceName)
    {
        // Создаём шаблон
        var UIItem = template.CloneTree();

        // Извлекаем корневой элемент из шаблона
        Root = UIItem.Q<VisualElement>($"ResourceItemRoot");
        Root.name = $"ResourceItemRoot_{resourceName}";

        // Находим элементы управления
        ResourceImage = Root.Q<VisualElement>("ResourceImage");
        ResourceNameAndAmount = Root.Q<Label>("ResourceNameAndAmount");
    }

    // Методы для изменения параметров
    public void SetIcon(Texture2D texture)
    {
        ResourceImage.style.backgroundImage = texture;
    }

    public void SetText(string text)
    {
        ResourceNameAndAmount.text = text;
    }
}