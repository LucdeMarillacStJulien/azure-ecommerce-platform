// delete.js (full, fixed)
// Requires jQuery + Bootstrap 5. Place after your table HTML & modal markup.

(function ($) {
    "use strict";

    let pending = null;

    // Finds an anti-forgery token anywhere on the page.
    function getToken() {
        return $('#__af input[name="__RequestVerificationToken"]').first().val()
            || $('input[name="__RequestVerificationToken"]').first().val()
            || '';
    }

    // Generic delete via POST to /{Controller}/Delete with id in the body.
    function ajaxDelete(urlBase, id, btn) {
        const token = getToken();
        $.ajax({
            url: `${urlBase}`,         // no "/{id}" — controller expects body param
            type: 'POST',
            data: {
                id: id,                  // common pattern
                RowKey: id,              // covers controllers that bind RowKey
                __RequestVerificationToken: token
            }
        })
            .done(function () {
                // Remove the row with a tiny animation
                const $row = $(btn).closest('[data-row], tr');
                if ($row.length) {
                    $row.css('will-change', 'opacity, transform')
                        .animate({ opacity: 0 }, 120, function () {
                            $row.slideUp(100, function () { $row.remove(); });
                        });
                }
                $('#toastDeleteMsg').text('Deleted successfully.');
                const te = document.getElementById('toastDelete');
                if (te) bootstrap.Toast.getOrCreateInstance(te).show();
            })
            .fail(function (xhr) {
                $('#toastDeleteMsg').text(xhr?.responseText || 'Delete failed. Please try again.');
                const te = document.getElementById('toastDelete');
                if (te) bootstrap.Toast.getOrCreateInstance(te).show();
            });
    }

    // Opens the confirmation modal and stores the target.
    function openConfirm(urlBase, id, btn, nameFallback) {
        if (!id) return;
        pending = { urlBase, id, $btn: btn };
        $('#cd-item-name').text($(btn).data('name') || nameFallback);
        bootstrap.Modal.getOrCreateInstance(document.getElementById('confirmDeleteModal')).show();
    }

    // === QUEUE-SPECIFIC ADDITIONS ===

    // POST /Inventory/DeleteQueueMessage with messageId + popReceipt
    function ajaxDeleteQueue(urlBase, messageId, popReceipt, btn) {
        const token = getToken();
        $.ajax({
            url: `${urlBase}`,
            type: 'POST',
            data: {
                messageId: messageId,
                popReceipt: popReceipt,
                __RequestVerificationToken: token
            }
        })
            .done(function () {
                const $row = $(btn).closest('[data-row], tr');
                if ($row.length) {
                    $row.css('will-change', 'opacity, transform')
                        .animate({ opacity: 0 }, 120, function () {
                            $row.slideUp(100, function () { $row.remove(); });
                        });
                }
                $('#toastDeleteMsg').text('Queue message deleted.');
                const te = document.getElementById('toastDelete');
                if (te) bootstrap.Toast.getOrCreateInstance(te).show();
            })
            .fail(function (xhr) {
                $('#toastDeleteMsg').text(xhr?.responseText || 'Queue delete failed. Please try again.');
                const te = document.getElementById('toastDelete');
                if (te) bootstrap.Toast.getOrCreateInstance(te).show();
            });
    }

    // Open confirm for queue with both id + popReceipt
    function openConfirmQueue(urlBase, messageId, popReceipt, btn, nameFallback) {
        if (!messageId || !popReceipt) return;
        pending = { urlBase, id: messageId, pop: popReceipt, $btn: btn };
        $('#cd-item-name').text($(btn).data('name') || nameFallback || 'this message');
        bootstrap.Modal.getOrCreateInstance(document.getElementById('confirmDeleteModal')).show();
    }

    // === /QUEUE-SPECIFIC ADDITIONS ===

    // Confirm button in the modal
    $(document).on('click', '#cd-confirm-btn', function () {
        if (!pending) return;
        bootstrap.Modal.getOrCreateInstance(document.getElementById('confirmDeleteModal')).hide();

        // If a popReceipt is present, it's a queue delete; otherwise use the generic delete.
        if (pending.pop) {
            ajaxDeleteQueue(pending.urlBase, pending.id, pending.pop, pending.$btn);
        } else {
            ajaxDelete(pending.urlBase, pending.id, pending.$btn);
        }
        pending = null;
    });

    // Triggers for each entity type
    $(document).on('click', '.js-delete-product', function (e) {
        e.preventDefault();
        openConfirm('/Product/Delete', $(this).data('id'), this, 'this product');
    });

    $(document).on('click', '.js-delete-customer', function (e) {
        e.preventDefault();
        openConfirm('/Customer/Delete', $(this).data('id'), this, 'this customer');
    });

    $(document).on('click', '.js-delete-order', function (e) {
        e.preventDefault();
        openConfirm('/Order/Delete', $(this).data('id'), this, 'this order');
    });

    // NEW: queue trigger (button must have data-id and data-pop)
    $(document).on('click', '.js-delete-queue', function (e) {
        e.preventDefault();
        openConfirmQueue('/Inventory/Delete',
            $(this).data('id'),
            $(this).data('pop'),
            this,
            'this queue message');
    });

})(jQuery);
