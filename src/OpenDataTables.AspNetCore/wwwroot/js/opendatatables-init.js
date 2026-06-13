/*!
 * OpenDataTables.AspNetCore — scanner / lifecycle.
 *
 * Finds the JSON config blocks emitted by the ViewComponents and initializes each component once, on
 * DOMContentLoaded, after every htmx:afterSwap, on any configured reinit events, and whenever you call
 * OpenDataTables.scan(rootElement). CSP-friendly: no inline executable script per table.
 */
(function (window, document) {
  'use strict';

  var ODT = (window.OpenDataTables = window.OpenDataTables || {});

  function parse(script) {
    try { return JSON.parse((script.textContent || '').trim()); }
    catch (e) { console.error('[OpenDataTables] invalid component config JSON', e, script); return null; }
  }

  function scanComponent(root, component, initFn) {
    var scripts = (root || document).querySelectorAll('script[type="application/json"][data-component="' + component + '"]');
    Array.prototype.forEach.call(scripts, function (script) {
      if (script.dataset.processed === '1') return;
      var cfg = parse(script);
      if (!cfg) return;
      script.dataset.processed = '1';
      try { initFn(cfg); } catch (e) { console.error('[OpenDataTables] init ' + component + ' failed', e); }
    });
  }

  /** Scan a subtree (defaults to document) and initialize any not-yet-processed components. */
  ODT.scan = function (root) {
    scanComponent(root, 'FilterCard', function (cfg) { if (ODT.FilterCard) ODT.FilterCard.init(cfg); });
    scanComponent(root, 'DataTable', function (cfg) { if (ODT.DataTable) ODT.DataTable.init(cfg); });
  };

  function bootstrap() {
    ODT.scan(document);
    if (!document.body) return;
    document.body.addEventListener('htmx:afterSwap', function (evt) {
      var target = evt && evt.detail && evt.detail.target ? evt.detail.target : document;
      ODT.scan(target);
    });
    (((ODT.config || {}).reinitEvents) || []).forEach(function (name) {
      document.body.addEventListener(name, function () { ODT.scan(document); });
    });
  }

  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', bootstrap, { once: true });
  else bootstrap();
})(window, document);
