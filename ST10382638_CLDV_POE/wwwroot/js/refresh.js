// Auto-refresh for Orders pages (MyOrders + Admin Index)
// Runs only on pages that contain an element with data-orders-autorefresh="true"
(function () {
    const REFRESH_INTERVAL_MS = 5000;
    const CONTAINER_SELECTOR = "[data-orders-autorefresh]";

    function getContainer() {
        return document.querySelector(CONTAINER_SELECTOR);
    }

    async function refreshOnce() {
        const root = getContainer();
        if (!root) return;

        const url = window.location.href;

        try {
            const response = await fetch(url, {
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });
            if (!response.ok) return;

            const html = await response.text();
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, "text/html");
            const fresh = doc.querySelector(CONTAINER_SELECTOR);
            if (!fresh) return;

            // Preserve scroll position inside the container (if it scrolls)
            const scrollTop = root.scrollTop;

            // Hard swap content with no animations so the user does not notice
            root.innerHTML = fresh.innerHTML;

            root.scrollTop = scrollTop;
        } catch (e) {
            // Fail silently – no console noise, no UI impact
        }
    }

    document.addEventListener("DOMContentLoaded", function () {
        const root = getContainer();
        if (!root) return; // Only run on orders pages

        setInterval(refreshOnce, REFRESH_INTERVAL_MS);
    });
})();
