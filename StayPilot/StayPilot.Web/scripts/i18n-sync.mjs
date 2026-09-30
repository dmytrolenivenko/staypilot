// npm run i18n  →  ng extract-i18n, then this.
//
// Rebuilds messages.pt-PT.xlf from the fresh English extract (messages.xlf):
//   - keeps every existing Portuguese <target>, in the extract's order;
//   - takes new targets from any src/locale/parts/*.xlf (drop-in batches of translated units);
//   - drops units whose id no longer exists in the app;
//   - reports ids with no translation (they fall back to English) and ids whose English changed
//     since they were translated (the target is kept, but should be re-read);
//   - fails if a target's placeholders don't match its source — that would break at build time.
// ng extract-i18n never merges on its own, which is why this exists.
import { existsSync, readdirSync, readFileSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

const dir = 'src/locale';
const sourceFile = join(dir, 'messages.xlf');
const targetFile = join(dir, 'messages.pt-PT.xlf');
const partsDir = join(dir, 'parts');

function readUnits(file) {
  const units = new Map();
  const xml = readFileSync(file, 'utf8');

  for (const [, id, body] of xml.matchAll(/<unit id="([^"]+)">([\s\S]*?)<\/unit>/g)) {
    units.set(id, {
      source: body.match(/<source>([\s\S]*?)<\/source>/)?.[1] ?? '',
      target: body.match(/<target>([\s\S]*?)<\/target>/)?.[1] ?? null
    });
  }

  return units;
}

// The placeholder ids a segment uses (<ph id="0"/>, <pc id="1">), as a sorted list.
function placeholders(text) {
  return [...text.matchAll(/<(ph|pc) id="([^"]+)"/g)].map(m => `${m[1]}:${m[2]}`).sort().join(',');
}

const source = readUnits(sourceFile);
const existing = existsSync(targetFile) ? readUnits(targetFile) : new Map();
const incoming = new Map();

if (existsSync(partsDir)) {
  for (const file of readdirSync(partsDir).filter(f => f.endsWith('.xlf'))) {
    for (const [id, unit] of readUnits(join(partsDir, file))) {
      incoming.set(id, unit);
    }
  }
}

const missing = [];
const stale = [];
const broken = [];
const units = [];

for (const [id, { source: text }] of source) {
  const fresh = incoming.get(id);
  const kept = existing.get(id);
  const from = fresh?.target != null ? fresh : kept?.target != null ? kept : null;

  if (!from) {
    missing.push(id);
    units.push(`    <unit id="${id}">\n      <segment>\n        <source>${text}</source>\n      </segment>\n    </unit>`);
    continue;
  }

  if (from.source !== text) {
    stale.push(id);
  }

  if (placeholders(from.target) !== placeholders(text)) {
    broken.push(id);
  }

  units.push(
    `    <unit id="${id}">\n      <segment>\n        <source>${text}</source>\n        <target>${from.target}</target>\n      </segment>\n    </unit>`
  );
}

// Everything above <xliff> (the glossary comment) survives a rebuild.
const header = existsSync(targetFile)
  ? readFileSync(targetFile, 'utf8').split('<xliff')[0]
  : '<?xml version="1.0" encoding="UTF-8" ?>\n';

// --check reports without writing, so several people can validate a batch at once.
const checkOnly = process.argv.includes('--check');

if (!checkOnly) {
  writeFileSync(
    targetFile,
    `${header}<xliff version="2.0" xmlns="urn:oasis:names:tc:xliff:document:2.0" srcLang="en-GB" trgLang="pt-PT">\n` +
      `  <file id="ngi18n" original="ng.template">\n${units.join('\n')}\n  </file>\n</xliff>\n`
  );
}

const dropped = [...existing.keys()].filter(id => !source.has(id));

console.log(`pt-PT: ${source.size - missing.length}/${source.size} translated, ${dropped.length} dropped`);

for (const [label, ids] of [['Missing (shows English)', missing], ['English changed, re-check', stale], ['Placeholder mismatch', broken]]) {
  if (ids.length) {
    console.log(`\n${label}:\n  ${ids.join('\n  ')}`);
  }
}

if (broken.length) {
  process.exit(1);
}
