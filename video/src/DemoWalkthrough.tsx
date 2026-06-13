import React from 'react';
import {AbsoluteFill, Audio, interpolate, Sequence, staticFile, useVideoConfig} from 'remotion';
import manifest from '../public/demo/manifest.json';
import {theme} from './theme';
import {TitleCard} from './components/TitleCard';
import {SectionCard} from './components/SectionCard';
import {DemoShot} from './components/DemoShot';
import {Cursor} from './components/Cursor';
import {buildTimeline, scaleCursor} from './demoTimeline.mjs';

const SECONDS = {intro: 2, outro: 2.5, section: 1.4, context: 2.2, result: 2.8, action: 2, glide: 0.5};
const SECTION_TITLES = {capture: 'Capture an inbox item', service: 'Where it lands'};

export const demoDurationInFrames = (fps: number): number => {
  const tl = buildTimeline(manifest.shots, fps, {...SECONDS, sectionTitles: SECTION_TITLES});
  return tl.reduce((n: number, s: {durationInFrames: number}) => n + s.durationInFrames, 0);
};

export const DemoWalkthrough: React.FC = () => {
  const {fps, width, height} = useVideoConfig();
  const scenes = buildTimeline(manifest.shots, fps, {...SECONDS, sectionTitles: SECTION_TITLES});
  const comp = {width, height};
  const cursorScenes = scenes.map((s) => ({
    startFrame: s.startFrame,
    durationInFrames: s.durationInFrames,
    cursorTarget: s.cursorTarget ? scaleCursor(s.cursorTarget, manifest.viewport, comp) : null,
  }));
  return (
    <AbsoluteFill style={{backgroundColor: theme.colors.bg}}>
      <Audio
        loop
        src={staticFile('audio/music/bed.mp3')}
        volume={(fr) => interpolate(fr, [0, 30], [0, 0.12], {extrapolateRight: 'clamp'})}
      />
      {scenes.map((s, i) => (
        <Sequence key={i} from={s.startFrame} durationInFrames={s.durationInFrames}>
          {s.type === 'intro' && <TitleCard title="FlowHub" subtitle="See it in action — the live demo" />}
          {s.type === 'outro' && <TitleCard title="FlowHub" subtitle="Try it: demo.flowhub.freaxnx01.ch" />}
          {s.type === 'section' && <SectionCard title={s.title as string} />}
          {s.type === 'shot' && s.shot && (
            <DemoShot file={s.shot.file} caption={s.shot.caption} durationInFrames={s.durationInFrames} />
          )}
        </Sequence>
      ))}
      <Cursor scenes={cursorScenes} glideFrames={Math.round(SECONDS.glide * fps)} clickFrames={10} rest={{x: width - 160, y: height - 120}} />
    </AbsoluteFill>
  );
};
