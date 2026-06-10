#!/usr/bin/env node
import {execFileSync} from 'node:child_process';
import {readFileSync, writeFileSync, mkdirSync} from 'node:fs';
import {dirname, join} from 'node:path';
import {fileURLToPath} from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const root = join(here, '..');

const PIPER = process.env.PIPER_BIN || 'piper';
const MODEL =
  process.env.PIPER_MODEL ||
  join(root, 'tools', 'voices', 'en_US-amy-medium.onnx');

const SCRIPTS = [
  {key: 'users', file: join(root, 'scripts', 'flowhub-users.en.md')},
  {key: 'technical', file: join(root, 'scripts', 'flowhub-technical.en.md')},
];

const outDir = join(root, 'public', 'audio', 'tts');
mkdirSync(outDir, {recursive: true});

function parseScenes(md) {
  const scenes = [];
  let current = null;
  for (const line of md.split('\n')) {
    const marker = line.match(/<!--\s*scene:\s*([a-z0-9_-]+)\s*-->/i);
    if (marker) {
      if (current) scenes.push(current);
      current = {id: marker[1], text: ''};
      continue;
    }
    if (current) current.text += line + ' ';
  }
  if (current) scenes.push(current);
  return scenes
    .map((s) => ({id: s.id, text: s.text.trim()}))
    .filter((s) => s.text.length > 0);
}

function durationSeconds(wav) {
  const out = execFileSync('ffprobe', [
    '-v',
    'error',
    '-show_entries',
    'format=duration',
    '-of',
    'default=noprint_wrappers=1:nokey=1',
    wav,
  ])
    .toString()
    .trim();
  const seconds = parseFloat(out);
  if (!Number.isFinite(seconds)) {
    throw new Error(`ffprobe returned no usable duration for ${wav}: "${out}"`);
  }
  return Math.round(seconds * 100) / 100;
}

const durations = {};
for (const {key, file} of SCRIPTS) {
  const scenes = parseScenes(readFileSync(file, 'utf8'));
  durations[key] = {};
  for (const scene of scenes) {
    if (scene.id in durations[key]) {
      throw new Error(`Duplicate scene id "${scene.id}" in ${file}`);
    }
    const wav = join(outDir, `${key}-${scene.id}.wav`);
    console.log(`TTS ${key}-${scene.id}: ${scene.text.slice(0, 60)}...`);
    execFileSync(PIPER, ['--model', MODEL, '--output_file', wav], {
      input: scene.text,
    });
    durations[key][scene.id] = durationSeconds(wav);
  }
}

writeFileSync(
  join(root, 'src', 'durations.json'),
  JSON.stringify(durations, null, 2) + '\n',
);
console.log('Wrote src/durations.json');
