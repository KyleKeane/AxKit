// Partner Center, the product's overview with an update in draft: the submission's number and the addresses of its
// pages, read from the links of the page and its shadow roots (docs/axKit/store-setup.md section 6, the fourth
// submission: a synthetic click on the overview's "Packages" link opens a summary in place, not the page).
// Run: EdgeDrive evalfile tools\EdgeDrive\store\submission-pages.js. Reads only.
(() => {
  const all = [];
  const walk = (root) => {
    for (const el of root.querySelectorAll('*')) {
      all.push(el);
      if (el.shadowRoot) walk(el.shadowRoot);
    }
  };
  walk(document);
  const pages = new Map();
  for (const el of all) {
    const href = el.tagName === 'A' ? el.getAttribute('href') || '' : '';
    const found = href.match(/submissions\/(\d+)\/([a-z]+)/i);
    if (found) pages.set(found[2], found[1]);
  }
  if (pages.size === 0) return 'No submission links: is an update in draft on this overview?';
  const ids = new Set(pages.values());
  if (ids.size !== 1) return 'Links to more than one submission: ' + Array.from(ids).join(', ');
  const id = Array.from(ids)[0];
  const base = location.origin + location.pathname.replace(/\/overview$/, '') + '/submissions/' + id + '/';
  return 'Submission ' + id + '\n' + Array.from(pages.keys()).map((page) => page + ': ' + base + page).join('\n')
    + '\nlistings (English): ' + base + 'listings?languageid=4';
})()
