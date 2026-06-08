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
