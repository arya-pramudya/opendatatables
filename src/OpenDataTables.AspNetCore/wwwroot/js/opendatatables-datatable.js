/*!
 * OpenDataTables.AspNetCore — server-side DataTable runtime (OpenDataTables.DataTable)
 *
 * A clean, decoupled re-implementation of the read-only server-side table: column/DOM building, the
 * DataTableQueryViewModel param mapping, filter reading, CSP-friendly delegated action buttons
 * (no inline onclick), custom buttons, child rows, and load triggers. Reuses OpenSelect2 for select
 * filters when present.
 */
(function (window, document) {
  'use strict';

  var $ = window.jQuery;
  if (!$ || !$.fn || !$.fn.DataTable) {
    // eslint-disable-next-line no-console
    console.error('[OpenDataTables] jQuery + datatables.net are required.');
    return;
  }

  var ODT = (window.OpenDataTables = window.OpenDataTables || {});
  var util = ODT.util || {};
  var DataTable = (ODT.DataTable = ODT.DataTable || {});

  // tableId → { config, api, handlers }
  var registry = (ODT._instances = ODT._instances || {});

  /** Register host action handlers programmatically: OpenDataTables.on(tableId, { onAdd, onEdit, ... }). */
  ODT.on = function (tableId, handlers) {
    registry[tableId] = registry[tableId] || {};
    registry[tableId].handlers = $.extend({}, registry[tableId].handlers, handlers);
  };

  function resolveHandler(tableId, name) {
    var reg = registry[tableId] || {};
    if (reg.handlers && typeof reg.handlers[name] === 'function') return reg.handlers[name];
    var cfgName = (reg.config || {})[name];
    if (typeof cfgName === 'string' && typeof window[cfgName] === 'function') return window[cfgName];
    if (typeof cfgName === 'function') return cfgName;
    return null;
  }

  // A window global is only callable by name when it's a plain identifier and not a dangerous built-in.
  // Shared by the row-button resolver (resolveGlobalHandler) and the named-fn resolver (resolveNamedFn,
  // used for formatters / rowCallback / childRowCallback) so neither path can invoke e.g. window.open,
  // window.fetch, or window.print by a coincidentally-matching config name.
  var IDENTIFIER_RE = /^[A-Za-z_$][A-Za-z0-9_$]*$/;
  var UNSAFE_GLOBAL_RE = /^(eval|Function|fetch|XMLHttpRequest|location|open|close|print|find|stop|focus|blur|document|window|globalThis|self|top|parent|navigator|alert|confirm|prompt|setTimeout|setInterval|setImmediate|requestAnimationFrame|queueMicrotask|Reflect|Proxy|importScripts|WebSocket|Worker|SharedWorker)$/;

  function safeWindowFn(name) {
    if (!name || typeof name !== 'string' || !IDENTIFIER_RE.test(name)) return null;
    if (UNSAFE_GLOBAL_RE.test(name)) return null;
    // Only resolve OWN globals — ignore inherited Object.prototype members (constructor, toString,
    // valueOf, hasOwnProperty, …), which every identifier would otherwise match via window[name].
    if (!Object.prototype.hasOwnProperty.call(window, name)) return null;
    var fn = window[name];
    if (typeof fn !== 'function') return null;
    // Reject native built-ins (Date, String, Number, Array, parseInt, …): a formatter/handler must be
    // host-defined. Built-ins stringify to "{ [native code] }"; host functions carry a real body.
    if (/\{\s*\[native code\]\s*\}/.test(Function.prototype.toString.call(fn))) return null;
    return fn;
  }

  // Last-resort lookup of a row-button handler by name on window, for hosts not using OpenDataTables.on.
  function resolveGlobalHandler(name) { return safeWindowFn(name); }

  function call(tableId, name, args) {
    var fn = resolveHandler(tableId, name);
    if (fn) { try { fn.apply(null, args || []); } catch (e) { console.error('[OpenDataTables] handler ' + name + ' threw', e); } return true; }
    return false;
  }

  // Resolve a named function: prefer the per-table registry (OpenDataTables.on), then a window global
  // (back-compat). Lets formatters / row callbacks be registered without polluting the global namespace.
  function resolveNamedFn(tableId, name) {
    if (!name || typeof name !== 'string') return null;
    var reg = registry[tableId];
    if (reg && reg.handlers && typeof reg.handlers[name] === 'function') return reg.handlers[name];
    return safeWindowFn(name);
  }

  // Array-replacing, prototype-pollution-safe deep merge. Defined once in opendatatables-core.js
  // (util.mergeOptions); aliased here for the escape-hatch call sites below.
  var mergeOptions = util.mergeOptions;

  function rowId(row) { return row && (row.id != null ? row.id : row.Id); }

  function isEditing(tableId) { return !!(registry[tableId] && registry[tableId].editing); }

  // ----- column / dom building -------------------------------------------------

  function buildColumns(config, tableId, editors) {
    var cols = Array.isArray(config.columns) ? config.columns : [];
    var defs = [];

    if (config.hasChildRows && Array.isArray(config.childColumns) && config.childColumns.length) {
      defs.push({ data: null, name: '__details', orderable: false, searchable: false, width: '32px',
        className: 'odt-details-control', defaultContent: '<span class="odt-details-toggle">+</span>' });
    }

    if (config.hasNumbering) {
      defs.push({ data: null, name: '__no', orderable: false, searchable: false, width: '40px',
        className: 'odt-rownum text-end', render: function () { return ''; } });
    }

    cols.forEach(function (col) {
      var key = col.data;
      var editor = editors && editors[key];
      // Resolve the named formatter once per column (not per cell render) — it's stable after init.
      var fmtFn = col.format ? resolveNamedFn(tableId, col.format) : null;
      defs.push({
        data: key,
        name: key,
        visible: col.isVisible !== false,
        orderable: col.isSortable !== false,
        className: (col.cellClass || '') + (col.noWrap ? ' odt-nowrap' : '') || undefined,
        width: col.width || undefined,
        render: function (data, type, row) {
          if (type !== 'display') return data;
          // Inline editor when this column is editable and the table is in edit mode.
          if (editor && isEditing(tableId)) return renderEditor(editor, data == null ? '' : data, rowId(row));
          if (data != null && col.format) {
            // A registered/global formatter (resolved once above) takes precedence; else the built-in
            // date formatter interprets `format` as a moment-style token string.
            if (fmtFn) { try { return fmtFn(data); } catch (e) { /* noop */ } }
            if (typeof data === 'string' && !isNaN(Date.parse(data))) return util.formatDateIntl(data, col.format);
          }
          return data;
        }
      });
    });

    if (hasActionColumn(config)) {
      defs.push({ data: null, name: 'Actions', orderable: false, searchable: false,
        className: 'odt-actions odt-nowrap', render: function (data, type, row) { return renderActions(config, row); } });
    }

    if (!defs.length) defs.push({ data: null, defaultContent: '' });
    return defs;
  }

  // A custom button's placement defaults to 'row' when absent (matches the C# model default), so a
  // host that builds config directly in JS without a placement still gets a row button.
  function buttonPlacement(b) { return b.placement || 'row'; }

  function hasButtonAt(config, placement) {
    return Array.isArray(config.customButtons) &&
      config.customButtons.some(function (b) { return buttonPlacement(b) === placement; });
  }

  function hasActionColumn(config) {
    return config.showView || config.showEdit || config.showDelete || hasButtonAt(config, 'row');
  }

  function renderActions(config, row) {
    var cfg = ODT.config || {};
    var loc = cfg.locale || {};
    // Theming hooks: hosts not using Bootstrap/Font Awesome can override icons + button classes via
    // ODT.config.icons / ODT.config.actionClasses (see opendatatables-core.js defaults).
    var icons = cfg.icons || {};
    var aCss = cfg.actionClasses || {};
    var id = rowId(row);
    var html = '<div class="' + (aCss.group || 'btn-group btn-group-sm') + '" role="group">';
    if (config.showView) html += actionBtn('view', id, aCss.view || 'btn-info', icons.view || 'fas fa-eye', loc.view || 'View');
    if (config.showEdit) html += actionBtn('edit', id, aCss.edit || 'btn-primary', icons.edit || 'fas fa-edit', config.customEditText || loc.edit || 'Edit');
    if (config.showDelete) html += actionBtn('delete', id, aCss.delete || 'btn-danger', icons.delete || 'fas fa-trash', config.customDeleteText || loc.delete || 'Delete');

    if (Array.isArray(config.customButtons)) {
      config.customButtons.forEach(function (btn) {
        if (buttonPlacement(btn) !== 'row') return;
        var icon = btn.icon ? '<i class="' + util.escapeHtml(btn.icon) + '"></i> ' : '';
        var rowData = util.escapeHtml(JSON.stringify(row));
        html += '<button type="button" id="' + util.escapeHtml(btn.id || '') + '" class="btn ' + util.escapeHtml(btn.cssClass || 'btn-secondary') + ' btn-sm me-1"' +
          (btn.style ? ' style="' + util.escapeHtml(btn.style) + '"' : '') +
          ' data-odt-action="custom" data-odt-handler="' + util.escapeHtml(btn.onClick || '') + '"' +
          ' data-odt-row="' + rowData + '" title="' + util.escapeHtml(btn.title || btn.text || '') + '">' + icon + util.escapeHtml(btn.text || '') + '</button>';
      });
    }
    return html + '</div>';
  }

  function actionBtn(action, id, css, icon, title) {
    // css/icon come from ODT.config theming hooks; escape them too so a misconfigured theme can't inject HTML.
    return '<button type="button" class="btn ' + util.escapeHtml(css) + ' btn-sm me-1" data-odt-action="' + action + '" data-id="' + util.escapeHtml(id) +
      '" title="' + util.escapeHtml(title) + '"><i class="' + util.escapeHtml(icon) + '"></i></button>';
  }

  function buildDom(config) {
    var hasTop = config.showAdd || hasButtonAt(config, 'top');
    return (hasTop ? '<"odt-top">' : '') + 'rt<"odt-bottom d-flex justify-content-between align-items-center"lpi>';
  }

  // ----- ajax param mapping (DataTables std → DataTableQueryViewModel) ----------

  function buildAjaxData(tableId, d, config) {
    var order = (d.order && d.order[0]) || {};
    var colIdx = order.column != null ? order.column : 0;
    var col = (d.columns && d.columns[colIdx]) || {};

    var mapped = {
      Draw: String(d.draw),
      Start: d.start,
      Length: d.length,
      SortColumnIndex: colIdx,
      SortColumnName: col.data || col.name || '',
      SortDirection: order.dir || 'asc'
    };

    // B5: emit the full multi-column sort list as FLAT, dot-notation form keys
    // (SortOrders[0].Column=…, SortOrders[0].Direction=…). A nested array/object would jQuery-serialize
    // as SortOrders[0][Column], which ASP.NET Core form binding does NOT map to List<SortDescriptor> —
    // so every secondary (ThenBy) sort would be silently dropped on the wire. The scalar SortColumnName/
    // SortDirection above stay in sync with order[0] for back-compat.
    if (Array.isArray(d.order)) {
      var sortIdx = 0;
      d.order.forEach(function (o) {
        var c = (d.columns && d.columns[o.column]) || {};
        var colName = c.data || c.name || '';
        if (!colName) return;
        mapped['SortOrders[' + sortIdx + '].Column'] = colName;
        mapped['SortOrders[' + sortIdx + '].Direction'] = o.dir || 'asc';
        sortIdx++;
      });
    }

    DataTable.Filters.getFilterData(tableId, mapped);

    if (config.customParameters) $.each(config.customParameters, function (k, v) { mapped[k] = v; });

    if (config.dynamicValueSources) {
      $.each(config.dynamicValueSources, function (paramName, selector) {
        if (typeof selector === 'string' && selector.indexOf('|') === -1) {
          var $el = $(selector);
          if ($el.length) {
            var val = $el.val();
            if (val != null && String(val).trim() !== '') mapped[paramName] = val;
          }
        }
      });
    }
    return mapped;
  }

  // ----- filters ---------------------------------------------------------------

  DataTable.Filters = {
    /** Reads filter inputs (FilterCard or top row) into the request object as {column}=value. */
    getFilterData: function (tableId, d) {
      var $form = $('#' + tableId + '-filter-form, #' + tableId + '-top-filters');
      if (!$form.length) return d;
      $form.find('[data-column]').each(function () {
        var column = $(this).data('column');
        if (!column) return;
        var val = $(this).val();
        if (Array.isArray(val)) val = val.filter(function (v) { return v != null && String(v) !== '-1'; }).join(',');
        if (val != null && String(val).trim() !== '' && String(val) !== '-1') d[column] = String(val);
      });
      return d;
    }
  };

  function wireFilters(tableId, config, api) {
    var formSel = '#' + tableId + '-filter-form';
    $(document)
      .off('click.odtfilter_' + tableId)
      .on('click.odtfilter_' + tableId, '#' + tableId + '-search-btn', function () { api.ajax.reload(); })
      .on('click.odtfilter_' + tableId, '#' + tableId + '-reset-btn', function () {
        $(formSel).find('[data-column]').each(function () {
          var $el = $(this);
          $el.val($el.is('select') ? null : '');
          if ($el.hasClass('select2-hidden-accessible')) $el.trigger('change.select2');
        });
        api.ajax.reload();
      });
  }

  // ----- action delegation (CSP-friendly, no inline onclick) -------------------

  function wireActions(tableId, config) {
    var ns = '.odtact_' + tableId;
    // Add button lives outside the table wrapper (rendered by the view).
    $(document).off('click' + ns).on('click' + ns, '[data-odt-action][data-odt-table="' + tableId + '"], #' + tableId + '_wrapper [data-odt-action]', function (e) {
      var $btn = $(this);
      var action = $btn.data('odt-action');
      if (action === 'add') { call(tableId, 'onAdd', []); return; }
      if (action === 'custom') {
        var handlerName = $btn.data('odt-handler');
        var row = parseRow($btn.attr('data-odt-row'));
        // Prefer registry (OpenDataTables.on); fall back to a safe window global (see resolveGlobalHandler).
        var customFn = resolveHandler(tableId, handlerName) || resolveGlobalHandler(handlerName);
        // Swallow + log (consistent with every other handler path); a throwing handler must not break
        // delegated event dispatch for other buttons.
        if (customFn) { try { customFn(row); } catch (e) { console.error('[OpenDataTables] custom handler ' + handlerName + ' threw', e); } }
        return;
      }
      var id = $btn.data('id');
      var map = { view: 'onView', edit: 'onEdit', delete: 'onDelete' };
      if (map[action]) call(tableId, map[action], [id]);
    });
  }

  function parseRow(json) {
    if (!json) return null;
    // The browser already HTML-decodes the attribute when .attr() reads it; decoding again (via
    // innerHTML) corrupts values containing '<' or '&', so parse the string directly.
    try { return JSON.parse(json); } catch (e) { return null; }
  }

  // ----- child rows ------------------------------------------------------------

  function wireChildRows(tableId, config, api) {
    var $tbody = $('#' + tableId + ' tbody');
    $tbody.off('click.odtchild').on('click.odtchild', 'td.odt-details-control', function () {
      var tr = $(this).closest('tr');
      var row = api.row(tr);
      if (row.child.isShown()) {
        row.child.hide(); tr.removeClass('odt-shown'); $(this).find('.odt-details-toggle').text('+');
        return;
      }
      var childId = tableId + '_child_' + rowId(row.data());
      row.child('<table id="' + childId + '" class="table table-sm table-bordered odt-child-table w-100"><thead><tr>' +
        config.childColumns.map(function (c) { return '<th>' + util.escapeHtml(c.title) + '</th>'; }).join('') +
        '</tr></thead></table>').show();
      tr.addClass('odt-shown'); $(this).find('.odt-details-toggle').text('−');
      initChildTable(tableId, childId, config, row.data());
    });
  }

  function initChildTable(tableId, childId, config, parentRow) {
    var params = {};
    if (config.childCustomParameters) {
      $.each(config.childCustomParameters, function (k, v) {
        params[k] = (typeof v === 'string' && v.indexOf('row.') === 0) ? parentRow[v.slice(4)] : v;
      });
    }
    var childApi = $('#' + childId).DataTable({
      processing: true, serverSide: true, searching: false, paging: false, info: false,
      ajax: { url: config.childAjaxUrl, type: 'POST', data: function (d) {
        var m = { Draw: String(d.draw), Start: 0, Length: 10000, SortColumnName: '', SortDirection: 'asc' };
        return $.extend(m, params);
      }, error: ajaxError },
      columns: config.childColumns.map(function (c) {
        return { data: c.data, name: c.data, orderable: c.isSortable !== false,
          render: function (data, type) {
            if (type === 'display' && data != null && c.format && typeof data === 'string' && !isNaN(Date.parse(data))) return util.formatDateIntl(data, c.format);
            return data;
          } };
      })
    });
    var childCb = resolveNamedFn(tableId, config.childRowCallback);
    if (childCb) {
      try { childCb(childId, parentRow, childApi, config); } catch (e) { /* noop */ }
    }
  }

  // ----- numbering -------------------------------------------------------------

  function applyNumbering(api) {
    var info = api.page.info();
    api.column('__no:name').nodes().each(function (cell, i) {
      cell.textContent = info.start + i + 1;
    });
  }

  // ----- inline editing (editable mode) ---------------------------------------
  // The grid is editable when it has editorConfigs. Save modes: Manual (Edit→Save/Cancel collect a
  // payload), Auto (POST per change), Custom (delegate to config.onSave). Static-select editors render
  // inline; dynamic Select2 cell editors are a documented follow-up.

  function editorMap(config) {
    var map = {};
    (config.editorConfigs || []).forEach(function (ec) {
      var col = ec.column || ec.Column;
      if (col) map[col] = ec;
    });
    return map;
  }

  function pad(n) { return n < 10 ? '0' + n : '' + n; }

  function toDateInputValue(value) {
    if (value == null || value === '') return '';
    var s = String(value);
    var iso = s.match(/^\d{4}-\d{2}-\d{2}/);
    if (iso) return iso[0];
    var d = new Date(s);
    return isNaN(d.getTime()) ? '' : d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate());
  }

  function editorTypeToken(ec) {
    // Enum: 0 Text, 1 Number, 2 Date, 3 Select2Static, 4 Select2Dynamic, 5 Select2Table
    var t = ec.editorType != null ? ec.editorType : ec.EditorType;
    switch (t) {
      case 1: return 'number';
      case 2: return 'date';
      case 3: return 'selectstatic';
      default: return 'text';
    }
  }

  function renderEditor(ec, value, id) {
    var type = editorTypeToken(ec);
    var field = ec.column || ec.Column;
    var common = 'data-odt-edit data-row-id="' + util.escapeHtml(id) + '" data-field="' + util.escapeHtml(field) + '" class="form-control form-control-sm"';
    if (type === 'selectstatic') {
      var opts = (ec.staticOptions || ec.StaticOptions || []).map(function (o) {
        var ov = o.id != null ? o.id : o.Id, ot = o.text != null ? o.text : o.Text;
        return '<option value="' + util.escapeHtml(ov) + '"' + (String(ov) === String(value) ? ' selected' : '') + '>' + util.escapeHtml(ot) + '</option>';
      }).join('');
      return '<select ' + common + '>' + opts + '</select>';
    }
    var inputType = type === 'number' ? 'number' : (type === 'date' ? 'date' : 'text');
    var v = util.escapeHtml(type === 'date' ? toDateInputValue(value) : value);
    return '<input type="' + inputType + '" ' + common + ' value="' + v + '" />';
  }

  function collectPayload(tableId) {
    var rows = {};
    $('#' + tableId + ' [data-odt-edit]').each(function () {
      var id = $(this).data('row-id');
      rows[id] = rows[id] || { id: id };
      rows[id][$(this).data('field')] = $(this).val();
    });
    return Object.keys(rows).map(function (k) { return rows[k]; });
  }

  function postSave(config, payload) {
    return $.ajax({ url: config.saveAjaxUrl, type: 'POST', contentType: 'application/json', data: JSON.stringify(payload) });
  }

  function toggleEditButtons(tableId, editing, saveMode) {
    if (saveMode !== 'manual') {
      $('#' + tableId + '-edit-mode-btn, #' + tableId + '-cancel-btn').hide();
      $('#' + tableId + '-save-btn').toggle(saveMode === 'custom');
      return;
    }
    $('#' + tableId + '-edit-mode-btn').toggle(!editing);
    $('#' + tableId + '-save-btn, #' + tableId + '-cancel-btn').toggle(editing);
  }

  function wireEditable(tableId, config, api, saveMode) {
    var ns = '.odtedit_' + tableId;
    var reg = registry[tableId];

    $(document)
      .off('click' + ns)
      .on('click' + ns, '#' + tableId + '-edit-mode-btn', function () { reg.editing = true; reg.dirty = false; toggleEditButtons(tableId, true, saveMode); api.draw(false); })
      .on('click' + ns, '#' + tableId + '-cancel-btn', function () { reg.editing = false; reg.dirty = false; toggleEditButtons(tableId, false, saveMode); api.draw(false); })
      .on('click' + ns, '#' + tableId + '-save-btn', function () {
        var payload = collectPayload(tableId);
        if (saveMode === 'custom') { reg.dirty = false; call(tableId, 'onSave', [payload]); return; }
        postSave(config, payload)
          .done(function (resp) {
            var parsed = util.parseApiResponse ? util.parseApiResponse(resp) : { isSuccess: true };
            if (parsed.isSuccess === false) { util.notify('error', parsed.message || 'Save failed'); return; }
            util.notify('success', parsed.message || 'Saved');
            reg.editing = false; reg.dirty = false; toggleEditButtons(tableId, false, saveMode); api.ajax.reload();
          })
          .fail(function (xhr, status) { util.handleAjaxError(xhr, status); });
      });

    if (saveMode === 'auto') {
      $(document).off('change.odtauto_' + tableId).on('change.odtauto_' + tableId, '#' + tableId + ' [data-odt-edit]', function () {
        var $el = $(this);
        var change = { id: $el.data('row-id') };
        change[$el.data('field')] = $el.val();
        postSave(config, [change])
          .done(function () { util.notify('success', 'Saved'); })
          .fail(function (xhr, status) { util.handleAjaxError(xhr, status); });
      });
    } else {
      // Manual/Custom accumulate edits in the DOM until saved — guard against losing them on a page change.
      wireEditGuard(tableId, config, api, saveMode);
    }

    toggleEditButtons(tableId, reg.editing, saveMode);
  }

  // Persist the current page's edits without leaving edit mode (used by the unsaved-edit page-change guard).
  // Manual posts to the save endpoint; Custom delegates to the host onSave. Auto never reaches here.
  function saveCurrentEdits(tableId, config, saveMode) {
    var reg = registry[tableId];
    var payload = collectPayload(tableId);
    if (reg) reg.dirty = false;
    if (saveMode === 'custom') { call(tableId, 'onSave', [payload]); return; }
    postSave(config, payload)
      .done(function (resp) {
        var parsed = util.parseApiResponse ? util.parseApiResponse(resp) : { isSuccess: true };
        if (parsed.isSuccess === false) { util.notify('error', parsed.message || 'Save failed'); return; }
        util.notify('success', parsed.message || 'Saved');
      })
      .fail(function (xhr, status) { util.handleAjaxError(xhr, status); });
  }

  // Guards page navigation while the current page has unsaved inline edits (Manual/Custom). Behavior comes
  // from config.unsavedEditBehavior: 'autosave' persists the edits then lets navigation proceed; 'warn'
  // blocks the pager click and asks (on "discard" we clear the flag and re-issue the click so DataTables
  // handles it natively); 'none' disables the guard. The listener is capture-phase on document (so it runs
  // before DataTables' own bubble handler) and matched to this table's wrapper, because the pagination DOM
  // is rebuilt on every draw.
  function wireEditGuard(tableId, config, api, saveMode) {
    var reg = registry[tableId];
    var mode = (config.unsavedEditBehavior || 'warn').toString().toLowerCase();

    // Mark unsaved edits whenever an editor value changes while editing.
    $(document)
      .off('input.odtdirty_' + tableId + ' change.odtdirty_' + tableId)
      .on('input.odtdirty_' + tableId + ' change.odtdirty_' + tableId, '#' + tableId + ' [data-odt-edit]', function () {
        if (reg.editing) reg.dirty = true;
      });

    if (mode === 'none') return;

    var wrapperSel = '#' + tableId + '_wrapper';
    if (reg._editGuardHandler) document.removeEventListener('click', reg._editGuardHandler, true);
    var handler = function (e) {
      if (!reg.editing || !reg.dirty) return;
      var target = e.target;
      if (!target || !target.closest) return;
      var link = target.closest(wrapperSel + ' .paginate_button, ' + wrapperSel + ' .page-link');
      if (!link) return;
      // Ignore no-op clicks on the active/disabled pager buttons (current page, ellipsis, edge arrows).
      var item = link.closest('.page-item') || link;
      if (item.classList.contains('disabled') || item.classList.contains('active') ||
          link.classList.contains('disabled') || link.classList.contains('current')) return;

      if (mode === 'autosave') { saveCurrentEdits(tableId, config, saveMode); return; }

      // warn: block the navigation, ask, and re-issue the same click on "discard".
      e.preventDefault();
      e.stopImmediatePropagation();
      var loc = (ODT.config && ODT.config.locale) || {};
      util.confirm({
        title: loc.unsavedChangesTitle || 'Unsaved changes',
        text: loc.unsavedChangesMessage || 'You have unsaved changes on this page. Discard them and continue?',
        confirmText: loc.discardChanges || 'Discard',
        cancelText: loc.keepEditing || 'Keep editing',
        icon: 'warning'
      }).then(function (discard) {
        if (discard) { reg.dirty = false; link.click(); }
      });
    };
    document.addEventListener('click', handler, true);
    reg._editGuardHandler = handler;
  }

  // ----- error ----------------------------------------------------------------

  function ajaxError(xhr, status) {
    if (util.handleAjaxError) util.handleAjaxError(xhr, status);
  }

  // ----- init ------------------------------------------------------------------

  DataTable.init = function (config) {
    if (!config || !config.tableId) return null;
    var tableId = config.tableId;
    var $table = $('#' + tableId);
    if (!$table.length) return null;
    if ($.fn.DataTable.isDataTable($table)) return $table.DataTable();

    registry[tableId] = $.extend(registry[tableId] || {}, { config: config });

    // Editable when editorConfigs are present (mirrors how Items makes a Select2 static).
    var editors = editorMap(config);
    var editable = Object.keys(editors).length > 0;
    var saveMode = (config.saveMode || 'Manual').toString().toLowerCase();
    registry[tableId].editing = editable && saveMode !== 'manual'; // auto/custom edit inline immediately

    var defs = buildColumns(config, tableId, editors);
    var orderArr = [];
    if (config.defaultSortColumn) {
      var idx = defs.findIndex(function (d) { return d.data === config.defaultSortColumn; });
      if (idx >= 0) orderArr = [[idx, (config.defaultSortDirection || 'asc')]];
    }

    var loadTrigger = (config.loadTrigger || 'immediate').toLowerCase();
    var deferLoad = loadTrigger !== 'immediate';

    // Resolve the row callback once at init rather than per row on every draw.
    var rowCbFn = resolveNamedFn(tableId, config.rowCallback);

    var options = {
      processing: true,
      serverSide: true,
      searching: false,
      ordering: true,
      order: orderArr,
      deferLoading: deferLoad ? 0 : null,
      pageLength: config.pageLength || (ODT.config && ODT.config.pageLength) || 50,
      lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, 'All']],
      dom: buildDom(config),
      columns: defs,
      ajax: { url: config.ajaxUrl, type: 'POST', data: function (d) { return buildAjaxData(tableId, d, config); }, error: ajaxError },
      drawCallback: function () {
        if (config.hasNumbering) { try { applyNumbering(this.api()); } catch (e) { /* noop */ } }
      },
      rowCallback: function (rowEl, data) {
        if (rowCbFn) { try { rowCbFn(rowEl, data); } catch (e) { /* noop */ } }
      }
    };

    // B0 escape hatch: merge raw datatables.net options, then allow host JS to patch via beforeInit.
    // mergeOptions replaces arrays wholesale (so `columns`/`order`/`lengthMenu` override cleanly) and is
    // prototype-pollution-safe. The structural ajax.data (DataTableQueryViewModel mapping) and ajax.error
    // are re-asserted after BOTH the merge and a beforeInit replacement, so neither can drop them; host
    // ajax sub-keys (url, headers, …) still merge through. A non-object `ajax` is normalized (never throws).
    var builtinAjaxUrl = options.ajax.url;
    var builtinAjaxData = options.ajax.data;
    var builtinAjaxError = options.ajax.error;
    function protectAjax(opts) {
      if (!opts.ajax || typeof opts.ajax !== 'object' || Array.isArray(opts.ajax)) opts.ajax = {};
      // data/error are structural (the DataTableQueryViewModel mapper + 401 handler) — always re-assert.
      opts.ajax.data = builtinAjaxData;
      opts.ajax.error = builtinAjaxError;
      // url stays host-overridable, but must never be LOST: a beforeInit that returns a fresh object
      // without ajax.url would otherwise make DataTables POST to the page URL. Restore only when absent.
      if (opts.ajax.url == null) opts.ajax.url = builtinAjaxUrl;
      return opts;
    }

    if (config.extraOptions) {
      mergeOptions(options, config.extraOptions);
      protectAjax(options);
    }
    var reg0 = registry[tableId];
    if (reg0 && reg0.handlers && typeof reg0.handlers.beforeInit === 'function') {
      var patched = reg0.handlers.beforeInit(options);
      if (patched) options = patched;
      protectAjax(options);
    }

    var api = $table.DataTable(options);
    registry[tableId].api = api;

    wireActions(tableId, config);
    wireFilters(tableId, config, api);
    if (config.hasChildRows && Array.isArray(config.childColumns) && config.childColumns.length) wireChildRows(tableId, config, api);
    wireLoadTrigger(tableId, config, api, loadTrigger);
    if (editable) wireEditable(tableId, config, api, saveMode);

    return api;
  };

  function wireLoadTrigger(tableId, config, api, loadTrigger) {
    if (loadTrigger === 'immediate') return;
    var fire = function () { api.ajax.reload(); };
    if (loadTrigger === 'onclick' && config.triggerSelector) {
      $(document).off('click.odtload_' + tableId).on('click.odtload_' + tableId, config.triggerSelector, fire);
    } else if (loadTrigger === 'custom' && config.triggerEvent) {
      $(document).off(config.triggerEvent + '.odtload_' + tableId).on(config.triggerEvent + '.odtload_' + tableId, fire);
    }
  }
})(window, document);
