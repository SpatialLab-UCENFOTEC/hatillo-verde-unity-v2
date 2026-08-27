using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>
/// Hold Space (or PushToTalkDown/Up on Quest) → Whisper → GPT JSON → HatilloVoiceBridge.
/// API key: Inspector, Resources/openai_key.txt, or env OPENAI_API_KEY.
/// </summary>
public class VoiceCopilot : MonoBehaviour
{
    static string _hud = "Mantén Espacio y habla";

    [SerializeField] string openaiApiKey;
    [SerializeField] int maxSeconds = 8;

    [TextArea(10, 22)]
    [SerializeField] string systemPrompt =
        "Eres el copiloto de voz de Hatillo Verde, una escena 3D con tres épocas de un terreno junto al río Tiribí.\n" +
        "El usuario habla en español. NO chateas. Respondes SOLO un JSON, sin markdown:\n" +
        "{\"action\":\"SetEra\",\"year\":1890}\n" +
        "{\"action\":\"FocusPOI\",\"id\":\"rana\"}\n" +
        "{\"action\":\"ClickButton\",\"id\":\"next\"}\n" +
        "{\"action\":\"Say\",\"text\":\"frase corta\"}\n" +
        "SetEra: 1890 pasado cafetalero; 2026 presente; 2100 futuro restaurado.\n" +
        "FocusPOI ids: rana, pinzon, rio, coyote, jaguar, armadillo, garza, mapache, zorro, beneficio, biodiversidad, especies, indigenas, vecinos, recreativas, neo, invasiones.\n" +
        "ClickButton id = texto visible del botón (continuar, atrás, saltar, comenzar, créditos, entrar, volver).\n" +
        "Si no puedes ejecutar, Say en una línea. Nunca inventes otras actions.";

    HatilloVoiceBridge _bridge;
    AudioClip _clip;
    bool _recording;
    string _mic;
    float _downTime;

    public static void ShowStatus(string msg)
    {
        _hud = msg;
        if (HatilloXrRig.StatusLabel != null)
            HatilloXrRig.StatusLabel.text = msg;
    }

    void Awake()
    {
        _bridge = GetComponent<HatilloVoiceBridge>();
        if (_bridge == null) _bridge = gameObject.AddComponent<HatilloVoiceBridge>();
        ResolveKey();
    }

    IEnumerator Start()
    {
#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            Permission.RequestUserPermission(Permission.Microphone);
#endif
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("[VoiceCopilot] No microphone.");
            ShowStatus("Unity no ve el micrófono. En macOS: Ajustes → Privacidad → Micrófono → Unity.");
        }
        else
        {
            _mic = Microphone.devices[0];
            Debug.Log("[VoiceCopilot] mic: " + _mic);
            var warmup = Microphone.Start(_mic, false, 1, 44100);
            yield return null;
            Microphone.End(_mic);
        }
    }

    void Update()
    {
        bool holding = false;
        if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)
            holding = true;
        if (HatilloXrRig.RightGripHeld)
            holding = true;

        if (holding && !_recording) PushToTalkDown();
        if (!holding && _recording) PushToTalkUp();
    }

    public void PushToTalkDown()
    {
        if (_recording || string.IsNullOrEmpty(_mic))
            return;
        if (string.IsNullOrEmpty(openaiApiKey))
        {
            ShowStatus("Falta API key de OpenAI.");
            return;
        }

        _clip = Microphone.Start(_mic, false, maxSeconds, 44100);
        if (_clip == null)
        {
            ShowStatus("No pude abrir el micrófono.");
            return;
        }

        _recording = true;
        _downTime = Time.unscaledTime;
        ShowStatus("Escuchando… suelta grip o Espacio.");
    }

    public void PushToTalkUp()
    {
        if (!_recording) return;
        _recording = false;
        StartCoroutine(StopAndProcess());
    }

    IEnumerator StopAndProcess()
    {
        yield return null;
        int pos = Microphone.GetPosition(_mic);
        var data = _clip != null ? new float[_clip.samples * _clip.channels] : null;
        if (_clip != null)
            _clip.GetData(data, 0);

        Microphone.End(_mic);

        if (pos <= 0 && data != null)
            pos = LastLoudSample(data);

        float held = Time.unscaledTime - _downTime;
        int fromTime = _clip != null ? Mathf.RoundToInt(held * _clip.frequency) : 0;
        if (data != null)
            fromTime = Mathf.Clamp(fromTime, 0, data.Length);
        // On macOS GetPosition often stays tiny while the clip actually filled.
        if (fromTime > pos * 2)
            pos = Mathf.Max(pos, fromTime);

        float rms = Rms(data, pos);
        Debug.Log($"[VoiceCopilot] held={held:0.00}s pos={pos} rms={rms:0.000} mic={_mic}");

        if (_clip == null || held < 0.25f || pos < 2400)
        {
            ShowStatus($"Mic corto ({held:0.0}s). Mantén Espacio.");
            yield break;
        }
        if (rms < 0.0015f)
        {
            ShowStatus("Mic en silencio. Habla más cerca.");
            yield break;
        }

        TrimClip(pos);
        StartCoroutine(TranscribeAndAct());
    }

    void TrimClip(int samplesRecorded)
    {
        var data = new float[_clip.samples * _clip.channels];
        _clip.GetData(data, 0);
        int n = Mathf.Min(samplesRecorded, data.Length);
        var trimmed = new float[n];
        System.Array.Copy(data, trimmed, n);
        var shortClip = AudioClip.Create("utt", n, _clip.channels, _clip.frequency, false);
        shortClip.SetData(trimmed, 0);
        _clip = shortClip;
    }

    static int LastLoudSample(float[] data)
    {
        int last = 0;
        for (int i = 0; i < data.Length; i++)
        {
            if (Mathf.Abs(data[i]) > 0.008f)
                last = i;
        }
        return last;
    }

    static float Rms(float[] data, int n)
    {
        if (data == null || n <= 0) return 0f;
        n = Mathf.Min(n, data.Length);
        double s = 0;
        for (int i = 0; i < n; i++)
            s += data[i] * data[i];
        return (float)System.Math.Sqrt(s / n);
    }

    IEnumerator TranscribeAndAct()
    {
        ShowStatus("Pensando…");
        var wav = WavEncoder.FromClip(_clip);
        var form = new WWWForm();
        form.AddBinaryData("file", wav, "speech.wav", "audio/wav");
        form.AddField("model", "whisper-1");
        form.AddField("language", "es");

        using var stt = UnityWebRequest.Post("https://api.openai.com/v1/audio/transcriptions", form);
        stt.SetRequestHeader("Authorization", "Bearer " + openaiApiKey);
        yield return stt.SendWebRequest();
        if (stt.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(stt.error + " " + stt.downloadHandler.text);
            ShowStatus("Falló el reconocimiento de voz.");
            yield break;
        }

        var transcript = ExtractJsonString(stt.downloadHandler.text, "text");
        Debug.Log("[VoiceCopilot] " + transcript);
        if (string.IsNullOrWhiteSpace(transcript))
        {
            ShowStatus("No te entendí.");
            yield break;
        }

        ShowStatus("«" + transcript + "»");

        var local = ParseNav(transcript);
        if (local != null)
        {
            yield return _bridge.Apply(local);
            yield break;
        }
        if (_bridge.TryClickSpoken(transcript))
            yield break;

        var body = "{\"model\":\"gpt-4o-mini\",\"temperature\":0,\"messages\":[" +
                   "{\"role\":\"system\",\"content\":" + JsonEscape(systemPrompt) + "}," +
                   "{\"role\":\"user\",\"content\":" + JsonEscape(transcript) + "}]}";

        using var chat = new UnityWebRequest("https://api.openai.com/v1/chat/completions", "POST");
        chat.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        chat.downloadHandler = new DownloadHandlerBuffer();
        chat.SetRequestHeader("Content-Type", "application/json");
        chat.SetRequestHeader("Authorization", "Bearer " + openaiApiKey);
        yield return chat.SendWebRequest();
        if (chat.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError(chat.error + " " + chat.downloadHandler.text);
            ShowStatus("Falló el copiloto.");
            yield break;
        }

        var json = StripFences(ExtractMessageContent(chat.downloadHandler.text));
        var cmd = JsonUtility.FromJson<CopilotCommand>(json);
        yield return _bridge.Apply(cmd);
    }

    static CopilotCommand ParseNav(string transcript)
    {
        if (string.IsNullOrEmpty(transcript)) return null;
        var t = transcript.ToLowerInvariant();
        if (t.Contains("salt"))
            return new CopilotCommand { action = "ClickButton", id = "skip" };
        if (t.Contains("volv") || t.Contains("atrás") || t.Contains("atras") || t.Contains("anterior"))
            return new CopilotCommand { action = "ClickButton", id = "back" };
        if (t.Contains("contin") || t.Contains("entrar") || t.Contains("siguiente") || t.Contains("adelante"))
            return new CopilotCommand { action = "ClickButton", id = "next" };
        return null;
    }

    void ResolveKey()
    {
        if (!string.IsNullOrEmpty(openaiApiKey)) return;
        var env = System.Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(env)) { openaiApiKey = env; return; }
        var ta = Resources.Load<TextAsset>("openai_key");
        if (ta != null) openaiApiKey = ta.text.Trim();
    }

    void OnGUI()
    {
        if (HatilloXrRig.IsActive) return;
        if (string.IsNullOrEmpty(_hud)) return;

        const float h = 20f;
        float w = Mathf.Min(280f, Screen.width * 0.36f);
        var style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false,
            padding = new RectOffset(6, 6, 1, 1)
        };
        style.normal.textColor = Color.white;

        float x = (Screen.width - w) * 0.5f;
        float y = Screen.height - 24f;
        GUI.Box(new Rect(x, y, w, h), _hud, style);
    }

    static string StripFences(string s)
    {
        if (string.IsNullOrEmpty(s)) return "{\"action\":\"Say\",\"text\":\"No te entendí.\"}";
        s = s.Trim();
        int a = s.IndexOf('{');
        int b = s.LastIndexOf('}');
        if (a >= 0 && b > a) return s.Substring(a, b - a + 1);
        return "{\"action\":\"Say\",\"text\":\"No te entendí.\"}";
    }

    static string JsonEscape(string s) =>
        "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "") + "\"";

    static string ExtractMessageContent(string raw)
    {
        const string key = "\"content\":";
        int i = raw.IndexOf(key);
        if (i < 0) return null;
        i = raw.IndexOf('"', i + key.Length);
        if (i < 0) return null;
        var sb = new StringBuilder();
        for (int j = i + 1; j < raw.Length; j++)
        {
            if (raw[j] == '\\' && j + 1 < raw.Length)
            {
                sb.Append(raw[j + 1] == 'n' ? '\n' : raw[j + 1]);
                j++;
                continue;
            }
            if (raw[j] == '"') break;
            sb.Append(raw[j]);
        }
        return sb.ToString();
    }

    static string ExtractJsonString(string raw, string key)
    {
        var token = "\"" + key + "\":";
        int i = raw.IndexOf(token);
        if (i < 0) return "";
        i = raw.IndexOf('"', i + token.Length);
        if (i < 0) return "";
        var sb = new StringBuilder();
        for (int j = i + 1; j < raw.Length; j++)
        {
            if (raw[j] == '\\' && j + 1 < raw.Length)
            {
                sb.Append(raw[j + 1] == 'n' ? '\n' : raw[j + 1]);
                j++;
                continue;
            }
            if (raw[j] == '"') break;
            sb.Append(raw[j]);
        }
        return sb.ToString();
    }
}
