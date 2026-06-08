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
