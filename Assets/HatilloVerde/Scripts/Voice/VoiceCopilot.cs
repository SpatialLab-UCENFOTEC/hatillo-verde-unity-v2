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
    static string _hud = "Mantén Espacio y habla.  «muéstrame 1890»  ·  «dónde está la rana»";

    [SerializeField] string openaiApiKey;
    [SerializeField] int maxSeconds = 8;

    [TextArea(10, 22)]
    [SerializeField] string systemPrompt =
        "Eres el copiloto de voz de Hatillo Verde, una escena 3D con tres épocas de un terreno junto al río Tiribí.\n" +
        "El usuario habla en español. NO chateas. Respondes SOLO un JSON, sin markdown:\n" +
        "{\"action\":\"SetEra\",\"year\":1890}\n" +
        "{\"action\":\"FocusPOI\",\"id\":\"rana\"}\n" +
        "{\"action\":\"Say\",\"text\":\"frase corta\"}\n" +
        "SetEra: 1890 pasado cafetalero; 2026 presente; 2100 futuro restaurado.\n" +
        "FocusPOI ids: rana, pinzon, rio, coyote, jaguar, armadillo, garza, mapache, zorro, beneficio, biodiversidad, especies, indigenas, vecinos, recreativas, neo, invasiones.\n" +
        "Si no puedes ejecutar, Say en una línea. Nunca inventes otras actions.";

    HatilloVoiceBridge _bridge;
    AudioClip _clip;
    bool _recording;
    string _mic;

    public static void ShowStatus(string msg) => _hud = msg;

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
            Debug.LogError("[VoiceCopilot] No microphone.");
        else
            _mic = Microphone.devices[0];
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.spaceKey.wasPressedThisFrame) PushToTalkDown();
        if (kb.spaceKey.wasReleasedThisFrame) PushToTalkUp();
    }

    public void PushToTalkDown()
    {
        if (_recording || string.IsNullOrEmpty(_mic))
            return;
        if (string.IsNullOrEmpty(openaiApiKey))
        {
            ShowStatus("Falta API key (Inspector o Resources/openai_key.txt).");
            return;
        }

        _clip = Microphone.Start(_mic, false, maxSeconds, 16000);
        _recording = true;
        ShowStatus("Escuchando… suelta Espacio.");
    }

    public void PushToTalkUp()
    {
        if (!_recording) return;
        _recording = false;
        int pos = Microphone.GetPosition(_mic);
        Microphone.End(_mic);
        if (_clip == null || pos < 1600)
        {
            ShowStatus("No escuché nada.");
            return;
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
        var style = new GUIStyle(GUI.skin.box)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true,
            padding = new RectOffset(12, 12, 8, 8)
        };
        style.normal.textColor = Color.white;
        GUI.Box(new Rect(16, Screen.height - 64, Mathf.Min(720, Screen.width - 32), 48), _hud, style);
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
