// The extension's coordination point. Content scripts message this background script for
// anything that needs the extension's own permissions: opening/closing tabs, or making the
// cross-origin request to localhost.
const LISTENER_URL = 'http://localhost:5099/capture';
// Idealista folds active filters (price, etc.) into this same path segment, comma-separated,
// which can push "apartamentos" anywhere in it (e.g. "com-preco-max_340000,apartamentos"
// instead of "com-apartamentos") — match on it appearing anywhere in that segment, not at a
// fixed position, or a filtered search URL silently fails this check.
const SEARCH_URL_PATTERN = /idealista\.pt\/comprar-casas\/faro-distrito\/[^/]*apartamentos/;

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

// ── Map-tab handling ────────────────────────────────────────────────────
// content-ad.js asks for coordinates FIRST (via 'need-coords') and does nothing else — no
// extraction, no save — until it hears back. This module owns the map tab's entire lifecycle
// and reports the outcome straight back to the ad tab that asked, via chrome.tabs.sendMessage.
//
// A reload() of the SAME tab turned out to be unreliable — whatever state gets a map stuck
// (a half-loaded Didomi consent flow, a throttled renderer) can survive a same-document reload.
// So instead of reloading, a stuck attempt gets its tab closed outright and a brand new tab
// opened for the same URL — a clean process every time. Requests are serialized one at a time
// (only one map tab open at once) since Idealista's map script only seems to progress on the
// actual foreground tab, and racing several at once would just starve each other of focus.
const MAP_TAB_TIMEOUT_MS = 3000;   // how long one attempt gets before it's killed and retried
const MAX_MAP_ATTEMPTS = 30;       // safety ceiling (~90s worst case) — not an expected outcome

let mapQueue = [];      // { sourceUrl, mapUrl, requesterTabId }
let mapProcessing = false;
let currentJob = null;  // { sourceUrl, mapUrl, requesterTabId, attempt, tabId, watchdog }

async function focusMapWindow(tab) {
  try {
    await chrome.windows.update(tab.windowId, { focused: true });
  } catch (err) {
    console.error('[Idealista Capture] Failed to focus map window:', err);
  }
}

async function safeRemoveTab(tabId) {
  if (tabId == null) return;
  try {
    await chrome.tabs.remove(tabId);
  } catch (err) {
    // Already closed — nothing to do.
  }
}

async function processMapQueue() {
  if (mapProcessing || mapQueue.length === 0) return;
  mapProcessing = true;
  const job = mapQueue.shift();
  await startMapAttempt(Object.assign({ attempt: 1 }, job));
}

async function startMapAttempt(job) {
  currentJob = job;
  try {
    const tab = await chrome.tabs.create({ url: job.mapUrl, active: true });
    currentJob.tabId = tab.id;
    await focusMapWindow(tab);
    console.log(`[Idealista Capture] Map tab opened (attempt ${job.attempt}/${MAX_MAP_ATTEMPTS}):`, tab.id, job.sourceUrl);
    currentJob.watchdog = setTimeout(function () { handleMapTimeout(job.sourceUrl); }, MAP_TAB_TIMEOUT_MS);
  } catch (err) {
    console.error('[Idealista Capture] Failed to open map tab:', err);
    await finishMapJob(job, null);
  }
}

async function handleMapTimeout(sourceUrl) {
  if (!currentJob || currentJob.sourceUrl !== sourceUrl) return; // stale timer from a finished job
  const job = currentJob;
  await safeRemoveTab(job.tabId);

  if (job.attempt >= MAX_MAP_ATTEMPTS) {
    console.warn('[Idealista Capture] Giving up on coordinates after', job.attempt, 'attempts:', job.sourceUrl);
    await finishMapJob(job, null);
    return;
  }

  console.log('[Idealista Capture] Map tab stuck, closing and reopening (attempt', job.attempt + 1, '):', job.sourceUrl);
  await startMapAttempt({ sourceUrl: job.sourceUrl, mapUrl: job.mapUrl, requesterTabId: job.requesterTabId, attempt: job.attempt + 1 });
}

async function finishMapJob(job, coordsData) {
  if (currentJob && currentJob.watchdog) clearTimeout(currentJob.watchdog);
  currentJob = null;

  try {
    await chrome.tabs.sendMessage(job.requesterTabId,
      coordsData
        ? { type: 'coords-ready', data: coordsData }
        : { type: 'coords-failed', data: { sourceUrl: job.sourceUrl } });
  } catch (err) {
    console.warn('[Idealista Capture] Could not notify ad tab (closed already?):', job.requesterTabId, err);
  }

  mapProcessing = false;
  await processMapQueue();
}

chrome.runtime.onMessage.addListener(function (message, sender) {
  handleMessage(message, sender).catch(function (err) {
    console.error('Error handling message', message && message.type, err);
  });
});

async function handleMessage(message, sender) {
  console.log('[Idealista Capture] Message received:', message.type, 'from tab', sender.tab && sender.tab.id);

  if (message.type === 'ad') {
    fetch(LISTENER_URL, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(message)
    }).catch(function (err) {
      console.error('AddProperty listener unreachable (is it running?):', err);
    });
  } else if (message.type === 'need-coords' && sender.tab) {
    mapQueue.push({ sourceUrl: message.sourceUrl, mapUrl: message.mapUrl, requesterTabId: sender.tab.id });
    await processMapQueue();
  } else if (message.type === 'coords' && sender.tab) {
    if (currentJob && currentJob.sourceUrl === message.data.sourceUrl && currentJob.tabId === sender.tab.id) {
      await safeRemoveTab(currentJob.tabId);
      await finishMapJob(currentJob, message.data);
    }
  } else if (message.type === 'debug') {
    console.log('[Idealista Capture]', message.data);
  } else if (message.type === 'close-self' && sender.tab) {
    await safeRemoveTab(sender.tab.id);
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
