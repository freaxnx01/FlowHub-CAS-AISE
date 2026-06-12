import {test} from 'node:test';
import assert from 'node:assert/strict';
import {validateManifest, SECTIONS, KINDS} from '../../src/demoManifest.mjs';

const good = {
  capturedAt: '2026-06-11T10:00:00Z',
  viewport: {width: 1920, height: 1080},
  shots: [
    {id: 'home', file: 'demo/home.png', section: 'intro', kind: 'context', caption: 'The public demo'},
    {id: 'cap-todo-list', file: 'demo/cap-todo-list.png', section: 'capture', kind: 'result',
     caption: 'AI → todo → Vikunja', sample: 'todo'},
    {id: 'svc-vikunja', file: 'demo/svc-vikunja.png', section: 'service', kind: 'action',
     caption: 'Vikunja board', cursor: {x: 100, y: 200, click: true}},
  ],
};

test('valid manifest passes', () => {
  assert.deepEqual(validateManifest(good), {ok: true, errors: []});
});

test('rejects wrong viewport', () => {
  const m = {...good, viewport: {width: 1280, height: 720}};
  assert.equal(validateManifest(m).ok, false);
});

test('rejects empty shots', () => {
  assert.equal(validateManifest({...good, shots: []}).ok, false);
});

test('rejects bad section/kind and missing caption', () => {
  const m = {...good, shots: [{id: 'x', file: 'f', section: 'nope', kind: 'huh'}]};
  const r = validateManifest(m);
  assert.equal(r.ok, false);
  assert.ok(r.errors.some((e) => e.includes('section')));
  assert.ok(r.errors.some((e) => e.includes('kind')));
  assert.ok(r.errors.some((e) => e.includes('caption')));
});

test('rejects cursor without numeric x/y', () => {
  const m = {...good, shots: [{...good.shots[2], cursor: {x: 'a', y: 1}}]};
  assert.equal(validateManifest(m).ok, false);
});

test('exports the section/kind vocabularies', () => {
  assert.deepEqual(SECTIONS, ['intro', 'capture', 'service', 'outro']);
  assert.deepEqual(KINDS, ['context', 'result', 'action']);
});
