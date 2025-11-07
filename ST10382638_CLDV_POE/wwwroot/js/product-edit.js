// wwwroot/js/product-edit.js
// Enhanced preview for Edit: supports images & videos, swaps elements as needed, and handles rapid re-selects.
(function () {
    function $(root, sel) {
        return (root && root.querySelector(sel)) || document.querySelector(sel);
    }

    // Create an <img> that matches your CSS class
    function createImg() {
        const img = document.createElement('img');
        img.className = 'product-preview__img';
        img.alt = 'Preview';
        img.style.maxWidth = '100%';
        img.style.maxHeight = '100%';
        img.style.objectFit = 'contain';
        return img;
    }

    // Create a <video> that matches your desired behavior (autoplay, muted, loop, no controls)
    function createVideo() {
        const vid = document.createElement('video');
        vid.className = 'product-preview__img';
        vid.autoplay = true;
        vid.muted = true;
        vid.loop = true;
        vid.playsInline = true;
        vid.preload = 'metadata';
        // remove visible controls & extras
        vid.controls = false;
        vid.setAttribute('controlslist', 'nodownload noplaybackrate noremoteplayback nofullscreen');
        vid.setAttribute('disablepictureinpicture', '');
        vid.oncontextmenu = () => false;
        vid.style.maxWidth = '100%';
        vid.style.maxHeight = '100%';
        vid.style.objectFit = 'contain';
        return vid;
    }

    function revoke(url) {
        try { if (url) URL.revokeObjectURL(url); } catch (_) { }
    }

    function showPreview(input) {
        const file = input.files && input.files[0];
        if (!file) return;

        // Find the nearest preview wrapper on this form/card
        const scope = input.closest('.glass-card, form') || document;
        const wrap = scope.querySelector('.product-preview__imgwrap') || scope.querySelector('.product-preview') || scope;
        let current = wrap.querySelector('.product-preview__img');

        // Ensure we have the correct element type for the selected file
        const isVideo = (file.type || '').toLowerCase().startsWith('video/');
        if (isVideo) {
            if (!current || current.tagName.toLowerCase() !== 'video') {
                const newVid = createVideo();
                if (current) current.replaceWith(newVid);
                else wrap.appendChild(newVid);
                current = newVid;
            }
        } else {
            if (!current || current.tagName.toLowerCase() !== 'img') {
                const newImg = createImg();
                if (current) current.replaceWith(newImg);
                else wrap.appendChild(newImg);
                current = newImg;
            }
        }

        // Revoke any previous blob URL we created for this input
        revoke(input.__previewURL);

        // Create and assign a fresh blob URL
        const url = URL.createObjectURL(file);
        input.__previewURL = url;

        if (isVideo) {
            const vid = current;
            // Revoke URL once it’s loaded; also attempt to play
            vid.onloadeddata = () => {
                revoke(url);
                // Keep muted autoplay smooth; ignore errors if user interacts mid-load
                vid.play().catch(() => { });
            };
            // Clear previous src first to force a clean load when users switch rapidly
            try { vid.removeAttribute('src'); vid.load(); } catch (_) { }
            vid.src = url;
            // If autoplay is blocked, try again shortly
            setTimeout(() => vid.play().catch(() => { }), 0);
        } else {
            const img = current;
            img.onload = () => revoke(url);
            img.src = url;
        }
    }

    function bind(input) {
        if (!input || input.__boundPreview) return;
        input.addEventListener('change', () => showPreview(input));
        input.__boundPreview = true;
    }

    // Bind eagerly (support both casings just in case)
    bind(document.querySelector('input[type="file"][name="imageFile"]'));
    bind(document.querySelector('input[type="file"][name="ImageFile"]'));

    // Bind dynamically if the form is swapped/partial-rendered
    document.addEventListener('change', (e) => {
        const sel = 'input[type="file"][name="imageFile"], input[type="file"][name="ImageFile"]';
        if (e.target && e.target.matches(sel)) bind(e.target);
    });
})();