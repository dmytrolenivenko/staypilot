// The extension's coordination point. Content scripts message this background script for
// anything that needs the extension's own permissions: opening/closing tabs, or making the
// cross-origin request to localhost.
const LISTENER_URL = 'http://localhost:5099/capture';
const SEARCH_URL_PATTERN = /idealista\.pt\/comprar-casas\/faro-distrito\/com-apartamentos/;

// ── Crawl mode ───────────────────────────────────────────────────────────
// Off by default. Click the toolbar icon to turn it on/off — a badge ("ON") shows the
// current state. When on, the extension opens the next listing and the next search-results
// page itself (content-search.js harvests links, this queues and opens them one at a time).
// When off, everything behaves exactly like manual use: you open tabs, it just captures them.
//
// The queue lives in chrome.storage.local, not a plain variable, because Manifest V3
// background scripts get shut down when idle — anything held only in memory (including a
// pending setTimeout) can silently disappear. Storage survives that; all the actual delays
// happen in content scripts on live tabs instead, which don't have this problem.

console.log('[Idealista Capture] background.js loaded and running.');

chrome.action.onClicked.addListener(async function (tab) {
  console.log('[Idealista Capture] Toolbar icon clicked. Current tab:', tab && tab.url);

  const state = await chrome.storage.local.get(['crawling']);
  const nowCrawling = !state.crawling;
  await chrome.storage.local.set({
    crawling: nowCrawling,
    crawlQueue: [],
    nextPageUrl: null,
    searchTabId: null
  });
  chrome.action.setBadgeText({ text: nowCrawling ? 'ON' : '' });
  chrome.action.setBadgeBackgroundColor({ color: '#2e7d32' });

  console.log('[Idealista Capture] Crawling is now:', nowCrawling);

  // If you're already sitting on a matching search-results page when you turn crawling on,
  // reload it so content-search.js actually runs with the new state — otherwise nothing
  // would happen until you manually refreshed, and it would look like the extension is broken.
  var isOnSearchPage = tab && tab.url && SEARCH_URL_PATTERN.test(tab.url);
  console.log('[Idealista Capture] Is current tab a matching search page?', isOnSearchPage);

  if (nowCrawling && isOnSearchPage) {
    console.log('[Idealista Capture] Reloading current tab to kick off the crawl.');
    chrome.tabs.reload(tab.id);
  }
});

// ── Map-tab visibility ──────────────────────────────────────────────────
// Idealista gates the Google Maps script (and the Didomi consent script in front of it)
// behind actual tab visibility — Chrome deprioritizes third-party script execution in
// hidden/background tabs, so a background map tab never even loads Didomi, let alone Maps.
// That means the map tab MUST be the active (focused) tab to have any chance of working.
//
// Doing that naively for every listing would mean each new map tab steals focus from an
// EARLIER map tab still waiting on its scripts — backgrounding it right when it needs to be
// visible, and starving it the same way. So map-tab opens are serialized here: only one is
// ever open (and focused) at a time. It does mean your browser's foreground tab flickers to
// each map page briefly during a crawl — an accepted trade-off since nothing works hidden.
let mapQueue = [];
let mapProcessing = false;
let currentMapTabId = null;

async function processMapQueue() {
  if (mapProcessing || mapQueue.length === 0) return;
  mapProcessing = true;
  const url = mapQueue.shift();
  try {
    const tab = await chrome.tabs.create({ url: url, active: true });
    currentMapTabId = tab.id;
    console.log('[Idealista Capture] Map tab opened (focused):', tab.id);
  } catch (err) {
    console.error('[Idealista Capture] Failed to open map tab:', err);
    mapProcessing = false;
    await processMapQueue();
  }
}

chrome.runtime.onMessage.addListener(function (message, sender) {
  handleMessage(message, sender).catch(function (err) {
    console.error('Error handling message', message && message.type, err);
  });
});

async function handleMessage(message, sender) {
  console.log('[Idealista Capture] Message received:', message.type, 'from tab', sender.tab && sender.tab.id);

  if (message.type === 'ad' || message.type === 'coords') {
    fetch(LISTENER_URL, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(message)
    }).catch(function (err) {
      console.error('AddProperty listener unreachable (is it running?):', err);
    });
  } else if (message.type === 'open-map') {
    mapQueue.push(message.url);
    await processMapQueue();
  } else if (message.type === 'debug') {
    console.log('[Idealista Capture]', message.data);
  } else if (message.type === 'close-self' && sender.tab) {
    const wasMapTab = sender.tab.id === currentMapTabId;
    chrome.tabs.remove(sender.tab.id);
    if (wasMapTab) {
      currentMapTabId = null;
      mapProcessing = false;
      await processMapQueue();
    }
  } else if (message.type === 'search-page') {
    await queueSearchPage(message.data, sender.tab ? sender.tab.id : null);
  } else if (message.type === 'open-next') {
    await runSerialized(openNextListing);
  }
}

async function queueSearchPage(data, senderTabId) {
  console.log('[Idealista Capture] Search page harvested', (data.listingUrls || []).length, 'listing(s). Next page:', data.nextPageUrl);

  const state = await chrome.storage.local.get(['crawling']);
  if (!state.crawling) {
    console.log('[Idealista Capture] Crawling is off — ignoring this search page.');
    return;
  }

  await chrome.storage.local.set({
    crawlQueue: data.listingUrls || [],
    nextPageUrl: data.nextPageUrl || null,
    searchTabId: senderTabId
  });
  await runSerialized(openNextListing);
}

// openNextListing reads-then-writes the shared queue in storage, which isn't atomic — if two
// "open-next" messages ever arrived close enough together, both could read the same queue
// before either saved it back, and end up opening the same URL while silently dropping
// another. Routing every call through this tiny in-memory chain makes them run one at a time
// instead, closing that gap.
let processingChain = Promise.resolve();
function runSerialized(fn) {
  const result = processingChain.then(fn, fn);
  processingChain = result.catch(function () {}); // one failure shouldn't jam the chain
  return result;
}

async function openNextListing() {
  const state = await chrome.storage.local.get(['crawling', 'crawlQueue', 'nextPageUrl', 'searchTabId']);
  if (!state.crawling) {
    console.log('[Idealista Capture] openNextListing called but crawling is off — doing nothing.');
    return;
  }

  const queue = state.crawlQueue || [];
  console.log('[Idealista Capture] openNextListing: queue has', queue.length, 'item(s) left.');

  if (queue.length > 0) {
    const nextUrl = queue.shift();
    await chrome.storage.local.set({ crawlQueue: queue });
    console.log('[Idealista Capture] Opening next listing:', nextUrl);
    chrome.tabs.create({ url: nextUrl, active: false });
    return;
  }

  if (state.nextPageUrl) {
    const nextPage = state.nextPageUrl;
    // Clear it now; content-search.js will supply a fresh one once that page loads.
    await chrome.storage.local.set({ nextPageUrl: null });
    console.log('[Idealista Capture] Queue empty — moving to next search page:', nextPage);
    navigateToNextPage(state.searchTabId, nextPage);
    return;
  }

  // No listings left in the queue and no next page known — reached the end of the
  // results. Stop crawling rather than opening an empty/broken page forever.
  console.log('[Idealista Capture] No queue and no next page — stopping crawl.');
  await chrome.storage.local.set({ crawling: false });
  chrome.action.setBadgeText({ text: '' });
}

// Reuses the original search-results tab for pagination (navigating it forward, exactly like
// clicking "next page" yourself) instead of opening a new tab every page — otherwise a long
// crawl through hundreds of pages leaves hundreds of abandoned tabs behind.
function navigateToNextPage(searchTabId, nextPageUrl) {
  if (!searchTabId) {
    chrome.tabs.create({ url: nextPageUrl, active: false });
    return;
  }

  chrome.tabs.update(searchTabId, { url: nextPageUrl }, function () {
    if (chrome.runtime.lastError) {
      // The original tab was closed or is otherwise gone — fall back to a new one.
      chrome.tabs.create({ url: nextPageUrl, active: false });
    }
  });
}
