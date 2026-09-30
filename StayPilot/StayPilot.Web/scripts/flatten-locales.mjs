// Runs after `npm run build` (npm's postbuild hook).
//
// A localized build writes one folder per locale: browser/en-GB/ and browser/pt-PT/. The site has
// always been served from the root, and MSAL's redirectUri is '/', so English moves up to
// browser/ and Portuguese sits under it as browser/pt/ - the baseHrefs set in angular.json.
import { existsSync, readFileSync, readdirSync, renameSync, rmSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

const out = 'dist/stay-pilot.web/browser';
const source = join(out, 'en-GB');
const portuguese = join(out, 'pt-PT');
const parked = 'dist/stay-pilot.web/pt-parked';

// A non-localized build (e.g. development) already writes straight to browser/.
if (!existsSync(source)) {
  process.exit(0);
}

rmSync(parked, { recursive: true, force: true });
renameSync(portuguese, parked);

for (const entry of readdirSync(source)) {
  renameSync(join(source, entry), join(out, entry));
}

rmSync(source, { recursive: true });
renameSync(parked, join(out, 'pt'));

// Static Web Apps only reads the root config; a second copy under /pt/ would just mislead.
rmSync(join(out, 'pt', 'staticwebapp.config.json'), { force: true });

// Angular i18n never touches index.html, so the <title> and description are swapped here.
const ptIndex = join(out, 'pt', 'index.html');
const html = readFileSync(ptIndex, 'utf8')
  .replace(/<title>[^<]*<\/title>/, '<title>StayPilot — quanto Portugal está realmente a pedir</title>')
  .replace(
    /<meta name="description" content="[^"]*">/,
    '<meta name="description" content="Todos os anúncios de imóveis em Portugal, lidos como um único mercado. Medianas em vez de médias e dimensão da amostra sempre visível.">'
  );
writeFileSync(ptIndex, html);

console.log('Locales laid out: / (en-GB), /pt/ (pt-PT)');
