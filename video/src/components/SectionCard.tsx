import React from 'react';
import {AbsoluteFill, spring, useCurrentFrame, useVideoConfig} from 'remotion';
import {theme} from '../theme';

export const SectionCard: React.FC<{title: string}> = ({title}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const scale = spring({frame, fps, config: {damping: 200}});
  return (
    <AbsoluteFill style={{backgroundColor: theme.colors.logoBg, alignItems: 'center', justifyContent: 'center'}}>
      <div style={{fontFamily: theme.fonts.heading, fontWeight: 800, fontSize: 96, color: theme.colors.text, transform: `scale(${scale})`}}>
        {title}
      </div>
    </AbsoluteFill>
  );
};
