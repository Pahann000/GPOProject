"""Генератор фракционных спрайтов (заглушки-пиксель-арт 32x32).

Создаёт для каждой фракции собственный набор текстур: юнитов (3 кадра анимации
на тип), фракционную базу и по спрайту на каждый тип здания. Стиль и палитра
у фракций разные, поэтому в игре видно, чей это юнит/здание.
"""

import os
from PIL import Image, ImageDraw

SIZE = 32

HERE = os.path.dirname(os.path.abspath(__file__))
PROJECT_ROOT = os.path.abspath(os.path.join(HERE, ".."))
ASSETS = os.path.join(PROJECT_ROOT, "Assets")
OUT_ROOT = os.path.join(ASSETS, "Sprites", "Factions")

PALETTES = {
    "Humans": {
        "primary": (74, 144, 226, 255),
        "light": (232, 241, 255, 255),
        "dark": (44, 62, 80, 255),
        "skin": (242, 201, 160, 255),
        "accent": (255, 196, 61, 255),
        "glass": (170, 205, 255, 200),
    },
    "Martians": {
        "primary": (123, 211, 137, 255),
        "light": (182, 255, 140, 255),
        "dark": (33, 64, 42, 255),
        "skin": (168, 224, 99, 255),
        "accent": (196, 92, 255, 255),
        "glass": (140, 255, 190, 200),
    },
}

BUILDINGS = [
    "House",
    "MainBuilding",
    "ExtendsBuilding",
    "Mine",
    "Greenhouse",
    "PowerPlant",
    "Turret",
    "StorageBuilding",
    "LivingModule",
    "ResearchLab",
]

UNITS = ["Worker", "Miner", "Soldier"]


def new_image():
    return Image.new("RGBA", (SIZE, SIZE), (0, 0, 0, 0))


def rect(d, box, color):
    d.rectangle(box, fill=color)


def ellipse(d, box, color):
    d.ellipse(box, fill=color)


def polygon(d, points, color):
    d.polygon(points, fill=color)


def line(d, points, color, width=1):
    d.line(points, fill=color, width=width)


# ---------------------------------------------------------------- юниты

def draw_unit(pal, faction, kind, frame):
    img = new_image()
    d = ImageDraw.Draw(img)

    bob = [1, 0, 1][frame % 3]
    leg_shift = [-1, 0, 1][frame % 3]

    head_size = 8 if faction == "Humans" else 7
    head_top = 4 + bob
    body_top = head_top + head_size
    body_bottom = body_top + 9

    # Ноги (сдвигаются по кадрам).
    rect(d, (13 + leg_shift, body_bottom, 15 + leg_shift, body_bottom + 5), pal["dark"])
    rect(d, (17 - leg_shift, body_bottom, 19 - leg_shift, body_bottom + 5), pal["dark"])

    # Корпус.
    rect(d, (12, body_top, 20, body_bottom), pal["primary"])
    rect(d, (12, body_top, 20, body_top + 2), pal["light"])

    # Голова + шлем.
    ellipse(d, (16 - head_size // 2, head_top, 16 + head_size // 2, head_top + head_size), pal["skin"])
    rect(d, (16 - head_size // 2, head_top + 1, 16 + head_size // 2, head_top + 3), pal["light"])

    if faction == "Humans":
        # Кислородный баллон и антенна.
        rect(d, (20, body_top + 2, 22, body_top + 7), pal["light"])
    else:
        # Две антенны марсиан.
        line(d, [(11, head_top + 2), (8, head_top - 2)], pal["accent"])
        line(d, [(21, head_top + 2), (24, head_top - 2)], pal["accent"])

    # Инструмент по типу юнита.
    if kind == "Miner":
        line(d, [(20, body_top + 3 + leg_shift), (26, body_top - 2 + leg_shift)], pal["dark"], 2)
        line(d, [(23, body_top - 3 + leg_shift), (28, body_top + 1 + leg_shift)], pal["accent"], 2)
    elif kind == "Soldier":
        line(d, [(19, body_top + 4), (28, body_top + 4)], pal["dark"], 2)
        rect(d, (26, body_top + 3, 28, body_top + 5), pal["accent"])
    else:
        rect(d, (20, body_top + 5, 24, body_top + 7), pal["accent"])

    # Блик по корпусу: цвет фракции виден даже в мелком масштабе.
    rect(d, (13, body_top + 3, 14, body_bottom - 1), pal["light"])

    return img


# ---------------------------------------------------------------- здания

def draw_building(pal, faction, kind):
    img = new_image()
    d = ImageDraw.Draw(img)
    base = 30

    if kind == "House":
        rect(d, (8, 16, 24, base), pal["primary"])
        polygon(d, [(6, 16), (26, 16), (16, 6)], pal["dark"])
        rect(d, (14, 23, 18, base), pal["accent"])
        rect(d, (10, 18, 13, 21), pal["glass"])

    elif kind == "MainBuilding":
        ellipse(d, (4, 10, 28, base + 2), pal["primary"])
        ellipse(d, (7, 13, 25, base), pal["light"])
        rect(d, (14, 2, 18, 12), pal["dark"])
        ellipse(d, (12, 0, 20, 5), pal["accent"])
        rect(d, (14, base - 8, 18, base), pal["dark"])

    elif kind == "ExtendsBuilding":
        rect(d, (5, 20, 27, base), pal["dark"])
        rect(d, (7, 14, 15, 22), pal["primary"])
        rect(d, (17, 17, 25, 23), pal["light"])
        rect(d, (9, 16, 13, 18), pal["accent"])
        line(d, [(7, 14), (15, 14)], pal["light"])

    elif kind == "Mine":
        rect(d, (4, 12, 28, base), pal["dark"])
        ellipse(d, (9, 12, 23, 28), pal["primary"])
        rect(d, (11, 20, 21, base), (0, 0, 0, 0))
        rect(d, (11, 20, 21, 21), pal["dark"])
        line(d, [(2, base - 2), (30, base - 2)], pal["accent"], 2)
        line(d, [(10, 10), (22, 10)], pal["light"], 2)

    elif kind == "Greenhouse":
        ellipse(d, (3, 10, 29, base + 1), pal["glass"])
        line(d, [(16, 10), (16, base)], pal["light"])
        line(d, [(3, 24), (29, 24)], pal["light"])
        rect(d, (8, 22, 11, 28), pal["primary"])
        rect(d, (21, 22, 24, 28), pal["primary"])
        rect(d, (14, 25, 18, 28), pal["accent"])

    elif kind == "PowerPlant":
        rect(d, (11, 12, 21, base), pal["primary"])
        rect(d, (13, 6, 19, 13), pal["dark"])
        ellipse(d, (12, 2, 20, 8), pal["accent"])
        line(d, [(4, 18), (12, 18)], pal["light"], 2)
        line(d, [(20, 18), (28, 18)], pal["light"], 2)
        rect(d, (14, base - 6, 18, base), pal["dark"])

    elif kind == "Turret":
        rect(d, (6, 24, 26, base), pal["dark"])
        rect(d, (12, 14, 20, 26), pal["primary"])
        rect(d, (17, 16, 29, 19), pal["light"])
        rect(d, (14, 12, 18, 16), pal["accent"])
        line(d, [(16, 26), (10, 30)], pal["dark"], 2)

    elif kind == "StorageBuilding":
        rect(d, (8, 10, 24, base), pal["primary"])
        ellipse(d, (8, 6, 24, 14), pal["light"])
        line(d, [(8, 16), (24, 16)], pal["dark"], 2)
        line(d, [(8, 23), (24, 23)], pal["dark"], 2)
        rect(d, (14, 18, 18, 21), pal["accent"])

    elif kind == "LivingModule":
        rect(d, (4, 12, 28, 26), pal["primary"])
        ellipse(d, (2, 11, 10, 27), pal["light"])
        ellipse(d, (22, 11, 30, 27), pal["light"])
        ellipse(d, (11, 15, 21, 23), pal["glass"])
        rect(d, (5, 26, 27, base), pal["dark"])

    else:  # ResearchLab
        rect(d, (6, 16, 26, base), pal["primary"])
        rect(d, (10, 12, 22, 17), pal["dark"])
        line(d, [(16, 12), (26, 4)], pal["light"], 2)
        ellipse(d, (22, 0, 30, 8), pal["accent"])
        rect(d, (9, 20, 13, 24), pal["glass"])
        rect(d, (19, 20, 23, 24), pal["glass"])

    # Полоса «своей» фракции под зданием, чтобы принадлежность читалась сразу.
    rect(d, (4, base, 28, base + 1), pal["accent"])

    return img


# ---------------------------------------------------------------- база

def draw_base(pal, faction):
    img = new_image()
    d = ImageDraw.Draw(img)

    rect(d, (4, 28, 28, 30), pal["dark"])

    if faction == "Humans":
        # Флагшток с флагом.
        rect(d, (15, 4, 17, 29), pal["light"])
        polygon(d, [(17, 5), (29, 9), (17, 13)], pal["primary"])
        rect(d, (8, 22, 12, 29), pal["accent"])
    else:
        # Каменный обелиск с кристаллом.
        polygon(d, [(16, 4), (24, 16), (16, 29), (8, 16)], pal["primary"])
        polygon(d, [(16, 8), (20, 16), (16, 24), (12, 16)], pal["light"])
        ellipse(d, (13, 1, 19, 7), pal["accent"])

    return img


# ---------------------------------------------------------------- запись

def save(img, faction, name):
    folder = os.path.join(OUT_ROOT, faction)
    os.makedirs(folder, exist_ok=True)

    path = os.path.join(folder, name + ".png")
    img.save(path)
    return path


def main():
    written = []

    for faction, pal in PALETTES.items():
        for kind in UNITS:
            for frame in range(3):
                img = draw_unit(pal, faction, kind, frame)
                written.append(save(img, faction, "unit_%s_%d" % (kind.lower(), frame)))

        written.append(save(draw_base(pal, faction), faction, "base"))

        for kind in BUILDINGS:
            img = draw_building(pal, faction, kind)
            written.append(save(img, faction, "building_%s" % kind.lower()))

    print("written %d sprites" % len(written))
    for path in written[:5]:
        print("  " + path)


if __name__ == "__main__":
    main()
