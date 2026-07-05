// The extension's coordination point. Content scripts can't reliably open/close
// tabs or make cross-origin requests themselves — they message this background
// script, which does it with the extension's own permissions.
const LISTENER_URL = 'http://localhost:5099/capture';

chrome.runtime.onMessage.addListener(function (message, sender) {
  if (message.type === 'ad' || message.type === 'coords') {
    fetch(LISTENER_URL, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(message)
    }).catch(function (err) {
      console.error('AddProperty listener unreachable (is it running?):', err);
    });
  } else if (message.type === 'open-map') {
    chrome.tabs.create({ url: message.url, active: false });
  } else if (message.type === 'close-self' && sender.tab) {
    chrome.tabs.remove(sender.tab.id);
  }
});
