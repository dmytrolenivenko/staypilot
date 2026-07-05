// Runs automatically on every Idealista ad page you open. Reads the same fields the app
// needs, then hands them to background.js, which forwards them to AddProperty.
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

  // Small random delay so this doesn't fire at a suspiciously identical instant
  // on every page — mimics the natural pause before a person starts reading.
  setTimeout(function () {
    var sourceUrl = location.href.replace(/\/$/, '') + '/';
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
      energyClassName: energyEl ? energyEl.className : ''
    };

    chrome.runtime.sendMessage({ type: 'ad', data: data });

    // Auto-open the coordinates sub-page in the background — you never have to
    // click into it yourself. Randomized delay again, same reasoning.
    var mapUrl = sourceUrl + 'mapa';
    var delay = 2000 + Math.floor(Math.random() * 3000);
    setTimeout(function () {
      chrome.runtime.sendMessage({ type: 'open-map', url: mapUrl });
    }, delay);
  }, 1500 + Math.floor(Math.random() * 1500));
})();
