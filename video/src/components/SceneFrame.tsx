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
