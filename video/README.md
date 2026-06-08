# FlowHub Explainer Videos

Two short German explainer videos (end-user + technical) built with Remotion,
narrated by local Piper TTS, with a background music bed. Isolated from the
.NET solution — its own Node toolchain. Final length is driven by the narration:
the committed placeholder `durations.json` renders ~34s / ~37s; real `npm run tts`
output sets the true per-scene timing.

## One-time setup

1. Install Node deps:
   ```bash
   cd video && npm install
   ```
2. Install [Piper](https://github.com/rhasspy/piper) and the German voice model
   `de_DE-thorsten-medium`. Put the model files here:
   ```
   video/tools/voices/de_DE-thorsten-medium.onnx
   video/tools/voices/de_DE-thorsten-medium.onnx.json
   ```
   Or point `PIPER_BIN` / `PIPER_MODEL` env vars at your install.
3. Ensure `ffmpeg`/`ffprobe` are on PATH.
4. (Optional) Replace the silent `public/audio/music/bed.mp3` with a real
   royalty-free track and record attribution in `public/audio/music/LICENSE.md`.

## Editing content

- Narration lives in `scripts/*.de.md`. Each scene is delimited by
  `<!-- scene: <id> -->`. Scene ids must match keys in `durations.json` and the
  `audio/tts/<key>-<id>.wav` paths used by the compositions.
- Visuals live in `src/*.tsx` (`UserVideo`, `TechnicalVideo`) and `src/components/`.

## Generate narration + durations

```bash
npm run tts
```
Generates `public/audio/tts/*.wav` and overwrites `src/durations.json` so scene
timing matches the narration length.

## Preview

```bash
npm run dev   # Remotion Studio at http://localhost:3000
```

## Render

```bash
npm run render:users        # → out/flowhub-users.de.mp4
npm run render:technical    # → out/flowhub-technical.de.mp4
```

Run `npm run tts` before a final render so narration audio exists.
