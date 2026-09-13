window.hcsChat = {
    scrollToBottom(id) {
        const el = document.getElementById(id);
        if (!el) {
            return;
        }
        el.scrollTop = el.scrollHeight;
    },
    scrollToMessage(id) {
        const el = document.getElementById(id);
        if (!el) {
            return false;
        }
        el.scrollIntoView({ block: "center", behavior: "smooth" });
        return true;
    },
    isNearTop(id, threshold = 80) {
        const el = document.getElementById(id);
        return !!el && el.scrollTop <= threshold;
    },
    scrollHeight(id) {
        return document.getElementById(id)?.scrollHeight ?? 0;
    },
    preserveScrollAfterPrepend(id, previousHeight) {
        const el = document.getElementById(id);
        if (!el) {
            return;
        }
        el.scrollTop = el.scrollHeight - previousHeight;
    },
    positionMenu(menuId, anchorSelector) {
        const menu = document.getElementById(menuId);
        const btn = document.querySelector(anchorSelector);
        if (!menu || !btn) {
            return;
        }
        menu.style.visibility = "hidden";
        menu.style.left = "0px";
        menu.style.top = "0px";
        const rect = btn.getBoundingClientRect();
        const width = menu.offsetWidth || 220;
        const height = menu.offsetHeight || 200;
        let left = rect.left;
        let top = rect.bottom + 6;
        if (left + width > window.innerWidth - 8) {
            left = Math.max(8, window.innerWidth - width - 8);
        }
        if (top + height > window.innerHeight - 8) {
            top = Math.max(8, rect.top - height - 6);
        }
        menu.style.left = `${Math.round(left)}px`;
        menu.style.top = `${Math.round(top)}px`;
        menu.style.visibility = "visible";
    }
};
