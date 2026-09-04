window.hcsSocial = {
    scrollToPost(id) {
        const post = document.getElementById(`post-${id}`)?.closest("article");
        if (!post) return;
        post.scrollIntoView({ behavior: "smooth", block: "center" });
        post.classList.add("is-highlighted");
        window.setTimeout(() => post.classList.remove("is-highlighted"), 2200);
    },
    async share(url, title, text) {
        try {
            if (navigator.share) {
                await navigator.share({ url, title, text });
                return "shared";
            }

            if (navigator.clipboard?.writeText) {
                await navigator.clipboard.writeText(url);
                return "copied";
            }

            const input = document.createElement("textarea");
            input.value = url;
            input.setAttribute("readonly", "");
            input.style.position = "fixed";
            input.style.opacity = "0";
            document.body.appendChild(input);
            input.select();
            const copied = document.execCommand("copy");
            input.remove();
            return copied ? "copied" : "failed";
        } catch (error) {
            return error?.name === "AbortError" ? "cancelled" : "failed";
        }
    }
};
