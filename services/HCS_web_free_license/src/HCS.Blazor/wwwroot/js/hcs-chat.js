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
    },
    bindComposerPaste(id, dotNetRef) {
        const el = document.getElementById(id);
        if (!el || el.dataset.hcsPasteBound === "1" || !dotNetRef) {
            return;
        }
        el.dataset.hcsPasteBound = "1";
        el.addEventListener("paste", async (event) => {
            const items = event.clipboardData && event.clipboardData.items;
            if (!items || !items.length) {
                return;
            }
            const files = [];
            for (const item of items) {
                if (item.kind === "file" && item.type && item.type.startsWith("image/")) {
                    const file = item.getAsFile();
                    if (file) {
                        files.push(file);
                    }
                }
            }
            if (!files.length) {
                return;
            }
            event.preventDefault();
            const payload = [];
            for (const file of files) {
                const bytes = new Uint8Array(await file.arrayBuffer());
                let binary = "";
                const chunk = 0x8000;
                for (let i = 0; i < bytes.length; i += chunk) {
                    binary += String.fromCharCode.apply(null, Array.from(bytes.subarray(i, i + chunk)));
                }
                payload.push({
                    name: file.name && file.name !== "image.png" ? file.name : "",
                    contentType: file.type || "image/png",
                    base64: btoa(binary)
                });
            }
            await dotNetRef.invokeMethodAsync("OnPastedImagesAsync", payload);
        });
    }
};
