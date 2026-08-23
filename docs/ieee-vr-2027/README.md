# IEEE VR 2027 — Hatillo Verde poster sources

Migrated from the local `3DInterfaces` workspace. Do not keep a full Unity clone on laptops that cannot hold ~3 GB.

## Already in this Unity repo

Voice copilot (the version wired into Play):

- `Assets/HatilloVerde/Scripts/Voice/`
- `TransitionManager.GoToPeriod`

## Poster

Title used: **If You Could Walk 1890: A Browser 3D Experience for Community Restoration**

- `make_academic_poster.py` — A0 academic poster
- `make_poster.py` — earlier layout
- `analyze_survey.py` — N=29 analysis (2 of 31 excluded). Reads `~/Downloads/Hatillo Verde Interactivo.csv` and writes `stats.json`

The compiled PDF is not stored here. Rebuild on a machine with the CSV:

```bash
python3 analyze_survey.py
python3 make_academic_poster.py
```

## Survey caveats (do not overclaim)

- Civic Likert after using the browser experience, not a usability study
- “Before” items were asked after use (retrospective)
- No control condition, no age/gender
- Post means sit on the ceiling (most answers 4–5)
