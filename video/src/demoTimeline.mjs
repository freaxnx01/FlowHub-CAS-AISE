// Pure layout/timeline math for the demo-walkthrough composition.

/**
 * @param {{x:number,y:number,click?:boolean}} cursor
 * @param {{width:number,height:number}} from
 * @param {{width:number,height:number}} to
 */
export function scaleCursor(cursor, from, to) {
  return {
    x: Math.round((cursor.x * to.width) / from.width),
    y: Math.round((cursor.y * to.height) / from.height),
    click: !!cursor.click,
  };
}

/**
 * Flat scene list with absolute start frames. Scene types:
 *  intro | outro | section (has `title`) | shot (has `shot`, maybe `cursorTarget`).
 */
export function buildTimeline(shots, fps, opts) {
  const frames = (sec) => Math.max(1, Math.round(sec * fps));
  const scenes = [];
  let start = 0;
  const push = (scene) => {
    scene.startFrame = start;
    start += scene.durationInFrames;
    scenes.push(scene);
  };

  push({type: 'intro', durationInFrames: frames(opts.intro)});

  let prevSection = 'intro';
  for (const shot of shots) {
    if (shot.section !== prevSection && opts.sectionTitles[shot.section]) {
      push({type: 'section', title: opts.sectionTitles[shot.section], durationInFrames: frames(opts.section)});
    }
    prevSection = shot.section;
    const sec = shot.kind === 'result' ? opts.result : shot.kind === 'action' ? opts.action : opts.context;
    push({
      type: 'shot',
      shot,
      cursorTarget: shot.cursor ?? null,
      durationInFrames: frames(sec),
    });
  }

  push({type: 'outro', durationInFrames: frames(opts.outro)});
  return scenes;
}

function lerp(a, b, t) {
  return a + (b - a) * t;
}
function clamp01(t) {
  return Math.max(0, Math.min(1, t));
}

/**
 * Cursor position at an absolute frame.
 * @returns {{x:number,y:number,visible:boolean,clickT:number}}
 */
export function cursorAt(frame, scenes, opts) {
  const targets = scenes.filter((s) => s.cursorTarget);
  if (targets.length === 0) return {x: opts.rest.x, y: opts.rest.y, visible: false, clickT: 0};

  let curIdx = -1;
  for (let i = 0; i < targets.length; i++) {
    if (targets[i].startFrame <= frame) curIdx = i;
  }
  if (curIdx === -1) {
    return {x: opts.rest.x, y: opts.rest.y, visible: true, clickT: 0};
  }
  const cur = targets[curIdx];
  const prevPos = curIdx === 0 ? opts.rest : targets[curIdx - 1].cursorTarget;
  const glideStart = cur.startFrame;
  const glideEnd = cur.startFrame + opts.glideFrames;
  const gt = clamp01((frame - glideStart) / Math.max(1, glideEnd - glideStart));
  const x = Math.round(lerp(prevPos.x, cur.cursorTarget.x, gt));
  const y = Math.round(lerp(prevPos.y, cur.cursorTarget.y, gt));

  const clickStart = cur.startFrame + cur.durationInFrames - opts.clickFrames;
  let clickT = 0;
  if (cur.cursorTarget.click && frame >= clickStart) {
    clickT = clamp01((frame - clickStart) / Math.max(1, opts.clickFrames));
  }
  return {x, y, visible: true, clickT};
}
