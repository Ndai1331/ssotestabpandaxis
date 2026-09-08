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
};

window.hcsNotifications = (() => {
    let outsideClickHandler = null;

    return {
        bindOutsideClick(dotNetRef) {
            this.unbindOutsideClick();
            outsideClickHandler = (event) => {
                const target = event.target;
                if (!(target instanceof Element) || target.closest(".hcs-notification-panel, [data-hcs-notification-trigger]")) {
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
