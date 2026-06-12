import {test} from 'node:test';
import assert from 'node:assert/strict';
import {scaleCursor, buildTimeline, cursorAt} from '../../src/demoTimeline.mjs';

const FPS = 30;
const OPTS = {
  intro: 2, outro: 2, section: 1.5,
  context: 2.5, result: 3.5, action: 3, glide: 0.5,
  sectionTitles: {capture: 'Capture an inbox item', service: 'Where it lands'},
};

const shots = [
  {id: 'home', section: 'intro', kind: 'context', caption: 'demo'},
  {id: 'cap-todo-list', section: 'capture', kind: 'result', caption: 'a',
   cursor: {x: 960, y: 540, click: true}},
  {id: 'svc-vikunja', section: 'service', kind: 'result', caption: 'b'},
];

test('scaleCursor maps proportionally and rounds', () => {
  assert.deepEqual(
    scaleCursor({x: 480, y: 270, click: true}, {width: 1920, height: 1080}, {width: 960, height: 540}),
    {x: 240, y: 135, click: true},
  );
});

test('buildTimeline brackets with intro/outro and inserts section cards', () => {
  const tl = buildTimeline(shots, FPS, OPTS);
  const types = tl.map((s) => s.type);
  assert.deepEqual(types, ['intro', 'shot', 'section', 'shot', 'section', 'shot', 'outro']);
  assert.equal(tl.find((s) => s.type === 'section').title, 'Capture an inbox item');
});

test('buildTimeline start frames are contiguous and summed', () => {
  const tl = buildTimeline(shots, FPS, OPTS);
  let cursor = 0;
  for (const s of tl) {
    assert.equal(s.startFrame, cursor);
    assert.ok(s.durationInFrames >= 1);
    cursor += s.durationInFrames;
  }
  assert.equal(cursor, Math.round((2 + 2.5 + 1.5 + 3.5 + 1.5 + 3.5 + 2) * FPS));
});

test('cursorAt rests before first target then reaches target after glide', () => {
  const tl = buildTimeline(shots, FPS, OPTS);
  const target = tl.find((s) => s.cursorTarget);
  const rest = {x: 1700, y: 1000};
  const opts = {glideFrames: Math.round(OPTS.glide * FPS), clickFrames: 10, rest};
  const start = cursorAt(0, tl, opts);
  assert.deepEqual({x: start.x, y: start.y}, rest);
  const atTarget = cursorAt(target.startFrame + opts.glideFrames, tl, opts);
  assert.equal(atTarget.x, target.cursorTarget.x);
  assert.equal(atTarget.y, target.cursorTarget.y);
  const clicking = cursorAt(target.startFrame + target.durationInFrames - 1, tl, opts);
  assert.ok(clicking.clickT > 0);
});
