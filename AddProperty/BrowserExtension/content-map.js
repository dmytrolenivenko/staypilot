// Runs automatically on the /mapa sub-page that content-ad.js auto-opens.
// Reads coordinates off the Google Maps link's href, reports them, then closes this
// tab — you never need to look at it.
//
// PATTERNS below is what to edit if Idealista changes this page's wording or link format.
var PATTERNS = {
  // The advertiser can opt out of showing an exact pin; this exact Portuguese phrase is
  // how the page says so. Must match IdealistaLocators.Map's expectations on the C# side
  // in spirit (both are looking for "was this exact or approximate"), even though this
  // specific string only lives here.
  approximateLocationMarker: 'não indicou a localização exata',
  googleMapsHref: /google\.com\/maps|maps\.google/i,
  // Coordinates appear as either "@lat,lng" (e.g. the "Report a map error" link) or
  // "ll=lat,lng" (e.g. the "Open this area in Google Maps" link) — match either form.
  coordinatesInHref: /(?:@|ll=)-?\d{1,3}\.\d{3,8},-?\d{1,3}\.\d{3,8}/
};

(function () {
  // This tab is opened as a background tab (doesn't steal your focus), and Chrome throttles
  // JS in background tabs to save resources — the map widget can take much longer than a
  // fixed guess to actually render its overlay controls. So poll for the link instead of
  // waiting a blind fixed delay: proceed the instant it's ready, give up after MAX_WAIT_MS
  // if it genuinely never appears (e.g. the listing truly has no location shown at all).
  var POLL_INTERVAL_MS = 1500;
  var MAX_WAIT_MS = 25000;
  var elapsedMs = 0;

  function findMapHref() {
    var links = Array.prototype.map.call(document.querySelectorAll('a'), function (a) { return a.href; });
    for (var i = 0; i < links.length; i++) {
      if (PATTERNS.googleMapsHref.test(links[i]) && PATTERNS.coordinatesInHref.test(links[i])) {
        return links[i];
      }
    }
    return null;
  }

  // Routed through background.js (via a plain runtime message) instead of console.log here:
  // this page is a 1x1 popup that closes itself within seconds of finishing, so its own
  // DevTools console is nearly impossible to open in time. background.js's service worker
  // console stays put — open it from chrome://extensions to see every line below.
  function debug() {
    var args = Array.prototype.slice.call(arguments);
    chrome.runtime.sendMessage({ type: 'debug', data: args.join(' ') });
  }

  // ONE-TIME dump of everything that could plausibly be "the map": every google-ish href
  // regardless of our coordinate regex, every iframe src (a Google Maps embed is usually an
  // <iframe>, not an <a> — our regex would never see that), and any cookie-consent overlay
  // that might be sitting on top of the widget and preventing it from ever initializing.
  function dumpPageOnce() {
    var allHrefs = Array.prototype.map.call(document.querySelectorAll('a'), function (a) { return a.href; });
    var googleish = allHrefs.filter(function (h) { return /google/i.test(h); });
    var iframeSrcs = Array.prototype.map.call(document.querySelectorAll('iframe'), function (f) { return f.src; });
    var consentIsh = Array.prototype.slice.call(document.querySelectorAll('[id*="consent" i],[class*="consent" i],[id*="cookie" i],[class*="cookie" i],[id*="didomi" i],[class*="qc-cmp" i]'))
      .map(function (el) { return el.tagName + (el.id ? '#' + el.id : '') + (el.className ? '.' + String(el.className).slice(0, 40) : ''); });
    var didomiStatus = 'no-window.Didomi';
    try {
      if (window.Didomi) didomiStatus = JSON.stringify(window.Didomi.getUserStatus ? window.Didomi.getUserStatus() : 'Didomi-present-no-getUserStatus');
    } catch (e) {
      didomiStatus = 'error: ' + e.message;
    }
    debug('[map-dump]', location.href,
      'googleHrefs=' + JSON.stringify(googleish),
      'iframeSrcs=' + JSON.stringify(iframeSrcs),
      'consentElements=' + JSON.stringify(consentIsh),
      'didomiStatus=' + didomiStatus);
  }

  function checkOnce() {
    var mapHref = findMapHref();
    debug('[map]', location.href,
      'elapsedMs=' + elapsedMs,
      'visibilityState=' + document.visibilityState,
      'hidden=' + document.hidden,
      'hasFocus=' + document.hasFocus(),
      'linkCount=' + document.querySelectorAll('a').length,
      'found=' + !!mapHref);
    if (elapsedMs === 0) dumpPageOnce();
    if (mapHref || elapsedMs >= MAX_WAIT_MS) {
      finish(mapHref, !mapHref);
      return;
    }
    elapsedMs += POLL_INTERVAL_MS;
    setTimeout(checkOnce, POLL_INTERVAL_MS);
  }

  function finish(mapHref, timedOut) {
    var sourceUrl = location.href.replace(/\/mapa\/?.*$/, '/');
    var approximate = document.body.innerText.indexOf(PATTERNS.approximateLocationMarker) !== -1;

    chrome.runtime.sendMessage({
      type: 'coords',
      data: { sourceUrl: sourceUrl, mapHref: mapHref, approximate: approximate, timedOut: !!timedOut }
    });

    chrome.runtime.sendMessage({ type: 'close-self' });
  }

  setTimeout(checkOnce, 2000); // don't even bother checking before the page has had a moment to start rendering
})();
