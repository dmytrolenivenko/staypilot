// Runs automatically on every Idealista ad page you open (or that the crawl opens for you).
// Reads the same fields the app needs, then hands them to background.js, which forwards
// them to AddProperty.
//
// Coordinates FIRST, then the ad: this page asks background.js for the /mapa coordinates
// and does nothing else until they come back (success or a genuine give-up). Extracting the
// ad's own fields is cheap and instant, so there's no reason to do it for a listing whose
// coordinates might still fail — an ad is never sent to AddProperty without a location
// already attached, and one that never gets coordinates is skipped outright rather than
// saved incomplete. background.js owns the actual map-tab retry loop; this page just waits.
//
// SELECTORS below is the ONLY thing that should need editing if Idealista changes its page
// markup and captures start coming back empty — see IdealistaLocators.cs in the C# project
// for the equivalent "if parsing breaks, start here" note about the C# side.
var SELECTORS = {
  price: '.info-data-price',
  title: 'h1',
  location: '.main-info__title-minor',
  ref: '.txt-ref',
  features: '.details-property-feature-one li, .details-property_features li',
  descriptionPrimary: '.comment',
  descriptionFallback: '.adCommentsLanguage',
  energyIcon: '[class*="icon-energy"]'
};

(function () {
  function q(sel) { return document.querySelector(sel); }

  var sourceUrl = location.href.replace(/\/$/, '') + '/';
  var mapUrl = sourceUrl + 'mapa';

  function moveOnIfCrawling(delayMs) {
    chrome.storage.local.get(['crawling'], function (state) {
      if (!state.crawling) return;
      setTimeout(function () {
        chrome.runtime.sendMessage({ type: 'open-next' });
        chrome.runtime.sendMessage({ type: 'close-self' });
      }, delayMs);
    });
  }

  function extractAndSend(coords) {
    var priceEl = q(SELECTORS.price);
    var titleEl = q(SELECTORS.title);
    var locEl = q(SELECTORS.location);
    var refEl = q(SELECTORS.ref);
    var features = Array.prototype.map.call(
      document.querySelectorAll(SELECTORS.features),
      function (li) { return li.innerText; }
    );
    var descEl = q(SELECTORS.descriptionPrimary) || q(SELECTORS.descriptionFallback);
    var energyEl = q(SELECTORS.energyIcon);

    var data = {
      sourceUrl: sourceUrl,
      price: priceEl ? priceEl.innerText : '',
      title: titleEl ? titleEl.innerText : '',
      location: locEl ? locEl.innerText : '',
      ref: refEl ? refEl.innerText : '',
      features: features,
      desc: descEl ? descEl.innerText : '',
      energyClassName: energyEl ? energyEl.className : '',
      mapHref: coords.mapHref,
      approximate: !!coords.approximate
    };

    chrome.runtime.sendMessage({ type: 'ad', data: data });

    // Crawl mode: if you're not manually browsing but let the extension drive itself
    // (toggled via the toolbar icon), this tab has done its job — move on to the next
    // listing and close this one. In normal (non-crawl) use, this does nothing and the
    // tab just stays open like it always did.
    moveOnIfCrawling(1000 + Math.floor(Math.random() * 2000));
  }

  chrome.runtime.onMessage.addListener(function (message) {
    if (message.type === 'coords-ready') {
      extractAndSend(message.data);
    } else if (message.type === 'coords-failed') {
      chrome.runtime.sendMessage({ type: 'debug', data: '[ad] skipping (coordinates never loaded): ' + sourceUrl });
      moveOnIfCrawling(500);
    }
  });

  // Small random delay so this doesn't fire at a suspiciously identical instant
  // on every page — mimics the natural pause before a person starts reading.
  setTimeout(function () {
    chrome.runtime.sendMessage({ type: 'need-coords', sourceUrl: sourceUrl, mapUrl: mapUrl });
  }, 1500 + Math.floor(Math.random() * 1500));
})();
