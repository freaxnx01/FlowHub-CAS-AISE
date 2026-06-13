import React from 'react';
import {interpolate, useCurrentFrame} from 'remotion';
import {theme} from '../theme';

export const BenefitList: React.FC<{
  title: string;
  items: {icon: string; label: string}[];
}> = ({title, items}) => {
  const frame = useCurrentFrame();
  return (
    <div style={{width: '100%', maxWidth: 1400}}>
      <div
        style={{fontSize: 72, fontWeight: 700, marginBottom: 48, textAlign: 'center'}}
      >
        {title}
      </div>
      <div style={{display: 'flex', flexDirection: 'column', gap: 28}}>
        {items.map((it, i) => {
          const appear = interpolate(frame, [i * 8, i * 8 + 12], [0, 1], {
            extrapolateLeft: 'clamp',
            extrapolateRight: 'clamp',
          });
          return (
            <div
              key={`${i}-${it.label}`}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: 28,
                fontSize: 52,
                opacity: appear,
                transform: `translateX(${(1 - appear) * 40}px)`,
              }}
            >
              <span style={{fontSize: 60}}>{it.icon}</span>
              <span>{it.label}</span>
            </div>
          );
        })}
      </div>
    </div>
  );
};
