// Runs on Idealista search-results pages. Does nothing unless "crawl mode" is on (toggle it
// with the extension's toolbar icon) — otherwise this is just a page you're looking at
// normally and the extension leaves it alone.
//
// When crawl mode is on: harvests every listing link on this page plus the URL of the next
// page, and hands them to background.js, which opens them one at a time (see content-ad.js
// for what happens on each of those, and background.js for the actual queue).
var SELECTORS = {
  listingLinks: 'article.item a.item-link'
};

(function () {
  console.log('[Idealista Capture] content-search.js loaded on', location.href);

  chrome.storage.local.get(['crawling'], function (state) {
    console.log('[Idealista Capture] Crawling flag on this search page:', state.crawling);
    if (!state.crawling) return;

    // Small delay so the page has actually finished rendering its results list.
    setTimeout(function () {
      var listingUrls = Array.prototype.map.call(
        document.querySelectorAll(SELECTORS.listingLinks),
        function (a) { return a.href; }
      );

      // Only claim a "next page" exists if THIS page actually had listings. Otherwise
      // we've gone past the end of the results, and background.js's "stop at the end"
      // check (which relies on nextPageUrl eventually being null) would never fire —
      // it would keep requesting emptier and emptier pages forever.
      var nextPageUrl = listingUrls.length > 0 ? getNextPageUrl() : null;

      chrome.runtime.sendMessage({
        type: 'search-page',
        data: { listingUrls: listingUrls, nextPageUrl: nextPageUrl }
      });
    }, 3000 + Math.floor(Math.random() * 3000));
  });

  // Idealista's pagination is just "/pagina-N/" in the URL (page 1 has no such segment at
  // all). Building the next page's URL directly from that is far more stable than hunting
  // for a "next page" button's CSS class, which is exactly the kind of thing that breaks
  // silently when a site redesigns its markup.
  function getNextPageUrl() {
    var match = location.pathname.match(/\/pagina-(\d+)\/?$/);
    var currentPage = match ? parseInt(match[1], 10) : 1;
    var nextPage = currentPage + 1;
    var basePath = location.pathname.replace(/\/pagina-\d+\/?$/, '').replace(/\/+$/, '');
    return location.origin + basePath + '/pagina-' + nextPage + location.search;
  }
})();
