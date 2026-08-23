#!/usr/bin/env python3
"""A0 print poster for IEEE VR 2027 — Hatillo Verde temporal 3D experience."""

from reportlab.lib.colors import Color, HexColor, white, black
from reportlab.lib.units import mm
from reportlab.pdfgen import canvas
from reportlab.pdfbase import pdfmetrics
from reportlab.lib.utils import ImageReader
from reportlab.graphics.barcode.qr import QrCodeWidget
from reportlab.graphics.shapes import Drawing
from reportlab.graphics import renderPDF
from pathlib import Path

OUT = Path("/Users/arianaportuguez/3DInterfaces/poster/HatilloVerde_IEEE-VR-2027_A0.pdf")
ASSETS = Path("/Users/arianaportuguez/3DInterfaces/poster/assets")
START = ASSETS / "start-screen.png"

# A0
W, H = 841 * mm, 1189 * mm

FOREST = HexColor("#1F3D24")
FOREST_MID = HexColor("#2D5A33")
LEAF = HexColor("#3D7A45")
CREAM = HexColor("#F3F0E8")
PAPER = HexColor("#FAF8F3")
INK = HexColor("#161A16")
MUTED = HexColor("#4A5248")
GOLD = HexColor("#8A6A2A")
SLATE = HexColor("#5A6158")
FUTURE = HexColor("#1E5C32")
RULE = HexColor("#C9C3B4")


def wrap(c, text, font, size, max_w):
    words = text.split()
    lines, cur = [], ""
    for w in words:
        trial = (cur + " " + w).strip()
        if c.stringWidth(trial, font, size) <= max_w:
            cur = trial
        else:
            if cur:
                lines.append(cur)
            cur = w
    if cur:
        lines.append(cur)
    return lines


def text_block(c, text, x, y, font, size, max_w, leading, fill=INK, align="left"):
    lines = wrap(c, text, font, size, max_w)
    c.setFillColor(fill)
    c.setFont(font, size)
    yy = y
    for line in lines:
        if align == "center":
            c.drawCentredString(x, yy, line)
        else:
            c.drawString(x, yy, line)
        yy -= leading
    return yy


def rounded(c, x, y, w, h, r, fill=None, stroke=None, sw=0.6):
    c.saveState()
    if fill:
        c.setFillColor(fill)
    if stroke:
        c.setStrokeColor(stroke)
        c.setLineWidth(sw)
    p = c.beginPath()
    p.roundRect(x, y, w, h, r)
    if fill and stroke:
        c.drawPath(p, fill=1, stroke=1)
    elif fill:
        c.drawPath(p, fill=1, stroke=0)
    else:
        c.drawPath(p, fill=0, stroke=1)
    c.restoreState()


def qr_draw(c, url, x, y, size):
    widget = QrCodeWidget(url)
    b = widget.getBounds()
    qw, qh = b[2] - b[0], b[3] - b[1]
    d = Drawing(size, size, transform=[size / qw, 0, 0, size / qh, 0, 0])
    d.add(widget)
    renderPDF.draw(d, c, x, y)


def build():
    c = canvas.Canvas(str(OUT), pagesize=(W, H))
    c.setTitle("If You Could Walk 1890: A Browser 3D Experience for Community Restoration")
    c.setAuthor("SpatialLab, Universidad CENFOTEC")
    c.setSubject("IEEE VR 2027 poster — A0")

    m = 28 * mm
    c.setFillColor(PAPER)
    c.rect(0, 0, W, H, fill=1, stroke=0)

    # Header
    header_h = 195 * mm
    c.setFillColor(FOREST)
    c.rect(0, H - header_h, W, header_h, fill=1, stroke=0)
    c.setFillColor(LEAF)
    c.rect(0, H - header_h, 10 * mm, header_h, fill=1, stroke=0)

    c.setFillColor(HexColor("#C5D9C4"))
    c.setFont("Helvetica", 13)
    c.drawString(m, H - 22 * mm, "IEEE VR 2027  ·  POSTER  ·  MELBOURNE, AUSTRALIA  ·  27 FEB – 3 MAR 2027")
    c.drawRightString(W - m, H - 22 * mm, "TRAINING AND EDUCATION")

    title = "If You Could Walk 1890:"
    sub = "A Browser 3D Experience for Community Restoration"
    c.setFillColor(white)
    c.setFont("Helvetica-Bold", 48)
    c.drawString(m, H - 54 * mm, title)
    c.setFont("Helvetica-Bold", 28)
    y = H - 76 * mm
    for line in wrap(c, sub, "Helvetica-Bold", 28, W - 2 * m - 10 * mm):
        c.drawString(m, y, line)
        y -= 34

    c.setFillColor(HexColor("#D5E6D4"))
    c.setFont("Helvetica", 14)
    c.drawString(m, H - 118 * mm, "SpatialLab  ·  Universidad CENFOTEC, Costa Rica  ·  Organización Hatillo Verde")
    c.setFont("Helvetica-Oblique", 12)
    c.drawString(
        m,
        H - 132 * mm,
        "A time-layered Unity WebGL 3D interface on one urban river terrain — past mill, degraded present, restored future.",
    )

    # Three tenses
    y0 = H - header_h - 8 * mm
    card_h = 118 * mm
    gap = 8 * mm
    card_w = (W - 2 * m - 2 * gap) / 3
    tenses = [
        ("1890", "PAST", GOLD, "Coffee mill on the Tiribí", "Walk the Finca El Hatillo at its export peak: hydraulic weir, washing beds, and the industrial river that shipped coffee to Europe."),
        ("2026", "PRESENT", SLATE, "A corridor under pressure", "The same ground, now an overlooked urban fragment in one of San José’s densest districts — still a living biological corridor."),
        ("RESTORED", "FUTURE", FUTURE, "The neoecosystem", "A speculative but site-specific future: endemic species return, the river is legible again, the community can see what a law would protect."),
    ]
    x = m
    for year, label, col, hook, body in tenses:
        rounded(c, x, y0 - card_h, card_w, card_h, 8, fill=white, stroke=RULE, sw=1)
        c.setFillColor(col)
        c.rect(x, y0 - 14 * mm, card_w, 14 * mm, fill=1, stroke=0)
        c.setFillColor(white)
        c.setFont("Helvetica-Bold", 11)
        c.drawString(x + 8 * mm, y0 - 10 * mm, f"{year}   ·   {label}")
        c.setFillColor(INK)
        c.setFont("Helvetica-Bold", 16)
        c.drawString(x + 8 * mm, y0 - 32 * mm, hook)
        text_block(c, body, x + 8 * mm, y0 - 46 * mm, "Helvetica", 11, card_w - 16 * mm, 15, MUTED)
        x += card_w + gap

    # Main columns
    col_top = y0 - card_h - 12 * mm
    col_bot = 78 * mm
    col_h = col_top - col_bot
    left_w = 92 * mm
    right_w = 78 * mm
    mid_w = W - 2 * m - left_w - right_w - 2 * gap

    # LEFT: Why + 3DUI
    rounded(c, m, col_bot, left_w, col_h, 8, fill=white, stroke=RULE, sw=1)
    lx, ly = m + 8 * mm, col_top - 12 * mm
    c.setFillColor(FOREST)
    c.setFont("Helvetica-Bold", 13)
    c.drawString(lx, ly, "WHY THIS 3D INTERFACE")
    c.setStrokeColor(FOREST)
    c.setLineWidth(2)
    c.line(lx, ly - 4, lx + 52 * mm, ly - 4)
    body = (
        "Urban river fragments are hard to defend when people cannot see what the land was, or what it could become. "
        "Photographs flatten time. Maps flatten place. We built a browser 3D experience so a resident, a visitor, or a lawmaker can stand on one terrain and switch tense."
    )
    ly = text_block(c, body, lx, ly - 18, "Helvetica", 10.5, left_w - 16 * mm, 14.2, INK)

    ly -= 8
    c.setFillColor(FOREST_MID)
    c.setFont("Helvetica-Bold", 12)
    c.drawString(lx, ly, "What is 3D about it")
    items = [
        "One shared spatial terrain, not a slideshow.",
        "Three stacked temporal layers on the same coordinates.",
        "Click / tap hotspots: photo + audio of species and ruins.",
        "Desktop and phone (landscape). No headset required.",
        "Unity WebGL — open the URL, walk in.",
    ]
    ly -= 16
    for it in items:
        c.setFillColor(LEAF)
        c.circle(lx + 2, ly + 3, 2.2, fill=1, stroke=0)
        ly = text_block(c, it, lx + 8, ly, "Helvetica", 10, left_w - 24 * mm, 13.5, INK)
        ly -= 6

    ly -= 6
    c.setFillColor(FOREST)
    c.setFont("Helvetica-Bold", 12)
    c.drawString(lx, ly, "The site")
    site = (
        "Hatillo 2 & 4, San José. Former beneficio cafetalero (c. 1870–1900). "
        "Today: >75 bird species, threatened fauna, and a proposed transfer of INVU land for a biological–heritage corridor (Bill 25.466)."
    )
    text_block(c, site, lx, ly - 16, "Helvetica", 10, left_w - 16 * mm, 13.5, MUTED)

    # MID: live UI + how
    mx = m + left_w + gap
    rounded(c, mx, col_bot, mid_w, col_h, 8, fill=white, stroke=RULE, sw=1)
    c.setFillColor(FOREST)
    c.setFont("Helvetica-Bold", 13)
    c.drawString(mx + 8 * mm, col_top - 12 * mm, "THE LIVE EXPERIENCE")
    c.setStrokeColor(FOREST)
    c.setLineWidth(2)
    c.line(mx + 8 * mm, col_top - 12 * mm - 4, mx + 58 * mm, col_top - 12 * mm - 4)

    img_top = col_top - 20 * mm
    img_h = 78 * mm
    img_w = mid_w - 16 * mm
    img_x = mx + 8 * mm
    img_y = img_top - img_h
    rounded(c, img_x - 2, img_y - 2, img_w + 4, img_h + 4, 4, fill=CREAM, stroke=RULE)
    crop_path = ASSETS / "start-screen-crop.png"
    if START.exists():
        from PIL import Image
        im = Image.open(START).convert("RGB")
        iw0, ih0 = im.size
        # Keep the centered CTA; drop empty chrome around it.
        box = (
            int(iw0 * 0.22),
            int(ih0 * 0.32),
            int(iw0 * 0.78),
            int(ih0 * 0.68),
        )
        im.crop(box).save(crop_path, "PNG")
        c.saveState()
        p = c.beginPath()
        p.roundRect(img_x, img_y, img_w, img_h, 3)
        c.clipPath(p, stroke=0, fill=0)
        ir = ImageReader(str(crop_path))
        iw, ih = ir.getSize()
        scale = max(img_w / iw, img_h / ih)
        dw, dh = iw * scale, ih * scale
        c.drawImage(
            ir,
            img_x + (img_w - dw) / 2,
            img_y + (img_h - dh) / 2,
            dw,
            dh,
            preserveAspectRatio=True,
            mask="auto",
        )
        c.restoreState()
    c.setFillColor(MUTED)
    c.setFont("Helvetica-Oblique", 8.5)
    c.drawString(img_x, img_y - 12, "Entry gate of the Unity WebGL build — one tap unlocks audio and the 3D terrain. neoecosistemashatillo.ucenfotec.ac.cr")

    hy = img_y - 28
    c.setFillColor(FOREST_MID)
    c.setFont("Helvetica-Bold", 12)
    c.drawString(img_x, hy, "How someone uses it")
    steps = [
        ("1", "Open the URL on a laptop or phone in landscape."),
        ("2", "Tap Comenzar experiencia — the same 3D ground loads."),
        ("3", "Switch tense: 1890 mill · 2026 corridor · restored future."),
        ("4", "Tap a hotspot. A species, ruin, or story opens with photo and sound."),
    ]
    hy -= 18
    for n, t in steps:
        c.setFillColor(FOREST)
        c.circle(img_x + 7, hy + 3, 7, fill=1, stroke=0)
        c.setFillColor(white)
        c.setFont("Helvetica-Bold", 9)
        c.drawCentredString(img_x + 7, hy, n)
        text_block(c, t, img_x + 18, hy, "Helvetica", 10.5, img_w - 22, 14, INK)
        hy -= 22

    # RIGHT: QR
    rx = mx + mid_w + gap
    rounded(c, rx, col_bot, right_w, col_h, 8, fill=FOREST, stroke=None)
    c.setFillColor(white)
    c.setFont("Helvetica-Bold", 13)
    c.drawString(rx + 8 * mm, col_top - 12 * mm, "WALK IT NOW")
    c.setFont("Helvetica", 10)
    qtxt = wrap(c, "Scan to open the 3D experience in a browser. No app store. No headset.", "Helvetica", 10, right_w - 16 * mm)
    qy = col_top - 28 * mm
    for line in qtxt:
        c.drawString(rx + 8 * mm, qy, line)
        qy -= 13

    qsize = 58 * mm
    qx = rx + (right_w - qsize) / 2
    qy = col_top - 48 * mm - qsize
    c.setFillColor(white)
    c.roundRect(qx - 6, qy - 6, qsize + 12, qsize + 12, 6, fill=1, stroke=0)
    qr_draw(c, "https://neoecosistemashatillo.ucenfotec.ac.cr/", qx, qy, qsize)

    c.setFillColor(HexColor("#D5E6D4"))
    c.setFont("Helvetica", 8)
    c.drawCentredString(rx + right_w / 2, qy - 14, "neoecosistemashatillo.ucenfotec.ac.cr")

    c.setFillColor(white)
    c.setFont("Helvetica-Bold", 11)
    c.drawString(rx + 8 * mm, col_bot + 48 * mm, "On this poster")
    blurb = "N=29 who used it (2 of 31 said they did not and were dropped). 5-point Likert after use. No age/gender. No control group."
    text_block(c, blurb, rx + 8 * mm, col_bot + 36 * mm, "Helvetica", 9, right_w - 16 * mm, 12.5, HexColor("#D5E6D4"))

    # RESULTS band
    res_h = 58 * mm
    res_y = col_bot - 8 * mm - res_h
    rounded(c, m, res_y, W - 2 * m, res_h, 8, fill=white, stroke=RULE, sw=1)
    c.setFillColor(FOREST)
    c.setFont("Helvetica-Bold", 13)
    c.drawString(
        m + 8 * mm,
        res_y + res_h - 11 * mm,
        "RATINGS AFTER USE   ·   N=29  ·  8 residents, 16 visitors  ·  17/29 (59%) had never heard of the site  ·  M (SD), 95% CI mean",
    )

    stats = [
        ("4.90", "0.31", "[4.76, 5.00]", "Restoration awareness  ·  90% scored 5"),
        ("4.79", "0.41", "[4.62, 4.93]", "Imagined restored future  ·  100% scored ≥4"),
        ("4.83", "0.38", "[4.69, 4.97]", "Hope for the corridor  ·  Mdn=5"),
        ("4.41", "0.68", "[4.17, 4.66]", "Greater personal connection  ·  lowest post item"),
    ]
    sw = (W - 2 * m - 16 * mm) / 4
    sx = m + 8 * mm
    for num, sd, ci, cap in stats:
        c.setFillColor(FOREST)
        c.setFont("Helvetica-Bold", 28)
        c.drawString(sx, res_y + 22 * mm, num)
        c.setFillColor(MUTED)
        c.setFont("Helvetica", 9)
        c.drawString(sx, res_y + 14 * mm, f"SD {sd}   {ci}")
        c.setFont("Helvetica", 9.5)
        for i, line in enumerate(wrap(c, cap, "Helvetica", 9.5, sw - 8 * mm)):
            c.drawString(sx, res_y + 8 * mm - i * 11, line)
        sx += sw

    # Quotes + takeaway
    q_h = 52 * mm
    qy2 = 18 * mm
    qw1 = (W - 2 * m - gap) * 0.62
    qw2 = (W - 2 * m - gap) * 0.38
    rounded(c, m, qy2, qw1, q_h, 8, fill=CREAM, stroke=None)
    c.setFillColor(GOLD)
    c.setFont("Helvetica-Bold", 20)
    c.drawString(m + 8 * mm, qy2 + q_h - 14 * mm, "“")
    quote = "Before, it was simply an ugly abandoned place. The experience let me see — and feel — how it could be if we gave it care."
    text_block(c, quote, m + 8 * mm, qy2 + q_h - 24 * mm, "Helvetica-Oblique", 12, qw1 - 16 * mm, 16, INK)
    c.setFillColor(MUTED)
    c.setFont("Helvetica", 9)
    c.drawString(m + 8 * mm, qy2 + 8 * mm, "Resident / visitor comments from the post-experience survey, July–August 2026.")

    rounded(c, m + qw1 + gap, qy2, qw2, q_h, 8, fill=FOREST, stroke=None)
    c.setFillColor(white)
    c.setFont("Helvetica-Bold", 12)
    c.drawString(m + qw1 + gap + 8 * mm, qy2 + q_h - 14 * mm, "TAKEAWAY")
    take = "A browser 3D interface that lets people walk 1890, 2026, and a restored future on the same ground can make an invisible urban river politically and emotionally visible — without a headset."
    text_block(
        c,
        take,
        m + qw1 + gap + 8 * mm,
        qy2 + q_h - 28 * mm,
        "Helvetica",
        10,
        qw2 - 16 * mm,
        13.5,
        HexColor("#E8F0E6"),
    )

    c.setFillColor(MUTED)
    c.setFont("Helvetica", 8)
    c.drawString(
        m,
        8 * mm,
        "Methods: 5-point Likert after use; 2 non-users excluded. 'Before' items are retrospective, not a pre-test. No control (video/photos). No age/gender collected. Ceiling on post items (α=.87, 13 items). Residents felt less prior disconnection than visitors (Mdn 2.5 vs 5; Mann–Whitney p=.011) — post ratings did not differ. Not a causal claim.  ·  A0.",
    )

    c.showPage()
    c.save()
    print("wrote", OUT, OUT.stat().st_size)


if __name__ == "__main__":
    build()
