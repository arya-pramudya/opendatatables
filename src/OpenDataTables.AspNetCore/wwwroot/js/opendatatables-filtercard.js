/*!
 * OpenDataTables.AspNetCore — FilterCard collapse/icon wiring (search/reset are handled by the
 * DataTable runtime via delegated events).
 */
(function (window, document) {
  'use strict';

  var ODT = (window.OpenDataTables = window.OpenDataTables || {});
  var FilterCard = (ODT.FilterCard = ODT.FilterCard || {});

  FilterCard.init = function (config) {
    if (!config || !config.formId) return;
    var collapse = document.getElementById(config.formId + '_collapse');
    var icon = document.getElementById(config.formId + '_collapse_icon');
    if (!collapse || !icon) return;

    icon.classList.add('odt-rotate-icon');
    sync(collapse.classList.contains('show'));

    collapse.addEventListener('hide.bs.collapse', function () {
      icon.classList.add('collapsed');
      window.setTimeout(function () { icon.classList.remove('fa-minus'); icon.classList.add('fa-plus'); }, 150);
    });
    collapse.addEventListener('show.bs.collapse', function () {
      icon.classList.remove('collapsed');
      window.setTimeout(function () { icon.classList.remove('fa-plus'); icon.classList.add('fa-minus'); }, 150);
    });

    function sync(shown) {
      if (shown) { icon.classList.remove('fa-plus'); icon.classList.add('fa-minus'); icon.classList.remove('collapsed'); }
      else { icon.classList.remove('fa-minus'); icon.classList.add('fa-plus'); icon.classList.add('collapsed'); }
    }
  };
})(window, document);
