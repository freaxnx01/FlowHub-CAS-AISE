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
          <Scene audio="audio/tts/technical-hook.wav" durationInFrames={f(d.hook)} bg={theme.colors.logoBg}>
            <TitleCard title="FlowHub" subtitle="An integration hub for your services" />
          </Scene>
        </Series.Sequence>

        <Series.Sequence durationInFrames={f(d.architecture)}>
          <Scene
            audio="audio/tts/technical-architecture.wav"
            durationInFrames={f(d.architecture)}
          >
            <ArchitectureDiagram
              title="Modular Monolith"
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
              steps={['Capture', 'AI classifier', 'Skill integration', 'Target service']}
            />
          </Scene>
        </Series.Sequence>

        <Series.Sequence durationInFrames={f(d.integrations)}>
          <Scene
            audio="audio/tts/technical-integrations.wav"
            durationInFrames={f(d.integrations)}
          >
            <WorkflowDiagram steps={['Ports & Adapters', 'Wallabag', 'Vikunja', 'Telegram']} />
          </Scene>
        </Series.Sequence>

        <Series.Sequence durationInFrames={f(d.stack)}>
          <Scene audio="audio/tts/technical-stack.wav" durationInFrames={f(d.stack)}>
            <BadgeRow
              title="Stack & Quality"
              badges={[
                '.NET 10',
                'Blazor',
                'EF Core',
                'OpenTelemetry',
                'Health endpoints',
                'Tests',
              ]}
            />
          </Scene>
        </Series.Sequence>

        <Series.Sequence durationInFrames={f(d.close)}>
          <Scene audio="audio/tts/technical-close.wav" durationInFrames={f(d.close)} bg={theme.colors.logoBg}>
            <TitleCard title="FlowHub" subtitle="Modular, testable, extensible" />
          </Scene>
        </Series.Sequence>
      </Series>
    </AbsoluteFill>
  );
};
