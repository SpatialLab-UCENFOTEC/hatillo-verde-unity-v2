using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Maps SetEra / FocusPOI / ClickButton / Say onto the live scene.
/// </summary>
public class HatilloVoiceBridge : MonoBehaviour
{
    TransitionManager _eras;

    static readonly Dictionary<string, string[]> Aliases = new Dictionary<string, string[]>
    {
        { "rana", new[] { "rana" } },
        { "pinzon", new[] { "melozone", "cabanisi", "pinzón", "pinzon" } },
        { "rio", new[] { "tiribí", "tiribi", "río", "rio" } },
        { "coyote", new[] { "coyote" } },
        { "jaguar", new[] { "jaguar" } },
        { "armadillo", new[] { "armadillo" } },
        { "garza", new[] { "garza" } },
        { "mapache", new[] { "mapache" } },
        { "zorro", new[] { "zorro" } },
        { "beneficio", new[] { "lechos", "lavado", "secado", "pileta" } },
        { "biodiversidad", new[] { "biodiversidad" } },
        { "especies", new[] { "especies ausentes" } },
        { "indigenas", new[] { "indígenas", "indigenas", "comunidades" } },
        { "vecinos", new[] { "vecinos" } },
        { "recreativas", new[] { "recreativas" } },
        { "neo", new[] { "neo-ecosistema", "neoecosistema", "neo" } },
        { "invasiones", new[] { "invasiones" } },
    };

    void Awake()
    {
        _eras = GetComponent<TransitionManager>();
        if (_eras == null) _eras = FindAnyObjectByType<TransitionManager>();
    }

    public IEnumerator Apply(CopilotCommand cmd)
    {
        if (cmd == null || string.IsNullOrEmpty(cmd.action))
        {
            Status("No te entendí.");
            yield break;
        }

        switch (cmd.action)
        {
            case "SetEra":
                yield return GoToYear(cmd.year);
                break;
            case "FocusPOI":
                yield return FocusPoi(cmd.id);
                break;
            case "ClickButton":
                if (!TryClickSpoken(cmd.id))
                    ClickUi(cmd.id);
                break;
            default:
                Status(string.IsNullOrEmpty(cmd.text) ? "Listo." : cmd.text);
                break;
        }
    }

    public bool TryClickSpoken(string spoken)
    {
        var needle = Fold(spoken);
        if (needle.Length < 3) return false;

        var buttons = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Button best = null;
        string bestLabel = "";
        int bestScore = 0;

        foreach (var b in buttons)
        {
            if (b == null || !b.isActiveAndEnabled || !b.interactable) continue;
            if (!b.gameObject.activeInHierarchy) continue;
            var label = ButtonLabel(b);
            var n = Fold(label);
            if (n.Length < 2 || n == "x") continue;

            int score = 0;
            if (needle == n) score = 100 + n.Length;
            else if (needle.Contains(n)) score = 80 + n.Length;
            else if (n.Length >= 4 && n.Contains(needle)) score = 70 + needle.Length;
            else if (WordsHit(needle, n)) score = 50 + n.Length;
            if (score > bestScore)
            {
                bestScore = score;
                best = b;
                bestLabel = label.Trim();
            }
        }

        if (best == null || bestScore < 50) return false;
        best.onClick.Invoke();
        Status(bestLabel);
        return true;
    }

    static string ButtonLabel(Button b)
    {
        var tmp = b.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null && !string.IsNullOrWhiteSpace(tmp.text))
            return tmp.text;
        var ui = b.GetComponentInChildren<Text>(true);
        if (ui != null && !string.IsNullOrWhiteSpace(ui.text))
            return ui.text;
        return b.gameObject.name.Replace("Button", "").Trim();
    }

    static bool WordsHit(string spoken, string label)
    {
        var parts = label.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        int need = 0, hits = 0;
        foreach (var p in parts)
        {
            if (p.Length < 4) continue;
            need++;
            if (spoken.Contains(p)) hits++;
        }
        return need > 0 && hits == need;
    }

    static string Fold(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        s = System.Text.RegularExpressions.Regex.Replace(s, "<.*?>", " ");
        s = s.ToLowerInvariant();
        var form = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in form)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c) || c == ' ') sb.Append(c);
        }
        return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "\\s+", " ").Trim();
    }

    void ClickUi(string id)
    {
        id = Fold(id);
        if (id == "next" || id == "continuar" || id == "entrar" || id == "siguiente")
            id = "next";
        else if (id == "back" || id == "volver" || id == "atras" || id == "anterior")
            id = "back";
        else if (id == "skip" || id == "saltar")
            id = "skip";

        var intro = FindAnyObjectByType<IntroManager>();
        bool introUp = intro != null &&
            ((intro.introPanel != null && intro.introPanel.gameObject.activeSelf && intro.introPanel.alpha > 0.05f) ||
             (intro.subIntroPanel != null && intro.subIntroPanel.gameObject.activeSelf && intro.subIntroPanel.alpha > 0.05f));

        if (id == "skip")
        {
            if (introUp) intro.SkipIntro();
            else if (_eras != null) _eras.SkipTransition();
            Status("Saltar.");
            return;
        }

        if (id == "next")
        {
            if (introUp) intro.StartExperience();
            else if (_eras != null) _eras.NextPeriod();
            Status("Continuar.");
            return;
        }

        if (id == "back")
        {
            if (_eras != null) _eras.PreviousPeriod();
            Status("Volver.");
            return;
        }

        Status("No encontré ese botón.");
    }

    IEnumerator GoToYear(int year)
    {
        if (_eras == null)
        {
            Status("No hay gestor de épocas.");
            yield break;
        }

        int index = YearToIndex(year, _eras.PeriodCount);
        _eras.GoToPeriod(index, playNarration: false);
        while (_eras.IsBusy) yield return null;
        Status(index == 0 ? "Época 1890." : index == 1 ? "Presente." : "Futuro restaurado.");
    }

    IEnumerator FocusPoi(string id)
    {
        var info = FindPoi(id);
        if (info == null)
        {
            Status("No encontré ese punto. Prueba rana, río, coyote, jaguar…");
            yield break;
        }

        int era = EraOf(info);
        if (era >= 0 && _eras != null && era != _eras.CurrentPeriodIndex)
        {
            _eras.GoToPeriod(era, playNarration: false);
            while (_eras.IsBusy) yield return null;
        }

        info.TriggerPopup();
        Status(StripTags(info.title));
    }

    InteractableInfo FindPoi(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        id = id.ToLowerInvariant();
        if (!Aliases.TryGetValue(id, out var keys))
            keys = new[] { id };

        var all = FindObjectsByType<InteractableInfo>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        InteractableInfo best = null;
        int bestEraDist = 99;

        foreach (var info in all)
        {
            var hay = (StripTags(info.title) + " " + info.gameObject.name).ToLowerInvariant();
            bool hit = false;
            foreach (var k in keys)
            {
                if (hay.Contains(k.ToLowerInvariant())) { hit = true; break; }
            }
            if (!hit) continue;

            int era = EraOf(info);
            int dist = _eras == null ? 0 : Mathf.Abs(era - _eras.CurrentPeriodIndex);
            if (best == null || dist < bestEraDist)
            {
                best = info;
                bestEraDist = dist;
            }
        }

        return best;
    }

    int EraOf(InteractableInfo info)
    {
        if (_eras == null || _eras.environments == null) return -1;
        var t = info.transform;
        while (t != null)
        {
            for (int i = 0; i < _eras.environments.Length; i++)
            {
                var env = _eras.environments[i];
                if (env != null && (t.gameObject == env || t.IsChildOf(env.transform)))
                    return i;
            }
            t = t.parent;
        }
        return _eras.CurrentPeriodIndex;
    }

    static int YearToIndex(int year, int count)
    {
        if (count <= 1) return 0;
        if (year <= 1950) return 0;
        if (year < 2050) return Mathf.Min(1, count - 1);
        return count - 1;
    }

    static string StripTags(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return System.Text.RegularExpressions.Regex.Replace(s, "<.*?>", "").Trim();
    }

    void Status(string msg)
    {
        Debug.Log("[Voice] " + msg);
        VoiceCopilot.ShowStatus(msg);
    }
}
