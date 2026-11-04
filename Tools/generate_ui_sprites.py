"""Генератор UI-спрайтов для интерфейса колонии (9-slice, сглаженные углы).

Создаёт панели, кнопки (normal/hover/pressed/disabled), чипы ресурсов и
подложку затемнения. Все спрайты рисуются в 4x и уменьшаются для сглаживания,
у прямоугольников задаётся border для режима Image.Type = Sliced.
"""

import os
from PIL import Image, ImageDraw

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.abspath(os.path.join(HERE, ".."))
OUT_ROOT = os.path.join(PROJECT_ROOT, "Assets", "Sprites", "UI")

SUPERSAMPLE = 4

# Палитра интерфейса (тёмный «космический» сланец с тёплым акцентом Марса)
PANEL_FILL = (26, 32, 44, 235)
PANEL_LIGHT = (38, 47, 64, 245)
PANEL_STROKE = (86, 103, 132, 255)
ACCENT = (232, 143, 76, 255)
ACCENT_DARK = (176, 96, 44, 255)

BUTTON_FILL = (44, 56, 76, 255)
BUTTON_HOVER = (62, 79, 106, 255)
BUTTON_PRESSED = (30, 39, 54, 255)
BUTTON_DISABLED = (46, 50, 58, 210)
BUTTON_STROKE = (104, 126, 160, 255)

CHIP_FILL = (18, 23, 32, 215)
CHIP_STROKE = (74, 90, 116, 255)


def rounded(size, radius, fill, stroke=None, stroke_width=2):
    """Рисует скруглённый прямоугольник с супер-сэмплингом и возвращает RGBA-изображение."""
    w, h = size
    big = Image.new("RGBA", (w * SUPERSAMPLE, h * SUPERSAMPLE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(big)
    box = (0, 0, w * SUPERSAMPLE - 1, h * SUPERSAMPLE - 1)
    r = radius * SUPERSAMPLE
    draw.rounded_rectangle(box, radius=r, fill=fill, outline=stroke, width=stroke_width * SUPERSAMPLE)
    return big.resize((w, h), Image.LANCZOS)


def save(img, name):
    os.makedirs(OUT_ROOT, exist_ok=True)
    path = os.path.join(OUT_ROOT, name + ".png")
    img.save(path)
    return path


def main():
    written = []

    # Панель-окно: скругление 18, рамка, лёгкий градиент сверху
    panel = rounded((96, 96), 18, PANEL_FILL, PANEL_STROKE, 2)
    overlay = rounded((96, 96), 18, PANEL_LIGHT, None, 0)
    panel = Image.alpha_composite(panel, Image.blend(Image.new("RGBA", panel.size, (0, 0, 0, 0)), overlay, 0.35))
    written.append(save(panel, "panel_window"))

    # Шапка окна: тот же скруглённый прямоугольник, но светлее и с акцентной полосой снизу
    header = rounded((96, 96), 18, PANEL_LIGHT, None, 0)
    d = ImageDraw.Draw(header)
    d.rectangle((14, 88, 96 - 14, 95), fill=ACCENT)
    written.append(save(header, "panel_header"))

    # Кнопки: один размер (64x64, радиус 12), четыре состояния
    written.append(save(rounded((64, 64), 12, BUTTON_FILL, BUTTON_STROKE, 2), "button_normal"))
    written.append(save(rounded((64, 64), 12, BUTTON_HOVER, ACCENT, 2), "button_hover"))
    written.append(save(rounded((64, 64), 12, BUTTON_PRESSED, ACCENT_DARK, 2), "button_pressed"))
    written.append(save(rounded((64, 64), 12, BUTTON_DISABLED, CHIP_STROKE, 2), "button_disabled"))

    # Кнопка-акцент (главное действие)
    written.append(save(rounded((64, 64), 12, ACCENT_DARK, ACCENT, 2), "button_accent"))
    written.append(save(rounded((64, 64), 12, ACCENT, (255, 196, 140, 255), 2), "button_accent_hover"))

    # Чип ресурса и строка списка
    written.append(save(rounded((48, 48), 10, CHIP_FILL, CHIP_STROKE, 2), "chip"))
    written.append(save(rounded((48, 48), 10, (36, 46, 62, 235), (92, 110, 140, 235), 2), "row_normal"))
    written.append(save(rounded((48, 48), 10, (46, 62, 82, 245), (232, 143, 76, 220), 2), "row_hover"))
    written.append(save(rounded((48, 48), 10, (28, 60, 40, 240), (110, 200, 130, 235), 2), "row_completed"))
    written.append(save(rounded((48, 48), 10, (34, 44, 62, 240), (104, 126, 160, 235), 2), "row_in_progress"))
    written.append(save(rounded((48, 48), 10, (30, 30, 34, 225), (70, 74, 82, 225), 2), "row_locked"))

    # Полоса прогресса
    written.append(save(rounded((24, 24), 8, (16, 20, 28, 240), (70, 84, 108, 240), 2), "progress_bg"))
    written.append(save(rounded((24, 24), 8, ACCENT, None, 0), "progress_fill"))

    # Затемнение под модальным окном
    dim = Image.new("RGBA", (8, 8), (6, 8, 12, 190))
    written.append(save(dim, "dimmer"))

    print("written %d sprites" % len(written))
    for w in written:
        print("  " + os.path.basename(w))


if __name__ == "__main__":
    main()
