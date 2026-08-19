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
            $el.append(new Option(item.text || "", item.id, true, true));
        }
        if (!multiple && items.length > 1) {
            $el.find("option").slice(2).remove();
        }
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
