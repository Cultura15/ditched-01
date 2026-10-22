# Cyber School Metaverse Redesign

- **Date:** 2026-10-22
- **Type:** feat

## What
Low-poly cyber school generator (plaza, 3 buildings, gate, colorful lamps, benches, trees, grass, flowers, a few motes), a dreamy purple-blue skybox with drifting clouds, a one-click atmosphere setup (fog, trilight ambient, warm low sun, bloom), a third-person avatar with smoother WASD movement, and a generated ambient music loop.

## Why
Horizon Worlds-style look with minimal complexity: flat-shaded procedural meshes, shared materials, static objects, no textures. Point lights rely on the existing Forward+ renderer (no 4-lights-per-object limit). Assumed root namespace `MyGame` (none existed).

## Result
Not compiled in Unity. C# type-checked with Unity's Roslyn compiler, and it compiles. The shader is unverified. Verify in Unity.

## Files
- `Assets/Scripts/PlayerMovement.cs` - modified - camera now pulls back from where you placed it (the head), ignores the player's own colliders, and leaves the existing CharacterController center alone. Extra child Rigidbody/colliders are disabled at play.
- `Assets/Scripts/Player/PlayerAvatar.cs` - added - avatar faces movement direction, walk bob, lean, idle float
- `Assets/Scripts/Environment/CyberSchoolGenerator.cs` - added - layout + scattering
- `Assets/Scripts/Environment/CyberSchoolBuildings.cs`, `CyberSchoolProps.cs`, `CyberSchoolBuildContext.cs`, `CyberSchoolPalette.cs`, `LowPolyMeshBuilder.cs` - added - generator parts
- `Assets/Editor/Environment/CyberSchoolGeneratorEditor.cs` - added - Inspector buttons
- `Assets/Editor/Environment/CyberSchoolAtmosphere.cs` - added - sky/fog/light/post setup
- `Assets/Editor/Player/CyberPlayerSetup.cs` - added - `Tools > MyGame > Player > Set Up Cyber Player`
- `Assets/Editor/Core/GeneratedAssetSaver.cs` - added - shared asset saving helper
- `Assets/Art/Shaders/Environment/DreamySkybox.shader` - added - gradient sky, clouds, sun glow, faint stars
- `Assets/Audio/Music/CyberSchoolAmbient.wav` - added - 64 s seamless ambient loop
- `Tools/Audio/generate_ambient_music.py` - added - regenerates the WAV

## Verify in Unity
1. Create empty GameObject > Add Component `MyGame/Environment/Cyber School Generator`.
2. Click **Hide Old Nature Environment** (if shown), then **Generate & Save Assets**.
3. Click **Apply Sky, Fog, Lighting & Post-Processing**.
4. Player: the existing Player already has `PlayerMovement` and Main Camera. Press Play and click the Game view. WASD, Shift run, Space jump, R crouch, Esc frees the cursor. Camera Mode on the component switches third/first person. Optional: add `Player Avatar` to the visual child (not the root) so the body faces walking, and the camera when standing still.
5. `Tools > MyGame > Player > Set Up Cyber Player` is optional. It replaces the capsule with the stylized avatar.

## Safe to delete after generating
- `Assets/Scripts/Environment/*` (all 6 generator scripts) and `Assets/Editor/Environment/*` (both editor scripts)
- `Assets/Editor/Player/CyberPlayerSetup.cs` - one-off avatar setup
- `Tools/Audio/generate_ambient_music.py` - one-off music synth
- Before deleting: remove the Cyber School Generator component from its GameObject.
- Generated output depends on these scripts? **No** (built-in components only).

## Keep (do not delete)
- `Assets/Generated/Environment/**` and `Assets/Generated/Player/Materials/**` - generated output
- `Assets/Materials/Environment/CyberSchoolSky.mat`, `Assets/Settings/CyberSchoolVolumeProfile.asset` - created once, then never overwritten, so tweak freely
- `PlayerMovement.cs`, `PlayerAvatar.cs`, `DreamySkybox.shader`, `CyberSchoolAmbient.wav`
- `Assets/Editor/Core/GeneratedAssetSaver.cs` - shared helper

## Follow-ups
- Set the WAV import to Load Type: Streaming to save ~11 MB of memory.
- Old `NatureGenerator` scripts and the duplicate disabled `PlayerMovement` components are left untouched.
- `PlayerMovement.cs` stays in `Assets/Scripts/` root (moving it was out of scope).
- Asset creation is not covered by Undo.
