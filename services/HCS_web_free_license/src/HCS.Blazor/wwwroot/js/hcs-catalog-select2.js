// Catalog lookup picker: Select2 4.x + Blazor JS interop (search by code and name).
(function () {
    window.hcsCatalogSelect2 = window.hcsCatalogSelect2 || {};

    function parseBool(v) {
        return v === true || v === "true" || v === "True" || v === 1 || v === "1";
    }

    function asArray(val) {
        if (val == null || val === "") return [];
        return Array.isArray(val) ? val : [val];
    }

    function fillOptions($el, items, multiple) {
        $el.empty();
        if (!multiple) {
            $el.append(new Option("", "", false, false));
        }
        if (!items || !items.length) {
            return;
        }
        for (var i = 0; i < items.length; i++) {
            var item = items[i];
            var option = new Option(item.text || "", item.id, true, true);
            if (item.description) option.setAttribute("data-description", item.description);
            if (item.avatarUrl) option.setAttribute("data-avatar-url", item.avatarUrl);
            if (item.initials) option.setAttribute("data-initials", item.initials);
            $el.append(option);
        }
        if (!multiple && items.length > 1) {
            $el.find("option").slice(2).remove();
        }
    }

    function initials(item) {
        if (item && item.initials) return item.initials;
        var text = (item && item.text) || "?";
        var parts = text.trim().split(/\s+/).filter(Boolean);
        return parts.length > 1
            ? (parts[0][0] + parts[parts.length - 1][0]).toUpperCase()
            : text.substring(0, 1).toUpperCase();
    }

    function userTemplate(item) {
        if (!item || !item.id) return item && item.text ? $("<span></span>").text(item.text) : "";
        var $root = $("<span class='hcs-user-option'></span>");
        if (item.avatarUrl) {
            var $avatar = $("<img class='hcs-user-option__avatar' alt='' loading='lazy'>").attr("src", item.avatarUrl);
            $avatar.on("error", function () {
                $(this).replaceWith($("<span class='hcs-user-option__avatar hcs-user-option__avatar--initials'></span>").text(initials(item)));
            });
            $avatar.appendTo($root);
        } else {
            $("<span class='hcs-user-option__avatar hcs-user-option__avatar--initials'></span>").text(initials(item)).appendTo($root);
        }
        var $copy = $("<span class='hcs-user-option__copy'></span>");
        $("<span class='hcs-user-option__name'></span>").text(item.text || "").appendTo($copy);
        if (item.description) $("<span class='hcs-user-option__phone'></span>").text(item.description).appendTo($copy);
        $copy.appendTo($root);
        return $root;
    }

    function userSelectionTemplate(item) {
        if (!item || !item.id) return item && item.text ? $("<span></span>").text(item.text) : "";
        var $root = $("<span class='hcs-user-option hcs-user-option--selection'></span>");
        if (item.avatarUrl) {
            var $avatar = $("<img class='hcs-user-option__avatar' alt='' loading='lazy'>").attr("src", item.avatarUrl);
            $avatar.on("error", function () {
                $(this).replaceWith($("<span class='hcs-user-option__avatar hcs-user-option__avatar--initials'></span>").text(initials(item)));
            });
            $avatar.appendTo($root);
        } else {
            $("<span class='hcs-user-option__avatar hcs-user-option__avatar--initials'></span>").text(initials(item)).appendTo($root);
        }
        var $copy = $("<span class='hcs-user-option__copy'></span>");
        $("<span class='hcs-user-option__name'></span>").text(item.text || "").appendTo($copy);
        $copy.appendTo($root);
        return $root;
    }

    function bindChange($el, dotNetRef) {
        $el.off("change.hcsCatalogSelect2");
        $el.on("change.hcsCatalogSelect2", function () {
            dotNetRef.invokeMethodAsync("OnSelectionChangeAsync", asArray($el.val()));
        });
    }

    window.hcsCatalogSelect2.init = function (selectId, dotNetRef, options, initialItems) {
        var $el = $("#" + selectId);
        if (!$el.length) {
            return;
        }
        if ($el.data("select2")) {
            $el.off(".hcsCatalogSelect2");
            $el.select2("destroy");
        }

        var placeholder = (options && options.placeholder) || "";
        var multiple = parseBool(options && options.multiple);
        var userTemplateEnabled = parseBool(options && options.userTemplate);
        $el.prop("multiple", multiple);
        fillOptions($el, initialItems, multiple);

        var dropdownParent = $el.closest(".modal").length ? $el.closest(".modal") : $(document.body);
        $el.select2({
            width: "100%",
            placeholder: placeholder,
            allowClear: true,
            multiple: multiple,
            dropdownParent: dropdownParent,
            minimumInputLength: typeof options.minimumInputLength === "number" ? options.minimumInputLength : 0,
            templateResult: userTemplateEnabled ? userTemplate : undefined,
            templateSelection: userTemplateEnabled ? userSelectionTemplate : undefined,
            escapeMarkup: function (markup) { return markup; },
            ajax: {
                delay: 250,
                transport: function (params, success, failure) {
                    var term = (params.data && params.data.term) ? params.data.term : "";
                    var page = params.data && params.data.page ? params.data.page : 1;
                    dotNetRef.invokeMethodAsync("SearchAsync", term, page).then(success).catch(failure);
                },
                processResults: function (data) {
                    return {
                        results: data.results || [],
                        pagination: { more: parseBool(data.more) }
                    };
                }
            }
        });

        $el.data("hcsCatalogSelect2DotNetRef", dotNetRef);
        bindChange($el, dotNetRef);
    };

    window.hcsCatalogSelect2.setSelection = function (selectId, items) {
        var $el = $("#" + selectId);
        if (!$el.length || !$el.data("select2")) {
            return;
        }
        var dotNetRef = $el.data("hcsCatalogSelect2DotNetRef");
        var multiple = $el.prop("multiple");
        $el.off("change.hcsCatalogSelect2");
        fillOptions($el, items, multiple);
        $el.trigger("change");
        if (dotNetRef) {
            bindChange($el, dotNetRef);
        }
    };

    window.hcsCatalogSelect2.destroy = function (selectId) {
        var $el = $("#" + selectId);
        if (!$el.length) {
            return;
        }
        $el.removeData("hcsCatalogSelect2DotNetRef");
        if ($el.data("select2")) {
            $el.off(".hcsCatalogSelect2");
            $el.select2("destroy");
        }
    };

    window.hcsCreateObjectUrl = function (contentType, bytes) {
        var binary;
        if (typeof bytes === "string") {
            var decoded = atob(bytes);
            binary = new Uint8Array(decoded.length);
            for (var i = 0; i < decoded.length; i++) {
                binary[i] = decoded.charCodeAt(i);
            }
        } else if (bytes instanceof ArrayBuffer) {
            binary = new Uint8Array(bytes);
        } else if (bytes instanceof Uint8Array) {
            binary = bytes;
        } else {
            binary = new Uint8Array(bytes || []);
        }
        var blob = new Blob([binary], { type: contentType || "application/octet-stream" });
        return URL.createObjectURL(blob);
    };

    window.hcsRevokeObjectUrl = function (url) {
        if (url) {
            URL.revokeObjectURL(url);
        }
    };

    window.hcsDownloadBytes = function (fileName, contentType, bytes) {
        var url = window.hcsCreateObjectUrl(contentType, bytes);
        var anchor = document.createElement("a");
        anchor.href = url;
        anchor.download = fileName || "file";
        document.body.appendChild(anchor);
        anchor.click();
        anchor.remove();
        URL.revokeObjectURL(url);
    };
})();

(function () {
    var GAP = 4;
    var PAD = 8;
    var placing = false;
    var scheduled = false;

    function calendars() {
        return document.querySelectorAll(".datepicker-calendar, .flatpickr-calendar");
    }

    function isOpen(el) {
        return !!(el && (el.classList.contains("show") || el.classList.contains("open")) && !el.classList.contains("d-none"));
    }

    function findInput(calendar) {
        if (calendar._hcsInput && document.contains(calendar._hcsInput)) return calendar._hcsInput;
        var wrap = calendar.closest(".hcs-datepicker")
            || calendar.closest(".b-is-datepicker")
            || calendar.closest(".field")
            || calendar.closest(".form-group");
        if (wrap) {
            var nested = wrap.querySelector("input:not([type=hidden])");
            if (nested) return nested;
        }
        var node = calendar.previousElementSibling;
        while (node) {
            if (node.matches && node.matches("input:not([type=hidden])")) return node;
            var found = node.querySelector && node.querySelector("input:not([type=hidden])");
            if (found) return found;
            node = node.previousElementSibling;
        }
        return null;
    }

    function restoreScroll(body, top) {
        if (body) body.scrollTop = top;
    }

    function keepFocus(e) {
        if (e.target.closest("input, select, textarea")) return;
        e.preventDefault();
    }

    function host(calendar) {
        if (calendar.parentElement && calendar.parentElement.classList.contains("hcs-datepicker-layer")) return;
        calendar._hcsParent = calendar.parentElement;
        calendar._hcsNext = calendar.nextSibling;
        calendar.dataset.hcsHosted = "1";
        if (!calendar._hcsKeepFocus) {
            calendar._hcsKeepFocus = keepFocus;
            calendar.addEventListener("mousedown", keepFocus, true);
        }
        var layer = document.createElement("div");
        layer.className = "datepicker b-is-datepicker hcs-datepicker-layer";
        document.body.appendChild(layer);
        layer.appendChild(calendar);
    }

    function unhost(calendar) {
        var layer = calendar.parentElement && calendar.parentElement.classList.contains("hcs-datepicker-layer")
            ? calendar.parentElement
            : null;
        var parent = calendar._hcsParent;
        if (parent && parent.isConnected) {
            if (calendar._hcsNext && calendar._hcsNext.parentNode === parent) {
                parent.insertBefore(calendar, calendar._hcsNext);
            } else {
                parent.appendChild(calendar);
            }
        }
        if (layer && layer.parentNode) layer.parentNode.removeChild(layer);
        calendar._hcsParent = null;
        calendar._hcsNext = null;
        delete calendar.dataset.hcsHosted;
    }

    function clearPlace(calendar) {
        unhost(calendar);
        if (!calendar.dataset.hcsPlace) return;
        delete calendar.dataset.hcsPlace;
        calendar.style.removeProperty("position");
        calendar.style.removeProperty("top");
        calendar.style.removeProperty("left");
        calendar.style.removeProperty("right");
        calendar.style.removeProperty("bottom");
        calendar.style.removeProperty("margin");
        calendar.style.removeProperty("margin-top");
        calendar.style.removeProperty("margin-bottom");
        calendar.style.removeProperty("z-index");
        calendar.style.removeProperty("transform");
        calendar.style.removeProperty("max-height");
        calendar.style.removeProperty("overflow-y");
    }

    function place(calendar) {
        var input = findInput(calendar);
        var modal = input && input.closest(".modal");
        if (!input || !modal) return;
        calendar._hcsInput = input;
        var body = modal.querySelector(".modal-body");
        var scrollTop = body ? body.scrollTop : 0;
        host(calendar);
        var inputRect = input.getBoundingClientRect();
        var vh = window.innerHeight;
        var vw = window.innerWidth;
        var height = calendar.offsetHeight || 360;
        var width = Math.max(calendar.offsetWidth || 0, 260);
        var below = inputRect.bottom + GAP;
        var above = inputRect.top - GAP - height;
        var fitsBelow = below + height <= vh - PAD;
        var fitsAbove = above >= PAD;
        var top;
        if (fitsBelow) top = below;
        else if (fitsAbove) top = above;
        else top = Math.max(PAD, Math.min(below, vh - PAD - height));
        var left = Math.max(PAD, Math.min(inputRect.left, vw - PAD - width));
        var key = [Math.round(top), Math.round(left), Math.round(height)].join(",");
        if (calendar.dataset.hcsPlace === key) {
            restoreScroll(body, scrollTop);
            return;
        }
        calendar.dataset.hcsPlace = key;
        calendar.style.setProperty("position", "fixed", "important");
        calendar.style.setProperty("top", top + "px", "important");
        calendar.style.setProperty("left", left + "px", "important");
        calendar.style.setProperty("right", "auto", "important");
        calendar.style.setProperty("bottom", "auto", "important");
        calendar.style.setProperty("margin", "0", "important");
        calendar.style.setProperty("transform", "none", "important");
        calendar.style.setProperty("z-index", "1065", "important");
        restoreScroll(body, scrollTop);
        requestAnimationFrame(function () { restoreScroll(body, scrollTop); });
    }

    function scan() {
        if (placing) return;
        placing = true;
        try {
            calendars().forEach(function (calendar) {
                var modal = calendar.closest(".modal")
                    || (calendar._hcsInput && calendar._hcsInput.closest(".modal"));
                if (!modal) return;
                if (isOpen(calendar)) place(calendar);
                else clearPlace(calendar);
            });
        } catch (err) {
            /* never break Select2 / page interaction */
        } finally {
            placing = false;
        }
    }

    function requestScan() {
        if (scheduled) return;
        scheduled = true;
        requestAnimationFrame(function () {
            scheduled = false;
            scan();
        });
    }

    function start() {
        if (!document.body) return;
        new MutationObserver(requestScan).observe(document.body, {
            subtree: true,
            childList: true,
            attributes: true,
            attributeFilter: ["class", "hidden"]
        });
        window.addEventListener("resize", requestScan);
    }

    if (document.body) start();
    else document.addEventListener("DOMContentLoaded", start);
})();
