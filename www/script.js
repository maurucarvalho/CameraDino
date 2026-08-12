document.addEventListener('DOMContentLoaded', () => {
    const videoPlayer = document.getElementById('videoPlayer');
    const errorMessage = document.getElementById('errorMessage');

    function showError(msg) {
        if (msg) {
            errorMessage.textContent = msg;
            errorMessage.classList.remove('hidden');
        } else {
            errorMessage.classList.add('hidden');
        }
    }

    async function connectCamera() {
        showError('');
        const streamName = 'live_camera';
        
        const pc = new RTCPeerConnection({
            iceServers: []
        });

        pc.addTransceiver('video', { direction: 'recvonly' });

        pc.ontrack = (event) => {
            if (event.receiver) {
                try {
                    event.receiver.playoutDelayHint = 0;
                } catch (e) {
                    console.warn("playoutDelayHint não suportado", e);
                }
            }

            if (videoPlayer.srcObject !== event.streams[0]) {
                videoPlayer.srcObject = event.streams[0];
                videoPlayer.play().catch(e => console.warn("Autoplay prevenido:", e));
            }
        };

        pc.onconnectionstatechange = () => {
            if (pc.connectionState === 'failed' || pc.connectionState === 'disconnected') {
                showError('Waiting for Camera Dino configuration');
            }
        };

        try {
            const offer = await pc.createOffer();
            await pc.setLocalDescription(offer);

            const res = await fetch(`/api/webrtc?src=${streamName}`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
                body: pc.localDescription.sdp
            });

            if (!res.ok) {
                throw new Error('Waiting for Camera Dino configuration');
            }

            const answerSdp = await res.text();
            await pc.setRemoteDescription({
                type: 'answer',
                sdp: answerSdp
            });
            
        } catch (err) {
            console.error(err);
            showError('Waiting for Camera Dino configuration');
        }
    }

    connectCamera();

    // Loop anti-atraso: força o player a pular para o momento mais recente do vídeo se houver lentidão/buffer
    setInterval(() => {
        if (!videoPlayer.paused && videoPlayer.buffered.length > 0) {
            const bufferedEnd = videoPlayer.buffered.end(videoPlayer.buffered.length - 1);
            const delay = bufferedEnd - videoPlayer.currentTime;
            // Se o atraso for maior que 1 segundo, pula para frente
            if (delay > 1.0) {
                videoPlayer.currentTime = bufferedEnd;
                console.log("Anti-atraso acionado: pulou " + delay.toFixed(2) + " segundos");
            }
        }
    }, 3000);
});
