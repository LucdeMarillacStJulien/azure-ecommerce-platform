(function () {
    const list = document.querySelector('.queue-list');
    const filter = document.getElementById('queueFilter');
    const copyAll = document.getElementById('copyAll');
    const copyVisible = document.getElementById('copyVisible');

    function visibleItems() {
        return Array.from(document.querySelectorAll('.queue-item')).filter(li => li.offsetParent !== null);
    }

    function copyText(text) {
        navigator.clipboard?.writeText(text).catch(() => { });
    }

    // Filter as you type
    filter?.addEventListener('input', () => {
        const q = filter.value.trim().toLowerCase();
        document.querySelectorAll('.queue-item').forEach(li => {
            const msg = li.querySelector('.queue-msg')?.textContent.toLowerCase() ?? '';
            li.style.display = q === '' || msg.includes(q) ? '' : 'none';
        });
    });

    // Copy buttons
    copyAll?.addEventListener('click', () => {
        const all = Array.from(document.querySelectorAll('.queue-msg')).map(x => x.textContent).join('\n\n');
        copyText(all);
    });

    copyVisible?.addEventListener('click', () => {
        const vis = visibleItems().map(li => li.querySelector('.queue-msg')?.textContent ?? '').join('\n\n');
        copyText(vis);
    });

    // Per-item copy (event delegation)
    list?.addEventListener('click', (e) => {
        const btn = e.target.closest('.btn-copy-one');
        if (!btn) return;
        copyText(btn.dataset.message || '');
    });
})();
