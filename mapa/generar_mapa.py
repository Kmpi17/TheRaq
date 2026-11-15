"""
Plano cenital del mundo de GTAChiraq (referencia para ir modificándolo).

Copia a mano las medidas de Assets/_Project/Editor/PrototypeSceneBuilder*.cs. Si cambias el generador,
actualiza las constantes de aquí y vuelve a ejecutar:  python mapa/generar_mapa.py  -> mapa/mapa.png

Lo que en el juego sale al azar (árboles del césped, basura, montones de nieve, solares con maleza) se dibuja
como zona o se indica en la leyenda; no se puede reproducir la posición exacta porque usa System.Random de C#.
Norte (z+) arriba, este (x+) a la derecha. Unidades: metros.
"""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

# ---------- Constantes del generador (PrototypeSceneBuilder.cs) ----------
BLOCK_HALF_X, BLOCK_HALF_Z = 100, 70
SIDEWALK, ROAD = 3, 14
D_MIN_X, D_MAX_X, D_HALF_Z = 120, 250, 88
D_ROAD = 12
D_LAWN_INNER = D_ROAD / 2 + SIDEWALK            # 9
WALKUP_Z, TOWER_Z = D_LAWN_INNER + 14, 62        # 23, 62
WALKUPS_X, TOWERS_X = [136, 176, 216], [156, 196]
WALKUP_SIZE, TOWER_SIZE = (24, 12), (30, 15)     # (x, z)
COURT, COURT_SIZE, COURT_MARGIN = (232, 58), (12, 20), 1.5
PLAZA, WRECK = (232, -58), (241, -34)

# ---------- Ampliación oeste del barrio de casas (PrototypeSceneBuilder.HousesWestExt.cs), en el mundo ----------
WX_MIN_X, WX_MAX_X, WX_MIN_Z, WX_MAX_Z, WX_STREET_END = -103, -30, 90, 226, -36
WX_ROADS = [(-103, 126.5, -36, 136.5), (-103, 180.5, -36, 190.5)]
WX_ROAD_NAMES = [("D", -70, 131.5), ("E", -70, 185.5)]
WX_ALLEYS = [(-100.5, 102.5, -26, 107.5), (-100.5, 156.5, -26, 161.5), (-100.5, 219, -26, 224)]
WX_SIDEWALKS = [(-103, 90, -100.5, 226), (-100.5, 124, -36, 126.5), (-100.5, 136.5, -36, 139), (-36, 124, -33.5, 139),
                (-100.5, 178, -36, 180.5), (-100.5, 190.5, -36, 193), (-36, 178, -33.5, 193)]
WX_LAWNS = [(-100.5, 90, -30, 126.5), (-36, 126.5, -30, 136.5), (-100.5, 136.5, -30, 180.5), (-36, 180.5, -30, 190.5),
            (-100.5, 190.5, -30, 226)]
WX_FENCE_GAPS_LOCAL = [(92, 97), (154.5, 159.5), (208.5, 213.5)]   # huecos en la valla este de la copia (z local)

# ---------- Oeste lejano (PrototypeSceneBuilder.FarWest.cs), en el mundo ----------
FW_ROADS = [(-262, 87, -252, 298), (-252, 126.5, -117, 136.5), (-252, 180.5, -117, 190.5), (-252, 248, -117, 258),
            (-252, 288, -117, 298)]
FW_ROAD_NAMES = [("J", -257, 210), ("D", -190, 131.5), ("E", -160, 185.5), ("F", -190, 253), ("H", -190, 293)]
FW_ALLEYS = [(-226, 156.5, -120, 161.5), (-205, 219, -120, 224)]
FW_SIDEWALKS = [(-264.5, 90, -262, 298), (-252, 90, -249.5, 298), (-249.5, 124, -120, 126.5), (-249.5, 136.5, -120, 139),
                (-249.5, 178, -120, 180.5), (-249.5, 190.5, -120, 193), (-249.5, 245.5, -120, 248),
                (-249.5, 258, -120, 260.5), (-249.5, 285.5, -120, 288)]
FW_LAWNS = [(-249.5, 102, -160, 124), (-249.5, 139, -120, 178), (-249.5, 193, -120, 245.5), (-249.5, 260.5, -120, 285.5),
            (-300, 90, -264.5, 298)]
FW_GAS_LOT = (-160, 90, -120, 124)
FW_GAS_DRIVES = [(-156, 87, -146, 90), (-134, 87, -124, 90), (-120, 104, -117, 114)]
FW_CANOPY = (-150, 98.5, -128, 110.5)
FW_MART = (-158, 113, -141, 120.5)
FW_SHOPS = [("7", -247, -235), ("8", -229, -219), ("9", -215, -202), ("10", -190, -176), ("11", -173, -163)]
FW_SHOPS_VACANT = (-201, 93, -191, 102)
FW_COURT = (-237.75, 158.5)
SH_FRONT_FW = 93                                  # misma línea de fachada que los negocios (ShopsFront)
FW_WASTELAND = (-249.5, 193, -205, 245.5)

# ---------- Ampliación norte (PrototypeSceneBuilder.HousesNorth.cs), en el mundo ----------
N_MAX_Z = 298                                    # borde norte de la calle H
NE_ROADS = [(117, 248, 207, 258), (207, 202, 217, 298), (117, 288, 207, 298)]   # Fe, G (sigue a B), H este
NE_ROAD_NAMES = [("Fe", 160, 253), ("G", 212, 255), ("H", 160, 293)]
NE_ALLEYS = [(241, 213.5, 246, 285.5)]
NE_SIDEWALKS = [(120, 245.5, 207, 248), (120, 258, 207, 260.5), (204.5, 202, 207, 208.5), (204.5, 213.5, 207, 285.5),
                (217, 202, 219.5, 208.5), (217, 213.5, 219.5, 298), (120, 285.5, 207, 288)]
NE_LAWNS = [(120, 214, 207, 248), (120, 258, 207, 288), (217, 214, 250, N_MAX_Z)]
NW_ROADS = [(-103, 248, 103, 258), (3, 224, 13, 248), (-103, 288, 103, 298)]   # F, B' (sigue), H oeste
NW_ROAD_NAMES = [("F", -60, 253), ("F", 50, 253), ("H", -60, 293), ("H", 50, 293), ("B'", 8, 236)]
NW_ALLEYS = [(-42, 260.5, -37, 285.5)]
NW_SIDEWALKS = [(-103, 226, -100.5, 288), (-100.5, 245.5, 100, 248), (-100.5, 258, 100, 260.5), (0.5, 224, 3, 245.5),
                (13, 224, 15.5, 245.5), (-100.5, 285.5, 100, 288)]
NW_LAWNS = [(-100.5, 226, 3, 248), (13, 226, 100, 248), (-100.5, 258, 100, 288)]

# ---------- Dibujo ----------
S = 4                                    # píxeles por metro
X0, X1, Z0, Z1 = -305, 262, -112, 302    # zona del mundo que se dibuja

# ---------- Barrio de casas (PrototypeSceneBuilder.Houses.cs), rectángulos (x0, z0, x1, z1) ----------
H_ROADS = [(117, 117, 207, 127), (207, 92, 217, 202), (117, 183, 173, 193), (163, 171, 207, 181), (163, 181, 173, 183)]
H_ROAD_NAMES = [("A", 150, 122), ("B", 212, 150), ("C", 140, 188), ("C", 190, 176)]
H_ALLEYS = [(120, 92, 241, 97), (120, 147, 204.5, 152), (185, 152, 190, 168.5), (120, 208.5, 246, 213.5),
            (241, 92, 246, 213.5), (219.5, 126, 241, 131)]
H_SIDEWALKS = [(120, 114.5, 207, 117), (120, 127, 207, 129.5), (204.5, 92, 207, 202), (217, 92, 219.5, 202),
               (120, 180.5, 163, 183), (120, 193, 175.5, 195.5), (173, 181, 175.5, 193), (175.5, 181, 207, 183.5),
               (163, 168.5, 207, 171), (160.5, 168.5, 163, 180.5)]
H_LAWNS = [(120, 90, 207, 117), (120, 127, 207, 171), (120, 171, 163, 183), (120, 193, 173, 214), (173, 181, 207, 214),
           (207, 202, 217, 214), (217, 90, 250, 214)]
MARGIN, LEGEND_W = 60, 470
W = int((X1 - X0) * S) + 2 * MARGIN + LEGEND_W
H = int((Z1 - Z0) * S) + 2 * MARGIN + 50

C = dict(
    bg=(238, 236, 230), asphalt=(70, 70, 74), line=(214, 170, 40), sidewalk=(178, 175, 168),
    lawn=(150, 150, 126), lawn_d=(128, 136, 92), brick=(150, 78, 58), brick_d=(118, 62, 48),
    tower=(126, 58, 42), house=(160, 118, 98), store=(132, 132, 130), path=(196, 192, 184),
    fence=(25, 25, 28), court=(92, 92, 96), plaza=(186, 182, 172), wreck=(150, 84, 40),
    car=(40, 40, 46), light=(255, 150, 40), pole=(110, 78, 50), dumpster=(40, 110, 60),
    weeds=(176, 150, 80), snow=(245, 248, 255), tree=(60, 90, 50), text=(25, 25, 28),
    grid=(255, 255, 255), player=(30, 110, 220), bus=(30, 70, 160), bollard=(170, 40, 30),
    alley=(112, 104, 92), wood=(120, 90, 60), garage=(150, 150, 140), porch=(170, 140, 100), train=(95, 110, 100),
    lot=(84, 84, 88), balcony=(150, 155, 160), row=(168, 96, 74),
)


def font(size, bold=False):
    for name in (("arialbd.ttf" if bold else "arial.ttf"), "DejaVuSans.ttf"):
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            pass
    return ImageFont.load_default()


F_S, F_M, F_L, F_T = font(13), font(16, True), font(20, True), font(28, True)
img = Image.new("RGB", (W, H), C["bg"])
d = ImageDraw.Draw(img, "RGBA")


def px(x, z):
    return MARGIN + (x - X0) * S, MARGIN + 50 + (Z1 - z) * S


def rect(cx, cz, w, h, fill, outline=None, width=1):
    """Rectángulo centrado en (cx, cz) de w (x) × h (z) metros."""
    box(cx - w / 2, cz - h / 2, cx + w / 2, cz + h / 2, fill, outline, width)


def box(x0, z0, x1, z1, fill, outline=None, width=1):
    # Recorta al marco del plano (las aceras y líneas del juego miden 600 m).
    x0, x1 = (max(X0, min(X1, v)) for v in (x0, x1))
    z0, z1 = (max(Z0, min(Z1, v)) for v in (z0, z1))
    a, b = px(min(x0, x1), max(z0, z1)), px(max(x0, x1), min(z0, z1))
    d.rectangle([a, b], fill=fill, outline=outline, width=width)


def dot(x, z, r_px, fill, outline=None):
    cx, cy = px(x, z)
    d.ellipse([cx - r_px, cy - r_px, cx + r_px, cy + r_px], fill=fill, outline=outline)


def label(x, z, text, f=F_S, fill=None, anchor="mm", halo=True):
    p = px(x, z)
    if halo:
        d.text(p, text, font=f, fill=(255, 255, 255, 200), anchor=anchor, stroke_width=3, stroke_fill=(255, 255, 255, 200))
    d.text(p, text, font=f, fill=fill or C["text"], anchor=anchor)


def hatch(x0, z0, x1, z1, color, step=2.0):
    """Zona rayada en diagonal (para zonas de maleza o nieve)."""
    box(x0, z0, x1, z1, color + (70,))
    xa, xb, za, zb = min(x0, x1), max(x0, x1), min(z0, z1), max(z0, z1)
    t = xa - (zb - za)
    while t < xb:
        # línea x - z = cte recortada al rectángulo
        p, q = (t, za), (t + (zb - za), zb)
        if p[0] < xa: p = (xa, za + (xa - p[0]))
        if q[0] > xb: q = (xb, zb - (q[0] - xb))
        if p[0] <= q[0]:
            d.line([px(*p), px(*q)], fill=color + (210,), width=2)
        t += step


# ---------- Suelo y calles ----------
box(X0, Z0, X1, Z1, C["asphalt"])
outer_x, outer_z = BLOCK_HALF_X + SIDEWALK, BLOCK_HALF_Z + SIDEWALK
road_z, road_x = outer_z + ROAD / 2, outer_x + ROAD / 2          # 80, 110
far_z, far_x = outer_z + ROAD + SIDEWALK / 2, outer_x + ROAD + SIDEWALK / 2
# Calles grandes (PrototypeSceneBuilder.MainRoads.cs): la doble amarilla para en la línea de stop, a 4.1 m del cruce,
# y en cada llegada hay un paso de cebra de 0.4 a 3.2 m del cruce.
STOP_GAP = ROAD / 2 + 4.1
for z in (road_z, -road_z):
    edges = [X0, -road_x - STOP_GAP, -road_x + STOP_GAP, road_x - STOP_GAP]   # acaba en la avenida
    for x0_, x1_ in zip(edges[0::2], edges[1::2]):
        box(x0_, z - 0.08, x1_, z + 0.08, C["line"])
for x in (road_x, -road_x):
    edges = [Z0] + [e for k in (-road_z, road_z) for e in (k - STOP_GAP, k + STOP_GAP)] + [Z1]
    for z0_, z1_ in zip(edges[0::2], edges[1::2]):
        box(x - 0.08, z0_, x + 0.08, z1_, C["line"])
for cx in (-road_x, road_x):
    for cz in (-road_z, road_z):
        for sgn in (1, -1):
            n0, n1 = ROAD / 2 + 0.4, ROAD / 2 + 3.2
            for i in range(14):                                                 # barras de 0.5 m cada metro
                a0 = -ROAD / 2 + 0.3 + i
                if not (cx > 0 and sgn > 0):                                    # z = ±80 acaba en la avenida (cruce en T)
                    box(cx + n0 if sgn > 0 else cx - n1, cz + a0, cx + n1 if sgn > 0 else cx - n0, cz + a0 + 0.5, (215, 215, 205))
                box(cx + a0, cz + sgn * n0 if sgn > 0 else cz - n1, cx + a0 + 0.5, cz + n1 if sgn > 0 else cz - n0, (215, 215, 205))
# Aceras de enfrente N/S y O, cortadas donde las cruza la otra calle grande (no hay bordillo atravesando la calzada).
for side in (1, -1):
    for x0_, x1_ in ((-300, -outer_x - ROAD), (-outer_x, outer_x), (outer_x + ROAD, 300)):
        box(x0_, side * far_z - SIDEWALK / 2, x1_, side * far_z + SIDEWALK / 2, C["sidewalk"])
for z0_, z1_ in ((Z0, -outer_z - ROAD), (-outer_z, outer_z), (outer_z + ROAD, Z1)):
    box(-far_x - 1.5, z0_, -far_x + 1.5, z1_, C["sidewalk"])
box(far_x - 1.5, D_ROAD / 2, far_x + 1.5, Z1, C["sidewalk"])                    # acera de enfrente E (cortada)
box(far_x - 1.5, -D_ROAD / 2, far_x + 1.5, Z0, C["sidewalk"])

# ---------- Manzana central: conjunto de torres (PrototypeSceneBuilder.Towers.cs) ----------
T_LOTS = [(-92, -64, -50, -47), (-22, -64, 22, -30), (36, -64, 76, -47)]
T_DRIVEWAYS = [(-75, -73, -67, -64), (12, -73, 20, -64), (64, -73, 72, -64)]
T_CROSSING = (-1.2, -64, 1.2, -30)
T_WALKS = [(-100, -8.2, 100, -5.8), (-1.2, -30, 1.2, -8.2), (-1.2, -70, 1.2, -64), (-63.2, 47.5, -60.8, 70),
           (72.5, 8.8, 100, 11.2), (74.6, -5.8, 77, 8.8)]
T_GATES = [(-100, -7, 1.2), (100, -7, 1.2), (0, -70, 1.2), (-62, 70, 1.2), (100, 10, 1.2)]
# Torres: (nombre, centro x, z, yaw, plantas). 27 × 15 m; portal en la cara −Z local, balcones en la +Z.
T_TOWERS = [("T1", -62, 40, 180, 12), ("T2", 0, 2, 0, 14), ("T3", 65, 10, -90, 11)]
# Adosadas: (centro de la fachada x, z, yaw, largo, jardín delantero). Fondo 9 m, 2 plantas.
T_ROWS = [(-65, 8, 0, 46, 13.8), (-65, -22, 180, 46, 13.8), (5, 60, 180, 44, 10), (62, 60, 180, 40, 10), (90, -28, -90, 38, 10)]
T_PARK_LAMPS = [-95, -35, -15, 25, 45, 65, 85]   # en el eje, z = −4.9 (donde hay sitio)
T_BENCHES = [-25, -5, 15, 35, 55, 75]            # en el eje, z = −9.2
T_CLOTHESLINES = [((-82, 23), (-70, 23)), ((-58, 24.5), (-47, 24.5)), ((-12, 44), (0, 44)), ((50, 44.5), (62, 44.5)), ((74, -40), (74, -28))]
T_DESIRE = [((-38, -9), (-39.5, -32), (-49, -46.5)), ((40, -8.6), (52, -46.5)), ((-8, 47.5), (-21, 34.5))]


def local_to_world(cx, cz, yaw, lx, lz):
    """Mismo giro que Unity (yaw en grados, sólo múltiplos de 90)."""
    return {0: (cx + lx, cz + lz), 180: (cx - lx, cz - lz), -90: (cx - lz, cz + lx), 90: (cx + lz, cz - lx)}[yaw]


def local_box(cx, cz, yaw, x0, z0, x1, z1, fill, outline=None, width=1):
    (ax, az), (bx, bz) = local_to_world(cx, cz, yaw, x0, z0), local_to_world(cx, cz, yaw, x1, z1)
    box(min(ax, bx), min(az, bz), max(ax, bx), max(az, bz), fill, outline, width)


outer_x, outer_z = BLOCK_HALF_X + SIDEWALK, BLOCK_HALF_Z + SIDEWALK
box(-outer_x, -outer_z, outer_x, outer_z, C["sidewalk"])
box(-BLOCK_HALF_X, -BLOCK_HALF_Z, BLOCK_HALF_X, BLOCK_HALF_Z, C["lawn_d"])
for r in T_LOTS + T_DRIVEWAYS:
    box(*r, C["lot"])
for (x0, z0, x1, z1) in T_LOTS:                                                    # filas de plazas (2.6 × 5 m)
    n = int((x1 - x0 - 1) // 2.6)
    sx0 = (x0 + x1) / 2 - n * 2.6 / 2
    rows = [z0] + ([z0 + 12, z0 + 17] if z1 - z0 >= 33 else []) + [z1 - 5]
    for rz in rows:
        for b in range(n + 1):
            x = sx0 + b * 2.6
            if T_CROSSING[0] - 1.4 < x < T_CROSSING[2] + 1.4 and z1 - z0 >= 33:
                continue
            d.line([px(x, rz), px(x, rz + 5)], fill=(225, 225, 220), width=1)
box(*T_CROSSING, (225, 225, 220, 120))
for r in T_WALKS:
    box(*r, C["path"])
for pts in T_DESIRE:                                                               # atajos de barro
    d.line([px(*p) for p in pts], fill=(120, 85, 50, 200), width=4)

for name, cx, cz, yaw, floors in T_TOWERS:
    W_, D_ = 27, 15
    local_box(cx, cz, yaw, -W_ / 2 - 0.4, D_ / 2 + 0.6, W_ / 2 + 0.4, D_ / 2 + 3.2, C["snow"] + (200,))   # nieve a la sombra
    local_box(cx, cz, yaw, -W_ / 2, -D_ / 2, W_ / 2, D_ / 2, C["tower"], outline=(40, 20, 15), width=2)
    for bx in (-6, 6):                                                             # balcones con malla
        local_box(cx, cz, yaw, bx - 2, D_ / 2, bx + 2, D_ / 2 + 1.4, C["balcony"], outline=(60, 60, 60))
    local_box(cx, cz, yaw, -2.3, -D_ / 2 - 2.2, 2.3, -D_ / 2, (150, 145, 135), outline=(90, 90, 90))  # marquesina
    local_box(cx, cz, yaw, W_ / 2 + 0.7, D_ / 2 - 6.3, W_ / 2 + 3.7, D_ / 2 - 1.1, C["dumpster"], outline=(20, 50, 30))
    label(cx, cz, f"torre {name}\n{floors} pl. ({floors * 3} m)", F_S, fill=(255, 255, 255), halo=False)

for cx, cz, yaw, length, yard in T_ROWS:
    n = max(2, round(length / 6.5))
    u, half = length / n, length / 2
    local_box(cx, cz, yaw, -half, 9, half, 11.6, (170, 168, 160))                  # patios traseros de hormigón
    local_box(cx, cz, yaw, -half, 0, half, 9, C["row"], outline=(40, 20, 15), width=2)
    for i in range(n):
        ux = -half + (i + 0.5) * u
        if i + 1 < n:
            a, b = local_to_world(cx, cz, yaw, ux + u / 2, 0), local_to_world(cx, cz, yaw, ux + u / 2, 9)
            d.line([px(*a), px(*b)], fill=(70, 40, 30), width=1)
        door = ux + (1 if i % 2 == 0 else -1) * (u / 2 - 1.1)
        local_box(cx, cz, yaw, door - 0.6, -yard, door + 0.6, -1.5, C["path"])
        local_box(cx, cz, yaw, door - 0.8, -1.1, door + 0.8, 0, (200, 200, 195))
        if yard > 12:                                                              # valla baja propia (las del eje)
            a, b = local_to_world(cx, cz, yaw, ux - u / 2 + 0.1, -yard + 0.5), local_to_world(cx, cz, yaw, ux + u / 2 - 0.1, -yard + 0.5)
            d.line([px(*a), px(*b)], fill=C["fence"] + (150,), width=1)
    mx, mz = local_to_world(cx, cz, yaw, 0, 4.5)
    label(mx, mz, f"adosadas ×{n}", F_S, fill=(255, 255, 255), halo=False)
    for i in range(n):                                                             # los caminos abren la valla del perímetro
        ux = -half + (i + 0.5) * u
        door = ux + (1 if i % 2 == 0 else -1) * (u / 2 - 1.1)
        T_GATES.append((*local_to_world(cx, cz, yaw, door, -yard), 0.6))

# Valla baja de hierro del perímetro, abierta donde llegan caminos y en las entradas de los aparcamientos.
fx, fz = BLOCK_HALF_X - 0.6, BLOCK_HALF_Z - 0.6
for along_x, line in ((True, fz), (True, -fz), (False, fx), (False, -fx)):
    lim = fx if along_x else fz
    openings = []
    for gx, gz, half in T_GATES:
        across, along = (gz, gx) if along_x else (gx, gz)
        if abs(across - line) <= 1.5:
            openings.append((along - half - 0.5, along + half + 0.5))
    if along_x and line < 0:
        openings += [(r[0] - 0.3, r[2] + 0.3) for r in T_DRIVEWAYS]
    cursor = -lim
    for a, b in sorted(openings) + [(lim, lim)]:
        if min(a, lim) - cursor > 0.3:
            p, q = ((cursor, line), (min(a, lim), line)) if along_x else ((line, cursor), (line, min(a, lim)))
            d.line([px(*p), px(*q)], fill=C["fence"], width=2)
        cursor = max(cursor, b)

rect(-26, 30, 10, 8, C["court"], outline=(200, 200, 205), width=2)                # losa con canasta portátil
label(-26, 30, "canasta", F_S, fill=(255, 255, 255), halo=False)
for a, b in T_CLOTHESLINES:
    d.line([px(*a), px(*b)], fill=(230, 60, 140), width=2)
    for p in (a, b):
        dot(*p, 2, (90, 90, 90))
rect(50, -30, 2.2, 1.2, (230, 130, 40), outline=(90, 50, 10))                     # sofá
label(50, -34, "sofá", F_S)
dot(6.5, -10.6, 3, (60, 120, 255))                                                 # cámara
for x in T_PARK_LAMPS:
    dot(x, -4.9, 2.5, (255, 200, 120), outline=(120, 90, 40))
for x in T_BENCHES:
    rect(x, -9.2, 1.9, 0.6, C["wood"])
for (x0, z0, x1, z1) in T_LOTS:                                                    # farolas de los aparcamientos
    for t in (0.25, 0.75):
        dot(x0 + (x1 - x0) * t, z1 + 0.8, 3, C["light"])
for name, (x0, z0, x1, z1) in zip(("P1", "P2", "P3"), T_LOTS):
    label((x0 + x1) / 2 + (8 if name == "P2" else 0), (z0 + z1) / 2 + (7 if name == "P2" else 0), f"aparcamiento {name}", F_S, fill=(255, 255, 255), halo=False)
label(-58, 66, "MANZANA CENTRAL · conjunto de torres", F_M)
label(-58, 62, "3 torres + adosadas, patios y 3 aparcamientos", F_S)
label(-50, -3, "eje peatonal E–O", F_S)
for x in range(-90, 91, 30):
    for z in (outer_z - 0.5, -outer_z + 0.5):
        dot(x, z, 3, C["light"])
for z in range(-60, 61, 30):
    for x in (outer_x - 0.5, -outer_x + 0.5):
        dot(x, z, 3, C["light"])

# ---------- Alrededores ----------
row_z = BLOCK_HALF_Z + SIDEWALK * 2 + ROAD + 9                    # 99
avenue_x = BLOCK_HALF_X + SIDEWALK + ROAD / 2                     # 110
x = -200
while x <= 200:
    if abs(abs(x) - avenue_x) >= 14:
        for side in (1, -1):
            if side > 0:
                continue  # al norte: oeste lejano, ampliación oeste, negocios y barrios de casas
            rect(x, side * row_z, 10, 14, C["house"], outline=(70, 50, 40))
    x += 13
label(-60, -row_z, "two-flats (2 plantas, color liso)", F_S)
rect(-avenue_x - 20, -row_z, 16, 14, C["store"], outline=(30, 30, 30), width=2)
box(-avenue_x - 26, -row_z + 7, -avenue_x - 14, -row_z + 7.6, (30, 70, 160))
label(-avenue_x - 20, -row_z - 10, "tienda de esquina", F_S)
for i in range(5):
    side = 1 if i % 2 == 0 else -1
    rect(-80 + i * 37, side * (BLOCK_HALF_Z + SIDEWALK + ROAD - 1.5), 4.5, 1.9, C["car"])

# ---------- Barrio de viviendas ----------
half_road = D_ROAD / 2
for side in (1, -1):
    box(D_MIN_X, side * D_LAWN_INNER, D_MAX_X, side * D_HALF_Z, C["lawn_d"])
    box(D_MIN_X, side * half_road, D_MAX_X, side * D_LAWN_INNER, C["sidewalk"])
box(D_MIN_X - SIDEWALK, -half_road, D_MAX_X, half_road, C["asphalt"])
for dz in (-0.2, 0.2):
    box(D_MIN_X - SIDEWALK, dz - 0.07, D_MAX_X, dz + 0.07, C["line"])

# Zonas de maleza alta (Vegetation.cs: BuildOvergrowth)
for side in (1, -1):
    hatch(D_MIN_X, side * (D_HALF_Z - 5.5), D_MAX_X, side * (D_HALF_Z - 0.6), C["weeds"])     # fondo
    hatch(D_MAX_X - 7, side * (D_LAWN_INNER + 0.8), D_MAX_X - 0.6, side * (D_HALF_Z - 1), C["weeds"])  # final de la calle
    hatch(D_MIN_X, side * (D_LAWN_INNER + 0.5), D_MAX_X, side * (D_LAWN_INNER + 1.0), C["weeds"], 1.2)  # pie de la valla
cx, cy = px(*WRECK)
d.ellipse([cx - 6 * S, cy - 6 * S, cx + 6 * S, cy + 6 * S], fill=C["weeds"] + (90,), outline=C["weeds"] + (230,), width=2)

# Edificios, caminos, explanadas
for side in (1, -1):
    for x in WALKUPS_X:
        z = side * WALKUP_Z
        face = abs(z) - WALKUP_SIZE[1] / 2
        box(x - 1.2, side * D_LAWN_INNER, x + 1.2, side * face, C["path"])
        # nieve a la sombra detrás
        back = abs(z) + WALKUP_SIZE[1] / 2
        box(x - WALKUP_SIZE[0] / 2, side * (back + 0.3), x + WALKUP_SIZE[0] / 2, side * (back + 3.2), C["snow"] + (200,))
        rect(x, z, *WALKUP_SIZE, C["brick"], outline=(40, 20, 15), width=2)
        label(x, z, "bloque bajo\n3 pl.", F_S, fill=(255, 255, 255), halo=False)
    for x in TOWERS_X:
        z = side * TOWER_Z
        face = abs(z) - TOWER_SIZE[1] / 2
        fore = face - 7.4
        box(x - 1.2, side * D_LAWN_INNER, x + 1.2, side * fore, C["path"])
        box(x - 5, side * fore, x + 5, side * face, C["path"], outline=(150, 145, 135))
        back = abs(z) + TOWER_SIZE[1] / 2
        box(x - TOWER_SIZE[0] / 2, side * (back + 0.3), x + TOWER_SIZE[0] / 2, side * (back + 3.2), C["snow"] + (200,))
        rect(x, z, *TOWER_SIZE, C["tower"], outline=(40, 20, 15), width=2)
        label(x, z, "torre · 8 pl. (24 m)", F_S, fill=(255, 255, 255), halo=False)
        dot(x + 2.5, side * (fore + 1.5), 3, (60, 120, 255))                      # cámara
        pad = (x + TOWER_SIZE[0] / 2 + 3, side * (TOWER_Z - 3))
        rect(*pad, 3.2, 5, C["dumpster"], outline=(20, 50, 30))                    # contenedores

# Valla de hierro (con huecos en los caminos)
gaps = sorted(WALKUPS_X + TOWERS_X)
for side in (1, -1):
    z = side * (D_LAWN_INNER + 0.25)
    start = D_MIN_X + 0.3
    for g in gaps + [None]:
        end = (g - 1.7) if g is not None else D_MAX_X - 0.3
        d.line([px(start, z), px(end, z)], fill=C["fence"], width=3)
        if g is not None:
            start = g + 1.7

# Cancha y plaza
fw, fh = COURT_SIZE[0] + 2 * COURT_MARGIN, COURT_SIZE[1] + 2 * COURT_MARGIN
rect(*COURT, fw, fh, C["court"], outline=(200, 200, 205), width=3)
rect(*COURT, *COURT_SIZE, None, outline=(235, 235, 235), width=1)
label(COURT[0], COURT[1], "cancha", F_S, fill=(255, 255, 255), halo=False)
rect(*PLAZA, 14, 16, C["plaza"], outline=(120, 115, 110), width=2)
box(PLAZA[0] - 7, PLAZA[1] - 8, PLAZA[0] + 7, PLAZA[1] - 7.3, (200, 60, 150))     # muro de grafitis (aprox.)
label(PLAZA[0], PLAZA[1], "plaza\n(grafitis)", F_S)
rect(*WRECK, 4.5, 2, C["wreck"], outline=(60, 30, 10), width=2)
label(WRECK[0], WRECK[1] + 4.5, "coche abandonado", F_S)

# Mobiliario de calle
for x in range(D_MIN_X + 15, D_MAX_X, 30):
    for side in (1, -1):
        dot(x, side * (D_LAWN_INNER - 0.5), 3, C["light"])
for x in range(D_MIN_X + 7, D_MAX_X, 32):
    dot(x, -(half_road + 0.5), 4, C["pole"], outline=(40, 25, 10))
rect(D_MIN_X + 9, D_LAWN_INNER - 1.1, 3.7, 1.6, C["bus"])
label(D_MIN_X + 3, D_LAWN_INNER + 4.5, "bus", F_S)
z = -half_road + 0.8
while z <= half_road - 0.8:
    dot(D_MAX_X - 0.8, z, 2, C["bollard"])
    z += 1.5
for zz in (-4, -1.5, 1, 3.5):                                                     # montón de nieve final
    rect(D_MAX_X - 2.6, zz, 2.6, 2.4, C["snow"] + (230,), outline=(180, 185, 195))
for x in range(D_MIN_X + 8, D_MAX_X - 4, 18):                                     # árboles de acera (aprox.)
    for side in (1, -1):
        dot(x, side * (D_LAWN_INNER + 2.5), 5, C["tree"] + (220,), outline=(30, 50, 25))
for x in range(D_MIN_X + 5, D_MAX_X - 3, 11):                                    # montones de nieve en el bordillo (indicativo)
    for side in (1, -1):
        rect(x + (3 if side < 0 else 0), side * (half_road + 0.55), 2.2, 0.9, C["snow"] + (220,))

label(185, 0, "BARRIO DE VIVIENDAS (texturizado, zona avanzada)", F_M)
label(185, 84.5, "maleza: franja del fondo", F_S)
label(185, -84.5, "maleza: franja del fondo", F_S)
label(246, 32, "maleza\nfinal\ncalle", F_S)

# ---------- Barrio de casas ----------
for r in H_LAWNS:
    box(*r, C["lawn_d"])
for r in H_ROADS:
    box(*r, C["asphalt"])
for r in H_SIDEWALKS:
    box(*r, C["sidewalk"])
for r in H_ALLEYS:
    box(*r, C["alley"])
box(far_x - 1.5, 117, far_x + 1.5, 127, C["asphalt"])                             # la acera de la avenida se corta
box(far_x - 1.5, 183, far_x + 1.5, 193, C["asphalt"])
for r in NE_LAWNS:                                                                  # ampliación norte (este)
    box(*r, C["lawn_d"])
for r in NE_ROADS:
    box(*r, C["asphalt"])
for r in NE_SIDEWALKS:
    box(*r, C["sidewalk"])
for r in NE_ALLEYS:
    box(*r, C["alley"])
box(far_x - 1.5, 248, far_x + 1.5, 258, C["asphalt"])
box(far_x - 1.5, 288, far_x + 1.5, 298, C["asphalt"])
d.line([px(219.5, N_MAX_Z - 0.2), px(250, N_MAX_Z - 0.2)], fill=C["wood"], width=3)  # vallas de madera del borde
d.line([px(250 - 0.2, 90), px(250 - 0.2, N_MAX_Z)], fill=C["wood"], width=3)
for a, b in ((122, 150), (152.5, 186), (188.5, 240)):
    d.line([px(a, 91.2), px(b, 91.2)], fill=C["wood"], width=3)
for z in (92.6,):                                                                  # B sin salida por el sur (sigue como G)
    box(207.2, z - 0.3, 216.8, z + 0.3, (200, 200, 195))
footprints = Path(__file__).with_name("edificios_casas.json")
if footprints.exists():
    import json
    kind_color = {"casa": C["brick"], "apartamentos": C["tower"], "garaje": C["garage"], "porche": C["porch"],
                  "tienda": C["store"], "solar": C["weeds"], "cancha": C["court"], "gasolinera": (225, 225, 220)}
    for e in json.loads(footprints.read_text(encoding="utf-8"))["edificios"]:
        pts = [px(x, z) for x, z in e["esquinas"]]
        if e["tipo"] == "solar":
            d.polygon(pts, fill=C["weeds"] + (110,), outline=C["weeds"])
        else:
            d.polygon(pts, fill=kind_color.get(e["tipo"], C["house"]), outline=(40, 20, 15))
else:
    label(185, 160, "casas: construye la escena en Unity\ny vuelve a ejecutar este script", F_M)
for name, x, z in H_ROAD_NAMES:
    label(x, z, f"calle {name}", F_S)
label(185, 205, "BARRIO DE CASAS (casas bajas, apartamentos de 3 pisos, callejones)", F_M)

# ---------- Copia oeste del barrio de casas (girada 180°: mundo = (220 − x, 316 − z)) ----------
HW_X, HW_Z = 220, 316


def west(r):
    return (HW_X - r[2], HW_Z - r[3], HW_X - r[0], HW_Z - r[1])


def west_line(a, b, z):
    d.line([px(HW_X - a, HW_Z - z), px(HW_X - b, HW_Z - z)], fill=C["wood"], width=3)


for r in H_LAWNS:
    box(*west(r), C["lawn_d"])
for r in H_ROADS:
    box(*west(r), C["asphalt"])
for r in H_SIDEWALKS:
    box(*west(r), C["sidewalk"])
for r in H_ALLEYS:
    box(*west(r), C["alley"])
west_line(120, 240.5, 214 - 0.2)                                                   # valla tras los negocios (hueco en a5)
west_line(246.5, 250, 214 - 0.2)
z_prev = 90                                                                        # valla hacia la ampliación, con huecos
for g0, g1 in WX_FENCE_GAPS_LOCAL + [(214, 214)]:
    d.line([px(HW_X - 249.8, HW_Z - z_prev), px(HW_X - 249.8, HW_Z - g0)], fill=C["wood"], width=3)
    z_prev = g1
for z in (201.4,):                                                                 # B' sigue hacia el norte hasta F
    box(HW_X - 216.8, HW_Z - z - 0.3, HW_X - 207.2, HW_Z - z + 0.3, (200, 200, 195))
for name, x, z in H_ROAD_NAMES:
    label(HW_X - x, HW_Z - z, f"calle {name}'", F_S)
# Acera oeste de la avenida (x 100–103), cortada donde entran A' y C1'.
z_prev = 90
cuts = sorted([(west(r)[1], west(r)[3]) for r in H_ROADS if r[0] < 118] + [(r[1], r[3]) for r in (NW_ROADS[0], NW_ROADS[2])])
for a, b in cuts:
    box(100, z_prev, 103, a, C["sidewalk"])
    z_prev = b
box(100, z_prev, 103, Z1, C["sidewalk"])
# Ampliación oeste (calles D y E sin salida, callejones que siguen los de la copia).
for r in WX_LAWNS:
    box(*r, C["lawn_d"])
for r in WX_ROADS:
    box(*r, C["asphalt"])
for r in WX_SIDEWALKS:
    box(*r, C["sidewalk"])
for r in WX_ALLEYS:
    box(*r, C["alley"])
for r in WX_ROADS:                                                                  # barrera del fondo
    box(r[2] - 0.9, r[1] + 0.2, r[2] - 0.3, r[3] - 0.2, (200, 200, 195))
for name, x, z in WX_ROAD_NAMES:
    label(x, z, f"calle {name}", F_S)
for r in NW_LAWNS:                                                                  # ampliación norte (oeste)
    box(*r, C["lawn_d"])
for r in NW_ROADS:
    box(*r, C["asphalt"])
for r in NW_SIDEWALKS:
    box(*r, C["sidewalk"])
for r in NW_ALLEYS:
    box(*r, C["alley"])
# Oeste lejano: calles que siguen D, E, F y H al otro lado de la calle x = −110, calle J, gasolinera y negocios.
for r in FW_LAWNS:
    box(*r, C["lawn_d"])
for r in FW_SIDEWALKS:
    box(*r, C["sidewalk"])
for r in FW_ROADS:
    box(*r, C["asphalt"])
for r in FW_ALLEYS:
    box(*r, C["alley"])
box(*FW_GAS_LOT, C["lot"])
for r in FW_GAS_DRIVES:
    box(*r, C["lot"])
box(FW_MART[0], FW_MART[1] - 2.4, FW_MART[2], FW_MART[1], C["sidewalk"])
box(*FW_MART, C["store"], outline=(30, 30, 30), width=2)
box(-249.5, 90, -160, 93, C["sidewalk"])
box(-249.5, 93, -160, 102, C["lawn_d"])
for _, x0, x1 in FW_SHOPS:
    box(x0, SH_FRONT_FW + 7.5, x1, 102, C["alley"])
    box(x0, SH_FRONT_FW, x1, SH_FRONT_FW + 7.5, C["store"], outline=(30, 30, 30), width=2)
hatch(*FW_SHOPS_VACANT, C["weeds"])
box(*FW_WASTELAND, (132, 112, 84))

# Las huellas del oeste (y de los negocios y la ampliación) se vuelven a dibujar encima del suelo de la copia.
if footprints.exists():
    for e in json.loads(footprints.read_text(encoding="utf-8"))["edificios"]:
        if max(x for x, _ in e["esquinas"]) > 101:
            continue
        pts = [px(x, z) for x, z in e["esquinas"]]
        if e["tipo"] == "solar":
            d.polygon(pts, fill=C["weeds"] + (110,), outline=C["weeds"])
        elif e["tipo"] != "aparcamiento":
            d.polygon(pts, fill=kind_color.get(e["tipo"], C["house"]), outline=(40, 20, 15))
if footprints.exists() and not any(min(x for x, _ in e["esquinas"]) < 100 and min(z for _, z in e["esquinas"]) > 102
                                   for e in json.loads(footprints.read_text(encoding="utf-8"))["edificios"]):
    label(62, 150, "casas oeste: construye la escena en Unity" + chr(10) + "y vuelve a ejecutar este script", F_M)
label(35, 216, "BARRIO DE CASAS OESTE (el mismo, girado 180°)", F_M)
label(-66, 216, "AMPLIACIÓN OESTE", F_M)
for name, x, z in NE_ROAD_NAMES + NW_ROAD_NAMES:
    label(x, z, f"calle {name}", F_S)
label(0, 276, "AMPLIACIÓN NORTE (oeste)", F_M)
# Detalles del oeste lejano encima de las huellas.
box(*FW_CANOPY, (225, 225, 220), outline=(160, 30, 25), width=2)
for z in (101.3, 107.7):
    box(-147, z - 0.65, -131, z + 0.65, (150, 150, 145))
    for x in (-142.5, -135.5):
        box(x - 0.55, z - 0.3, x + 0.55, z + 0.3, (170, 30, 25))
rect(-123.5, 93.5, 2.6, 2.6, (170, 30, 25))
label(-139, 104.5, "GASOLINERA", F_S)
label(-149.5, 116.8, "food mart", F_S, fill=(255, 255, 255), halo=False)
for n, x0, x1 in FW_SHOPS:
    label((x0 + x1) / 2, SH_FRONT_FW + 3.75, n, F_S, fill=(255, 255, 255), halo=False)
cx, cz = FW_COURT
box(cx - 8, cz - 12, cx + 8, cz + 12, C["court"], outline=(230, 230, 230), width=2)
label(cx, cz, "cancha", F_S, fill=(255, 255, 255), halo=False)
wx0, wz0, wx1, wz1 = FW_WASTELAND
label((wx0 + wx1) / 2, (wz0 + wz1) / 2, "DESCAMPADO" + chr(10) + "(barro, escombros," + chr(10) + "coche, bidón)", F_S)
for name, x, z in FW_ROAD_NAMES:
    label(x, z, f"calle {name}", F_S)
label(-200, 270, "OESTE LEJANO", F_M)
label(-205, 76, "negocios: 7 alitas · 8 empeños · 9 belleza · 10 neumáticos · 11 lavandería", F_S)
label(160, 276, "AMPLIACIÓN NORTE (este)", F_M)

# ---------- Franja de negocios (PrototypeSceneBuilder.Shops.cs) ----------
SH_LINK = (-26, 87, -21, 102.5)
SH_LOT = (10, 87, 26, 101.5)
SH_VACANT = (54, 93, 64, 102)
SH_FRONT, SH_DEPTH = 93, 7.5
SHOPS = [("1", -18, -6), ("2", -2, 8), ("3", 28, 37), ("4", 42, 54), ("5", 64, 74), ("6", 79, 99)]
box(-30, 90, 100, 93, C["sidewalk"])
box(-30, 93, 100, 102, C["lawn_d"])
for _, x0, x1 in SHOPS:
    box(x0, SH_FRONT + SH_DEPTH, x1, 102, C["alley"])
    box(x0, SH_FRONT, x1, SH_FRONT + SH_DEPTH, C["store"], outline=(30, 30, 30), width=2)
for n, x0, x1 in SHOPS:
    label((x0 + x1) / 2, SH_FRONT + SH_DEPTH / 2, n, F_S, fill=(255, 255, 255), halo=False)
box(*SH_LINK, C["alley"])
box(*SH_LOT, C["lot"])
for i in range(7):
    d.line([px(10.2 + i * 2.6, 96.5), px(10.2 + i * 2.6, 101.5)], fill=(235, 235, 230), width=1)
hatch(*SH_VACANT, C["weeds"])
for x in (-16, 35, 62, 92):
    dot(x, 87.6, 3, C["light"])
for z in (110, 140, 170, 215, 245):
    if z < Z1:
        dot(100.5, z, 3, C["light"])
dot(25.4, 100.7, 3, C["light"])
label(40, 76, "negocios: 1 iglesia · 2 lavandería · P parking · 3 barbería · 4 pollo · 5 cheques · 6 licores", F_S)
label(18, 94, "P", F_S)
label(232, 128.5, "pasaje", F_S)

# ---------- Tren elevado (PrototypeSceneBuilder.Train.cs) sobre la avenida x = 110 ----------
TRAIN_W, TRAIN_E = 101.8, 118.2
TRAIN_CROSSINGS = [(80, 7), (-80, 7), (0, 6), (122, 5), (188, 5), (128, 5), (194, 5), (253, 5), (293, 5)]
STATION_Z0, STATION_Z1 = 4, 68
box(TRAIN_W + 0.4, Z0, TRAIN_E - 0.4, Z1, C["train"] + (95,))                     # tablero (semitransparente)
for tx in (106.5, 113.5):                                                          # vías
    box(tx - 1.3, Z0, tx + 1.3, Z1, (90, 70, 55, 150))
    for dx in (-0.72, 0.72):
        d.line([px(tx + dx, Z0), px(tx + dx, Z1)], fill=(210, 210, 215), width=1)
z = -292.5
while z <= 292.5:                                                                  # pórticos
    if all(abs(z - cz) >= half + 1.5 for cz, half in TRAIN_CROSSINGS) and Z0 < z < Z1:
        d.line([px(TRAIN_W, z), px(TRAIN_E, z)], fill=C["train"], width=2)
        for x in (TRAIN_W, TRAIN_E):
            rect(x, z, 0.8, 0.8, (40, 45, 42))
    z += 15
for side, track in ((-1, 106.5), (1, 113.5)):                                     # andenes y escaleras
    inner, outer = track + side * 1.55, track + side * 5.4
    box(min(inner, outer), STATION_Z0, max(inner, outer), STATION_Z1, (190, 186, 175), outline=(90, 90, 90))
    sx = outer + side * 0.65
    box(sx - 0.6, STATION_Z0 + 6, sx + 0.6, STATION_Z0 + 6 + 9.45 * 1.25, (120, 125, 120))
label(110, 50, "ESTACIÓN", F_S)
label(110, 150, "tren\nelevado", F_S)
label(110, -25, "tren\nelevado", F_S)

# ---------- Spawns ----------
dot(-10, -BLOCK_HALF_Z - 1.5, 6, C["player"], outline=(255, 255, 255))
rect(-16, -BLOCK_HALF_Z - SIDEWALK - 3, 4.5, 2, (200, 40, 40), outline=(255, 255, 255))
label(-13, -BLOCK_HALF_Z - 12, "inicio: jugador + coche", F_S)

# ---------- Etiquetas de calles ----------
label(60, road_z, "calle E–O  z = 80", F_S)
label(60, -road_z, "calle E–O  z = −80", F_S)
label(road_x, -55, "avenida principal\nx = 110", F_S)
label(-road_x, 30, "calle\nx = −110", F_S)

# ---------- Cuadrícula y ejes ----------
for gx in range(-200, 261, 20):
    x_, _ = px(gx, 0)
    d.line([(x_, px(0, Z1)[1]), (x_, px(0, Z0)[1])], fill=C["grid"] + (40,), width=1)
    d.text((x_, px(0, Z0)[1] + 6), str(gx), font=F_S, fill=C["text"], anchor="mt")
for gz in range(-100, Z1 - 1, 20):
    _, y_ = px(0, gz)
    d.line([(px(X0, 0)[0], y_), (px(X1, 0)[0], y_)], fill=C["grid"] + (40,), width=1)
    d.text((px(X0, 0)[0] - 6, y_), str(gz), font=F_S, fill=C["text"], anchor="rm")
d.rectangle([px(X0, Z1), px(X1, Z0)], outline=C["text"], width=2)
d.text((MARGIN, 18), "GTAChiraq · plano del mundo (vista cenital)", font=F_T, fill=C["text"])
d.text((px(X1, 0)[0], px(0, Z0)[1] + 26), "x (m) →   ·   z (m) ↑ norte   ·   1 cuadro = 20 m", font=F_S, fill=C["text"], anchor="rt")

# Escala
sx, sy = px(-200, -106)
d.rectangle([sx, sy - 6, sx + 50 * S, sy], fill=(255, 255, 255))
d.text((sx + 25 * S, sy - 10), "50 m", font=F_S, fill=(255, 255, 255), anchor="mb")

# ---------- Leyenda ----------
lx = W - LEGEND_W + 20
ly = MARGIN + 60
d.text((lx, ly - 34), "Leyenda", font=F_L, fill=C["text"])


def legend(kind, color, text, outline=None):
    global ly
    if kind == "rect":
        d.rectangle([lx, ly, lx + 28, ly + 18], fill=color, outline=outline or (60, 60, 60))
    elif kind == "dot":
        d.ellipse([lx + 6, ly + 1, lx + 22, ly + 17], fill=color, outline=outline)
    elif kind == "line":
        d.line([lx, ly + 9, lx + 28, ly + 9], fill=color, width=3)
    elif kind == "hatch":
        d.rectangle([lx, ly, lx + 28, ly + 18], fill=color + (70,), outline=color)
        for k in range(0, 28, 7):
            run = min(18, 28 - k)
            d.line([(lx + k, ly + 18), (lx + k + run, ly + 18 - run)], fill=color, width=2)
    d.text((lx + 40, ly + 9), text, font=F_S, fill=C["text"], anchor="lm")
    ly += 28


legend("rect", C["asphalt"], "asfalto / calzada")
legend("rect", C["line"], "línea amarilla central")
legend("rect", C["sidewalk"], "acera (3 m)")
legend("rect", C["lawn_d"], "césped seco texturizado")
legend("rect", C["brick"], "bloque de ladrillo (3 plantas)")
legend("rect", C["tower"], "torre de ladrillo")
legend("rect", C["balcony"], "balcones cerrados con malla (torres)")
legend("rect", C["row"], "casas adosadas (2 pl., tejado a dos aguas)")
legend("rect", C["lot"], "aparcamiento (plazas pintadas)")
legend("line", (120, 85, 50), "atajo de barro por el césped")
legend("line", (230, 60, 140), "tendedero con ropa")
legend("rect", C["house"], "two-flat / casa")
legend("rect", C["path"], "camino y explanada de acceso")
legend("line", C["fence"], "valla de hierro (huecos en caminos)")
legend("rect", C["court"], "cancha de baloncesto con malla")
legend("rect", C["dumpster"], "contenedores (sofá, colchón)")
legend("dot", C["light"], "farola de sodio")
legend("dot", C["pole"], "poste de madera con cables")
legend("dot", C["bollard"], "pilonas rojas (fin de calle)")
legend("rect", C["bus"], "parada de bus")
legend("dot", C["tree"], "árbol de acera (posición aprox.)")
legend("rect", C["snow"], "restos de nieve")
legend("hatch", C["weeds"], "maleza alta / solar vacío")
legend("rect", C["alley"], "callejón trasero (5 m)")
legend("line", C["wood"], "valla de madera")
legend("rect", C["garage"], "garaje")
legend("rect", C["porch"], "porche trasero de madera")
legend("rect", C["train"], "tren elevado (tablero y pórticos)")
legend("rect", (190, 186, 175), "andén de la estación")
legend("dot", (60, 120, 255), "cámara de vigilancia")
legend("dot", C["player"], "inicio del jugador", outline=(255, 255, 255))
ly += 14
notes = [
    "Sale al azar (semilla fija) y no se dibuja:",
    " · 22 árboles del césped del barrio y 34 en la",
    "   manzana central, de 4 tipos:",
    "   pelado, hoja verde, nevado, hoja seca",
    " · 3 solares con maleza en el césped",
    " · ~40 placas de nieve en el césped",
    " · basura, manchas, charcos, grafitis",
    " · coches en los aparcamientos (55 % de plazas)",
    " · arbustos delante de cada fachada",
    "",
    "Regenerar: python mapa/generar_mapa.py",
    "Medidas: PrototypeSceneBuilder*.cs",
]
for n in notes:
    d.text((lx, ly), n, font=F_S, fill=C["text"])
    ly += 20

out = Path(__file__).with_name("mapa.png")
img.save(out)
print(f"Guardado {out} ({W}x{H})")
