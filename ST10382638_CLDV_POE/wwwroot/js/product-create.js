// wwwroot/js/product-create.js
(function () {
    const input = document.getElementById('imageFile');
    if (!input) return;

    const img = document.getElementById('previewImage');
    const vid = document.getElementById('previewVideo');

    if (!img || !vid) return;

    let lastURL = null;

    function resetPreview() {
        if (lastURL) {
            URL.revokeObjectURL(lastURL);
            lastURL = null;
        }
        // Hide and clear both
        img.style.display = 'none';
        img.removeAttribute('src');

        vid.pause();
        vid.removeAttribute('src');
        // Force <video> to drop any buffered data
        try { vid.load(); } catch (_) { }
        vid.style.display = 'none';
    }

    function showImage(url) {
        img.onload = () => URL.revokeObjectURL(url);
        img.src = url;
        img.style.display = '';
    }

    function showVideo(url) {
        // revoke when data is loaded to keep preview responsive
        vid.onloadeddata = () => URL.revokeObjectURL(url);
        vid.src = url;
        vid.style.display = '';
        // optional: don't autoplay; user can press play
        // if you want autoplay muted, uncomment:
        // vid.muted = true; vid.play().catch(() => {});
    }

    input.addEventListener('change', function () {
        const file = this.files && this.files[0];
        resetPreview();
        if (!file) return;

        const url = URL.createObjectURL(file);
        lastURL = url;

        const type = (file.type || '').toLowerCase();
        if (type.startsWith('video/')) {
            showVideo(url);
        } else {
            showImage(url);
        }
    });
})();