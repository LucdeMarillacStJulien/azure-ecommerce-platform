(function () {
    // tiny namespace
    const UX = {
        qs: (sel, el = document) => el.querySelector(sel),
        make(el, attrs = {}) { const n = document.createElement(el); Object.assign(n, attrs); return n; },
        getParam(name) { return new URLSearchParams(window.location.search).get(name); },
        cleanParams(keys) {
            const url = new URL(window.location.href);
            keys.forEach(k => url.searchParams.delete(k));
            window.history.replaceState({}, "", url.toString());
        }
    };

    function showToast(orderId) {
        // root container
        let root = UX.qs(".ui-order-toast-root");
        if (!root) {
            root = UX.make("div", { className: "ui-order-toast-root", role: "region", ariaLive: "polite" });
            document.body.appendChild(root);
        }

        // toast
        const toast = UX.make("div", { className: "ui-order-toast ui-order-toast-animate-in" });

        const icon = UX.make("div", { className: "ui-order-toast__icon", textContent: "✓" });
        const textWrap = UX.make("div");
        const title = UX.make("h3", { className: "ui-order-toast__title", textContent: "Order placed" });
        const meta = UX.make("p", { className: "ui-order-toast__meta", textContent: `Order ID: ${orderId}` });
        textWrap.appendChild(title);
        textWrap.appendChild(meta);

        const close = UX.make("button", { className: "ui-order-toast__close", type: "button", title: "Close", innerHTML: "&times;" });

        toast.appendChild(icon);
        toast.appendChild(textWrap);
        toast.appendChild(close);
        root.appendChild(toast);

        const remove = () => {
            toast.classList.remove("ui-order-toast-animate-in");
            toast.classList.add("ui-order-toast-animate-out");
            setTimeout(() => toast.remove(), 220);
        };

        close.addEventListener("click", remove);
        setTimeout(remove, 6000);
    }

    document.addEventListener("DOMContentLoaded", function () {
        const ok = UX.getParam("orderSuccess");
        const id = UX.getParam("orderId");
        if (ok === "1" && id) {
            showToast(id);
            // clean URL so refresh doesn’t re-show
            UX.cleanParams(["orderSuccess", "orderId"]);
        }
    });
})();
