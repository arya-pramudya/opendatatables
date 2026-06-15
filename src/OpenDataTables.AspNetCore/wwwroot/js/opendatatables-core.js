/*!
 * OpenDataTables.AspNetCore — core runtime (window.OpenDataTables + .util + .config)
 *
 * Self-contained generic helpers (no host site.js globals) plus the host-configurable object the
 * components read. SweetAlert2 is used only when present; otherwise native confirm/alert are used.
 *
 * Peer deps (host-provided): jQuery, datatables.net. Optional: SweetAlert2, select2 (for select filters).
 */
(function (window, document) {
  'use strict';

  var $ = window.jQuery;
  if (!$) {
    // eslint-disable-next-line no-console
    console.error('[OpenDataTables] jQuery is required but was not found.');
    return;
  }

  var ODT = (window.OpenDataTables = window.OpenDataTables || {});

  // --- Host configuration (the <odt-scripts/> tag helper overwrites this; defaults below). ---
  ODT.config = $.extend(
    true,
    {
      loginUrl: null,
      pageLength: 50,
      reinitEvents: [],
      onUnauthorized: null, // function(jqXHR): return true if fully handled
      onError: null, // function(jqXHR): return true if fully handled
      notify: null, // function(type, message): return true if fully handled
      // Theming hooks for the JS-rendered row action buttons. Override to retarget a different icon set
      // (e.g. Font Awesome 6, Bootstrap Icons) or CSS framework without forking the runtime.
      icons: {
        view: 'fas fa-eye',
        edit: 'fas fa-edit',
        delete: 'fas fa-trash'
      },
      actionClasses: {
        group: 'btn-group btn-group-sm',
        view: 'btn-info',
        edit: 'btn-primary',
        delete: 'btn-danger'
      },
      locale: {
        add: 'Add',
        edit: 'Edit',
        delete: 'Delete',
        view: 'View',
        search: 'Search',
        resetFilters: 'Reset Filters',
        actions: 'Actions',
        filterTitle: 'Filter Data',
        sessionExpiredTitle: 'Warning',
        sessionExpiredMessage: 'Your session has expired. Please log in again.'
      }
    },
    ODT.config
  );

  function locale() {
    return ODT.config.locale || {};
  }
  function hasSwal() {
    return typeof window.Swal !== 'undefined';
  }

  var util = (ODT.util = ODT.util || {});

  util.escapeHtml = function (value) {
    if (value == null) return '';
    return String(value)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  };

  // Deep-merge that REPLACES arrays wholesale (so option arrays like columns/order/lengthMenu override
  // cleanly instead of merging by index the way $.extend(true, …) does) and skips prototype-polluting
  // keys (__proto__/constructor/prototype). Shared by the DataTable + Select2 escape-hatch merges.
  util.mergeOptions = function mergeOptions(target, src) {
    if (!src || typeof src !== 'object') return target;
    Object.keys(src).forEach(function (key) {
      if (key === '__proto__' || key === 'constructor' || key === 'prototype') return;
      var val = src[key];
      if (Array.isArray(val)) {
        target[key] = val.slice();
      } else if (val && typeof val === 'object' &&
                 target[key] && typeof target[key] === 'object' && !Array.isArray(target[key])) {
        mergeOptions(target[key], val);
      } else {
        target[key] = val;
      }
    });
    return target;
  };

  var MONTHS = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
  var MONTHS_LONG = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];

  function pad(n) {
    return n < 10 ? '0' + n : '' + n;
  }

  /**
   * Format a date value using moment-ish tokens: YYYY, YY, MMMM, MMM, MM, M, DD, D, HH, mm, ss.
   * Returns the original value unchanged if it is not parseable.
   */
  util.formatDateIntl = function (value, format) {
    if (value == null || value === '') return '';
    var d = value instanceof Date ? value : new Date(value);
    if (isNaN(d.getTime())) return value;
    var fmt = format && format !== 'date' && format !== 'datetime' ? format : 'DD MMM YYYY';
    return fmt.replace(/YYYY|YY|MMMM|MMM|MM|M|DD|D|HH|mm|ss/g, function (token) {
      switch (token) {
        case 'YYYY': return '' + d.getFullYear();
        case 'YY': return ('' + d.getFullYear()).slice(-2);
        case 'MMMM': return MONTHS_LONG[d.getMonth()];
        case 'MMM': return MONTHS[d.getMonth()];
        case 'MM': return pad(d.getMonth() + 1);
        case 'M': return '' + (d.getMonth() + 1);
        case 'DD': return pad(d.getDate());
        case 'D': return '' + d.getDate();
        case 'HH': return pad(d.getHours());
        case 'mm': return pad(d.getMinutes());
        case 'ss': return pad(d.getSeconds());
        default: return token;
      }
    });
  };

  util.formatNumber = function (value, decimals) {
    var n = Number(value);
    if (isNaN(n)) return value;
    return n.toLocaleString(undefined, {
      minimumFractionDigits: decimals || 0,
      maximumFractionDigits: decimals == null ? 20 : decimals
    });
  };

  /** Normalizes a server envelope to { isSuccess, message, data }. */
  util.parseApiResponse = function (response) {
    if (response == null) return { isSuccess: false, message: '', data: null };
    if (typeof response === 'string') {
      try { response = JSON.parse(response); } catch (e) { return { isSuccess: false, message: response, data: null }; }
    }
    var isSuccess = response.isSuccess != null ? response.isSuccess
      : (response.IsSuccess != null ? response.IsSuccess : response.Result === 'Success');
    return {
      isSuccess: !!isSuccess,
      message: response.message || response.Message || '',
      data: response.data != null ? response.data : response.Data
    };
  };

  /** Flattens a (Model)State / errors payload into an HTML string. */
  util.unwrapModelStateErrorsAsHtml = function (response) {
    var messages = [];
    if (typeof response === 'string') {
      try { response = JSON.parse(response); } catch (e) { return response; }
    }
    if (typeof response === 'object' && response !== null) {
      var source = response.errors || response;
      for (var key in source) {
        if (!Object.prototype.hasOwnProperty.call(source, key)) continue;
        var errs = source[key];
        if (Array.isArray(errs)) {
          errs.forEach(function (e) {
            messages.push(key.toLowerCase() === 'service' ? e : '<strong>' + key + '</strong>: ' + e);
          });
        } else if (typeof errs === 'string') {
          messages.push(errs);
        }
      }
      return messages.length ? messages.join('<br>') : 'An unknown error occurred.';
    }
    return 'An unexpected error occurred.';
  };

  /** Toast/alert. type: 'success' | 'error' | 'warning' | 'info'. SweetAlert2-optional. */
  util.notify = function (type, message) {
    if (typeof ODT.config.notify === 'function' && ODT.config.notify(type, message) === true) return;
    if (hasSwal()) {
      window.Swal.fire({ icon: type === 'error' ? 'error' : type, title: message, toast: true, position: 'top-end', timer: 3000, showConfirmButton: false });
    } else if (type === 'error') {
      window.alert(message);
    } else {
      // eslint-disable-next-line no-console
      console.log('[OpenDataTables] ' + type + ': ' + message);
    }
  };

  /** Confirmation dialog returning a Promise<boolean>. SweetAlert2-optional. */
  util.confirm = function (opts) {
    opts = opts || {};
    var text = opts.text || 'Are you sure?';
    if (hasSwal()) {
      return window.Swal.fire({
        title: opts.title || 'Confirm',
        text: text,
        icon: opts.icon || 'warning',
        showCancelButton: true,
        confirmButtonText: opts.confirmText || 'OK',
        cancelButtonText: opts.cancelText || 'Cancel'
      }).then(function (r) { return !!r.isConfirmed; });
    }
    return Promise.resolve(window.confirm(text));
  };

  function redirectToLogin() {
    if (ODT.config.loginUrl) window.location.href = ODT.config.loginUrl;
    else window.location.reload();
  }

  /** Standard AJAX failure handling (401 → login; else surface the error). Returns true if handled. */
  util.handleAjaxError = function (jqXHR, textStatus) {
    if (textStatus === 'abort') return true;
    if (!jqXHR || jqXHR.status === 0) return false;

    if (jqXHR.status === 401) {
      if (typeof ODT.config.onUnauthorized === 'function' && ODT.config.onUnauthorized(jqXHR) === true) return true;
      if (hasSwal()) {
        window.Swal.fire({
          title: locale().sessionExpiredTitle, text: locale().sessionExpiredMessage, icon: 'warning',
          confirmButtonText: 'Ok'
        }).then(function (r) { if (r.isConfirmed) redirectToLogin(); });
      } else if (window.confirm(locale().sessionExpiredMessage)) {
        redirectToLogin();
      }
      return true;
    }

    if (typeof ODT.config.onError === 'function' && ODT.config.onError(jqXHR) === true) return true;
    var html = util.unwrapModelStateErrorsAsHtml(jqXHR.responseText);
    if (hasSwal()) window.Swal.fire({ icon: 'error', title: locale().sessionExpiredTitle, html: html });
    else window.alert(typeof html === 'string' ? html.replace(/<[^>]+>/g, '') : 'Error');
    return true;
  };
})(window, document);
