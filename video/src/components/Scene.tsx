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
