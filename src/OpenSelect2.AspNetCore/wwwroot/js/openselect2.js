/*!
 * OpenSelect2.AspNetCore — client runtime
 * Renders AJAX & static Select2 dropdowns from a JSON config block, with cascading parent/child
 * chains, infinite-scroll paging, a "Select All" option, and read-only support.
 *
 * Lifecycle: a `<script type="application/json" data-component="Select2">{config}</script>` block is
 * emitted next to each dropdown by the ViewComponent. This module scans for those on DOMContentLoaded,
 * after every `htmx:afterSwap`, and whenever you call `OpenSelect2.scan(rootElement)` — no per-instance
 * inline script, no retry hacks.
 *
 * Peer deps (host-provided): jQuery, select2. SweetAlert2 is used only if present.
 */
(function (window, document) {
  'use strict';

  var $ = window.jQuery;
  if (!$) {
    // eslint-disable-next-line no-console
    console.error('[OpenSelect2] jQuery is required but was not found.');
    return;
  }

  var OpenSelect2 = (window.OpenSelect2 = window.OpenSelect2 || {});

  // --- Host configuration (the <os2-scripts/> tag helper overwrites this; defaults below). ---
  // Deep-merge (like opendatatables-core) so a host that sets only part of `locale` keeps the other
  // default locale keys instead of dropping them.
  OpenSelect2.config = $.extend(
    true,
    {
      loginUrl: null,
      ajaxDelayMs: 250,
      defaultLimit: 10,
      reinitEvents: [], // extra DOM events (besides htmx:afterSwap) that should re-scan
      onUnauthorized: null, // function(jqXHR): return true if you fully handled the 401
      onError: null, // function(jqXHR): return true if you fully handled the error
      locale: {
        sessionExpiredTitle: 'Warning',
        sessionExpiredMessage: 'Your session has expired. Please log in again.',
        selectAllText: '(Select All)',
        errorTitle: 'Error'
      }
    },
    OpenSelect2.config
  );

  // Per-id handler registry: OpenSelect2.on(id, { beforeInit, templateResult, templateSelection }).
  // Object.create(null) prevents __proto__ / constructor prototype-pollution attacks.
  var os2Registry = (OpenSelect2._instances = OpenSelect2._instances || Object.create(null));

  /** Register host callbacks for a dropdown id. Call before DOMContentLoaded for best results. */
  OpenSelect2.on = function (id, handlers) {
    if (!id || typeof id !== 'string') return;
    os2Registry[id] = os2Registry[id] || {};
    os2Registry[id] = $.extend({}, os2Registry[id], handlers);
  };

  // True only for plain {} objects — NOT Date/RegExp/jQuery/DOM nodes/class instances. The deep merge
  // recurses only into plain objects and REPLACES everything else by reference, so a special value such
  // as a jQuery dropdownParent passed through the escape hatch is assigned, not recursed-into and lost.
  function isPlainObject(o) {
    if (!o || typeof o !== 'object' || Array.isArray(o)) return false;
    var proto = Object.getPrototypeOf(o);
    return proto === Object.prototype || proto === null;
  }

  // Deep-merge that REPLACES arrays wholesale (jQuery's $.extend(true, …) merges arrays by index, which
  // silently corrupts array-valued select2 options like `data` passed through the escape hatch) and skips
  // prototype-polluting keys (__proto__/constructor/prototype).
  function mergeOptions(target, src) {
    if (!src || typeof src !== 'object') return target;
    Object.keys(src).forEach(function (key) {
      if (key === '__proto__' || key === 'constructor' || key === 'prototype') return;
      var val = src[key];
      if (Array.isArray(val)) {
        target[key] = val.slice();
      } else if (isPlainObject(val) && isPlainObject(target[key])) {
        mergeOptions(target[key], val);
      } else {
        target[key] = val;
      }
    });
    return target;
  }

  // Apply registered host callbacks (templateResult/templateSelection, then beforeInit, which may
  // return a replacement settings object) onto a select2 settings object. Returns the settings to use.
  function applyRegistry(settings, reg) {
    if (!reg) return settings;
    if (typeof reg.templateResult === 'function') settings.templateResult = reg.templateResult;
    if (typeof reg.templateSelection === 'function') settings.templateSelection = reg.templateSelection;
    if (typeof reg.beforeInit === 'function') { var patched = reg.beforeInit(settings); if (patched) settings = patched; }
    return settings;
  }

  function locale() {
    return OpenSelect2.config.locale || {};
  }

  // ---------------------------------------------------------------------------
  // "Select All" option registry (was window.select2ShowAllOptionMap in site.js).
  // ---------------------------------------------------------------------------
  var allOptionMap = {};

  /** Enable/disable the synthetic "(Select All)" option for a given dropdown id at runtime. */
  OpenSelect2.setAllOptionEnabled = function (id, enabled) {
    allOptionMap[id] = !!enabled;
    var $el = $('#' + id);
    if ($el.length) $el.val(null).trigger('change.select2');
  };

  function isAllOptionEnabled(id, fallback) {
    return Object.prototype.hasOwnProperty.call(allOptionMap, id) ? allOptionMap[id] : fallback;
  }

  function addAllOption(items, text) {
    var allOption = { id: '-1', text: text || 'All' };
    if (!items.some(function (item) { return item.id == allOption.id; })) {
      items.unshift(allOption);
    }
    return items;
  }

  // ---------------------------------------------------------------------------
  // Ported helpers (verbatim behavior from the original site.js).
  // ---------------------------------------------------------------------------

  /** Flattens a (Model)State / errors payload into an HTML string for display. */
  function unwrapModelStateErrorsAsHtml(response) {
    var messages = [];

    if (typeof response === 'string') {
      try {
        response = JSON.parse(response);
      } catch (e) {
        return response; // plain-text fallback
      }
    }

    if (typeof response === 'object' && response !== null) {
      if (response.errors) {
        var errorObj = response.errors;
        for (var key in errorObj) {
          if (Object.prototype.hasOwnProperty.call(errorObj, key)) {
            var fieldErrors = errorObj[key];
            if (Array.isArray(fieldErrors)) {
              fieldErrors.forEach(function (error) {
                if (key.toLowerCase() !== 'service') messages.push('<strong>' + key + '</strong>: ' + error);
                else messages.push(error);
              });
            }
          }
        }
      } else {
        for (var k in response) {
          if (Object.prototype.hasOwnProperty.call(response, k)) {
            var errors = response[k];
            if (Array.isArray(errors)) {
              errors.forEach(function (error) {
                if (k.toLowerCase() !== 'service') messages.push('<strong>' + k + '</strong>: ' + error);
                else messages.push(error);
              });
            } else if (typeof errors === 'string') {
              messages.push(errors);
            }
          }
        }
      }

      return messages.length === 0 ? 'An unknown error occurred.' : messages.join('<br>');
    }

    return 'An unexpected error occurred.';
  }
  OpenSelect2.unwrapModelStateErrorsAsHtml = unwrapModelStateErrorsAsHtml;

  /** Recursively resets and disables every descendant of a parent dropdown (DOM-driven registry). */
  function resetAllChildren(parentId) {
    $("select[data-parent-id='" + parentId + "']").each(function () {
      var childId = $(this).attr('id');
      $(this).val(null).trigger('change.select2');
      $(this).prop('disabled', true);
      resetAllChildren(childId);
    });
  }
  OpenSelect2.resetAllChildren = resetAllChildren;

  function setReadOnly($select, isReadOnly) {
    if (isReadOnly) {
      $select.next('.select2-container').css({ 'pointer-events': 'none', opacity: '0.65' });
      $select.on('select2:opening', function (e) {
        e.preventDefault();
        return false;
      });
    } else {
      $select.next('.select2-container').css({ 'pointer-events': '', opacity: '' });
      $select.off('select2:opening');
    }
  }
  OpenSelect2.setReadOnly = setReadOnly;

  // ---------------------------------------------------------------------------
  // Failure handling (401 / generic), SweetAlert2-optional.
  // ---------------------------------------------------------------------------
  function redirectToLogin() {
    var url = OpenSelect2.config.loginUrl;
    if (url) window.location.href = url;
    else window.location.reload();
  }

  function handleUnauthorized(jqXHR) {
    if (typeof OpenSelect2.config.onUnauthorized === 'function' && OpenSelect2.config.onUnauthorized(jqXHR) === true) {
      return;
    }
    if (typeof window.Swal !== 'undefined') {
      window.Swal.fire({
        title: locale().sessionExpiredTitle,
        text: locale().sessionExpiredMessage,
        icon: 'warning',
        showCancelButton: false,
        confirmButtonColor: '#3085d6',
        confirmButtonText: 'Ok'
      }).then(function (result) {
        if (result.isConfirmed) redirectToLogin();
      });
    } else if (window.confirm(locale().sessionExpiredMessage)) {
      redirectToLogin();
    }
  }

  function handleError(jqXHR) {
    if (typeof OpenSelect2.config.onError === 'function' && OpenSelect2.config.onError(jqXHR) === true) {
      return;
    }
    var html = unwrapModelStateErrorsAsHtml(jqXHR.responseText);
    if (typeof window.Swal !== 'undefined') {
      window.Swal.fire({ icon: 'error', title: locale().errorTitle, html: html });
    } else {
      window.alert(typeof html === 'string' ? html.replace(/<[^>]+>/g, '') : 'Error');
    }
  }

  // ---------------------------------------------------------------------------
  // Cascade (parent → child enable/disable + descendant reset).
  // ---------------------------------------------------------------------------
  function wireCascade($el, config) {
    var $parent = $('#' + config.parentId);
    if (!$parent.length) return;

    var forceDisabled = !!config.forceDisabled;
    var enableChildrenIfPreSelected = !!config.enableChildrenIfPreSelected;
    var childNs = '.os2child_' + $el.attr('id');
    var parentVal = $parent.val();
    var parentIsDisabled = $parent.prop('disabled');

    $el.data('force-disabled', forceDisabled);
    $el.prop('disabled', true);

    if (!forceDisabled && parentVal && (!parentIsDisabled || (parentIsDisabled && enableChildrenIfPreSelected))) {
      $el.prop('disabled', false);
    }

    $parent
      .off('change' + childNs + ' select2:change' + childNs)
      .on('change' + childNs + ' select2:change' + childNs, function () {
        var val = $(this).val();
        var previousVal = $(this).data('previous-val');
        $(this).data('previous-val', val);

        if (val) {
          if (previousVal && previousVal !== val) {
            $el.val(null).trigger('change.select2');
            resetAllChildren($el.attr('id'));
          }
          if (!forceDisabled) $el.prop('disabled', false);
        } else {
          $el.val(null).trigger('change.select2');
          resetAllChildren($el.attr('id'));
          $el.prop('disabled', true);
        }
      });

    // Sync child state once the parent's initial value has settled.
    setTimeout(function () {
      if (parentVal) $parent.trigger('change');
    }, 100);
  }

  // ---------------------------------------------------------------------------
  // init — one shared implementation (ported from the old per-instance inline script).
  // ---------------------------------------------------------------------------
  function init(config) {
    if (!config || !config.id) return;
    var $el = $('#' + config.id);
    if (!$el.length) return;

    if ($el.hasClass('select2-hidden-accessible')) {
      try { $el.select2('destroy'); } catch (e) { /* noop */ }
    }

    // Inside a modal anchor the dropdown to the modal element (keeps stacking + focus correct); outside a
    // modal use body — consistent across the static + AJAX paths and avoids overflow-clip from parents.
    var $modal = $el.closest('.modal');

    // Static (local list): options are already in the markup — enhance without an AJAX data source.
    if (config.isStatic || !config.ajaxUrl) {
      var staticSettings = {
        placeholder: config.placeholder || 'Select...',
        allowClear: true,
        width: '100%',
        dropdownParent: $modal.length ? $modal : $('body')
      };
      if (config.select2Options) mergeOptions(staticSettings, config.select2Options);
      staticSettings = applyRegistry(staticSettings, os2Registry[config.id]);
      $el.select2(staticSettings);
      setReadOnly($el, !!config.isReadOnly);
      return;
    }

    allOptionMap[config.id] = !!config.canSelectAll;

    var limit = config.limit || OpenSelect2.config.defaultLimit || 10;
    var placeholder = config.placeholder || 'Select...';
    var extraParams = $.extend({}, config.extraParams || {});
    var extraParamsHTML = config.extraParamsHTML || {};

    if (config.parentId) wireCascade($el, config);

    var ajaxSettings = {
      placeholder: placeholder,
      allowClear: true,
      width: '100%',
      dropdownParent: $modal.length ? $modal : $('body'),
      ajax: {
        url: config.ajaxUrl,
        dataType: 'json',
        delay: OpenSelect2.config.ajaxDelayMs,
        data: function (params) {
          for (var key in extraParamsHTML) {
            if (Object.prototype.hasOwnProperty.call(extraParamsHTML, key)) {
              extraParams[key] = $(extraParamsHTML[key]).val() || '';
            }
          }
          var query = $.extend(
            { searchTerm: params.term || '', page: params.page || 1, limit: limit },
            extraParams
          );
          if (config.parentId) query.parentValue = $('#' + config.parentId).val();
          return query;
        },
        processResults: function (data, params) {
          params.page = params.page || 1;
          var showAll = !!config.canSelectAll && isAllOptionEnabled(config.id, true);
          if (showAll && params.page === 1) {
            data.items = addAllOption(data.items, locale().selectAllText);
          }
          return { results: data.items, pagination: { more: data.hasMore } };
        },
        cache: true,
        transport: function (params, success, failure) {
          var request = $.ajax(params);
          request.fail(function (jqXHR, textStatus) {
            if (textStatus === 'abort') return; // typing fast cancels prior requests — ignore
            if (jqXHR.status === 401) handleUnauthorized(jqXHR);
            else if (jqXHR.status === 400) console.error('[OpenSelect2] Bad request:', jqXHR.responseText);
            else if (jqXHR.status > 0) handleError(jqXHR);
          });
          request.done(success);
          return request;
        }
      }
    };

    // B0 escape hatch: merge raw select2 options, then allow host JS to patch via beforeInit / templates.
    // mergeOptions replaces arrays wholesale. The built-in ajax.transport (handles 401/errors) is
    // re-asserted after BOTH the merge and a beforeInit replacement, so neither can drop it; host ajax
    // sub-keys (url, delay, …) still merge through. A non-object `ajax` is normalized (never throws).
    var _url = ajaxSettings.ajax.url;
    var _transport = ajaxSettings.ajax.transport;
    function protectTransport(s) {
      if (!s.ajax || typeof s.ajax !== 'object' || Array.isArray(s.ajax)) s.ajax = {};
      s.ajax.transport = _transport;
      // url stays host-overridable but must never be LOST (a beforeInit returning a fresh object without
      // ajax.url would otherwise break the remote source). Restore only when absent.
      if (s.ajax.url == null) s.ajax.url = _url;
      return s;
    }

    if (config.select2Options) {
      mergeOptions(ajaxSettings, config.select2Options);
      protectTransport(ajaxSettings);
    }
    ajaxSettings = applyRegistry(ajaxSettings, os2Registry[config.id]);
    protectTransport(ajaxSettings);

    $el.select2(ajaxSettings);

    setReadOnly($el, !!config.isReadOnly);
  }
  OpenSelect2.init = init;

  // ---------------------------------------------------------------------------
  // Scanner — finds config blocks and initializes them once.
  // ---------------------------------------------------------------------------
  function scanComponent(root, component, initFn) {
    var scope = root || document;
    var scripts = scope.querySelectorAll('script[type="application/json"][data-component="' + component + '"]');
    Array.prototype.forEach.call(scripts, function (script) {
      if (script.dataset.processed === '1') return;
      var config;
      try {
        config = JSON.parse((script.textContent || '').trim());
      } catch (e) {
        console.error('[OpenSelect2] Invalid ' + component + ' config JSON:', e, script);
        return;
      }
      script.dataset.processed = '1';
      initFn(config);
    });
  }

  /** Scan a subtree (defaults to the whole document) and initialize any not-yet-processed dropdowns. */
  OpenSelect2.scan = function (root) {
    scanComponent(root, 'Select2', init);
  };

  // ---------------------------------------------------------------------------
  // Lifecycle.
  // ---------------------------------------------------------------------------
  function bootstrap() {
    OpenSelect2.scan(document);

    if (document.body) {
      document.body.addEventListener('htmx:afterSwap', function (evt) {
        var target = evt && evt.detail && evt.detail.target ? evt.detail.target : document;
        OpenSelect2.scan(target);
      });

      (OpenSelect2.config.reinitEvents || []).forEach(function (name) {
        document.body.addEventListener(name, function () {
          OpenSelect2.scan(document);
        });
      });
    }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bootstrap, { once: true });
  } else {
    bootstrap();
  }
})(window, document);
