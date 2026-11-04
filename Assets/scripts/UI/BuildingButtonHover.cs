using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Подсветка строки списка при наведении: подменяет спрайт фона на «hover»
/// и возвращает исходный при уходе курсора.
/// </summary>
/// <remarks>
/// Раньше компонент перекрашивал фон в белый, из-за чего пропадала
/// стилизация строки (9-slice спрайт). Теперь меняется спрайт, а исходный
/// запоминается.
/// </remarks>
public class BuildingButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Sprite hoverSprite;

    private Button button;
    private Image backgroundImage;
    private Sprite originalSprite;

    /// <summary>
    /// Инициализация из кода списка: фон подсвечивается спрайтом <paramref name="hover"/>.
    /// </summary>
    public void Initialize(Button btn, Sprite hover, Image background)
    {
        button = btn;
        hoverSprite = hover;
        backgroundImage = background;

        if (backgroundImage != null) originalSprite = backgroundImage.sprite;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (backgroundImage == null || hoverSprite == null) return;
        if (button != null && !button.interactable) return;

        backgroundImage.sprite = hoverSprite;
        backgroundImage.type = Image.Type.Sliced;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (backgroundImage == null) return;

        backgroundImage.sprite = originalSprite;
    }
}
