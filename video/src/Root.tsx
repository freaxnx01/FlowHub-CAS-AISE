import React from 'react';
import {Composition} from 'remotion';
import durations from './durations.json';
import {UserVideo} from './UserVideo';
import {TechnicalVideo} from './TechnicalVideo';

const FPS = 30;

// Frames for one scene — MUST match the `f` helper used inside the compositions
// (Math.max(1, Math.round(sec * fps))) so the composition length equals the
// sum of the Series.Sequence lengths exactly, even for fractional-second durations.
const sceneFrames = (seconds: number) => Math.max(1, Math.round(seconds * FPS));

const totalFrames = (scenes: Record<string, number>) =>
  Object.values(scenes).reduce((sum, seconds) => sum + sceneFrames(seconds), 0);

export const RemotionRoot: React.FC = () => (
  <>
    <Composition
      id="flowhub-users"
      component={UserVideo}
      durationInFrames={totalFrames(durations.users as Record<string, number>)}
      fps={FPS}
      width={1920}
      height={1080}
    />
    <Composition
      id="flowhub-technical"
      component={TechnicalVideo}
      durationInFrames={totalFrames(durations.technical as Record<string, number>)}
      fps={FPS}
      width={1920}
      height={1080}
    />
  </>
);
