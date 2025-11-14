// delete-product.js
// Handles delete actions for products using the shared _ConfirmDeleteModal

(function () {

    let pendingProduct = null;

    // Utility: show toast if available
    function showToast(message) {
        if (window.toastr) {
            toastr.error(message);
        } else {
            alert(message);
        }
    }

    // Open delete modal with given product name
    function openModal(name) {
        const modalEl = document.getElementById("confirmDeleteModal");
        if (!modalEl) return;

        const label = modalEl.querySelector("[data-confirm-name]");
        if (label) label.textContent = name;

        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();
    }

    // Close modal
    function closeModal() {
        const modalEl = document.getElementById("confirmDeleteModal");
        if (!modalEl) return;

        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.hide();
    }

    // Click handler for Product delete button
    document.addEventListener("click", function (e) {

        const btn = e.target.closest(".js-delete-product");
        if (!btn) return;

        e.preventDefault();

        const id = btn.dataset.id;
        const name = btn.dataset.name || "this product";

        if (!id) {
            showToast("Missing product ID.");
            return;
        }

        pendingProduct = { id, btn };
        openModal(name);
    });

    // Handler for confirm-delete button (the one INSIDE the modal)
    document.addEventListener("click", function (e) {

        const btn = e.target.closest("#cd-confirm-btn");
        if (!btn) return;

        if (!pendingProduct) return;

        const token = document.querySelector("input[name='__RequestVerificationToken']")?.value;
        if (!token) {
            showToast("Verification token missing.");
            return;
        }

        fetch("/Product/Delete", {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded",
                "RequestVerificationToken": token
            },
            body: `id=${encodeURIComponent(pendingProduct.id)}`
        })
            .then(res => {
                if (!res.ok) throw new Error();
                closeModal();
                window.location.reload();
            })
            .catch(() => showToast("Delete failed. Please try again."));
    });


})();
