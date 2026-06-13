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

  function call(tableId, name, args) {
    var fn = resolveHandler(tableId, name);
    if (fn) { try { fn.apply(null, args || []); } catch (e) { console.error('[OpenDataTables] handler ' + name + ' threw', e); } return true; }
    return false;
  }

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
            // Host-defined global formatter takes precedence; else the built-in date formatter.
            if (typeof window[col.format] === 'function') { try { return window[col.format](data); } catch (e) { /* noop */ } }
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

  function hasActionColumn(config) {
    return config.showView || config.showEdit || config.showDelete ||
      (Array.isArray(config.customButtons) && config.customButtons.some(function (b) { return (b.placement || 'row') === 'row'; }));
  }

  function renderActions(config, row) {
    var loc = (ODT.config && ODT.config.locale) || {};
    var id = rowId(row);
    var html = '<div class="btn-group btn-group-sm" role="group">';
    if (config.showView) html += actionBtn('view', id, 'btn-info', 'fas fa-eye', loc.view || 'View');
    if (config.showEdit) html += actionBtn('edit', id, 'btn-primary', 'fas fa-edit', config.customEditText || loc.edit || 'Edit');
    if (config.showDelete) html += actionBtn('delete', id, 'btn-danger', 'fas fa-trash', config.customDeleteText || loc.delete || 'Delete');

    if (Array.isArray(config.customButtons)) {
      config.customButtons.forEach(function (btn) {
        if ((btn.placement || 'row') !== 'row') return;
        var icon = btn.icon ? '<i class="' + btn.icon + '"></i> ' : '';
        var rowData = util.escapeHtml(JSON.stringify(row));
        html += '<button type="button" id="' + (btn.id || '') + '" class="btn ' + (btn.cssClass || 'btn-secondary') + ' btn-sm me-1"' +
          (btn.style ? ' style="' + btn.style + '"' : '') +
          ' data-odt-action="custom" data-odt-handler="' + util.escapeHtml(btn.onClick || '') + '"' +
          ' data-odt-row="' + rowData + '" title="' + util.escapeHtml(btn.title || btn.text || '') + '">' + icon + util.escapeHtml(btn.text || '') + '</button>';
      });
    }
    return html + '</div>';
  }

  function actionBtn(action, id, css, icon, title) {
    return '<button type="button" class="btn ' + css + ' btn-sm me-1" data-odt-action="' + action + '" data-id="' + util.escapeHtml(id) +
      '" title="' + util.escapeHtml(title) + '"><i class="' + icon + '"></i></button>';
  }

  function buildDom(config) {
    var hasTop = config.showAdd || (Array.isArray(config.customButtons) && config.customButtons.some(function (b) { return (b.placement || 'row') === 'top'; }));
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
        if (typeof window[handlerName] === 'function') window[handlerName](row);
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
      initChildTable(childId, config, row.data());
    });
  }

  function initChildTable(childId, config, parentRow) {
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
    if (config.childRowCallback && typeof window[config.childRowCallback] === 'function') {
      try { window[config.childRowCallback](childId, parentRow, childApi, config); } catch (e) { /* noop */ }
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
      .on('click' + ns, '#' + tableId + '-edit-mode-btn', function () { reg.editing = true; toggleEditButtons(tableId, true, saveMode); api.draw(false); })
      .on('click' + ns, '#' + tableId + '-cancel-btn', function () { reg.editing = false; toggleEditButtons(tableId, false, saveMode); api.draw(false); })
      .on('click' + ns, '#' + tableId + '-save-btn', function () {
        var payload = collectPayload(tableId);
        if (saveMode === 'custom') { call(tableId, 'onSave', [payload]); return; }
        postSave(config, payload)
          .done(function (resp) {
            var parsed = util.parseApiResponse ? util.parseApiResponse(resp) : { isSuccess: true };
            if (parsed.isSuccess === false) { util.notify('error', parsed.message || 'Save failed'); return; }
            util.notify('success', parsed.message || 'Saved');
            reg.editing = false; toggleEditButtons(tableId, false, saveMode); api.ajax.reload();
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
    }

    toggleEditButtons(tableId, reg.editing, saveMode);
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
        if (config.rowCallback && typeof window[config.rowCallback] === 'function') {
          try { window[config.rowCallback](rowEl, data); } catch (e) { /* noop */ }
        }
      }
    };

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
