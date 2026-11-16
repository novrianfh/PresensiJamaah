(function () {
    const config = document.getElementById('scan-config');
    const url = config.dataset.url;
    const token = document.querySelector('input[name="__RequestVerificationToken"]').value;
    const modalEl = document.getElementById('result-modal');
    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    const modalHeader = document.getElementById('result-header');
    const modalBadge = document.getElementById('result-badge');
    const modalTitle = document.getElementById('result-title');
    const modalDetail = document.getElementById('result-detail');
    const modalExtra = document.getElementById('result-extra');
    const modalProgress = document.getElementById('result-progress');
    const modalOk = document.getElementById('result-ok');
    const cameraError = document.getElementById('camera-error');
    const btnStart = document.getElementById('btn-start');
    const btnStop = document.getElementById('btn-stop');

    const COOLDOWN_MS = 4000;     // QR yang sama tidak dikirim ulang selama jeda ini
    const POPUP_MS = 5000;        // popup menutup sendiri setelah 5 detik
    let scanner = null;
    let busy = false;
    let lastText = null;
    let lastAt = 0;
    let clearTimer = null;

    const BADGES = { success: 'Berhasil', warning: 'Sudah tercatat', danger: 'Gagal' };

    // Semua teks dari server dimasukkan lewat textContent (bukan innerHTML).
    // Scan baru saat popup masih terbuka menggantikan isi popup dan mengulang hitungan 5 detik.
    let closing = false;   // popup sedang proses menutup
    let pending = null;    // hasil scan yang datang saat popup menutup

    function show(kind, title, detail, extra) {
        if (closing) {
            pending = [kind, title, detail, extra];
            return;
        }
        modalHeader.className = 'modal-header justify-content-center py-2 text-bg-' + kind;
        modalBadge.textContent = BADGES[kind] || '';
        modalTitle.textContent = title;
        modalDetail.textContent = detail || '';
        modalExtra.textContent = extra || '';
        modalProgress.className = 'progress-bar bg-' + kind;

        // Ulang animasi bar waktu dari penuh ke kosong selama POPUP_MS.
        modalProgress.style.transition = 'none';
        modalProgress.style.width = '100%';
        void modalProgress.offsetWidth;
        modalProgress.style.transition = 'width ' + POPUP_MS + 'ms linear';
        modalProgress.style.width = '0%';

        modal.show();
        clearTimeout(clearTimer);
        clearTimer = setTimeout(function () { modal.hide(); }, POPUP_MS);
    }

    modalEl.addEventListener('shown.bs.modal', function () { modalOk.focus(); });
    modalEl.addEventListener('hide.bs.modal', function () { closing = true; clearTimeout(clearTimer); });
    modalEl.addEventListener('hidden.bs.modal', function () {
        closing = false;
        if (pending) {
            const args = pending;
            pending = null;
            show.apply(null, args);
        }
    });

    async function kirim(nomor) {
        if (busy) return;
        busy = true;
        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: { 'RequestVerificationToken': token, 'X-Requested-With': 'XMLHttpRequest' },
                body: new URLSearchParams({ nomorJamaah: nomor })
            });
            if (!response.ok) {
                show('danger', 'Gagal mencatat',
                    'Server menjawab ' + response.status +
                    (response.status === 400 ? ' (muat ulang halaman lalu coba lagi).' : '.'));
                return;
            }
            const r = await response.json();
            if (r.berhasil) {
                show('success', r.nama, r.pesan, r.nomor + ' · ' + r.waktu);
            } else if (r.status === 'SudahTercatat') {
                show('warning', r.nama, r.pesan, r.nomor);
            } else {
                show('danger', r.nama || 'Scan tidak dapat dicatat', r.pesan, r.nomor || '');
            }
        } catch (err) {
            show('danger', 'Gagal menghubungi server', 'Periksa koneksi lalu coba lagi.');
        } finally {
            busy = false;
        }
    }

    function onScanSuccess(text) {
        const now = Date.now();
        if (text === lastText && now - lastAt < COOLDOWN_MS) return;
        lastText = text;
        lastAt = now;
        kirim(text);
    }

    function showCameraError(message) {
        cameraError.textContent = message;
        cameraError.hidden = false;
        btnStart.hidden = false;
        btnStop.hidden = true;
    }

    async function startCamera() {
        cameraError.hidden = true;
        if (typeof Html5Qrcode === 'undefined') {
            showCameraError('Pustaka pembaca QR gagal dimuat. Muat ulang halaman.');
            return;
        }
        if (!window.isSecureContext) {
            showCameraError('Kamera hanya bisa dipakai lewat HTTPS atau localhost.');
            return;
        }
        scanner = scanner || new Html5Qrcode('reader');
        try {
            await scanner.start(
                { facingMode: 'environment' },
                {
                    fps: 10,
                    // Area baca besar: 80% sisi terpendek tampilan kamera.
                    qrbox: function (width, height) {
                        const size = Math.floor(Math.min(width, height) * 0.8);
                        return { width: size, height: size };
                    }
                },
                onScanSuccess,
                function () { /* frame tanpa QR: abaikan */ });
            btnStart.hidden = true;
            btnStop.hidden = false;
        } catch (err) {
            showCameraError('Kamera tidak dapat diakses (izin ditolak atau tidak ada kamera).');
        }
    }

    async function stopCamera() {
        if (!scanner) return;
        try { await scanner.stop(); } catch (e) { /* sudah berhenti */ }
        btnStart.hidden = false;
        btnStop.hidden = true;
    }

    btnStart.addEventListener('click', startCamera);
    btnStop.addEventListener('click', stopCamera);

    startCamera(); // langsung aktif saat halaman dibuka
})();
