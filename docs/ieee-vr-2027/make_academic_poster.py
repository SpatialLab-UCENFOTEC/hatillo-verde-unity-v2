#!/usr/bin/env python3
"""IEEE-style A0 scientific poster. Vector type, one results figure, no marketing chrome."""

from __future__ import annotations

import json
import os
from pathlib import Path

os.environ["MPLCONFIGDIR"] = "/Users/arianaportuguez/3DInterfaces/poster/.mpl"

import matplotlib

matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib import rcParams
from reportlab.graphics.barcode.qr import QrCodeWidget
from reportlab.graphics.shapes import Drawing
from reportlab.graphics import renderPDF
from reportlab.lib.colors import HexColor, white, black, Color
from reportlab.lib.enums import TA_JUSTIFY, TA_LEFT, TA_CENTER, TA_RIGHT
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen import canvas
from reportlab.platypus import Paragraph

ROOT = Path("/Users/arianaportuguez/3DInterfaces/poster")
STATS = json.loads((ROOT / "stats.json").read_text())
FIG = ROOT / "assets" / "fig1_means_ci.png"
OUT = ROOT / "HatilloVerde_IEEE-VR-2027_A0.pdf"

FONT = "/System/Library/Fonts/Supplemental"
pdfmetrics.registerFont(TTFont("TNR", f"{FONT}/Times New Roman.ttf"))
pdfmetrics.registerFont(TTFont("TNR-B", f"{FONT}/Times New Roman Bold.ttf"))
pdfmetrics.registerFont(TTFont("TNR-I", f"{FONT}/Times New Roman Italic.ttf"))
pdfmetrics.registerFont(TTFont("TNR-BI", f"{FONT}/Times New Roman Bold Italic.ttf"))
pdfmetrics.registerFont(TTFont("AR", f"{FONT}/Arial.ttf"))
pdfmetrics.registerFont(TTFont("AR-B", f"{FONT}/Arial Bold.ttf"))
pdfmetrics.registerFont(TTFont("AR-I", f"{FONT}/Arial Italic.ttf"))

W, H = 841 * mm, 1189 * mm
NAVY = HexColor("#0A2540")
LINE = HexColor("#0A2540")
RULE = HexColor("#C5CAD3")
INK = HexColor("#1A1A1A")
MUTED = HexColor("#3F4650")
BOX = HexColor("#F3F5F7")
WARN = HexColor("#F7F1E8")
ACCENT = HexColor("#0A2540")

POST_KEYS = [
    ("after_greater_connection", "Personal connection"),
    ("after_community_meaning", "Community meaning"),
    ("after_nature_connection", "Nature connection"),
    ("after_eco_understand", "Ecological importance"),
    ("recognize_problems", "Recognize env. problems"),
    ("awareness_restore", "Restoration awareness"),
    ("protect_urban_green", "Protect urban green"),
    ("imagine_future", "Imagined restored future"),
    ("future_changed_value", "Future changed perceived value"),
    ("hope", "Hope"),
    ("willing_participate", "Willingness to participate"),
    ("willing_support", "Willingness to support"),
    ("willing_share", "Willingness to share"),
]


def make_figure():
    ROOT.joinpath("assets").mkdir(exist_ok=True)
    items = STATS["items"]
    labels = [lab for _, lab in POST_KEYS][::-1]
    means = [items[k]["M"] for k, _ in POST_KEYS][::-1]
    lo = [items[k]["CI95_mean"][0] for k, _ in POST_KEYS][::-1]
    hi = [items[k]["CI95_mean"][1] for k, _ in POST_KEYS][::-1]
    xerr = [[m - a for m, a in zip(means, lo)], [b - m for m, b in zip(means, hi)]]

    rcParams.update(
        {
            "font.family": "Times New Roman",
            "font.size": 11,
            "axes.linewidth": 0.6,
            "xtick.major.width": 0.6,
            "ytick.major.width": 0.6,
        }
    )
    fig, ax = plt.subplots(figsize=(11.2, 8.4), dpi=220)
    fig.patch.set_facecolor("white")
    ax.set_facecolor("white")
    y = range(len(labels))
    ax.errorbar(
        means,
        list(y),
        xerr=xerr,
        fmt="o",
        color="#0A2540",
        ecolor="#0A2540",
        elinewidth=1.15,
        capsize=3.5,
        markersize=5.5,
        markerfacecolor="white",
        markeredgewidth=1.2,
        zorder=3,
    )
    ax.set_yticks(list(y), labels)
    ax.set_xlim(3.85, 5.08)
    ax.set_xlabel("Mean rating (1–5) · 95% bootstrap CI  ·  axis truncated at 3.85")
    ax.set_title("Figure 1. Post-experience item means (N = 29)", loc="left", fontsize=12, pad=8)
    ax.spines["top"].set_visible(False)
    ax.spines["right"].set_visible(False)
    ax.grid(axis="x", color="#E6E8EC", lw=0.6)
    ax.tick_params(length=3)
    fig.text(
        0.01,
        0.01,
        "Dashed line at 3 is the scale midpoint (off-axis to the left). Axis starts at 3.85 so CI width is readable; all scores remain ≤ 5. Ceiling is the result.",
        fontsize=8,
        color="#4A4A4A",
    )
    fig.tight_layout(rect=(0.01, 0.04, 0.99, 0.98))
    fig.savefig(FIG, dpi=220, bbox_inches="tight", facecolor="white")
    plt.close()


def P(text, style):
    return Paragraph(text.replace("\n", "<br/>"), style)


def draw_p(c, text, style, x, y, w, h):
    p = P(text, style)
    w_used, h_used = p.wrap(w, h)
    p.drawOn(c, x, y - h_used)
    return h_used


def qr(c, url, x, y, size):
    widget = QrCodeWidget(url)
    b = widget.getBounds()
    qw, qh = b[2] - b[0], b[3] - b[1]
    d = Drawing(size, size, transform=[size / qw, 0, 0, size / qh, 0, 0])
    d.add(widget)
    renderPDF.draw(d, c, x, y)


def hline(c, x, y, w, color=RULE, sw=0.6):
    c.setStrokeColor(color)
    c.setLineWidth(sw)
    c.line(x, y, x + w, y)


def section_head(c, title, x, y):
    c.setFillColor(NAVY)
    c.setFont("AR-B", 11)
    c.drawString(x, y, title.upper())
    hline(c, x, y - 3.2, 62 * mm, NAVY, 1.1)
    return y - 12


def build():
    make_figure()
    c = canvas.Canvas(str(OUT), pagesize=(W, H))
    c.setTitle("If You Could Walk 1890: A Browser 3D Experience for Community Restoration")
    c.setAuthor("SpatialLab, Universidad CENFOTEC")
    c.setSubject("IEEE VR 2027 scientific poster A0")

    m = 22 * mm
    c.setFillColor(white)
    c.rect(0, 0, W, H, fill=1, stroke=0)
    c.setFillColor(NAVY)
    c.rect(0, H - 14 * mm, W, 14 * mm, fill=1, stroke=0)
    c.setFillColor(white)
    c.setFont("AR", 8.5)
    c.drawString(m, H - 9 * mm, "IEEE CONFERENCE ON VIRTUAL REALITY AND 3D USER INTERFACES  ·  IEEE VR 2027  ·  MELBOURNE, AUSTRALIA")
    c.drawRightString(W - m, H - 9 * mm, "POSTER  ·  TRAINING AND EDUCATION")

    # Title block
    y = H - 28 * mm
    c.setFillColor(NAVY)
    c.setFont("TNR-B", 26)
    title1 = "If You Could Walk 1890:"
    c.drawString(m, y, title1)
    y -= 11 * mm
    c.setFont("TNR-B", 20)
    c.drawString(m, y, "A Browser-Based 3D Interface for Community Ecological Restoration")
    y -= 8 * mm
    c.setFillColor(MUTED)
    c.setFont("AR", 10)
    c.drawString(m, y, "SpatialLab, Universidad CENFOTEC, Costa Rica  ·  in collaboration with Organización Hatillo Verde")
    y -= 5.5 * mm
    c.setFont("AR-I", 9)
    c.drawString(
        m,
        y,
        "Corresponding work: a time-layered Unity WebGL 3D terrain (past / present / restored future) evaluated with a post-experience survey (N = 29).",
    )

    # Abstract
    y -= 8 * mm
    abs_h = 46 * mm
    c.setFillColor(BOX)
    c.rect(m, y - abs_h, W - 2 * m, abs_h, fill=1, stroke=0)
    c.setFillColor(NAVY)
    c.setFont("AR-B", 9)
    c.drawString(m + 6 * mm, y - 6 * mm, "ABSTRACT")
    body = ParagraphStyle(
        "abs",
        fontName="TNR",
        fontSize=9.5,
        leading=12.4,
        textColor=INK,
        alignment=TA_JUSTIFY,
    )
    abstract = (
        "Urban river fragments are difficult to protect when stakeholders cannot inspect what a site <i>was</i> or what it <i>could become</i>. "
        "We present a browser-based 3D user interface that registers a single spatial terrain in three tenses—a nineteenth-century coffee mill (c. 1890), "
        "the degraded present, and a site-specific restored future—implemented in Unity WebGL for desktop and mobile. "
        "After using the system, 29 people (8 residents, 16 visitors; 17 had never heard of the site) completed a 15-item Likert instrument on place meaning, "
        "imagined ecological futures, and conservation intent. Post-item means ranged from 4.41 to 4.90 on a 1–5 scale (bootstrap 95% CIs entirely above 4.1). "
        "We report these ratings as descriptive evidence of reception, not as a causal test of the 3D interface: the survey is retrospective, has no control condition, "
        "and exhibits a ceiling. The contribution is a deployed temporal-3D system for civic ecology and an honest account of what the available data can and cannot support."
    )
    draw_p(c, abstract, body, m + 6 * mm, y - 9 * mm, W - 2 * m - 12 * mm, abs_h - 12 * mm)

    # Three columns
    gap = 8 * mm
    col_w = (W - 2 * m - 2 * gap) / 3
    col_top = y - abs_h - 10 * mm
    footer = 28 * mm
    col_h = col_top - footer - 52 * mm  # leave band for limitations
    x1, x2, x3 = m, m + col_w + gap, m + 2 * (col_w + gap)

    just = ParagraphStyle("j", fontName="TNR", fontSize=9.2, leading=12.1, textColor=INK, alignment=TA_JUSTIFY)
    just_s = ParagraphStyle("js", fontName="TNR", fontSize=8.7, leading=11.4, textColor=INK, alignment=TA_JUSTIFY)
    cap = ParagraphStyle("cap", fontName="TNR-I", fontSize=8, leading=10.4, textColor=MUTED, alignment=TA_LEFT)
    bullet = ParagraphStyle("b", fontName="TNR", fontSize=9.2, leading=12.0, textColor=INK, leftIndent=8, bulletIndent=0)

    # --- COL 1 ---
    x, y = x1, col_top
    y = section_head(c, "1.  Problem", x, y)
    t = (
        "Application-oriented 3D user interfaces are an explicit IEEE VR contribution class: they must show how existing techniques were applied to a real problem "
        "and how success was assessed [1]. Civic ecology is such a problem. Photographs and 2D maps collapse time. Policy documents are unread. "
        "The former Finca El Hatillo (Hatillo 2 and 4, San José) is a dense urban river corridor with industrial coffee heritage (c. 1870–1900), "
        "documented biodiversity, and a pending land-transfer bill. Residents and outsiders do not share a spatial model of that history or of a restored state."
    )
    y -= draw_p(c, t, just, x, y, col_w, 80 * mm) + 4 * mm

    y = section_head(c, "2.  Related framing", x, y)
    t = (
        "Place attachment is a person–place bond that can be disrupted when a landscape is illegible [2]. Immersive and 3D media have been used to support "
        "environmental learning and distant-place empathy [3,4], typically in head-worn VR. We instead treat <i>temporal registration on one 3D terrain</i> "
        "as the interface: the user does not teleport to a new world; they change tense in place. That is a desktop/mobile 3DUI, not a headset study [5]."
    )
    y -= draw_p(c, t, just, x, y, col_w, 70 * mm) + 4 * mm

    y = section_head(c, "3.  Interface contribution", x, y)
    t = (
        "The 3DUI is a <b>shared spatial coordinate frame with three temporal layers</b> and point-of-interest (POI) inspection:"
    )
    y -= draw_p(c, t, just, x, y, col_w, 30 * mm) + 2 * mm
    for b in [
        "Layer <i>t</i> = 1890: beneficio cafetalero, hydraulic weir, drying beds.",
        "Layer <i>t</i> = 2026: present corridor, pressure, remaining habitat.",
        "Layer <i>t</i> = restored: speculative but site-grounded neoecosystem.",
        "POIs: tap/click → photograph + audio (species, ruins, hydrology).",
        "Delivery: Unity WebGL; laptop or phone in landscape; URL only.",
    ]:
        h = draw_p(c, "•  " + b, bullet, x, y, col_w, 20 * mm)
        y -= h + 1.2 * mm

    y -= 3 * mm
    # schematic
    box_h = 38 * mm
    c.setStrokeColor(NAVY)
    c.setLineWidth(0.8)
    c.setFillColor(BOX)
    layers = [("t = restored future", HexColor("#D9E2EA")), ("t = 2026 present", HexColor("#C5D0DC")), ("t = 1890 mill", HexColor("#AEB9C6"))]
    for i, (lab, fill) in enumerate(layers):
        yy = y - box_h + i * 11.5 * mm
        c.setFillColor(fill)
        c.rect(x + i * 4 * mm, yy, col_w - 12 * mm, 14 * mm, fill=1, stroke=1)
        c.setFillColor(NAVY)
        c.setFont("AR", 8)
        c.drawString(x + i * 4 * mm + 4 * mm, yy + 5 * mm, lab)
    c.setFont("TNR-I", 8)
    c.setFillColor(MUTED)
    c.drawString(x, y - box_h - 5 * mm, "Fig. schematic. One terrain; three tenses; identical coordinates.")
    y = y - box_h - 10 * mm

    y = section_head(c, "4.  System", x, y)
    t = (
        "Unity WebGL build, publicly deployed. Interaction is inspect-and-switch rather than free locomotion with controllers. "
        "That choice is a constraint: it maximises access (no HMD) and limits claims about immersive presence or cybersickness. "
        "Live system: neoecosistemashatillo.ucenfotec.ac.cr"
    )
    draw_p(c, t, just, x, y, col_w, 40 * mm)

    # --- COL 2 Method ---
    x, y = x2, col_top
    y = section_head(c, "5.  Method", x, y)
    t = (
        "<b>Design.</b> Uncontrolled post-experience survey. Items were written in Spanish for place meaning, imagined restoration, and behavioural intent. "
        "Two items asked participants to recall prior disconnection (“poca conexión”); those items are <i>retrospective</i>, not a pre-test."
    )
    y -= draw_p(c, t, just, x, y, col_w, 45 * mm) + 3 * mm
    t = (
        "<b>Sample.</b> 31 responses (17 Jul–14 Aug 2026). Analysis <b>N = 29</b> who reported using the experience; 2 non-users excluded. "
        "Relation to site: 8 residents, 16 visitors, 2 organizers, 1 works on site, 2 other. "
        "12/29 had heard of Hatillo Verde; <b>17/29 (59%) had not</b>. Age and gender were <b>not collected</b>."
    )
    y -= draw_p(c, t, just, x, y, col_w, 50 * mm) + 3 * mm
    t = (
        "<b>Instrument.</b> 15 Likert items (1 = disagree, 5 = fully agree) plus two open questions. "
        "The 13 post-experience items show Cronbach’s α = .87 (internal consistency, not a validated scale)."
    )
    y -= draw_p(c, t, just, x, y, col_w, 35 * mm) + 3 * mm
    t = (
        "<b>Analysis.</b> We treat items as ordinal and report M, SD, median, IQR, and percentile-bootstrap 95% CIs for the mean (10,000 resamples). "
        "Group contrasts: Mann–Whitney <i>U</i> with tie-corrected normal approximation; effect size <i>r</i> = |Z|/√N. "
        "We do <b>not</b> interpret Wilcoxon tests against the midpoint 3 as evidence of interface efficacy: under a ceiling those tests are almost certain to reject."
    )
    y -= draw_p(c, t, just, x, y, col_w, 50 * mm) + 4 * mm

    y = section_head(c, "6.  Results", x, y)
    t = (
        "All 13 post-item means lie between 4.41 and 4.90. Every bootstrap 95% CI lies entirely above 4.10 (Fig. 1). "
        "Medians are 5. Restoration awareness: <i>M</i> = 4.90, <i>SD</i> = 0.31, 95% CI [4.76, 5.00]; 90% scored 5. "
        "Imagined restored future: <i>M</i> = 4.79, <i>SD</i> = 0.41, CI [4.62, 4.93]; 100% scored ≥ 4. "
        "Hope: <i>M</i> = 4.83, <i>SD</i> = 0.38, CI [4.69, 4.97]. The lowest post mean is greater personal connection (<i>M</i> = 4.41, <i>SD</i> = 0.68, CI [4.17, 4.66])."
    )
    y -= draw_p(c, t, just, x, y, col_w, 70 * mm) + 3 * mm
    t = (
        "<b>The only non-ceiling contrast.</b> Retrospective prior disconnection differed by group. "
        "Residents <i>Mdn</i> = 2.5 vs visitors <i>Mdn</i> = 5.0 (Mann–Whitney <i>p</i> = .011, <i>r</i> = .52, <i>n</i> = 8 vs 16). "
        "Had heard of the site <i>Mdn</i> = 3.0 vs never heard <i>Mdn</i> = 5.0 (<i>p</i> = .009, <i>r</i> = .48). "
        "Post-item ratings did <b>not</b> differ between residents and visitors (all <i>p</i> ≥ .20). "
        "People who already knew the land reported less prior disconnection; after use, scores saturate for both groups. That is a baseline difference, not an estimated treatment effect."
    )
    y -= draw_p(c, t, just, x, y, col_w, 80 * mm) + 4 * mm

    y = section_head(c, "7.  What we do not claim", x, y)
    t = (
        "We do not claim that 3D caused attitude change; that the interface is more effective than video or maps; that ratings generalise beyond this convenience sample; "
        "or that the system is a VR HMD contribution. The data show high post-use agreement in this cohort, with a ceiling that limits further inference."
    )
    draw_p(c, t, just, x, y, col_w, 45 * mm)

    # --- COL 3 figure + table ---
    x, y = x3, col_top
    y = section_head(c, "8.  Figure and estimates", x, y)
    img_h = 92 * mm
    c.drawImage(str(FIG), x, y - img_h, width=col_w, height=img_h, preserveAspectRatio=True, mask="auto")
    y = y - img_h - 3 * mm
    y -= draw_p(
        c,
        "Error bars = 95% CI of the mean. Scale 1–5. <i>N</i> = 29 participants who used the experience.",
        cap,
        x,
        y,
        col_w,
        12 * mm,
    )
    y -= 3 * mm

    # compact table
    c.setFont("AR-B", 8)
    c.setFillColor(NAVY)
    rows = [
        ["Item", "M", "SD", "95% CI"],
        ["Restoration awareness", "4.90", "0.31", "4.76–5.00"],
        ["Protect urban green", "4.86", "0.35", "4.72–4.97"],
        ["Hope", "4.83", "0.38", "4.69–4.97"],
        ["Ecological importance", "4.83", "0.47", "4.66–4.97"],
        ["Imagined restored future", "4.79", "0.41", "4.62–4.93"],
        ["Recognize env. problems", "4.76", "0.58", "4.55–4.93"],
        ["Willingness to share", "4.66", "0.61", "4.41–4.86"],
        ["Nature connection", "4.66", "0.55", "4.45–4.83"],
        ["Future changed value", "4.62", "0.56", "4.41–4.79"],
        ["Community meaning", "4.55", "0.51", "4.38–4.72"],
        ["Willingness to support", "4.55", "0.63", "4.31–4.76"],
        ["Personal connection", "4.41", "0.68", "4.17–4.66"],
        ["Willingness to participate", "4.41", "0.78", "4.10–4.69"],
    ]
    row_h = 5.6 * mm
    table_h = row_h * len(rows)
    ty = y - 4
    # header bg
    for i, row in enumerate(rows):
        yy = ty - (i + 1) * row_h
        if i == 0:
            c.setFillColor(NAVY)
            c.rect(x, yy, col_w, row_h, fill=1, stroke=0)
            c.setFillColor(white)
            c.setFont("AR-B", 7.2)
        else:
            c.setFillColor(BOX if i % 2 == 0 else white)
            c.rect(x, yy, col_w, row_h, fill=1, stroke=0)
            c.setFillColor(INK)
            c.setFont("TNR", 7.4)
        c.drawString(x + 2 * mm, yy + 1.6 * mm, row[0])
        c.drawRightString(x + col_w * 0.58, yy + 1.6 * mm, row[1])
        c.drawRightString(x + col_w * 0.72, yy + 1.6 * mm, row[2])
        c.drawRightString(x + col_w - 2 * mm, yy + 1.6 * mm, row[3])
    y = ty - table_h - 6 * mm
    y -= draw_p(c, "Table 1. Post-experience items, descending mean. Analysis N = 29.", cap, x, y, col_w, 10 * mm)
    y -= 4 * mm

    y = section_head(c, "9.  Implications", x, y)
    t = (
        "For IEEE VR: this is a <b>system + application</b> poster, not a technique paper. The 3DUI claim is temporal layering on one terrain in a browser, "
        "with measured reception and stated limits. For partners and funders: the artefact is already public, used by residents and first-time visitors, "
        "and the next empirical step is a controlled comparison (3D vs video vs map) with pre-registered outcomes, demographics, and IRB. "
        "A headset port would be a different study, not a reskin of these data."
    )
    draw_p(c, t, just, x, y, col_w, 55 * mm)

    # Limitations band
    lim_y = footer + 8 * mm
    lim_h = 42 * mm
    c.setFillColor(WARN)
    c.rect(m, lim_y, W - 2 * m - 42 * mm, lim_h, fill=1, stroke=0)
    c.setFillColor(NAVY)
    c.setFont("AR-B", 9)
    c.drawString(m + 6 * mm, lim_y + lim_h - 6.5 * mm, "LIMITATIONS  (READ THIS FIRST IF YOU ARE REVIEWING)")
    lim = (
        "(i) Convenience sample, N = 29 after exclusion; residents n = 8 is underpowered for subgroup claims. "
        "(ii) No experimental control; no random assignment; no task-performance or usability instrument (SUS, UEQ, presence). "
        "(iii) Retrospective “before” wording cannot recover a true baseline. (iv) Ceiling / acquiescence: most post scores are 4–5; variance is compressed. "
        "(v) No age, gender, education, or device (phone vs desktop) — a gap relative to IEEE VR reporting norms. "
        "(vi) The live artefact is WebGL 3D, not an HMD condition. (vii) Open-ended comments were not systematically coded. "
        "These limits are why we present means and CIs as description of reception, not as an efficacy result."
    )
    draw_p(
        c,
        lim,
        ParagraphStyle("lim", fontName="TNR", fontSize=8.4, leading=11.0, textColor=INK, alignment=TA_JUSTIFY),
        m + 6 * mm,
        lim_y + lim_h - 10 * mm,
        W - 2 * m - 54 * mm,
        lim_h - 12 * mm,
    )

    # QR
    q = 36 * mm
    qx = W - m - q
    c.setFillColor(white)
    c.rect(qx - 3 * mm, lim_y, q + 3 * mm, lim_h, fill=1, stroke=0)
    qr(c, "https://neoecosistemashatillo.ucenfotec.ac.cr/", qx - 1 * mm, lim_y + 8 * mm, q - 2 * mm)
    c.setFillColor(MUTED)
    c.setFont("AR", 6.5)
    c.drawCentredString(qx + q / 2 - 2 * mm, lim_y + 2.5 * mm, "Live 3D system")

    # Footer refs
    c.setFillColor(NAVY)
    c.rect(0, 0, W, footer, fill=1, stroke=0)
    c.setFillColor(white)
    c.setFont("AR-B", 7)
    c.drawString(m, footer - 6 * mm, "REFERENCES")
    refs = (
        "[1] IEEE VR 2027 Call for Papers, application/system contribution classes.  "
        "[2] Scannell & Gifford, J. Environ. Psychol. 2010 (place attachment).  "
        "[3] Markowitz et al., Front. Psychol. 2018 (immersive field trips / environmental learning).  "
        "[4] Ahn, Bailenson & Park, Media Psychol. 2014 (embodied environmental experience).  "
        "[5] LaViola, Kruijff, McMahan, Bowman & Poupyrev, 3D User Interfaces, 2nd ed., 2017.  "
        "Contact: SpatialLab, Universidad CENFOTEC  ·  Print: A0 (841 × 1189 mm), portrait, colour, uncoated."
    )
    draw_p(
        c,
        refs,
        ParagraphStyle("rf", fontName="AR", fontSize=7, leading=9.2, textColor=white, alignment=TA_LEFT),
        m,
        footer - 8.5 * mm,
        W - 2 * m,
        20 * mm,
    )

    c.showPage()
    c.save()
    print("wrote", OUT, OUT.stat().st_size)


if __name__ == "__main__":
    build()
