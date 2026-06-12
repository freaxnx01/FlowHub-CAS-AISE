// Ambient types for the plain-ESM helper imported by the TSX composition.
declare module '*demoTimeline.mjs' {
  export interface CursorTarget {
    x: number;
    y: number;
    click: boolean;
  }
  export interface TimelineScene {
    type: 'intro' | 'outro' | 'section' | 'shot';
    startFrame: number;
    durationInFrames: number;
    title?: string;
    shot?: {file: string; caption: string; [k: string]: unknown};
    cursorTarget?: CursorTarget | null;
  }
  export function scaleCursor(
    cursor: CursorTarget,
    from: {width: number; height: number},
    to: {width: number; height: number},
  ): CursorTarget;
  export function buildTimeline(
    shots: Array<Record<string, unknown>>,
    fps: number,
    opts: {
      intro: number; outro: number; section: number;
      context: number; result: number; action: number; glide: number;
      sectionTitles: Record<string, string>;
    },
  ): TimelineScene[];
  export function cursorAt(
    frame: number,
    scenes: Array<{startFrame: number; durationInFrames: number; cursorTarget?: CursorTarget | null}>,
    opts: {glideFrames: number; clickFrames: number; rest: {x: number; y: number}},
  ): {x: number; y: number; visible: boolean; clickT: number};
}
