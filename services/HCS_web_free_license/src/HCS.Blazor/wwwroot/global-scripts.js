(function hideBlazoriseLicenseBanner() {
    const HOST_ID = "blazorise-license-banner-host";
    const GLOBAL = "__blazoriseBannerState__";

    const state = (window[GLOBAL] ||= {
        dismissed: false,
        bodyObserver: null,
        attrObserver: null
    });
    state.dismissed = true;

    const removeHost = () => {
        const host = document.getElementById(HOST_ID);
        if (!host) {
            return;
        }

        if (state.bodyObserver) {
            try { state.bodyObserver.disconnect(); } catch { }
        }
        if (state.attrObserver) {
            try { state.attrObserver.disconnect(); } catch { }
        }
        host.remove();
    };

    const watchBody = () => {
        if (!document.body) {
            return;
        }

        removeHost();
        new MutationObserver(removeHost).observe(document.body, { childList: true });
    };

    if (document.body) {
        watchBody();
    } else {
        document.addEventListener("DOMContentLoaded", watchBody, { once: true });
    }
})();

window.hcsDownloadTextFile = (fileName, content, mimeType) => {
    const blob = new Blob(["\uFEFF", content], { type: mimeType || "text/plain;charset=utf-8" });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    anchor.remove();
    URL.revokeObjectURL(url);
};

window.hcsApplySystemBranding = (title, description, logoUrl, faviconUrl, backgroundUrl) => {
    const nextTitle = typeof title === "string" && title.trim() ? title.trim() : "HCS";
    const nextDescription = typeof description === "string" && description.trim()
        ? description.trim()
        : "Hệ thống hành chính số";
    const nextLogoUrl = typeof logoUrl === "string" && logoUrl ? logoUrl : "/images/logo/hcs-icon.png";
    const nextFaviconUrl = typeof faviconUrl === "string" && faviconUrl ? faviconUrl : "/images/logo/hcs-icon.png";
    const nextBackgroundImage = typeof backgroundUrl === "string" && backgroundUrl
        ? `url("${backgroundUrl}")`
        : "none";
    const currentTitle = (document.title || "").trim();
    const previousTitle = window.__hcsSystemBrandingTitle;
    let pageTitle = currentTitle;

    for (const marker of [previousTitle, "HCS"]) {
        if (typeof marker !== "string" || !marker) {
            continue;
        }

        if (pageTitle === marker) {
            pageTitle = "";
            break;
        }

        const prefix = `${marker} · `;
        const suffix = ` · ${marker}`;
        if (pageTitle.startsWith(prefix)) {
            pageTitle = pageTitle.slice(prefix.length).trim();
            break;
        }
        if (pageTitle.endsWith(suffix)) {
            pageTitle = pageTitle.slice(0, -suffix.length).trim();
            break;
        }
    }

    document.title = pageTitle ? `${pageTitle} · ${nextTitle}` : nextTitle;
    window.__hcsSystemBrandingTitle = nextTitle;

    document.querySelectorAll("[data-hcs-system-branding-favicon]").forEach((element) => {
        element.setAttribute("href", nextFaviconUrl);
    });
    // The boot screen is rendered before the interactive Blazor app starts. Keep
    // its logo in the same branding update path as the app-shell logo so a
    // configured logo is visible while the application is loading as well.
    document.querySelectorAll("[data-hcs-branding-logo], .hcs-boot-screen__logo").forEach((element) => {
        element.setAttribute("src", nextLogoUrl);
        element.setAttribute("alt", nextTitle);
    });
    document.querySelectorAll("[data-hcs-branding-title]").forEach((element) => {
        element.textContent = nextTitle;
    });
    document.querySelectorAll("[data-hcs-branding-description]").forEach((element) => {
        element.textContent = nextDescription;
        element.setAttribute("title", nextDescription);
    });
    document.querySelectorAll(".hcs-app-shell").forEach((element) => {
        element.style.setProperty("--hcs-branding-background-image", nextBackgroundImage);
    });
};

// Load public branding before Blazor becomes interactive so the boot screen
// uses the configured API asset URL instead of briefly showing the fallback.
window.hcsLoadSystemBranding = async () => {
    const configuredOrigin = document.documentElement.dataset.hcsBrandingApiOrigin;
    const origin = (configuredOrigin || window.location.origin).replace(/\/+$/, "");

    try {
        const response = await fetch(`${origin}/api/hcs/system-branding/public`, {
            credentials: "omit",
            headers: { Accept: "application/json" }
        });
        if (!response.ok) {
            return;
        }

        const branding = await response.json();
        const absoluteAssetUrl = (asset) => {
            if (!asset || typeof asset.url !== "string" || !asset.url.trim()) {
                return null;
            }

            try {
                return new URL(asset.url, origin).href;
            } catch {
                return null;
            }
        };

        window.hcsApplySystemBranding(
            branding.title,
            branding.description,
            absoluteAssetUrl(branding.logo),
            absoluteAssetUrl(branding.favicon),
            absoluteAssetUrl(branding.background));
    } catch {
        // The interactive branding state retries after Blazor starts.
    }
};

window.hcsLoadSystemBranding();

const hcsNormalizeCulture = (value) => {
    if (typeof value !== "string") {
        return null;
    }

    const candidate = value.trim().replaceAll("_", "-");
    if (!/^[A-Za-z]{2,8}(?:-[A-Za-z0-9]{1,8})*$/.test(candidate)) {
        return null;
    }

    try {
        return Intl.getCanonicalLocales(candidate)[0] || null;
    } catch {
        return null;
    }
};

const hcsReadCookie = (cookies, name) => {
    const item = cookies.find((part) => part.startsWith(`${name}=`));
    if (!item) {
        return null;
    }

    try {
        return decodeURIComponent(item.slice(name.length + 1));
    } catch {
        return null;
    }
};

window.hcsGetCulture = () => {
    const fromStorage = hcsNormalizeCulture(window.localStorage?.getItem("hcs.culture"));
    if (fromStorage) {
        return fromStorage;
    }

    const cookies = document.cookie.split(";").map((part) => part.trim());
    const named = hcsNormalizeCulture(hcsReadCookie(cookies, "hcs.culture"));
    if (named) {
        return named;
    }

    const abp = hcsNormalizeCulture(hcsReadCookie(cookies, "Abp.Localization.CultureName"));
    if (abp) {
        return abp;
    }

    const aspNet = hcsReadCookie(cookies, ".AspNetCore.Culture");
    const match = aspNet?.match(/(?:^|\|)uic=([^|]+)/i);
    const aspNetCulture = hcsNormalizeCulture(match?.[1]);
    if (aspNetCulture) {
        return aspNetCulture;
    }

    return "en";
};

window.hcsSetCulture = (culture) => {
    const selected = hcsNormalizeCulture(culture) || "en";
    const encoded = encodeURIComponent(`c=${selected}|uic=${selected}`);
    const secure = window.location.protocol === "https:" ? "; secure" : "";
    const attrs = `; path=/; max-age=31536000; samesite=lax${secure}`;
    document.cookie = `hcs.culture=${selected}${attrs}`;
    document.cookie = `Abp.Localization.CultureName=${selected}${attrs}`;
    document.cookie = `.AspNetCore.Culture=${encoded}${attrs}`;
    window.localStorage?.setItem("hcs.culture", selected);
    try {
        document.documentElement.lang = selected;
    } catch {
        // ignore
    }
};

window.hcsNotifications = (() => {
    let outsideClickHandler = null;

    return {
        bindOutsideClick(dotNetRef) {
            this.unbindOutsideClick();
            outsideClickHandler = (event) => {
                const target = event.target;
                if (!(target instanceof Element) || target.closest(".hcs-notification-panel, .hcs-top-pop, [data-hcs-notification-trigger]")) {
                    return;
                }

                dotNetRef.invokeMethodAsync("ClosePanelFromOutsideAsync").catch(() => { });
            };
            document.addEventListener("pointerdown", outsideClickHandler, true);
        },

        unbindOutsideClick() {
            if (!outsideClickHandler) {
                return;
            }

            document.removeEventListener("pointerdown", outsideClickHandler, true);
            outsideClickHandler = null;
        }
    };
})();

window.hcsPop = (() => {
    const handlers = new WeakMap();
    const IGNORE = [
        ".flatpickr-calendar",
        ".datepicker-calendar",
        ".datepicker-backdrop",
        ".hcs-datepicker-layer",
        ".select2-dropdown",
        ".select2-container",
        ".select2-results",
        ".hcs-select2-pop",
        ".msg-menu",
        ".b-modal",
        ".modal"
    ].join(",");

    const close = (dotNetRef) => {
        try {
            dotNetRef.invokeMethod("CloseFromOutside");
        } catch {
            dotNetRef.invokeMethodAsync("CloseFromOutside").catch(() => { });
        }
    };

    return {
        bind(element, dotNetRef) {
            if (!(element instanceof Element)) {
                return;
            }

            this.unbind(element);
            const handler = (event) => {
                const target = event.target;
                if (!(target instanceof Element) || element.contains(target) || target.closest(IGNORE)) {
                    return;
                }

                close(dotNetRef);
            };
            document.addEventListener("pointerdown", handler, true);
            handlers.set(element, handler);
        },

        unbind(element) {
            if (!(element instanceof Element)) {
                return;
            }

            const handler = handlers.get(element);
            if (!handler) {
                return;
            }

            document.removeEventListener("pointerdown", handler, true);
            handlers.delete(element);
        }
    };
})();

window.hcsBlurActive = () => {
    const el = document.activeElement;
    if (el instanceof HTMLElement && el !== document.body) {
        el.blur();
    }
};
