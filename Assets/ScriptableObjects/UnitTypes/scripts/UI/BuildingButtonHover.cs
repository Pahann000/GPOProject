using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BuildingButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Button button;
    private GameObject tooltipObject;
    private Image backgroundImage;
    private Color originalColor;
    private Color hoverColor;

    public void Initialize(Button btn, GameObject tooltip, Image background, Color hoverCol)
    {
        button = btn;
        tooltipObject = tooltip;
        backgroundImage = background;
        hoverColor = hoverCol;

        if (backgroundImage != null)
            originalColor = backgroundImage.color;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Подсветка фона при наведении
        if (backgroundImage != null && button.interactable)
        {
            backgroundImage.color = hoverColor;
        }

        // Показываем тултип
        if (tooltipObject != null && button.interactable)
        {
            tooltipObject.SetActive(true);

            // Позиционируем тултип рядом с кнопкой
            RectTransform tooltipRect = tooltipObject.GetComponent<RectTransform>();
            if (tooltipRect != null)
            {
                Vector3[] corners = new Vector3[4];
                GetComponent<RectTransform>().GetWorldCorners(corners);
                tooltipRect.position = new Vector3(corners[2].x + 10, corners[2].y, corners[2].z);
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Возвращаем исходный цвет
        if (backgroundImage != null)
        {
            backgroundImage.color = originalColor;
        }

        // Скрываем тултип
        if (tooltipObject != null)
        {
            tooltipObject.SetActive(false);
        }
    }
}