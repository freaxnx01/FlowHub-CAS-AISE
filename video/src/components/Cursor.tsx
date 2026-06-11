import React from 'react';
import {useCurrentFrame} from 'remotion';
import {cursorAt} from '../demoTimeline.mjs';

type Scene = {startFrame: number; durationInFrames: number; cursorTarget: {x: number; y: number; click: boolean} | null};

export const Cursor: React.FC<{
  scenes: Scene[];
  glideFrames: number;
  clickFrames: number;
  rest: {x: number; y: number};
}> = ({scenes, glideFrames, clickFrames, rest}) => {
  const frame = useCurrentFrame();
  const {x, y, visible, clickT} = cursorAt(frame, scenes, {glideFrames, clickFrames, rest});
  if (!visible) return null;
  const ringScale = clickT > 0 ? 1 + clickT * 1.6 : 0;
  const ringOpacity = clickT > 0 ? 0.5 * (1 - clickT) : 0;
  const pointerScale = clickT > 0 ? 1 - 0.18 * Math.sin(clickT * Math.PI) : 1;
  return (
    <div style={{position: 'absolute', left: x, top: y, transform: 'translate(-4px,-2px)', pointerEvents: 'none'}}>
      <div
        style={{
          position: 'absolute', left: 0, top: 0, width: 64, height: 64,
          marginLeft: -32, marginTop: -32, borderRadius: '50%',
          border: '4px solid #00C9A7', transform: `scale(${ringScale})`, opacity: ringOpacity,
        }}
      />
      <svg width="40" height="40" viewBox="0 0 24 24" style={{transform: `scale(${pointerScale})`, transformOrigin: '0 0'}}>
        <path d="M4 2 L4 20 L9 15 L12.5 22 L15 21 L11.5 14 L18 14 Z"
          fill="#FFFFFF" stroke="#1A1A2E" strokeWidth="1.5" strokeLinejoin="round" />
      </svg>
    </div>
  );
};
