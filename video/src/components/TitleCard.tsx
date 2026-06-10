import React from 'react';
import {Img, spring, staticFile, useCurrentFrame, useVideoConfig} from 'remotion';
import {theme} from '../theme';

// Brand title card: the FlowHub logo. The logo PNG has a baked-in navy
// background (theme.colors.logoBg), so render it on a scene whose bg is logoBg
// to avoid a visible rectangle. `title` is kept as the image alt text.
export const TitleCard: React.FC<{title: string; subtitle?: string}> = ({
  title,
  subtitle,
}) => {
  const frame = useCurrentFrame();
  const {fps} = useVideoConfig();
  const scale = spring({frame, fps, config: {damping: 200}});
  return (
    <div style={{textAlign: 'center', transform: `scale(${scale})`}}>
      <Img
        src={staticFile('flowhub-logo.png')}
        alt={title}
        style={{width: 680, height: 'auto', display: 'block', margin: '0 auto'}}
      />
      {subtitle && (
        <div style={{fontSize: 48, color: theme.colors.textMuted, marginTop: 44}}>
          {subtitle}
        </div>
      )}
    </div>
  );
};
