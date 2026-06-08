import React from 'react';
import {AbsoluteFill, interpolate, useCurrentFrame} from 'remotion';
import {theme} from '../theme';

export const SceneFrame: React.FC<{
  durationInFrames: number;
  bg?: string;
  children: React.ReactNode;
}> = ({durationInFrames, bg, children}) => {
  const frame = useCurrentFrame();
  // Clamp the fade width so the input range stays strictly increasing even for
  // very short scenes (durationInFrames <= 24). fade < 1 → no fade, constant opacity.
  const fade = Math.min(12, Math.floor((durationInFrames - 1) / 2));
  const opacity =
    fade < 1
      ? 1
      : interpolate(
          frame,
          [0, fade, durationInFrames - fade, durationInFrames],
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
