// customer-delete.js — Delete for Customers (SQL-backed)
// Requires jQuery + Bootstrap 5. Works with _ConfirmDeleteModal and the hidden AF form (#__af).

(function ($) {
    "use strict";

    if (window.__customerDeleteBound) return; // avoid double-binding
    window.__customerDeleteBound = true;

    let pending = null; // { url, id, $btn }

    // Anti-forgery token helper (works with your hidden form in _ConfirmDeleteModal)
    function getToken() {
        return $('#__af input[name="__RequestVerificationToken"]').first().val()
            || $('input[name="__RequestVerificationToken"]').first().val()
            || '';
    }

    // UI helpers
    function openModal(itemLabel) {
        $('#cd-item-name').text(itemLabel || 'this customer');
        const modalEl = document.getElementById('confirmDeleteModal');
        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    }
    function closeModal() {
        const modalEl = document.getElementById('confirmDeleteModal');
        bootstrap.Modal.getOrCreateInstance(modalEl).hide();
    }
    function showToast(msg) {
        const $msg = $('#toastDeleteMsg');
        if ($msg.length) $msg.text(msg || 'Deleted.');
        const toastEl = document.getElementById('toastDelete');
        if (toastEl) bootstrap.Toast.getOrCreateInstance(toastEl).show();
    }

    // Attempt to remove a visible UI element for this customer; fallback to reload
    function removeRow($trigger) {
        // Prefer explicit row containers if present:
        const $row = $trigger.closest('[data-row], tr, .card, li');
        if ($row.length) {
            $row.fadeOut(150, function () { $(this).remove(); });
        } else {
            // If we can't confidently remove a row, ensure the UI is consistent:
            setTimeout(function () { window.location.reload(); }, 300);
        }
    }

    // Core AJAX POST to /Customer/Delete
    async function ajaxDelete(url, id, $btn) {
        const token = getToken();
        // Graceful state on the trigger
        const prevHtml = $btn.html();
        $btn.prop('disabled', true).html('<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>Deleting');

        try {
            const res = await $.ajax({
                url: url,
                method: 'POST',
                data: {
                    id: id,
                    __RequestVerificationToken: token
                },
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            // Success (HTTP 200): update UI
            showToast('Customer deleted.');
            removeRow($btn);
        } catch (xhr) {
            // If the action returns 404/400/etc, surface a clear message
            const msg = (xhr && xhr.responseText) ? xhr.responseText : 'Delete failed.';
            showToast(msg);
        } finally {
            $btn.prop('disabled', false).html(prevHtml);
        }
    }

    // Wire confirm button in the modal
    $(document).off('click.customerDelete', '#cd-confirm-btn')
        .on('click.customerDelete', '#cd-confirm-btn', function () {
            if (!pending) return;
            closeModal();
            ajaxDelete(pending.url, pending.id, pending.$btn);
            pending = null;
        });

    // Trigger: any element with .js-delete-customer
    // Expected data attributes on the trigger:
    //   data-id   : numeric or string CustomerId
    //   data-name : optional display name for the modal text
    //   data-url  : optional custom endpoint; defaults to '/Customer/Delete'
    $(document).off('click.customerDelete', '.js-delete-customer')
        .on('click.customerDelete', '.js-delete-customer', function (e) {
            e.preventDefault();

            const $btn = $(this);
            const id = $btn.data('id');
            const name = $btn.data('name') || 'this customer';
            const url = $btn.data('url') || '/Customer/Delete';

            if (id === undefined || id === null || id === '') {
                showToast('Missing customer id.');
                return;
            }

            pending = { url, id, $btn };
            openModal(name);
        });

})(jQuery);
