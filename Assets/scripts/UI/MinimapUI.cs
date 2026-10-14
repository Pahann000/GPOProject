using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Показ миникарты в углу экрана.
/// </summary>
/// <remarks>
/// Перенос из ветки features/12_minimap: там компонент висел на объекте внутри
/// префаба MainUIPrefab. В нашей ветке UI собирается динамически, поэтому объект
/// создаётся кодом и правка префаба не нужна.
/// </remarks>
public class MinimapUI : MonoBehaviour
{
    private const float PanelSize = 180f;
    private const float Margin = 8f;

    private RawImage _image;

    /// <summary>Создаёт миникарту внутри переданного Canvas.</summary>
    public static MinimapUI Create(Transform parent, Texture2D texture)
    {
        GameObject panel = new GameObject("Minimap", typeof(RectTransform), typeof(RawImage));
        panel.transform.SetParent(parent, false);

        var rect = (RectTransform)panel.transform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-Margin, -Margin);
        rect.sizeDelta = new Vector2(PanelSize, PanelSize);

        MinimapUI ui = panel.AddComponent<MinimapUI>();
        ui._image = panel.GetComponent<RawImage>();
        ui._image.texture = texture;
        ui._image.color = Color.white;
        ui._image.raycastTarget = false;

        return ui;
    }

    public void SetTexture(Texture2D texture)
    {
        if (_image != null) _image.texture = texture;
    }
}
