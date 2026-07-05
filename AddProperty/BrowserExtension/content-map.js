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
  coordinatesInHref: /@-?\d{1,3}\.\d{3,8},-?\d{1,3}\.\d{3,8}/
};

(function () {
  setTimeout(function () {
    var sourceUrl = location.href.replace(/\/mapa\/?.*$/, '/');
    var links = Array.prototype.map.call(document.querySelectorAll('a'), function (a) { return a.href; });
    var mapHref = null;
    for (var i = 0; i < links.length; i++) {
      if (PATTERNS.googleMapsHref.test(links[i]) && PATTERNS.coordinatesInHref.test(links[i])) {
        mapHref = links[i];
        break;
      }
    }
    var approximate = document.body.innerText.indexOf(PATTERNS.approximateLocationMarker) !== -1;

    chrome.runtime.sendMessage({
      type: 'coords',
      data: { sourceUrl: sourceUrl, mapHref: mapHref, approximate: approximate }
    });

    chrome.runtime.sendMessage({ type: 'close-self' });
  }, 8000); // give the map widget time to actually render before reading the link
})();
