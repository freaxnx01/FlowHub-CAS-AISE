// Shape of video/public/demo/manifest.json — the contract between
// tools/capture-demo.mjs (writer) and src/DemoWalkthrough.tsx (reader).

// The capture script only emits `intro` (home shot), `capture`, and `service`.
// `outro` is reserved: the composition synthesizes the closing logo card, so
// there is no `outro` shot today — it stays in the vocabulary for a future
// closing screenshot.
export const SECTIONS = ['intro', 'capture', 'service', 'outro'];
export const KINDS = ['context', 'result', 'action'];

/**
 * @param {any} m
 * @returns {{ok: boolean, errors: string[]}}
 */
export function validateManifest(m) {
  const errors = [];
  if (!m || typeof m !== 'object') return {ok: false, errors: ['manifest is not an object']};
  if (!m.viewport || m.viewport.width !== 1920 || m.viewport.height !== 1080) {
    errors.push('viewport must be 1920x1080');
  }
  if (!Array.isArray(m.shots) || m.shots.length === 0) {
    errors.push('shots must be a non-empty array');
    return {ok: false, errors};
  }
  m.shots.forEach((s, i) => {
    if (!s || typeof s !== 'object') return errors.push(`shot[${i}] is not an object`);
    if (!s.id) errors.push(`shot[${i}].id missing`);
    if (!s.file) errors.push(`shot[${i}].file missing`);
    if (!SECTIONS.includes(s.section)) errors.push(`shot[${i}].section invalid: ${s.section}`);
    if (!KINDS.includes(s.kind)) errors.push(`shot[${i}].kind invalid: ${s.kind}`);
    if (typeof s.caption !== 'string' || !s.caption) errors.push(`shot[${i}].caption missing`);
    if (s.cursor !== undefined) {
      const c = s.cursor;
      if (!c || typeof c.x !== 'number' || typeof c.y !== 'number') {
        errors.push(`shot[${i}].cursor x/y must be numbers`);
      }
    }
  });
  return {ok: errors.length === 0, errors};
}
