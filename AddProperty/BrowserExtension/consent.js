// Idealista gates third-party scripts — including the Google Maps JS API that renders the
// coordinates link content-map.js depends on — behind a Didomi cookie-consent banner. Nothing
// in this automated flow can click "Accept", so without this, Maps never initializes at all
// and every coordinate capture fails, regardless of tab visibility/size/focus (that's what
// content-map.js's own diagnostics showed: googleHrefs never contained anything from
// maps.google.com, only Idealista's own Play Store link — the Maps script simply never ran).
//
// This is Didomi's own documented integration pattern for programmatic consent, not a button
// click or shadow-DOM workaround: push a callback onto didomiOnReady, which Didomi calls
// immediately if it's already initialized, or once it finishes initializing if not yet ready.
// Runs at document_start (its own manifest entry, separate from content-ad.js/content-map.js
// which need document_idle) to win the race against Didomi's own script as often as possible.
window.didomiOnReady = window.didomiOnReady || [];
window.didomiOnReady.push(function (Didomi) {
  try {
    Didomi.setUserAgreeToAll();
  } catch (e) {
    console.error('[Idealista Capture] Failed to auto-accept Didomi consent:', e);
  }
});
