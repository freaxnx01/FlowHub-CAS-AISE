import React from 'react';
import {interpolate, useCurrentFrame} from 'remotion';
import {theme} from '../theme';

export const BadgeRow: React.FC<{title: string; badges: string[]}> = ({
  title,
  badges,
}) => {
  const frame = useCurrentFrame();
  return (
    <div style={{textAlign: 'center', maxWidth: 1500}}>
      <div style={{fontSize: 64, fontWeight: 700, marginBottom: 48}}>{title}</div>
      <div
        style={{display: 'flex', flexWrap: 'wrap', gap: 24, justifyContent: 'center'}}
      >
        {badges.map((b, i) => {
          const appear = interpolate(frame, [i * 5, i * 5 + 10], [0, 1], {
            extrapolateLeft: 'clamp',
            extrapolateRight: 'clamp',
          });
          return (
            <div
              key={`${i}-${b}`}
              style={{
                opacity: appear,
                background: theme.colors.primary,
                color: '#fff',
                borderRadius: 999,
                padding: '20px 36px',
                fontSize: 38,
                fontWeight: 600,
              }}
            >
              {b}
            </div>
          );
        })}
      </div>
    </div>
  );
};
