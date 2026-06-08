import React from 'react';
import {interpolate, useCurrentFrame} from 'remotion';
import {theme} from '../theme';

export const WorkflowDiagram: React.FC<{steps: string[]}> = ({steps}) => {
  const frame = useCurrentFrame();
  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 32,
        flexWrap: 'wrap',
      }}
    >
      {steps.map((s, i) => {
        const appear = interpolate(frame, [i * 12, i * 12 + 12], [0, 1], {
          extrapolateLeft: 'clamp',
          extrapolateRight: 'clamp',
        });
        return (
          <React.Fragment key={`${i}-${s}`}>
            {i > 0 && (
              <span style={{fontSize: 64, color: theme.colors.accent, opacity: appear}}>
                →
              </span>
            )}
            <div
              style={{
                opacity: appear,
                background: theme.colors.surface,
                border: `3px solid ${theme.colors.primary}`,
                borderRadius: 20,
                padding: '28px 36px',
                fontSize: 40,
                fontWeight: 600,
              }}
            >
              {s}
            </div>
          </React.Fragment>
        );
      })}
    </div>
  );
};
