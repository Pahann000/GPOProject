using UnityEngine;
using UnityEngine.UI;

public class MinimapUI : MonoBehaviour
{
    private RawImage _rawImage;
    private bool _isLinked = false;

    void Start()
    {
        _rawImage = GetComponent<RawImage>();
    }

    void Update()
    {
        // Пытаемся получить текстуру из Ядра, если еще не сделали этого
        if (!_isLinked && GameKernel.Instance != null)
        {
            var minimapSys = GameKernel.Instance.GetSystem<MinimapSystem>();
            if (minimapSys != null && minimapSys.MapTexture != null)
            {
                _rawImage.texture = minimapSys.MapTexture;
                _isLinked = true;
            }
        }
    }
}