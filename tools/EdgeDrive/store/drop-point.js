// Partner Center, a submission's Packages page: the element that takes a dropped package (the one with the
// libfilesdroped attribute) scrolled into view, and a point inside it that is on no link, checked through the shadow
// roots, as "x y" for EdgeDrive dropxy (docs/axKit/store-setup.md section 6). The fourth submission (2026-11-18)
// dropped first on the row around it, which is wider, and Edge saved the file into Downloads instead of uploading
// it; the third dropped on the area's "browse your files" link, with the same result.
// Run: EdgeDrive evalfile tools\EdgeDrive\store\drop-point.js, then EdgeDrive dropxy <x> <y> <AxKit-<version>.msix>
// at once, before anything scrolls the page. Reads only.
(() => {
  const all = [];
  const walk = (root) => {
    for (const el of root.querySelectorAll('*')) {
      all.push(el);
      if (el.shadowRoot) walk(el.shadowRoot);
    }
  };
  walk(document);
  const zones = all.filter((el) => el.hasAttribute('libfilesdroped'));
  if (zones.length !== 1) return 'Not one drop zone but ' + zones.length + ': read the page before dropping.';
  const zone = zones[0];
  zone.scrollIntoView({ block: 'center' });
  const deepAt = (x, y) => {
    let el = document.elementFromPoint(x, y);
    while (el && el.shadowRoot) {
      const inner = el.shadowRoot.elementFromPoint(x, y);
      if (!inner || inner === el) break;
      el = inner;
    }
    return el;
  };
  const r = zone.getBoundingClientRect();
  // Low in the zone first: the words and their link are in its upper part.
  for (const [fx, fy] of [[0.4, 0.92], [0.6, 0.92], [0.05, 0.1], [0.95, 0.1], [0.95, 0.92]]) {
    const x = Math.round(r.left + r.width * fx);
    const y = Math.round(r.top + r.height * fy);
    const at = deepAt(x, y);
    const inZone = !!at && (zone === at || zone.contains(at));
    const onLink = !!at && !!at.closest && !!at.closest('a, button, input, label');
    if (inZone && !onLink) return x + ' ' + y;
  }
  return 'No point of the drop zone is free of its link: read the page before dropping.';
})()
