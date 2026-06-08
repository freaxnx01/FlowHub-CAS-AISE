import React from 'react';
import {theme} from '../theme';

export const FeatureSlide: React.FC<{
  headline: string;
  sub?: string;
  icon?: string;
}> = ({headline, sub, icon}) => (
  <div style={{textAlign: 'center', maxWidth: 1400}}>
    {icon && <div style={{fontSize: 120, marginBottom: 32}}>{icon}</div>}
    <div style={{fontSize: 84, fontWeight: 700, lineHeight: 1.1}}>{headline}</div>
    {sub && (
      <div style={{fontSize: 44, color: theme.colors.textMuted, marginTop: 28}}>
        {sub}
      </div>
    )}
  </div>
);
