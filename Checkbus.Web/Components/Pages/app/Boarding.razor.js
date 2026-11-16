// Isolated JS module for Boarding.razor's DEMO-ONLY live-tracking switch
// (odd/tasks/seguimiento-tiempo-real-demo.md) and the QR boarding-validation camera scanner
// (odd/tasks/validacion-embarque-qr.md). Follows the same colocated `.razor.js` convention
// established by LocationPicker.razor/.razor.js: loaded by the component via IJSRuntime as an
// IJSObjectReference, exporting plain functions invoked from .NET.
//
// Unlike LocationPicker (which can have multiple instances on a page), there is at most one
// Boarding screen open at a time — and therefore at most one driver-device geolocation watcher
// and one camera scan loop — so this module keeps single module-level state instead of an
// instance map.

let watchId = null;
let lastSentAtMs = 0;

function describeGeolocationError(error) {
    switch (error.code) {
        case error.PERMISSION_DENIED:
            return "Permiso de ubicación denegado por el navegador.";
        case error.POSITION_UNAVAILABLE:
            return "No se pudo determinar la ubicación del dispositivo.";
        case error.TIMEOUT:
            return "Se agotó el tiempo de espera al obtener la ubicación.";
        default:
            return "No se pudo obtener la ubicación del dispositivo.";
    }
}

// Starts watching the browser's Geolocation API and throttles how often a received position is
// relayed back to .NET (watchPosition can fire far more often than this demo's backend needs to
// be posted to) — minIntervalMs is enforced here, client-side, before ever invoking .NET.
export function startWatching(dotNetRef, minIntervalMs) {
    if (!("geolocation" in navigator)) {
        dotNetRef.invokeMethodAsync("OnGeolocationError", "Este navegador no soporta geolocalización.");
        return;
    }

    stopWatching();
    lastSentAtMs = 0;

    watchId = navigator.geolocation.watchPosition(
        (position) => {
            const now = Date.now();
            if (now - lastSentAtMs < minIntervalMs) {
                return;
            }
            lastSentAtMs = now;

            dotNetRef.invokeMethodAsync(
                "OnPositionReceived",
                position.coords.latitude,
                position.coords.longitude);
        },
        (error) => {
            dotNetRef.invokeMethodAsync("OnGeolocationError", describeGeolocationError(error));
        },
        { enableHighAccuracy: true, maximumAge: 0 });
}

// Idempotent — safe to call even when nothing is being watched (e.g. on component dispose after
// the switch was already off).
export function stopWatching() {
    if (watchId !== null) {
        navigator.geolocation.clearWatch(watchId);
        watchId = null;
    }
}

// QR boarding-validation camera scanner (odd/tasks/validacion-embarque-qr.md). Decodes using the
// browser's native BarcodeDetector (Shape Detection API) rather than a vendored third-party
// library — no new dependency to ship, and nothing to keep patched. Not every browser implements
// it yet (notably Firefox/Safari without a flag); isQrScanningSupported() lets the component
// detect that up front and fall back to the existing manual Código/Búsqueda methods.
let qrStream = null;
let qrAnimationFrameId = null;
let qrCanvas = null;
let qrLastDecodedText = null;
let qrLastDecodedAtMs = 0;
const QR_REPEAT_COOLDOWN_MS = 3000;

export function isQrScanningSupported() {
    return "BarcodeDetector" in window;
}

// Starts the camera and a per-frame decode loop against the given <video> element. Calls back
// to .NET via OnQrCodeScanned at most once per distinct decoded value within the cooldown
// window — the same QR code can otherwise stay in frame across dozens of animation frames while
// the chofer repositions the camera for the next passenger.
export async function startQrScanning(dotNetRef, videoElementId) {
    stopQrScanning();
    qrLastDecodedText = null;
    qrLastDecodedAtMs = 0;

    const video = document.getElementById(videoElementId);
    if (!video) {
        return;
    }

    if (!isQrScanningSupported()) {
        dotNetRef.invokeMethodAsync("OnCameraError", "Este navegador no soporta escaneo de QR por cámara.");
        return;
    }

    try {
        qrStream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: "environment" } });
    } catch {
        dotNetRef.invokeMethodAsync("OnCameraError", "No se pudo acceder a la cámara. Revisá los permisos del navegador.");
        return;
    }

    video.srcObject = qrStream;
    await video.play();

    const detector = new window.BarcodeDetector({ formats: ["qr_code"] });
    qrCanvas = document.createElement("canvas");
    const context = qrCanvas.getContext("2d");

    const decodeFrame = async () => {
        if (!qrStream) {
            return;
        }

        if (video.videoWidth > 0 && video.videoHeight > 0) {
            qrCanvas.width = video.videoWidth;
            qrCanvas.height = video.videoHeight;
            context.drawImage(video, 0, 0, qrCanvas.width, qrCanvas.height);

            try {
                const codes = await detector.detect(qrCanvas);
                if (codes.length > 0) {
                    const decodedText = codes[0].rawValue;
                    const now = Date.now();
                    const isRepeat = decodedText === qrLastDecodedText && now - qrLastDecodedAtMs < QR_REPEAT_COOLDOWN_MS;
                    if (!isRepeat) {
                        qrLastDecodedText = decodedText;
                        qrLastDecodedAtMs = now;
                        dotNetRef.invokeMethodAsync("OnQrCodeScanned", decodedText);
                    }
                }
            } catch {
                // Transient decode failure on a single frame (e.g. motion blur) — just retry on
                // the next animation frame rather than surfacing an error to the chofer.
            }
        }

        qrAnimationFrameId = requestAnimationFrame(decodeFrame);
    };

    qrAnimationFrameId = requestAnimationFrame(decodeFrame);
}

// Idempotent — safe to call even when nothing is being scanned (e.g. on component dispose after
// the camera was already off).
export function stopQrScanning() {
    if (qrAnimationFrameId !== null) {
        cancelAnimationFrame(qrAnimationFrameId);
        qrAnimationFrameId = null;
    }

    if (qrStream !== null) {
        qrStream.getTracks().forEach(track => track.stop());
        qrStream = null;
    }

    qrCanvas = null;
}
