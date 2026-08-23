using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Maps SetEra / FocusPOI / Say onto TransitionManager + InteractableInfo.
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
            default:
                Status(string.IsNullOrEmpty(cmd.text) ? "Listo." : cmd.text);
                break;
        }
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
