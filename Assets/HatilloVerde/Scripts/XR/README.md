# Quest / OpenXR (branch `feature/quest-xr`)

This branch adds a Quest build path without changing the public WebGL flow.

On Android (Quest) the runtime bootstrap:

1. Spawns an **XR Origin** at the current `Main Camera` viewpoint
2. Turns overlay canvases into **world space** and skips the intro video
3. Uses the **right-hand trigger** to open POIs (`SelectionManager`)
4. Uses the **grip** as push-to-talk (same `VoiceCopilot` as Space on desktop)

## One-time in Unity

Editor 6000.3.13f1 (not 6000.4). Open `Assets/HatilloVerde/HatilloVerde_scene.unity`.

Menu **Hatillo Verde → Setup Quest XR** (also ran from batchmode).

Then:

1. File → Build Settings → **Android** → Switch Platform
2. XR Plug-in Management → Android → **OpenXR** + Meta Quest features
3. Build APK
4. Quest Developer Mode + `adb install -r HatilloVerde.apk`

## In the headset

- Look around with your head (same river viewpoint as the browser camera)
- Point the right ray, pull **trigger** on a POI
- Hold **grip**, speak Spanish, release (needs `openai_key.txt` and internet)
- Continuar / épocas: world-space buttons from the converted overlay canvas

WebGL / Mac Play without a headset still uses the original camera.
