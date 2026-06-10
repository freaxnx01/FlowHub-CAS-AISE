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
          <Scene audio="audio/tts/users-hook.wav" durationInFrames={f(d.hook)} bg={theme.colors.logoBg}>
            <TitleCard title="FlowHub" subtitle="A note here, a link there, a to-do somewhere…" />
          </Scene>
        </Series.Sequence>

        <Series.Sequence durationInFrames={f(d.problem)}>
          <Scene audio="audio/tts/users-problem.wav" durationInFrames={f(d.problem)}>
            <WorkflowDiagram steps={['Note', 'copy', 'App A', 'App B', 'App C']} />
          </Scene>
        </Series.Sequence>

        <Series.Sequence durationInFrames={f(d.solution)}>
          <Scene audio="audio/tts/users-solution.wav" durationInFrames={f(d.solution)}>
            <FeatureSlide
              icon="📥"
              headline="One inbox for everything"
              sub="Drop it in — FlowHub does the rest"
            />
          </Scene>
        </Series.Sequence>

        <Series.Sequence durationInFrames={f(d.demo)}>
          <Scene audio="audio/tts/users-demo.wav" durationInFrames={f(d.demo)}>
            <WorkflowDiagram steps={['"Inception"', '🎬', 'Movie list']} />
          </Scene>
        </Series.Sequence>

        <Series.Sequence durationInFrames={f(d.benefits)}>
          <Scene audio="audio/tts/users-benefits.wav" durationInFrames={f(d.benefits)}>
            <BenefitList
              title="What you get"
              items={[
                {icon: '⏱️', label: 'Time saved'},
                {icon: '🚫', label: 'No more copy-paste'},
                {icon: '🎯', label: 'Everything in its place'},
                {icon: '🔒', label: 'Self-hosted & private'},
              ]}
            />
          </Scene>
        </Series.Sequence>

        <Series.Sequence durationInFrames={f(d.close)}>
          <Scene audio="audio/tts/users-close.wav" durationInFrames={f(d.close)} bg={theme.colors.logoBg}>
            <TitleCard title="FlowHub" subtitle="Your smart inbox" />
          </Scene>
        </Series.Sequence>
      </Series>
    </AbsoluteFill>
  );
};
