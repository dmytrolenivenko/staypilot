// Runs automatically on the /mapa sub-page that background.js opens on the ad tab's behalf.
// Reads coordinates off the Google Maps link's href and reports them the instant they appear.
//
// Timing/retries are NOT this script's job anymore — background.js owns the tab's whole
// lifecycle (see MAP_TAB_TIMEOUT_MS there): if this script hasn't reported coordinates within
// a few seconds, background.js kills this tab outright and opens a brand new one for the same
// URL rather than reloading it. A fresh tab/process sidesteps whatever state (stuck Didomi
// consent flow, throttled background renderer) a same-tab reload could get stuck carrying
// forward. So this script only ever needs to do one thing: poll fast and report the moment it
// finds a usable link, right up until the tab is closed out from under it.
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
  var POLL_INTERVAL_MS = 300;

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
  // this page frequently gets killed by background.js mid-poll, so its own DevTools console
  // is nearly impossible to open in time. background.js's service worker console stays put —
  // open it from chrome://extensions to see every line below.
  function debug() {
    var args = Array.prototype.slice.call(arguments);
    chrome.runtime.sendMessage({ type: 'debug', data: args.join(' ') });
  }

  function checkOnce() {
    var mapHref = findMapHref();
    if (!mapHref) {
      setTimeout(checkOnce, POLL_INTERVAL_MS);
      return;
    }

    var sourceUrl = location.href.replace(/\/mapa\/?.*$/, '/');
    var approximate = document.body.innerText.indexOf(PATTERNS.approximateLocationMarker) !== -1;
    debug('[map] found coordinates:', sourceUrl);
    chrome.runtime.sendMessage({
      type: 'coords',
      data: { sourceUrl: sourceUrl, mapHref: mapHref, approximate: approximate }
    });
    // No 'close-self' here — background.js closes this tab itself as soon as it processes
    // the 'coords' message above, since it's the one tracking which tab belongs to this job.
  }

  setTimeout(checkOnce, 300); // don't even bother checking before the page has had a moment to start rendering
})();
