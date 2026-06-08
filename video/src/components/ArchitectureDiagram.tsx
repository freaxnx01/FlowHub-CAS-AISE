import React from 'react';
import {interpolate, useCurrentFrame} from 'remotion';
import {theme} from '../theme';

export const ArchitectureDiagram: React.FC<{title: string; modules: string[]}> = ({
  title,
  modules,
}) => {
  const frame = useCurrentFrame();
  return (
    <div style={{width: '100%', maxWidth: 1500, textAlign: 'center'}}>
      <div style={{fontSize: 64, fontWeight: 700, marginBottom: 48}}>{title}</div>
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(3, 1fr)',
          gap: 32,
        }}
      >
        {modules.map((m, i) => {
          const appear = interpolate(frame, [i * 6, i * 6 + 10], [0, 1], {
            extrapolateLeft: 'clamp',
            extrapolateRight: 'clamp',
          });
          return (
            <div
              key={`${i}-${m}`}
              style={{
                opacity: appear,
                background: theme.colors.surface,
                border: `3px solid ${theme.colors.primary}`,
                borderRadius: 18,
                padding: '36px 20px',
                fontSize: 40,
                fontWeight: 600,
              }}
            >
              {m}
            </div>
          );
        })}
      </div>
    </div>
  );
};
