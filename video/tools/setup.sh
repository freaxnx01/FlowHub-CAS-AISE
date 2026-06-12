#!/usr/bin/env bash
#
# Vendor the local prerequisites the video pipeline needs, without touching the
# system: Piper (TTS engine), the en_US-amy-medium voice model, a static
# ffmpeg/ffprobe build, and the Noto Color Emoji font (so headless Chromium
# renders emoji icons instead of empty squares). Everything lands under
# video/tools/ and is git-ignored; the emoji font is also activated in the user
# font dir so the Remotion renderer's Chromium can discover it.
#
# Idempotent: re-running skips anything already present. Pin versions here so the
# toolchain is reproducible across machines and CI.
set -euo pipefail

here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

PIPER_VERSION="2023.11.14-2"
PIPER_URL="https://github.com/rhasspy/piper/releases/download/${PIPER_VERSION}/piper_linux_x86_64.tar.gz"

# English female voice (US). Swap VOICE_BASE/VOICE_NAME to use a different Piper
# voice; keep PIPER_MODEL in the justfile pointed at the matching .onnx.
VOICE_BASE="https://huggingface.co/rhasspy/piper-voices/resolve/main/en/en_US/amy/medium"
VOICE_NAME="en_US-amy-medium"

FFMPEG_URL="https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-amd64-static.tar.xz"

EMOJI_FONT_URL="https://github.com/googlefonts/noto-emoji/raw/main/fonts/NotoColorEmoji.ttf"

arch="$(uname -m)"
if [[ "$arch" != "x86_64" ]]; then
  echo "setup.sh: only x86_64 is supported by the pinned binaries (got: $arch)." >&2
  echo "Set PIPER_BIN / PIPER_MODEL and put ffprobe on PATH manually instead." >&2
  exit 1
fi

# ── Piper ────────────────────────────────────────────────────────────────────
piper_bin="$here/piper/piper"
if [[ -x "$piper_bin" ]]; then
  echo "✓ piper already present ($piper_bin)"
else
  echo "↓ downloading piper $PIPER_VERSION ..."
  tmp="$(mktemp -d)"
  curl -fL --retry 3 -o "$tmp/piper.tar.gz" "$PIPER_URL"
  # Archive extracts to a top-level piper/ dir containing the binary + libs.
  tar -xzf "$tmp/piper.tar.gz" -C "$here"
  rm -rf "$tmp"
  chmod +x "$piper_bin"
  echo "✓ piper installed at $piper_bin"
fi

# ── Voice model ──────────────────────────────────────────────────────────────
voices_dir="$here/voices"
mkdir -p "$voices_dir"
for ext in onnx onnx.json; do
  target="$voices_dir/$VOICE_NAME.$ext"
  if [[ -s "$target" ]]; then
    echo "✓ voice $VOICE_NAME.$ext already present"
  else
    echo "↓ downloading voice $VOICE_NAME.$ext ..."
    curl -fL --retry 3 -o "$target" "$VOICE_BASE/$VOICE_NAME.$ext"
    echo "✓ $target"
  fi
done

# ── ffmpeg / ffprobe (static) ────────────────────────────────────────────────
ffmpeg_dir="$here/ffmpeg"
if [[ -x "$ffmpeg_dir/ffprobe" && -x "$ffmpeg_dir/ffmpeg" ]]; then
  echo "✓ ffmpeg/ffprobe already present ($ffmpeg_dir)"
else
  echo "↓ downloading static ffmpeg ..."
  mkdir -p "$ffmpeg_dir"
  tmp="$(mktemp -d)"
  curl -fL --retry 3 -o "$tmp/ffmpeg.tar.xz" "$FFMPEG_URL"
  tar -xJf "$tmp/ffmpeg.tar.xz" -C "$tmp"
  # Archive extracts to ffmpeg-*-amd64-static/ — flatten the two binaries we need.
  extracted="$(find "$tmp" -maxdepth 1 -type d -name 'ffmpeg-*-static' | head -1)"
  cp "$extracted/ffmpeg" "$extracted/ffprobe" "$ffmpeg_dir/"
  rm -rf "$tmp"
  chmod +x "$ffmpeg_dir/ffmpeg" "$ffmpeg_dir/ffprobe"
  echo "✓ ffmpeg/ffprobe installed at $ffmpeg_dir"
fi

# ── Noto Color Emoji (so Chromium renders emoji, not tofu squares) ────────────
fonts_dir="$here/fonts"
emoji_font="$fonts_dir/NotoColorEmoji.ttf"
mkdir -p "$fonts_dir"
if [[ -s "$emoji_font" ]]; then
  echo "✓ Noto Color Emoji already vendored ($emoji_font)"
else
  echo "↓ downloading Noto Color Emoji ..."
  curl -fL --retry 3 -o "$emoji_font" "$EMOJI_FONT_URL"
  echo "✓ $emoji_font"
fi
# Activate it for the renderer's Chromium via the user font dir (no sudo). The
# vendored copy under tools/fonts/ stays the source of truth.
user_fonts="${XDG_DATA_HOME:-$HOME/.local/share}/fonts"
mkdir -p "$user_fonts"
if ! cmp -s "$emoji_font" "$user_fonts/NotoColorEmoji.ttf" 2>/dev/null; then
  cp "$emoji_font" "$user_fonts/NotoColorEmoji.ttf"
  command -v fc-cache >/dev/null && fc-cache -f "$user_fonts" >/dev/null 2>&1 || true
  echo "✓ Noto Color Emoji activated in $user_fonts"
else
  echo "✓ Noto Color Emoji already active in $user_fonts"
fi

# ── Playwright Chromium (for the demo-walkthrough capture script) ─────────────
if [[ -d "$here/../node_modules/playwright" ]]; then
  echo "↓ ensuring Playwright Chromium is installed ..."
  (cd "$here/.." && npx --yes playwright install chromium) >/dev/null 2>&1 \
    && echo "✓ Playwright Chromium ready" \
    || echo "⚠ Playwright Chromium install failed — run 'npx playwright install chromium' in video/ manually"
fi

echo
echo "Done. Generate the videos with:  just video"
