(function () {
    // 1. Config
    const rtcConfig = {
        iceServers: [{ urls: 'stun:stun.l.google.com:19302' }] // Free STUN
    };

    // UI Elements
    const elements = {
        btnStartCall: document.getElementById('btnStartCall'),
        btnAccept: document.getElementById('btnAcceptCall'),
        btnReject: document.getElementById('btnRejectCall'),
        btnHangUp: document.getElementById('btnHangUp'),
        incomingModal: document.getElementById('incomingCallModal'),
        activeOverlay: document.getElementById('activeCallOverlay'),
        remoteAudio: document.getElementById('remoteAudio'),
        targetUserId: document.getElementById('chatUserId').value
    };

    let peerConnection;
    let localStream;
    let incomingOffer = null;
    let currentCallerId = null;

    // Wait for chat.js to initialize SignalR
    const waitForConnection = setInterval(() => {
        if (window.signalRConnection && window.signalRConnection.state === "Connected") {
            clearInterval(waitForConnection);
            initializeCallFeatures(window.signalRConnection);
        }
    }, 500);

    function initializeCallFeatures(connection) {
        console.log("Call features initialized with existing SignalR connection");

        // --- SignalR Listeners ---

        connection.on("IncomingCall", (callerId, offerData) => {
            console.log("Incoming call...");
            incomingOffer = JSON.parse(offerData);
            currentCallerId = callerId;
            elements.incomingModal.classList.remove("hidden");
        });

        connection.on("CallAccepted", async (answerData) => {
            console.log("Call accepted by remote");
            if (peerConnection) {
                await peerConnection.setRemoteDescription(JSON.parse(answerData));
                elements.activeOverlay.classList.remove("hidden");
            }
        });

        connection.on("ReceiveIceCandidate", async (candidateData) => {
            if (peerConnection) {
                try {
                    await peerConnection.addIceCandidate(JSON.parse(candidateData));
                } catch (e) { console.error("Error adding ICE:", e); }
            }
        });

        connection.on("CallEnded", () => {
            endCall();
            alert("Call ended");
        });

        // --- Button Events ---

        elements.btnStartCall.addEventListener('click', startCall);

        elements.btnAccept.addEventListener('click', async () => {
            elements.incomingModal.classList.add("hidden");
            elements.activeOverlay.classList.remove("hidden");

            await setupMediaAndConnection(); // 1. Mic & PC

            await peerConnection.setRemoteDescription(incomingOffer); // 2. Set Offer

            const answer = await peerConnection.createAnswer(); // 3. Create Answer
            await peerConnection.setLocalDescription(answer);

            connection.invoke("AnswerCall", currentCallerId, JSON.stringify(answer));
        });

        elements.btnReject.addEventListener('click', () => {
            elements.incomingModal.classList.add("hidden");
            connection.invoke("HangUp", currentCallerId);
        });

        elements.btnHangUp.addEventListener('click', () => {
            connection.invoke("HangUp", elements.targetUserId || currentCallerId);
            endCall();
        });

        // --- Core WebRTC Functions ---

        async function startCall() {
            try {
                await setupMediaAndConnection();

                const offer = await peerConnection.createOffer();
                await peerConnection.setLocalDescription(offer);

                connection.invoke("CallUser", elements.targetUserId, JSON.stringify(offer));

                // Optimistic UI
                alert("Calling...");
            } catch (err) {
                console.error("Start call failed:", err);
                alert("Could not start call. Check microphone permissions.");
            }
        }

        async function setupMediaAndConnection() {
            // 1. Get Mic
            localStream = await navigator.mediaDevices.getUserMedia({ audio: true });

            // 2. Create PC
            peerConnection = new RTCPeerConnection(rtcConfig);

            // 3. Add Tracks
            localStream.getTracks().forEach(track => peerConnection.addTrack(track, localStream));

            // 4. Handle ICE (Network paths)
            peerConnection.onicecandidate = (event) => {
                if (event.candidate) {
                    const target = currentCallerId || elements.targetUserId;
                    connection.invoke("SendIceCandidate", target, JSON.stringify(event.candidate));
                }
            };

            // 5. Handle Audio Stream
            peerConnection.ontrack = (event) => {
                elements.remoteAudio.srcObject = event.streams[0];
            };
        }

        function endCall() {
            if (peerConnection) {
                peerConnection.close();
                peerConnection = null;
            }
            if (localStream) {
                localStream.getTracks().forEach(track => track.stop());
                localStream = null;
            }
            elements.incomingModal.classList.add("hidden");
            elements.activeOverlay.classList.add("hidden");
            incomingOffer = null;
        }
    }
})();