'use strict';

// Tiny in-process mutex shared between git.js (push) and pull.js (poller).
//
// Node is single-threaded, so the ONLY place control can yield mid-push is the
// `await getInstallationToken()` network call inside commitGameEdits. During
// that await the background pull poller could otherwise fire and run `git
// merge` while a commit/push is in flight. This flag lets the poller skip a
// tick when a push is active. git.js sets busy=true around its commit+push
// (try/finally); pull.js checks isBusy() at the top of each tick.

let busy = false;

function isBusy() {
  return busy;
}

function set(v) {
  busy = !!v;
}

module.exports = { isBusy, set };
