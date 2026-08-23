# Voice copilot (Hatillo Verde)

Hold **Space**, speak, release.

- «muéstrame 1890» / «el presente» / «el futuro restaurado» → changes era
- «dónde está la rana» / «el río» / «el coyote» → opens that POI popup

## API key (do not commit)

One of:

1. Inspector on `VoiceCopilot` (if you add the component in the scene)
2. `Assets/HatilloVerde/Scripts/Voice/Resources/openai_key.txt` (filename must be `openai_key.txt`)
3. Environment variable `OPENAI_API_KEY`

Example file: copy `Resources/openai_key.txt.example` to `openai_key.txt`.

The copilot auto-attaches to `TransitionManager` on Play. Quest: wire a button to `PushToTalkDown` / `PushToTalkUp`.
