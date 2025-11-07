// order-delete.js — dedicated deleter for ORDERS only (jQuery + Bootstrap 5)
(function ($) {
    "use strict";

    if (window.__orderDeleteBound) return; // avoid double-binding
    window.__orderDeleteBound = true;

    let pending = null; // { urlBase, id, $btn }

    function getToken() {
        return $('#__af input[name="__RequestVerificationToken"]').first().val()
            || $('input[name="__RequestVerificationToken"]').first().val()
            || '';
    }

    function ajaxDeleteOrder(urlBase, id, btn) {
        const token = getToken();
        $.ajax({
            url: `${urlBase}`, // controller expects POST body
            type: 'POST',
            data: {
                id: id,
                __RequestVerificationToken: token
            }
        })
            .done(function (msg) {
                // remove the row with a tiny animation
                const $row = $(btn).closest('[data-row], tr');
                if ($row.length) {
                    $row.css('will-change', 'opacity, transform')
                        .animate({ opacity: 0 }, 120, function () {
                            $row.slideUp(100, function () { $row.remove(); });
                        });
                }
                $('#toastOrderDeleteMsg').text(msg || 'Order deleted.');
                const te = document.getElementById('toastOrderDelete');
                if (te) bootstrap.Toast.getOrCreateInstance(te).show();
            })
            .fail(function (xhr) {
                $('#toastOrderDeleteMsg').text(xhr?.responseText || 'Delete failed. Please try again.');
                const te = document.getElementById('toastOrderDelete');
                if (te) bootstrap.Toast.getOrCreateInstance(te).show();
            });
    }

    function openConfirmOrder(urlBase, id, btn, nameFallback) {
        if (!id) return;
        pending = { urlBase, id, $btn: btn };
        $('#od-item-name').text($(btn).data('name') || nameFallback || 'this order');
        bootstrap.Modal.getOrCreateInstance(document.getElementById('confirmOrderDeleteModal')).show();
    }

    // Confirm button in the modal
    $(document).on('click', '#od-confirm-btn', function () {
        if (!pending) return;
        bootstrap.Modal.getOrCreateInstance(document.getElementById('confirmOrderDeleteModal')).hide();
        ajaxDeleteOrder(pending.urlBase, pending.id, pending.$btn);
        pending = null;
    });

    // Trigger — buttons/links with .js-delete-order (unique to Orders)
    $(document).on('click', '.js-delete-order', function (e) {
        e.preventDefault();
        openConfirmOrder('/Order/Delete', $(this).data('id'), this, 'this order');
    });

})(jQuery);
