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
            <WorkflowDiagram steps={['„Inception"', '🎬', 'Filmliste']} />
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
