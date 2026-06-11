const SAMPLE = {
  movie: 'Sample: a movie tip',
  zitat: 'Sample: a quote',
  todo: 'Sample: a to-do',
  url: 'Sample: a link',
};
const SERVICE = {
  Vikunja: 'Lands in Vikunja',
  Zitate: 'Lands in Vikunja · Zitate',
  Wallabag: 'Lands in Wallabag',
  'paperless-ngx': 'Lands in paperless-ngx',
};

/**
 * @param {'sample'|'service'} kind
 * @param {string} key
 * @returns {string}
 */
export function captionFor(kind, key) {
  if (kind === 'sample') return SAMPLE[key] ?? 'Sample';
  if (kind === 'service') return SERVICE[key] ?? `Lands in ${key}`;
  return key;
}
