import React from 'react';
import {AbsoluteFill, Img, interpolate, staticFile, useCurrentFrame} from 'remotion';
import {theme} from '../theme';

export const DemoShot: React.FC<{file: string; caption: string; durationInFrames: number}> = ({
  file,
  caption,
  durationInFrames,
}) => {
  const frame = useCurrentFrame();
  const scale = interpolate(frame, [0, durationInFrames], [1.04, 1.0], {extrapolateRight: 'clamp'});
  const fade = Math.min(8, Math.floor((durationInFrames - 1) / 2));
  const opacity =
    fade < 1 ? 1 : interpolate(frame, [0, fade, durationInFrames - fade, durationInFrames], [0, 1, 1, 0], {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'});
  return (
    <AbsoluteFill style={{backgroundColor: theme.colors.bg, opacity, alignItems: 'center', justifyContent: 'center'}}>
      <Img src={staticFile(file)} style={{width: '100%', height: '100%', objectFit: 'contain', transform: `scale(${scale})`}} />
      <div
        style={{
          position: 'absolute', bottom: 56, left: '50%', transform: 'translateX(-50%)',
          background: 'rgba(9,24,57,0.85)', color: theme.colors.text, fontFamily: theme.fonts.body,
          fontSize: 40, padding: '16px 36px', borderRadius: 14, border: `2px solid ${theme.colors.accent}`,
        }}
      >
        {caption}
      </div>
    </AbsoluteFill>
  );
};
