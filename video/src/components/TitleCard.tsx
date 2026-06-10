import React from 'react';
import {spring, useCurrentFrame, useVideoConfig} from 'remotion';
import {theme} from '../theme';

export const TitleCard: React.FC<{title: string; subtitle?: string}> = ({
  title,
  subtitle,
}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const scale = spring({frame, fps, config: {damping: 200}});
  return (
    <div style={{textAlign: 'center', transform: `scale(${scale})`}}>
      <div
        style={{
          fontSize: 140,
          fontWeight: 800,
          fontFamily: theme.fonts.heading,
          color: theme.colors.primary,
        }}
      >
        {title}
      </div>
      {subtitle && (
        <div style={{fontSize: 48, color: theme.colors.textMuted, marginTop: 24}}>
          {subtitle}
        </div>
      )}
    </div>
  );
};
