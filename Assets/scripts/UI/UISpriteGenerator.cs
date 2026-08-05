using UnityEngine;

/// <summary>
/// Процедурный генератор спрайтов.
/// Создаёт текстуры рамок, кнопок и т.д.
/// </summary>
public static class UISpriteGenerator
{
    public static Sprite PanelBackground { get; private set; }
    public static Sprite ButtonNormal { get; private set; }
    public static Sprite ButtonHover { get; private set; }
    public static Sprite BarFill { get; private set; }
    public static Sprite HammerIcon { get; private set; }

    // Спрайты ресурсов
    public static Sprite IceIcon { get; private set; }
    public static Sprite RockIcon { get; private set; }
    public static Sprite MineralIcon { get; private set; }
    public static Sprite EnergyIcon { get; private set; }

    /// <summary>
    /// Генерирует всю графику интерфейса при запуске игры.
    /// </summary>
    public static void GenerateAll()
    {
        if (PanelBackground != null) return; // Уже сгенерировано

        PanelBackground = CreateBorderedSprite(32, 32, new Color(0.1f, 0.12f, 0.15f, 0.85f), new Color(0.3f, 0.5f, 0.7f, 1f), 2);
        ButtonNormal = CreateBorderedSprite(32, 32, new Color(0.15f, 0.2f, 0.25f, 0.9f), new Color(0.4f, 0.6f, 0.8f, 1f), 2);
        ButtonHover = CreateBorderedSprite(32, 32, new Color(0.25f, 0.35f, 0.45f, 0.95f), new Color(0.2f, 0.9f, 1f, 1f), 2);

        // Белая монохромная плашка для полосы HP (цвет задается через Image.color)
        BarFill = CreateSolidSprite(8, 8, Color.white);

        // Рисуем иконку молотка (32x32)
        HammerIcon = CreateHammerSprite(32, 32);

        // Рисуем цветные значки ресурсов
        IceIcon = CreateShapeSprite(32, 32, new Color(0.4f, 0.8f, 1f), 1);      // Синий кристал
        RockIcon = CreateShapeSprite(32, 32, new Color(0.6f, 0.5f, 0.4f), 2);     // Коричневый камень
        MineralIcon = CreateShapeSprite(32, 32, new Color(0.8f, 0.3f, 1f), 1);   // Фиолетовый кристал
        EnergyIcon = CreateShapeSprite(32, 32, new Color(1f, 0.9f, 0.2f), 3);    // Желтая молния

        Debug.Log("[UISpriteGenerator] Процедурные спрайты интерфейса успешно сгенерированы.");
    }

    private static Sprite CreateSolidSprite(int width, int height, Color color)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    private static Sprite CreateBorderedSprite(int width, int height, Color fillColor, Color borderColor, int borderThickness)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                bool isBorder = x < borderThickness || x >= width - borderThickness ||
                                y < borderThickness || y >= height - borderThickness;

                tex.SetPixel(x, y, isBorder ? borderColor : fillColor);
            }
        }

        tex.Apply();
        // Настраиваем 9-slice бордеры для правильного масштабирования без растяжения углов
        Vector4 border = new Vector4(borderThickness + 1, borderThickness + 1, borderThickness + 1, borderThickness + 1);
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
    }

    private static Sprite CreateHammerSprite(int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        // Заливаем прозрачным
        Color transparent = new Color(0, 0, 0, 0);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                tex.SetPixel(x, y, transparent);

        Color handleColor = new Color(0.7f, 0.5f, 0.3f, 1f); // Дерево
        Color headColor = new Color(0.8f, 0.85f, 0.9f, 1f);   // Металл

        // Рисуем диагональную рукоятку
        for (int i = 4; i < 20; i++)
        {
            tex.SetPixel(i, i, handleColor);
            tex.SetPixel(i + 1, i, handleColor);
            tex.SetPixel(i, i + 1, handleColor);
        }

        // Рисуем головку молотка (в верхнем правом углу)
        for (int x = 16; x < 28; x++)
        {
            for (int y = 20; y < 28; y++)
            {
                tex.SetPixel(x, y, headColor);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }

    private static Sprite CreateShapeSprite(int width, int height, Color color, int shapeType)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color transparent = new Color(0, 0, 0, 0);
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                tex.SetPixel(x, y, transparent);

        int center = width / 2;

        for (int x = 4; x < width - 4; x++)
        {
            for (int y = 4; y < height - 4; y++)
            {
                bool draw = false;
                if (shapeType == 1) // Ромб / Кристал
                {
                    draw = (Mathf.Abs(x - center) + Mathf.Abs(y - center)) <= (center - 4);
                }
                else if (shapeType == 2) // Круг / Камень
                {
                    draw = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) <= (center - 5);
                }
                else // Молния / Энергия
                {
                    draw = (x + y > 24 && x + y < 38) && (x - y < 8 && y - x < 12);
                }

                if (draw) tex.SetPixel(x, y, color);
            }
        }

        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
}