#!/usr/bin/env python3
"""Descriptive + nonparametric analysis of Hatillo Verde Interactivo survey.
Truthful: exclude non-users; no fake pre-post; Likert treated as ordinal.
"""
from __future__ import annotations

import csv
import json
import math
import random
from collections import Counter
from pathlib import Path

CSV = Path("/Users/arianaportuguez/Downloads/Hatillo Verde Interactivo.csv")
OUT = Path(__file__).resolve().parent / "stats.json"

ITEMS = {
    "before_little_connection": "Antes: poca conexión personal con el lugar",
    "before_little_urban_nature": "Antes: pensaba poco en naturaleza urbana",
    "after_greater_connection": "Después: mayor conexión personal",
    "after_community_meaning": "Después: mayor significado para la comunidad",
    "after_nature_connection": "Después: más conexión con la naturaleza",
    "after_eco_understand": "Después: comprendo mejor la importancia ecológica",
    "recognize_problems": "Ayudó a reconocer problemas ambientales",
    "awareness_restore": "Aumentó conciencia de restaurar/conservar",
    "protect_urban_green": "Más importante proteger verdes urbanos",
    "imagine_future": "Ayudó a imaginar el futuro restaurado",
    "future_changed_value": "El escenario futuro cambió el valor percibido",
    "hope": "La restauración me genera esperanza",
    "willing_participate": "Dispuesto a participar en actividades",
    "willing_support": "Dispuesto a apoyar conservación local",
    "willing_share": "Dispuesto a compartir información",
}

COL = {
    "before_little_connection": 0,
    "before_little_urban_nature": 1,
    "after_greater_connection": 2,
    "after_community_meaning": 3,
    "after_nature_connection": 4,
    "after_eco_understand": 5,
    "recognize_problems": 6,
    "awareness_restore": 7,
    "protect_urban_green": 8,
    "imagine_future": 9,
    "future_changed_value": 10,
    "hope": 11,
    "willing_participate": 12,
    "willing_support": 13,
    "willing_share": 14,
}

LIKERT_HEADER_SLICE = slice(4, 19)


def mean(xs):
    return sum(xs) / len(xs)


def sd_sample(xs):
    n = len(xs)
    m = mean(xs)
    return math.sqrt(sum((x - m) ** 2 for x in xs) / (n - 1))


def median(xs):
    s = sorted(xs)
    n = len(s)
    if n % 2:
        return float(s[n // 2])
    return (s[n // 2 - 1] + s[n // 2]) / 2.0


def iqr(xs):
    s = sorted(xs)
    n = len(s)

    def pct(p):
        if n == 1:
            return float(s[0])
        k = (n - 1) * p
        f = math.floor(k)
        c = math.ceil(k)
        if f == c:
            return float(s[int(k)])
        return s[f] * (c - k) + s[c] * (k - f)

    return pct(0.25), pct(0.75)


def bootstrap_ci(xs, func=mean, n_boot=10000, alpha=0.05, seed=42):
    rng = random.Random(seed)
    n = len(xs)
    dist = []
    for _ in range(n_boot):
        sample = [xs[rng.randrange(n)] for _ in range(n)]
        dist.append(func(sample))
    dist.sort()
    lo = dist[int(alpha / 2 * n_boot)]
    hi = dist[int((1 - alpha / 2) * n_boot) - 1]
    return lo, hi


def norm_sf(z):
    """Survival function 1-Phi(|z|) two-tailed p via erfc."""
    return math.erfc(abs(z) / math.sqrt(2))


def wilcoxon_vs_mu(xs, mu=3.0):
    """Wilcoxon signed-rank vs mu. Z with tie correction. Two-sided p (normal approx)."""
    diffs = [x - mu for x in xs]
    nz = [d for d in diffs if d != 0]
    n = len(nz)
    if n < 6:
        return {"n_nonzero": n, "W": None, "z": None, "p": None, "r": None, "note": "too few nonzero diffs"}
    absd = [abs(d) for d in nz]
    # midranks for ties
    order = sorted(range(n), key=lambda i: absd[i])
    ranks = [0.0] * n
    i = 0
    while i < n:
        j = i
        while j + 1 < n and absd[order[j + 1]] == absd[order[i]]:
            j += 1
        mid = (i + 1 + j + 1) / 2.0
        for k in range(i, j + 1):
            ranks[order[k]] = mid
        i = j + 1
    W_pos = sum(ranks[i] for i in range(n) if nz[i] > 0)
    W_neg = sum(ranks[i] for i in range(n) if nz[i] < 0)
    W = min(W_pos, W_neg)
    T = n * (n + 1) / 4.0
    # tie correction on variance
    from collections import Counter
    tie_counts = Counter(absd)
    tie_term = sum(t ** 3 - t for t in tie_counts.values()) / 2.0
    var = n * (n + 1) * (2 * n + 1) / 24.0 - tie_term / 48.0
    z = (W_pos - T) / math.sqrt(var)  # positive z => values > mu
    p = min(1.0, max(0.0, norm_sf(z)))  # two-sided
    r = abs(z) / math.sqrt(len(xs))  # Rosenthal r using full N
    return {
        "n_nonzero": n,
        "n_zero": len(xs) - n,
        "W_pos": W_pos,
        "W_neg": W_neg,
        "W": W,
        "z": z,
        "p": p,
        "r": r,
    }


def mannwhitney(a, b):
    """Two-sided Mann-Whitney U, normal approx with tie correction."""
    n1, n2 = len(a), len(b)
    combined = [(x, 0) for x in a] + [(x, 1) for x in b]
    combined.sort(key=lambda t: t[0])
    n = n1 + n2
    ranks = [0.0] * n
    i = 0
    while i < n:
        j = i
        while j + 1 < n and combined[j + 1][0] == combined[i][0]:
            j += 1
        mid = (i + 1 + j + 1) / 2.0
        for k in range(i, j + 1):
            ranks[k] = mid
        i = j + 1
    R1 = sum(ranks[i] for i in range(n) if combined[i][1] == 0)
    U1 = R1 - n1 * (n1 + 1) / 2.0
    U2 = n1 * n2 - U1
    U = min(U1, U2)
    from collections import Counter
    ties = Counter(x for x, _ in combined)
    tie_term = sum(t ** 3 - t for t in ties.values())
    mu = n1 * n2 / 2.0
    var = n1 * n2 / 12.0 * ((n + 1) - tie_term / (n * (n - 1)))
    z = (U1 - mu) / math.sqrt(var) if var > 0 else 0.0
    p = min(1.0, max(0.0, norm_sf(z)))
    r = abs(z) / math.sqrt(n1 + n2)
    return {"n1": n1, "n2": n2, "U": U, "U1": U1, "z": z, "p": p, "r": r, "median1": median(a), "median2": median(b)}


def cronbach(matrix):
    """matrix: list of item vectors, each length N. Cronbach's alpha."""
    k = len(matrix)
    n = len(matrix[0])
    item_vars = [sd_sample(row) ** 2 for row in matrix]
    totals = [sum(matrix[j][i] for j in range(k)) for i in range(n)]
    total_var = sd_sample(totals) ** 2
    return (k / (k - 1)) * (1 - sum(item_vars) / total_var)


def rel_bucket(s):
    s = (s or "").strip().lower()
    if s.startswith("residente"):
        return "resident"
    if s.startswith("visitante"):
        return "visitor"
    if s.startswith("organizador"):
        return "organizer"
    if "trabajo" in s:
        return "works_there"
    return "other"


def fmt_p(p):
    if p is None:
        return None
    if p < 0.001:
        return "< .001"
    return f"{p:.3f}"


def summarize(xs, label):
    m = mean(xs)
    s = sd_sample(xs)
    lo, hi = bootstrap_ci(xs, mean, seed=42)
    q1, q3 = iqr(xs)
    w = wilcoxon_vs_mu(xs, 3.0)
    return {
        "n": len(xs),
        "M": round(m, 2),
        "SD": round(s, 2),
        "Mdn": median(xs),
        "IQR": [round(q1, 2), round(q3, 2)],
        "CI95_mean": [round(lo, 2), round(hi, 2)],
        "pct_ge4": round(100 * sum(1 for x in xs if x >= 4) / len(xs), 1),
        "pct_eq5": round(100 * sum(1 for x in xs if x == 5) / len(xs), 1),
        "pct_eq1": round(100 * sum(1 for x in xs if x == 1) / len(xs), 1),
        "counts": dict(Counter(xs)),
        "wilcoxon_vs_3": {
            "z": None if w["z"] is None else round(w["z"], 2),
            "p": fmt_p(w["p"]),
            "p_raw": w["p"],
            "r": None if w["r"] is None else round(w["r"], 2),
            "n_zero": w.get("n_zero"),
            "n_nonzero": w.get("n_nonzero"),
        },
    }


def main():
    with CSV.open(newline="", encoding="utf-8") as f:
        raw = list(csv.reader(f))
    header, data = raw[0], raw[1:]
    participated = [r for r in data if r[1].strip().lower() == "si"]
    excluded = [r for r in data if r[1].strip().lower() != "si"]

    def vec(rows, key):
        i = 4 + COL[key]
        return [int(r[i]) for r in rows]

    users = participated
    n = len(users)
    heard_yes = sum(1 for r in users if r[2].strip().lower() == "si")
    heard_no = n - heard_yes
    buckets = Counter(rel_bucket(r[3]) for r in users)

    item_stats = {}
    for key, label in ITEMS.items():
        item_stats[key] = {"label": label, **summarize(vec(users, key), key)}

    post_keys = [k for k in ITEMS if not k.startswith("before_")]
    post_matrix = [vec(users, k) for k in post_keys]
    alpha = cronbach(post_matrix)

    residents = [r for r in users if rel_bucket(r[3]) == "resident"]
    visitors = [r for r in users if rel_bucket(r[3]) == "visitor"]
    heard = [r for r in users if r[2].strip().lower() == "si"]
    unheard = [r for r in users if r[2].strip().lower() != "si"]

    subgroup_keys = [
        "after_greater_connection",
        "imagine_future",
        "awareness_restore",
        "hope",
        "willing_support",
        "before_little_connection",
    ]
    subgroups = {"resident_vs_visitor": {}, "heard_vs_unheard": {}}
    for k in subgroup_keys:
        subgroups["resident_vs_visitor"][k] = mannwhitney(vec(residents, k), vec(visitors, k))
        subgroups["heard_vs_unheard"][k] = mannwhitney(vec(heard, k), vec(unheard, k))
        for blk in subgroups.values():
            blk[k]["p"] = fmt_p(blk[k]["p"])
            blk[k]["z"] = round(blk[k]["z"], 2)
            blk[k]["r"] = round(blk[k]["r"], 2)
            blk[k]["U"] = round(blk[k]["U"], 1)

    # Open-ended: did view of place change? crude coding
    changed_col = header.index("¿Cambió su forma de ver este lugar después de la experiencia virtual? Explique brevemente")
    changed_raw = [r[changed_col].strip() for r in users]
    yes_n = sum(1 for t in changed_raw if t.lower().startswith(("si", "sí", "claro", "mejora", "convencida")))
    n_a = sum(1 for t in changed_raw if t.lower() in {"n/a", "na", ""})

    # Honesty notes
    notes = [
        "N analysis = 29 people who answered that they participated. 2 of 31 said they did not participate and were excluded.",
        "No age or gender was collected. IEEE VR papers require demographics; this poster sample cannot report them.",
        "Before items are retrospective (asked after the experience) and worded as 'poca conexión' / 'pensaba poco' — they are NOT a pre-test. Do not treat them as paired pre-post of the same construct.",
        "After items show a ceiling: many scores are 4–5. Means are high; variance is low; p-values vs midpoint 3 are expected and not evidence of a controlled effect.",
        "No control condition (video, photos, or no experience). Ratings cannot be attributed causally to the 3D interface.",
        "Likert treated as ordinal for tests (Wilcoxon / Mann–Whitney) and summarized with M/SD because that is conventional; median/IQR are the ordinal summaries.",
        "95% CIs for the mean are percentile bootstrap (10,000 resamples).",
        "Wilcoxon signed-rank vs 3 (scale midpoint), two-sided, normal approximation with tie correction. Effect size r = |Z|/sqrt(N).",
        "Cronbach's alpha on the 13 post items is an internal-consistency check, not a validated scale.",
    ]

    # Key numbers for poster (truthful)
    poster = {
        "N": n,
        "excluded_nonusers": len(excluded),
        "heard_before_n": heard_yes,
        "never_heard_n": heard_no,
        "never_heard_pct": round(100 * heard_no / n, 1),
        "residents": buckets.get("resident", 0),
        "visitors": buckets.get("visitor", 0),
        "organizers": buckets.get("organizer", 0),
        "imagine_future": item_stats["imagine_future"],
        "awareness_restore": item_stats["awareness_restore"],
        "hope": item_stats["hope"],
        "protect_urban_green": item_stats["protect_urban_green"],
        "after_community_meaning": item_stats["after_community_meaning"],
        "cronbach_alpha_13_post": round(alpha, 3),
    }

    out = {
        "sample": {
            "n_total_responses": len(data),
            "n_participated": n,
            "n_excluded_nonusers": len(excluded),
            "heard_before": {"yes": heard_yes, "no": heard_no},
            "relation": dict(buckets),
            "residents_n": buckets.get("resident", 0),
            "visitors_n": buckets.get("visitor", 0),
        },
        "items": item_stats,
        "cronbach_alpha_13_post_items": round(alpha, 3),
        "subgroups": subgroups,
        "open_ended_changed_view": {
            "coded_yes_prefix": yes_n,
            "n_a": n_a,
            "note": "Crude prefix coding of free text, not a validated codebook.",
        },
        "notes": notes,
        "poster": poster,
    }
    OUT.write_text(json.dumps(out, ensure_ascii=False, indent=2), encoding="utf-8")

    print("N participated", n, "excluded", len(excluded))
    print("relation", dict(buckets))
    print("heard", heard_yes, heard_no)
    print("alpha", round(alpha, 3))
    print("\nITEM  M  SD  Mdn  CI95  >=4  =5  Wilcoxon z p r")
    for k, s in item_stats.items():
        w = s["wilcoxon_vs_3"]
        print(
            f"{k:28} {s['M']:4.2f} {s['SD']:4.2f} {s['Mdn']:3.1f}  [{s['CI95_mean'][0]:.2f},{s['CI95_mean'][1]:.2f}]  "
            f"{s['pct_ge4']:5.1f}% {s['pct_eq5']:5.1f}%   z={w['z']} p={w['p']} r={w['r']}"
        )
    print("\nResident vs visitor (Mdn res, vis, U, p, r)")
    for k, v in subgroups["resident_vs_visitor"].items():
        print(f"  {k:28} Mdn {v['median1']} vs {v['median2']}  U={v['U']} p={v['p']} r={v['r']}")
    print("\nHeard vs never heard")
    for k, v in subgroups["heard_vs_unheard"].items():
        print(f"  {k:28} Mdn {v['median1']} vs {v['median2']}  U={v['U']} p={v['p']} r={v['r']}")
    print("wrote", OUT)


if __name__ == "__main__":
    main()
