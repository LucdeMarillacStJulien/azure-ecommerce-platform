// queue-delete.js — queue-only deleter (jQuery + Bootstrap 5)
// Uses: _ConfirmDeleteModal (#confirmDeleteModal, #cd-item-name, #cd-confirm-btn)
// Uses: Anti-forgery token inside #__af
// Uses: Toast with #toastDelete + #toastDeleteMsg

(function ($) {
    "use strict";

    // Avoid double-binding if the file is included twice.
    if (window.__queueDeleteBound) return;
    window.__queueDeleteBound = true;

    let pending = null; // { url, id, pop, $btn }

    function getToken() {
        return $('#__af input[name="__RequestVerificationToken"]').first().val()
            || $('input[name="__RequestVerificationToken"]').first().val()
            || '';
    }

    function showToast(msg) {
        $('#toastDeleteMsg').text(msg || '');
        const el = document.getElementById('toastDelete');
        if (el) bootstrap.Toast.getOrCreateInstance(el).show();
    }

    function removeRow(btn) {
        const $row = $(btn).closest('[data-row], tr, li');
        if (!$row.length) return;
        $row.css('will-change', 'opacity, transform')
            .animate({ opacity: 0 }, 120, function () {
                $row.slideUp(100, function () { $row.remove(); });
            });
    }

    function openConfirmQueue(url, messageId, popReceipt, btn, nameFallback) {
        if (!messageId || !popReceipt) return;
        pending = { url, id: messageId, pop: popReceipt, $btn: btn };
        const name = $(btn).data('name') || nameFallback || 'this message';
        $('#cd-item-name').text(name);
        bootstrap.Modal.getOrCreateInstance(document.getElementById('confirmDeleteModal')).show();
    }

    function ajaxDeleteQueue(url, messageId, popReceipt, btn) {
        $.ajax({
            url: url,            // e.g. /Inventory/DeleteQueueMessage
            type: 'POST',
            dataType: 'json',
            data: {
                messageId: messageId,
                popReceipt: popReceipt,
                __RequestVerificationToken: getToken()
            }
        })
            .done(function () {
                removeRow(btn);
                showToast('Queue message deleted.');
            })
            .fail(function (xhr) {
                showToast(xhr?.responseText || 'Queue delete failed. Please try again.');
            });
    }

    // Trigger: queue delete button
    $(document)
        .off('click.queueDelete', '.js-delete-queue')
        .on('click.queueDelete', '.js-delete-queue', function (e) {
            e.preventDefault();
            openConfirmQueue(
                '/Inventory/Delete',
                $(this).data('id'),   // messageId
                $(this).data('pop'),  // popReceipt
                this,
                $(this).data('name') || 'this message'
            );
        });

    // Confirm button inside the modal
    $(document)
        .off('click.queueDelete', '#cd-confirm-btn')
        .on('click.queueDelete', '#cd-confirm-btn', function () {
            if (!pending) return;
            bootstrap.Modal.getOrCreateInstance(document.getElementById('confirmDeleteModal')).hide();
            ajaxDeleteQueue(pending.url, pending.id, pending.pop, pending.$btn);
            pending = null;
        });

})(jQuery);