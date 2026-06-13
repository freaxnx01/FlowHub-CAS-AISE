# FlowHub Explainer Videos Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build two short (~90s) German explainer videos for FlowHub — one technical, one for end users — code-based with Remotion, narrated by local Piper TTS, with a background music bed.

**Architecture:** A self-contained `video/` directory at the repo root, fully isolated from the .NET solution. Narration scripts (`.md`) drive a Piper TTS step that emits per-scene `.wav` files plus a `durations.json`; Remotion compositions read those durations so on-screen scene length always matches narration length. Two compositions share one component library and theme.

**Tech Stack:** Remotion 4 (React → MP4), TypeScript, Node.js, Piper TTS (`de_DE-thorsten-medium`), ffprobe, ffmpeg.

**Note on a deliberate deviation from the spec:** the spec named the TTS driver `tools/tts.sh`. This plan implements it as `tools/tts.mjs` (Node) instead — Node is already a hard dependency (Remotion), and a Node script avoids fragile bash/awk parsing and `jq`. Functionally identical, run via `npm run tts`.

---

## File Structure

```
video/
├── package.json                 ← Remotion + deps, npm scripts (isolated toolchain)
├── tsconfig.json                ← TS config (resolveJsonModule on)
├── .gitignore                   ← node_modules, out/, generated tts wavs
├── remotion.config.ts           ← codec/overwrite config
├── scripts/
│   ├── flowhub-users.de.md      ← end-user narration (scene-marked)
│   └── flowhub-technical.de.md  ← technical narration (scene-marked)
├── tools/
│   ├── tts.mjs                  ← Piper + ffprobe → public/audio/tts/*.wav + src/durations.json
│   └── voices/                  ← (gitignored) place Piper .onnx model here
├── public/
│   └── audio/
│       ├── music/bed.mp3        ← background track (silent placeholder committed)
│       └── tts/                 ← (gitignored) generated narration wavs
├── src/
│   ├── index.ts                 ← registerRoot entry
│   ├── Root.tsx                 ← registers both Compositions, computes durations
│   ├── theme.ts                 ← shared colors/fonts
│   ├── durations.json           ← committed placeholder; overwritten by tts.mjs
│   ├── components/
│   │   ├── SceneFrame.tsx       ← AbsoluteFill bg + fade in/out
│   │   ├── Scene.tsx            ← narration <Audio> + SceneFrame wrapper
│   │   ├── TitleCard.tsx
│   │   ├── FeatureSlide.tsx
│   │   ├── BenefitList.tsx
│   │   ├── WorkflowDiagram.tsx
│   │   ├── ArchitectureDiagram.tsx
│   │   └── BadgeRow.tsx
│   ├── UserVideo.tsx            ← composition: flowhub-users
│   └── TechnicalVideo.tsx       ← composition: flowhub-technical
├── out/                         ← (gitignored) rendered mp4s
└── README.md                    ← setup + render instructions
```

All commands below run from the `video/` directory unless stated otherwise.

---

### Task 1: Scaffold the isolated Remotion project

**Files:**
- Create: `video/package.json`
- Create: `video/tsconfig.json`
- Create: `video/remotion.config.ts`
- Create: `video/.gitignore`
- Create: `video/src/index.ts`

- [ ] **Step 1: Create `video/package.json`**

```json
{
  "name": "flowhub-video",
  "private": true,
  "version": "1.0.0",
  "type": "module",
  "scripts": {
    "dev": "remotion studio src/index.ts",
    "tts": "node tools/tts.mjs",
    "typecheck": "tsc --noEmit",
    "render:users": "remotion render src/index.ts flowhub-users out/flowhub-users.de.mp4",
    "render:technical": "remotion render src/index.ts flowhub-technical out/flowhub-technical.de.mp4"
  },
  "dependencies": {
    "@remotion/cli": "^4.0.0",
    "react": "^18.3.1",
    "react-dom": "^18.3.1",
    "remotion": "^4.0.0"
  },
  "devDependencies": {
    "@types/react": "^18.3.0",
    "@types/react-dom": "^18.3.0",
    "typescript": "^5.5.0"
  }
}
```

- [ ] **Step 2: Create `video/tsconfig.json`**

```json
{
  "compilerOptions": {
    "target": "ESNext",
    "module": "ESNext",
    "moduleResolution": "Bundler",
    "jsx": "react-jsx",
    "strict": true,
    "esModuleInterop": true,
    "resolveJsonModule": true,
    "skipLibCheck": true,
    "noEmit": true,
    "lib": ["DOM", "DOM.Iterable", "ESNext"]
  },
  "include": ["src"]
}
```

- [ ] **Step 3: Create `video/remotion.config.ts`**

```ts
import {Config} from '@remotion/cli/config';

Config.setVideoImageFormat('jpeg');
Config.setCodec('h264');
Config.setOverwriteOutput(true);
```

- [ ] **Step 4: Create `video/.gitignore`**

```gitignore
node_modules/
out/
public/audio/tts/
tools/voices/
```

- [ ] **Step 5: Create `video/src/index.ts`**

```ts
import {registerRoot} from 'remotion';
import {RemotionRoot} from './Root';

registerRoot(RemotionRoot);
```

- [ ] **Step 6: Install dependencies**

Run (from `video/`): `npm install`
Expected: dependencies install, `node_modules/` created, no peer-dep errors that abort install.

- [ ] **Step 7: Commit**

```bash
git add video/package.json video/package-lock.json video/tsconfig.json video/remotion.config.ts video/.gitignore video/src/index.ts
git commit -m "chore(video): scaffold isolated Remotion project"
```

---

### Task 2: Shared theme and placeholder durations

**Files:**
- Create: `video/src/theme.ts`
- Create: `video/src/durations.json`

- [ ] **Step 1: Create `video/src/theme.ts`**

```ts
export const theme = {
  colors: {
    bg: '#1A1A2E',
    surface: '#16213E',
    primary: '#594AE2',
    secondary: '#FF4081',
    accent: '#00C9A7',
    text: '#FFFFFF',
    textMuted: '#B0B0C3',
  },
  fonts: {
    heading: '"Segoe UI", "Inter", system-ui, sans-serif',
    body: '"Segoe UI", "Inter", system-ui, sans-serif',
  },
} as const;
```

- [ ] **Step 2: Create `video/src/durations.json` (committed placeholder so the project renders before TTS is run; `tools/tts.mjs` overwrites it with real values)**

```json
{
  "users": {
    "hook": 5,
    "problem": 6,
    "solution": 5,
    "demo": 8,
    "benefits": 7,
    "close": 3
  },
  "technical": {
    "hook": 6,
    "architecture": 6,
    "airouting": 8,
    "integrations": 7,
    "stack": 7,
    "close": 3
  }
}
```

- [ ] **Step 3: Commit**

```bash
git add video/src/theme.ts video/src/durations.json
git commit -m "feat(video): add shared theme and placeholder scene durations"
```

---

### Task 3: Narration scripts

Scene markers are HTML comments `<!-- scene: <id> -->`; `tts.mjs` reads the text between markers. Scene ids MUST match the keys in `durations.json` and the audio filenames used by the compositions.

**Files:**
- Create: `video/scripts/flowhub-users.de.md`
- Create: `video/scripts/flowhub-technical.de.md`

- [ ] **Step 1: Create `video/scripts/flowhub-users.de.md`**

```markdown
# FlowHub — Erklärvideo (Endnutzer)

<!-- scene: hook -->
Eine Notiz hier, ein Link da, eine Aufgabe irgendwo. Den ganzen Tag landen Infoschnipsel an den unterschiedlichsten Orten.

<!-- scene: problem -->
Und dann beginnt die eigentliche Arbeit: sortieren, kopieren, einfügen — von einer App in die nächste. Das kostet Zeit und Nerven.

<!-- scene: solution -->
FlowHub macht damit Schluss. Du wirfst alles in einen einzigen Posteingang — den Rest erledigt FlowHub für dich.

<!-- scene: demo -->
Schreib „Inception – nochmal ansehen“, und es landet als Karte in deiner Filmliste. Schick einen Artikel-Link, und er wartet in deiner Leseliste.

<!-- scene: benefits -->
Das heißt für dich: weniger Copy-Paste, gesparte Zeit, und alles automatisch am richtigen Ort — komplett selbst gehostet und privat.

<!-- scene: close -->
FlowHub — dein intelligenter Posteingang.
```

- [ ] **Step 2: Create `video/scripts/flowhub-technical.de.md`**

```markdown
# FlowHub — Erklärvideo (Technisch)

<!-- scene: hook -->
FlowHub ist ein Integrations-Hub, der deine selbst gehosteten Dienste orchestriert — statt dass du sie alle einzeln bedienst.

<!-- scene: architecture -->
Im Kern ein modularer Monolith: getrennte Module für Domäne, KI, Persistenz, Integrationen und das Blazor-Web-Frontend.

<!-- scene: airouting -->
Jeder Capture durchläuft einen KI-Klassifikator auf Basis von Microsoft Extensions AI. Er erkennt die Kategorie und routet an die passende Skill-Integration.

<!-- scene: integrations -->
Über Ports und Adapter sprechen austauschbare Integrationen an: Wallabag für Lesezeichen, Vikunja für Aufgaben, Telegram als Eingangskanal.

<!-- scene: stack -->
Gebaut auf .NET 10 und Blazor, mit Entity Framework Core, OpenTelemetry, Health-Endpoints und einer durchgehenden Testabdeckung.

<!-- scene: close -->
FlowHub — modular, testbar, erweiterbar.
```

- [ ] **Step 3: Commit**

```bash
git add video/scripts/flowhub-users.de.md video/scripts/flowhub-technical.de.md
git commit -m "feat(video): add German narration scripts for both videos"
```

---

### Task 4: Shared visual components

**Files:**
- Create: `video/src/components/SceneFrame.tsx`
- Create: `video/src/components/Scene.tsx`
- Create: `video/src/components/TitleCard.tsx`
- Create: `video/src/components/FeatureSlide.tsx`
- Create: `video/src/components/BenefitList.tsx`
- Create: `video/src/components/WorkflowDiagram.tsx`
- Create: `video/src/components/ArchitectureDiagram.tsx`
- Create: `video/src/components/BadgeRow.tsx`

- [ ] **Step 1: Create `video/src/components/SceneFrame.tsx`** — background + fade in/out. `durationInFrames` is passed in because inside a `Series.Sequence`, `useVideoConfig()` returns the composition duration, not the sequence duration.

```tsx
import React from 'react';
import {AbsoluteFill, interpolate, useCurrentFrame} from 'remotion';
import {theme} from '../theme';

export const SceneFrame: React.FC<{
  durationInFrames: number;
  bg?: string;
  children: React.ReactNode;
}> = ({durationInFrames, bg, children}) => {
  const frame = useCurrentFrame();
  const opacity = interpolate(
    frame,
    [0, 12, durationInFrames - 12, durationInFrames],
    [0, 1, 1, 0],
    {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'},
  );
  return (
    <AbsoluteFill
      style={{
        backgroundColor: bg ?? theme.colors.bg,
        opacity,
        alignItems: 'center',
        justifyContent: 'center',
        fontFamily: theme.fonts.body,
        color: theme.colors.text,
        padding: 120,
      }}
    >
      {children}
    </AbsoluteFill>
  );
};
```

- [ ] **Step 2: Create `video/src/components/Scene.tsx`** — pairs a scene's narration audio with its visual frame.

```tsx
import React from 'react';
import {Audio, staticFile} from 'remotion';
import {SceneFrame} from './SceneFrame';

export const Scene: React.FC<{
  audio: string;
  durationInFrames: number;
  bg?: string;
  children: React.ReactNode;
}> = ({audio, durationInFrames, bg, children}) => (
  <>
    <Audio src={staticFile(audio)} />
    <SceneFrame durationInFrames={durationInFrames} bg={bg}>
      {children}
    </SceneFrame>
  </>
);
```

- [ ] **Step 3: Create `video/src/components/TitleCard.tsx`**

```tsx
import React from 'react';
import {spring, useCurrentFrame, useVideoConfig} from 'remotion';
import {theme} from '../theme';

export const TitleCard: React.FC<{title: string; subtitle?: string}> = ({
  title,
  subtitle,
}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const scale = spring({frame, fps, config: {damping: 200}});
  return (
    <div style={{textAlign: 'center', transform: `scale(${scale})`}}>
      <div
        style={{
          fontSize: 140,
          fontWeight: 800,
          fontFamily: theme.fonts.heading,
          color: theme.colors.primary,
        }}
      >
        {title}
      </div>
      {subtitle && (
        <div style={{fontSize: 48, color: theme.colors.textMuted, marginTop: 24}}>
          {subtitle}
        </div>
      )}
    </div>
  );
};
```

- [ ] **Step 4: Create `video/src/components/FeatureSlide.tsx`**

```tsx
import React from 'react';
import {theme} from '../theme';

export const FeatureSlide: React.FC<{
  headline: string;
  sub?: string;
  icon?: string;
}> = ({headline, sub, icon}) => (
  <div style={{textAlign: 'center', maxWidth: 1400}}>
    {icon && <div style={{fontSize: 120, marginBottom: 32}}>{icon}</div>}
    <div style={{fontSize: 84, fontWeight: 700, lineHeight: 1.1}}>{headline}</div>
    {sub && (
      <div style={{fontSize: 44, color: theme.colors.textMuted, marginTop: 28}}>
        {sub}
      </div>
    )}
  </div>
);
```

- [ ] **Step 5: Create `video/src/components/BenefitList.tsx`** — staggered slide-in list.

```tsx
import React from 'react';
import {interpolate, useCurrentFrame} from 'remotion';
import {theme} from '../theme';

export const BenefitList: React.FC<{
  title: string;
  items: {icon: string; label: string}[];
}> = ({title, items}) => {
  const frame = useCurrentFrame();
  return (
    <div style={{width: '100%', maxWidth: 1400}}>
      <div
        style={{fontSize: 72, fontWeight: 700, marginBottom: 48, textAlign: 'center'}}
      >
        {title}
      </div>
      <div style={{display: 'flex', flexDirection: 'column', gap: 28}}>
        {items.map((it, i) => {
          const appear = interpolate(frame, [i * 8, i * 8 + 12], [0, 1], {
            extrapolateLeft: 'clamp',
            extrapolateRight: 'clamp',
          });
          return (
            <div
              key={it.label}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 28,
                fontSize: 52,
                opacity: appear,
                transform: `translateX(${(1 - appear) * 40}px)`,
              }}
            >
              <span style={{fontSize: 60}}>{it.icon}</span>
              <span>{it.label}</span>
            </div>
          );
        })}
      </div>
    </div>
  );
};
```

- [ ] **Step 6: Create `video/src/components/WorkflowDiagram.tsx`** — staggered node chain with arrows.

```tsx
import React from 'react';
import {interpolate, useCurrentFrame} from 'remotion';
import {theme} from '../theme';

export const WorkflowDiagram: React.FC<{steps: string[]}> = ({steps}) => {
  const frame = useCurrentFrame();
  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 32,
        flexWrap: 'wrap',
      }}
    >
      {steps.map((s, i) => {
        const appear = interpolate(frame, [i * 12, i * 12 + 12], [0, 1], {
          extrapolateLeft: 'clamp',
          extrapolateRight: 'clamp',
        });
        return (
          <React.Fragment key={s}>
            {i > 0 && (
              <span style={{fontSize: 64, color: theme.colors.accent, opacity: appear}}>
                →
              </span>
            )}
            <div
              style={{
                opacity: appear,
                background: theme.colors.surface,
                border: `3px solid ${theme.colors.primary}`,
                borderRadius: 20,
                padding: '28px 36px',
                fontSize: 40,
                fontWeight: 600,
              }}
            >
              {s}
            </div>
          </React.Fragment>
        );
      })}
    </div>
  );
};
```

- [ ] **Step 7: Create `video/src/components/ArchitectureDiagram.tsx`** — module grid.

```tsx
import React from 'react';
import {interpolate, useCurrentFrame} from 'remotion';
import {theme} from '../theme';

export const ArchitectureDiagram: React.FC<{title: string; modules: string[]}> = ({
  title,
  modules,
}) => {
  const frame = useCurrentFrame();
  return (
    <div style={{width: '100%', maxWidth: 1500, textAlign: 'center'}}>
      <div style={{fontSize: 64, fontWeight: 700, marginBottom: 48}}>{title}</div>
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(3, 1fr)',
          gap: 32,
        }}
      >
        {modules.map((m, i) => {
          const appear = interpolate(frame, [i * 6, i * 6 + 10], [0, 1], {
            extrapolateLeft: 'clamp',
            extrapolateRight: 'clamp',
          });
          return (
            <div
              key={m}
              style={{
                opacity: appear,
                background: theme.colors.surface,
                border: `3px solid ${theme.colors.primary}`,
                borderRadius: 18,
                padding: '36px 20px',
                fontSize: 40,
                fontWeight: 600,
              }}
            >
              {m}
            </div>
          );
        })}
      </div>
    </div>
  );
};
```

- [ ] **Step 8: Create `video/src/components/BadgeRow.tsx`** — pill badges.

```tsx
import React from 'react';
import {interpolate, useCurrentFrame} from 'remotion';
import {theme} from '../theme';

export const BadgeRow: React.FC<{title: string; badges: string[]}> = ({
  title,
  badges,
}) => {
  const frame = useCurrentFrame();
  return (
    <div style={{textAlign: 'center', maxWidth: 1500}}>
      <div style={{fontSize: 64, fontWeight: 700, marginBottom: 48}}>{title}</div>
      <div
        style={{display: 'flex', flexWrap: 'wrap', gap: 24, justifyContent: 'center'}}
      >
        {badges.map((b, i) => {
          const appear = interpolate(frame, [i * 5, i * 5 + 10], [0, 1], {
            extrapolateLeft: 'clamp',
            extrapolateRight: 'clamp',
          });
          return (
            <div
              key={b}
              style={{
                opacity: appear,
                background: theme.colors.primary,
                color: '#fff',
                borderRadius: 999,
                padding: '20px 36px',
                fontSize: 38,
                fontWeight: 600,
              }}
            >
              {b}
            </div>
          );
        })}
      </div>
    </div>
  );
};
```

- [ ] **Step 9: Commit**

```bash
git add video/src/components/
git commit -m "feat(video): add shared scene and slide components"
```

---

### Task 5: Compositions and root registration

**Files:**
- Create: `video/src/UserVideo.tsx`
- Create: `video/src/TechnicalVideo.tsx`
- Create: `video/src/Root.tsx`

- [ ] **Step 1: Create `video/src/UserVideo.tsx`**

```tsx
import React from 'react';
import {
  AbsoluteFill,
  Audio,
  Series,
  interpolate,
  staticFile,
  useVideoConfig,
} from 'remotion';
import durations from './durations.json';
import {theme} from './theme';
import {Scene} from './components/Scene';
import {TitleCard} from './components/TitleCard';
import {FeatureSlide} from './components/FeatureSlide';
import {WorkflowDiagram} from './components/WorkflowDiagram';
import {BenefitList} from './components/BenefitList';

export const UserVideo: React.FC = () => {
  const {fps} = useVideoConfig();
  const d = durations.users;
  const f = (sec: number) => Math.max(1, Math.round(sec * fps));
  return (
    <AbsoluteFill style={{backgroundColor: theme.colors.bg}}>
      <Audio
        loop
        src={staticFile('audio/music/bed.mp3')}
        volume={(fr) =>
          interpolate(fr, [0, 30], [0, 0.12], {extrapolateRight: 'clamp'})
        }
      />
      <Series>
        <Series.Sequence durationInFrames={f(d.hook)}>
          <Scene audio="audio/tts/users-hook.wav" durationInFrames={f(d.hook)}>
            <TitleCard title="FlowHub" subtitle="Notiz hier, Link da, To-do irgendwo…" />
          </Scene>
        </Series.Sequence>

        <Series.Sequence durationInFrames={f(d.problem)}>
          <Scene audio="audio/tts/users-problem.wav" durationInFrames={f(d.problem)}>
            <WorkflowDiagram steps={['Notiz', 'kopieren', 'App A', 'App B', 'App C']} />
          </Scene>
        </Series.Sequence>

        <Series.Sequence durationInFrames={f(d.solution)}>
          <Scene audio="audio/tts/users-solution.wav" durationInFrames={f(d.solution)}>
            <FeatureSlide
              icon="📥"
              headline="Ein Posteingang für alles"
              sub="Reinwerfen — FlowHub erledigt den Rest"
            />
          </Scene>
        </Series.Sequence>

        <Series.Sequence durationInFrames={f(d.demo)}>
          <Scene audio="audio/tts/users-demo.wav" durationInFrames={f(d.demo)}>
            <WorkflowDiagram steps={['„Inception“', '🎬', 'Filmliste']} />
          </Scene>
        </Series.Sequence>

        <Series.Sequence durationInFrames={f(d.benefits)}>
          <Scene audio="audio/tts/users-benefits.wav" durationInFrames={f(d.benefits)}>
            <BenefitList
              title="Was du davon hast"
              items={[
                {icon: '⏱️', label: 'Zeit gespart'},
                {icon: '🚫', label: 'Kein Copy-Paste mehr'},
                {icon: '🎯', label: 'Alles am richtigen Ort'},
                {icon: '🔒', label: 'Selbst gehostet & privat'},
              ]}
            />
          </Scene>
        </Series.Sequence>

        <Series.Sequence durationInFrames={f(d.close)}>
          <Scene audio="audio/tts/users-close.wav" durationInFrames={f(d.close)}>
            <TitleCard title="FlowHub" subtitle="Dein intelligenter Posteingang" />
          </Scene>
        </Series.Sequence>
      </Series>
    </AbsoluteFill>
  );
};
```

- [ ] **Step 2: Create `video/src/TechnicalVideo.tsx`**

```tsx
import React from 'react';
import {
  AbsoluteFill,
  Audio,
  Series,
  interpolate,
  staticFile,
  useVideoConfig,
} from 'remotion';
import durations from './durations.json';
import {theme} from './theme';
import {Scene} from './components/Scene';
import {TitleCard} from './components/TitleCard';
import {ArchitectureDiagram} from './components/ArchitectureDiagram';
import {WorkflowDiagram} from './components/WorkflowDiagram';
import {BadgeRow} from './components/BadgeRow';

export const TechnicalVideo: React.FC = () => {
  const {fps} = useVideoConfig();
  const d = durations.technical;
  const f = (sec: number) => Math.max(1, Math.round(sec * fps));
  return (
    <AbsoluteFill style={{backgroundColor: theme.colors.bg}}>
      <Audio
        loop
        src={staticFile('audio/music/bed.mp3')}
        volume={(fr) =>
          interpolate(fr, [0, 30], [0, 0.12], {extrapolateRight: 'clamp'})
        }
      />
      <Series>
        <Series.Sequence durationInFrames={f(d.hook)}>
          <Scene audio="audio/tts/technical-hook.wav" durationInFrames={f(d.hook)}>
            <TitleCard title="FlowHub" subtitle="Ein Integrations-Hub für deine Dienste" />
          </Scene>
        </Series.Sequence>

        <Series.Sequence durationInFrames={f(d.architecture)}>
          <Scene
            audio="audio/tts/technical-architecture.wav"
            durationInFrames={f(d.architecture)}
          >
            <ArchitectureDiagram
              title="Modularer Monolith"
              modules={['Core', 'AI', 'Persistence', 'Skills', 'Integrations', 'Web']}
            />
          </Scene>
        </Series.Sequence>

        <Series.Sequence durationInFrames={f(d.airouting)}>
          <Scene
            audio="audio/tts/technical-airouting.wav"
            durationInFrames={f(d.airouting)}
          >
            <WorkflowDiagram
              steps={['Capture', 'KI-Klassifikator', 'Skill-Integration', 'Zieldienst']}
            />
          </Scene>
        </Series.Sequence>

        <Series.Sequence durationInFrames={f(d.integrations)}>
          <Scene
            audio="audio/tts/technical-integrations.wav"
            durationInFrames={f(d.integrations)}
          >
            <WorkflowDiagram steps={['Ports & Adapter', 'Wallabag', 'Vikunja', 'Telegram']} />
          </Scene>
        </Series.Sequence>

        <Series.Sequence durationInFrames={f(d.stack)}>
          <Scene audio="audio/tts/technical-stack.wav" durationInFrames={f(d.stack)}>
            <BadgeRow
              title="Stack & Qualität"
              badges={[
                '.NET 10',
                'Blazor',
                'EF Core',
                'OpenTelemetry',
                'Health-Endpoints',
                'Tests',
              ]}
            />
          </Scene>
        </Series.Sequence>

        <Series.Sequence durationInFrames={f(d.close)}>
          <Scene audio="audio/tts/technical-close.wav" durationInFrames={f(d.close)}>
            <TitleCard title="FlowHub" subtitle="Modular, testbar, erweiterbar" />
          </Scene>
        </Series.Sequence>
      </Series>
    </AbsoluteFill>
  );
};
```

- [ ] **Step 3: Create `video/src/Root.tsx`** — computes each composition's total length from `durations.json`.

```tsx
import React from 'react';
import {Composition} from 'remotion';
import durations from './durations.json';
import {UserVideo} from './UserVideo';
import {TechnicalVideo} from './TechnicalVideo';

const FPS = 30;

const sumSeconds = (o: Record<string, number>) =>
  Object.values(o).reduce((a, b) => a + b, 0);

const totalFrames = (o: Record<string, number>) =>
  Math.max(1, Math.round(sumSeconds(o) * FPS));

export const RemotionRoot: React.FC = () => (
  <>
    <Composition
      id="flowhub-users"
      component={UserVideo}
      durationInFrames={totalFrames(durations.users)}
      fps={FPS}
      width={1920}
      height={1080}
    />
    <Composition
      id="flowhub-technical"
      component={TechnicalVideo}
      durationInFrames={totalFrames(durations.technical)}
      fps={FPS}
      width={1920}
      height={1080}
    />
  </>
);
```

- [ ] **Step 4: Typecheck**

Run (from `video/`): `npm run typecheck`
Expected: no output / exit code 0 (no type errors).

- [ ] **Step 5: Create a silent placeholder music bed so the project renders before a real track is chosen**

Run (from `video/`):
```bash
mkdir -p public/audio/music public/audio/tts
ffmpeg -f lavfi -i anullsrc=r=44100:cl=stereo -t 90 -q:a 9 public/audio/music/bed.mp3
```
Expected: `public/audio/music/bed.mp3` created (~silent, 90s).

- [ ] **Step 6: Verify both compositions load in Studio**

Run (from `video/`): `npm run dev`
Expected: Remotion Studio opens (default http://localhost:3000) and lists two compositions: `flowhub-users` and `flowhub-technical`. Scrub the timeline — slides fade and animate; narration is silent (TTS not generated yet), which is expected. Stop the studio (Ctrl-C) when verified.

- [ ] **Step 7: Commit**

```bash
git add video/src/UserVideo.tsx video/src/TechnicalVideo.tsx video/src/Root.tsx public/audio/music/bed.mp3
git commit -m "feat(video): add user + technical compositions and root registration"
```

> Note: `public/audio/music/bed.mp3` lives under `video/public/...`. Adjust the `git add` path to `video/public/audio/music/bed.mp3` if your shell is at the repo root.

---

### Task 6: TTS pipeline (Piper → wavs + durations.json)

**Files:**
- Create: `video/tools/tts.mjs`

- [ ] **Step 1: Create `video/tools/tts.mjs`**

```js
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
  join(root, 'tools', 'voices', 'de_DE-thorsten-medium.onnx');

const SCRIPTS = [
  {key: 'users', file: join(root, 'scripts', 'flowhub-users.de.md')},
  {key: 'technical', file: join(root, 'scripts', 'flowhub-technical.de.md')},
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
  return Math.round(parseFloat(out) * 100) / 100;
}

const durations = {};
for (const {key, file} of SCRIPTS) {
  const scenes = parseScenes(readFileSync(file, 'utf8'));
  durations[key] = {};
  for (const scene of scenes) {
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
```

- [ ] **Step 2: Verify the script parses scenes correctly without invoking Piper**

Run (from `video/`):
```bash
node --input-type=module -e "
import {readFileSync} from 'node:fs';
const md = readFileSync('scripts/flowhub-users.de.md','utf8');
const re = /<!--\s*scene:\s*([a-z0-9_-]+)\s*-->/gi;
const ids = [...md.matchAll(re)].map(m=>m[1]);
console.log(ids.join(','));
"
```
Expected output: `hook,problem,solution,demo,benefits,close`

- [ ] **Step 3: Commit**

```bash
git add video/tools/tts.mjs
git commit -m "feat(video): add Piper TTS pipeline generating wavs and durations.json"
```

---

### Task 7: README, end-to-end render verification, and music

**Files:**
- Create: `video/README.md`
- Create: `video/public/audio/music/LICENSE.md`

- [ ] **Step 1: Create `video/README.md`**

````markdown
# FlowHub Explainer Videos

Two ~90s German explainer videos (end-user + technical) built with Remotion,
narrated by local Piper TTS, with a background music bed. Isolated from the
.NET solution — its own Node toolchain.

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
````

- [ ] **Step 2: Create `video/public/audio/music/LICENSE.md`**

```markdown
# Background music attribution

The committed `bed.mp3` is a silent placeholder generated with ffmpeg.

Before publishing, replace it with a royalty-free track and record here:

- **Track:** <title>
- **Artist:** <artist>
- **Source:** <url>
- **License:** <e.g. Pixabay Content License / CC-BY 4.0>
- **Attribution required:** <yes/no — exact text if yes>
```

- [ ] **Step 3: Generate narration (requires Piper installed)**

Run (from `video/`): `npm run tts`
Expected: console logs one `TTS <key>-<id>` line per scene (12 total), `public/audio/tts/` fills with 12 `.wav` files, and `src/durations.json` is rewritten with real per-scene seconds. If Piper is not yet installed, this step is blocked — install it (README step 2) before proceeding.

- [ ] **Step 4: Draft render to verify the full pipeline (fast, low-res)**

Run (from `video/`):
```bash
npx remotion render src/index.ts flowhub-users out/draft-users.mp4 --scale=0.5
```
Expected: render completes without asset-not-found errors and writes `out/draft-users.mp4`. Open it: slides are synced to narration, music plays quietly underneath. Repeat for `flowhub-technical` if desired.

- [ ] **Step 5: Commit**

```bash
git add video/README.md video/public/audio/music/LICENSE.md video/src/durations.json
git commit -m "docs(video): add README, music license note, real durations"
```

---

## Self-Review

**Spec coverage:**
- Isolated `video/` layout, two compositions, shared theme/components → Tasks 1, 2, 4, 5 ✔
- Script-first, audio-driven timing (ffprobe → durations.json → composition) → Tasks 2, 6 ✔
- Piper local German TTS → Task 6 ✔
- Both storyboards (6 scenes each, end-user + technical framing) → Tasks 3, 5 ✔
- Features & benefits thread, pitched per audience → narration (Task 3) + BenefitList/BadgeRow (Tasks 4, 5) ✔
- Music bed with ducking (low volume + fade-in) → Task 5 ✔
- Theme from MudBlazor-ish palette → Task 2 ✔
- Prerequisites (Node/Piper/ffprobe), feedback loop (Studio preview, draft render), licensing note → Tasks 1, 5, 7 ✔
- Risk: swappable TTS engine — `tts.mjs` honors `PIPER_BIN`/`PIPER_MODEL` and is the only TTS-coupled file ✔

**Placeholder scan:** No TBD/TODO left; every code step has complete content; the only intentional placeholders are the silent `bed.mp3` and the music `LICENSE.md` fields, both explicitly described as user-fillable.

**Type/name consistency:** Component props match usage — `FeatureSlide{headline,sub,icon}`, `BenefitList{title,items:{icon,label}[]}`, `WorkflowDiagram{steps}`, `ArchitectureDiagram{title,modules}`, `BadgeRow{title,badges}`, `Scene{audio,durationInFrames,bg?}`, `TitleCard{title,subtitle?}`. Scene ids (`hook/problem/solution/demo/benefits/close`, `hook/architecture/airouting/integrations/stack/close`) are consistent across scripts, `durations.json`, audio filenames, and compositions. `RemotionRoot` is exported and imported by `src/index.ts`.
