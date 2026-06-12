import {test} from 'node:test';
import assert from 'node:assert/strict';
import {captionFor} from '../demoCaptions.mjs';

test('known sample captions', () => {
  assert.equal(captionFor('sample', 'todo'), 'Sample: a to-do');
  assert.equal(captionFor('sample', 'movie'), 'Sample: a movie tip');
  assert.equal(captionFor('sample', 'zitat'), 'Sample: a quote');
  assert.equal(captionFor('sample', 'url'), 'Sample: a link');
});

test('known service captions', () => {
  assert.equal(captionFor('service', 'Vikunja'), 'Lands in Vikunja');
  assert.equal(captionFor('service', 'Zitate'), 'Lands in Vikunja · Zitate');
  assert.equal(captionFor('service', 'Wallabag'), 'Lands in Wallabag');
  assert.equal(captionFor('service', 'paperless-ngx'), 'Lands in paperless-ngx');
});

test('unknown key falls back, never throws', () => {
  assert.equal(captionFor('service', 'Nextcloud'), 'Lands in Nextcloud');
  assert.equal(captionFor('sample', 'weird'), 'Sample');
});
