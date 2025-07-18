let localStream = null;
let screenStream = null;
let peerConnections = {};
let boardId = null;
let userId = null;
let srConnection = null;
let dotNetObjRef = null;
let cameraOn = true;
let micOn = true;
let screenSharing = false;
let joinedBoardGroup = false;
let inCall = false;

const mediaConstraints = { video: true, audio: true };
const MAX_RETRY_ATTEMPTS = 3;
const RETRY_DELAY = 2000; // 2 seconds

window.BoardCallInterop = {
    init: function (board, localVideoId, dotNetRef, realUserId) {
        boardId = board;
        dotNetObjRef = dotNetRef;
        userId = realUserId;

        srConnection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:5071/webrtc")
            .configureLogging(signalR.LogLevel.Information)
            .build();
            
        srConnection.onclose(startSignalR);
        srConnection.on("Receive", onSignalReceived);
        
        const localVideo = document.getElementById(localVideoId);
        if (!localVideo) {
            console.error('[BoardCallInterop] Local video element not found:', localVideoId);
            return;
        }
        
        navigator.mediaDevices.getUserMedia(mediaConstraints)
            .then(stream => {
                localStream = stream;
                localVideo.srcObject = stream;
                startSignalR();
            })
            .catch(err => {
                console.error('[BoardCallInterop] Error getting user media:', err);
            });
    },

    startCall: function () {
        inCall = true;
        ensureJoinedBoardGroup().then(() => {
            sendSignal({
                type: 'user-joined',
                userId: userId,
                displayName: `User ${userId.slice(-4)}`,
                board: boardId
            });
            
            sendSignal({
                type: 'request-users',
                userId: userId,
                board: boardId
            });
        });
    },

    hangUp: function () {
        inCall = false;
        
        if (screenSharing) {
            stopScreenShare();
        }
        
        Object.keys(peerConnections).forEach(remoteUserId => {
            if (peerConnections[remoteUserId]) {
                peerConnections[remoteUserId].close();
                delete peerConnections[remoteUserId];
            }
        });
        
        if (srConnection && srConnection.state === "Connected") {
            sendSignal({
                type: 'user-left',
                userId: userId,
                board: boardId
            });
        }
        
        if (localStream) {
            const tracks = localStream.getTracks();
            tracks.forEach(track => {
                track.enabled = true;
            });
        }
    },

    setLocalVideoStream: function (videoId) {
        const videoElement = document.getElementById(videoId);
        if (videoElement && localStream) {
            videoElement.srcObject = localStream;
            return true;
        }
        return false;
    },

    toggleCamera: function (on) {
        cameraOn = on;
        if (localStream) {
            localStream.getVideoTracks().forEach(track => {
                track.enabled = cameraOn;
            });
        }
    },

    toggleMic: function (on) {
        micOn = on;
        if (localStream) {
            localStream.getAudioTracks().forEach(track => {
                track.enabled = micOn;
            });
        }
    },

    toggleScreenShare: function () {

        if (screenSharing) {
            stopScreenShare();
        } else {
            startScreenShare();
        }
    }
};

function startSignalR() {
    if (!srConnection) return;
    
    srConnection.start().then(() => {

        ensureJoinedBoardGroup();
        if (dotNetObjRef) {
            dotNetObjRef.invokeMethodAsync('OnWebRtcConnected');
        }
    }).catch(err => {
        console.error('[BoardCallInterop] SignalR connection error:', err);
        setTimeout(startSignalR, 5000);
    });
}

function ensureJoinedBoardGroup() {
    if (joinedBoardGroup || !srConnection || srConnection.state !== "Connected") {
        return Promise.resolve();
    }
    
    return srConnection.invoke("JoinBoardGroup", boardId).then(() => {
        joinedBoardGroup = true;

    }).catch(err => {
        console.error('[BoardCallInterop] Failed to join board group:', err);
    });
}

function sendSignal(message) {
    if (srConnection && srConnection.state === "Connected") {
        srConnection.invoke("Send", JSON.stringify(message), boardId);
    } else {
        console.warn('[BoardCallInterop] SignalR not connected, message not sent:', message);
    }
}

function onSignalReceived(data) {
    try {
        const message = JSON.parse(data);
        if (message.board !== boardId) return;
        if (message.userId === userId) return;
        
        switch (message.type) {
            case 'user-joined':
                handleUserJoined(message);
                break;
            case 'user-left':
                handleUserLeft(message);
                break;
            case 'request-users':
                handleRequestUsers(message);
                break;
            case 'offer':
                handleOffer(message);
                break;
            case 'answer':
                handleAnswer(message);
                break;
            case 'ice-candidate':
                handleIceCandidate(message);
                break;
            case 'screen-share-started':
                handleScreenShareStarted(message);
                break;
            case 'screen-share-stopped':
                handleScreenShareStopped(message);
                break;
            default:
                console.warn('[BoardCallInterop] Unknown message type:', message.type);
        }
    } catch (err) {
        console.error('[BoardCallInterop] Error parsing signal message:', err);
    }
}

function handleUserJoined(message) {
    
    if (dotNetObjRef) {
        dotNetObjRef.invokeMethodAsync('AddRemoteUser', message.userId, message.displayName);
    }
    
    createPeerConnectionWithRetry(message.userId, 0);
}

function handleUserLeft(message) {
    
    if (peerConnections[message.userId]) {
        peerConnections[message.userId].close();
        delete peerConnections[message.userId];
    }
    
    if (dotNetObjRef) {
        dotNetObjRef.invokeMethodAsync('RemoveRemoteUser', message.userId);
    }
}

function handleRequestUsers(message) {
    
    if (inCall) {
        sendSignal({
            type: 'user-joined',
            userId: userId,
            displayName: `User ${userId.slice(-4)}`,
            board: boardId
        });
        
        if (screenSharing) {
            sendSignal({
                type: 'screen-share-started',
                userId: userId,
                board: boardId
            });
        }
    }
}

function handleScreenShareStarted(message) {
    if (dotNetObjRef) {
        dotNetObjRef.invokeMethodAsync('UpdateUserScreenShareStatus', message.userId, true);
    }
}

function handleScreenShareStopped(message) {
    if (dotNetObjRef) {
        dotNetObjRef.invokeMethodAsync('UpdateUserScreenShareStatus', message.userId, false);
    }
}

function handleOffer(message) {
    
    if (!peerConnections[message.userId]) {
        createPeerConnection(message.userId);
    }
    
    const pc = peerConnections[message.userId];
    pc.setRemoteDescription(new RTCSessionDescription(message.sdp))
        .then(() => pc.createAnswer())
        .then(answer => pc.setLocalDescription(answer))
        .then(() => {
            sendSignal({
                type: 'answer',
                userId: userId,
                sdp: pc.localDescription,
                board: boardId
            });
        })
        .catch(err => {
            console.error('[BoardCallInterop] Error handling offer:', err);
            setTimeout(() => createPeerConnectionWithRetry(message.userId, 0), RETRY_DELAY);
        });
}

function handleAnswer(message) {
    
    const pc = peerConnections[message.userId];
    if (pc && pc.signalingState !== 'stable') {
        pc.setRemoteDescription(new RTCSessionDescription(message.sdp))
            .then(() => {
                if (dotNetObjRef) {
                    dotNetObjRef.invokeMethodAsync('UpdateUserConnectionStatus', message.userId, 'connected');
                }
            })
            .catch(err => {
                console.error('[BoardCallInterop] Error setting remote answer:', err);
                setTimeout(() => createPeerConnectionWithRetry(message.userId, 0), RETRY_DELAY);
            });
    }
}

function handleIceCandidate(message) {
    
    const pc = peerConnections[message.userId];
    if (pc) {
        pc.addIceCandidate(new RTCIceCandidate(message.candidate))
            .catch(err => {
                console.error('[BoardCallInterop] Error adding ICE candidate:', err);
            });
    }
}

function startScreenShare() {
    
    if (!navigator.mediaDevices || !navigator.mediaDevices.getDisplayMedia) {
        console.error('[BoardCallInterop] Screen sharing not supported');
        return;
    }
    
    navigator.mediaDevices.getDisplayMedia({
        video: true,
        audio: true
    })
    .then(stream => {
        screenStream = stream;
        screenSharing = true;
        
        const localVideo = document.getElementById('localVideo');
        if (localVideo) {
            localVideo.srcObject = screenStream;
        }
        
        const videoTrack = screenStream.getVideoTracks()[0];
        Object.keys(peerConnections).forEach(remoteUserId => {
            const pc = peerConnections[remoteUserId];
            const sender = pc.getSenders().find(s => s.track && s.track.kind === 'video');
            if (sender) {
                sender.replaceTrack(videoTrack);
            }
        });
        
        videoTrack.onended = () => {
            stopScreenShare();
        };
        
        sendSignal({
            type: 'screen-share-started',
            userId: userId,
            board: boardId
        });
        
        if (dotNetObjRef) {
            dotNetObjRef.invokeMethodAsync('OnScreenShareStatusChanged', true);
        }
        
    })
    .catch(err => {
        console.error('[BoardCallInterop] Error starting screen share:', err);
        screenSharing = false;
        
        if (dotNetObjRef) {
            dotNetObjRef.invokeMethodAsync('OnScreenShareStatusChanged', false);
        }
    });
}

function stopScreenShare() {
    
    if (screenStream) {
        screenStream.getTracks().forEach(track => track.stop());
        screenStream = null;
    }
    
    screenSharing = false;
    
    const localVideo = document.getElementById('localVideo');
    if (localVideo && localStream) {
        localVideo.srcObject = localStream;
    }
    
    if (localStream) {
        const videoTrack = localStream.getVideoTracks()[0];
        Object.keys(peerConnections).forEach(remoteUserId => {
            const pc = peerConnections[remoteUserId];
            const sender = pc.getSenders().find(s => s.track && s.track.kind === 'video');
            if (sender && videoTrack) {
                sender.replaceTrack(videoTrack);
            }
        });
    }
    
    sendSignal({
        type: 'screen-share-stopped',
        userId: userId,
        board: boardId
    });
    
    if (dotNetObjRef) {
        dotNetObjRef.invokeMethodAsync('OnScreenShareStatusChanged', false);
    }
    
}

function createPeerConnectionWithRetry(remoteUserId, attemptNumber) {
    
    try {
        createPeerConnection(remoteUserId);
        createOffer(remoteUserId);
    } catch (err) {
        console.error(`[BoardCallInterop] Error creating peer connection (attempt ${attemptNumber + 1}):`, err);
        
        if (attemptNumber < MAX_RETRY_ATTEMPTS - 1) {
            if (dotNetObjRef) {
                dotNetObjRef.invokeMethodAsync('UpdateUserConnectionStatus', remoteUserId, 'failed');
            }
            
            setTimeout(() => {
                createPeerConnectionWithRetry(remoteUserId, attemptNumber + 1);
            }, RETRY_DELAY * (attemptNumber + 1));
        } else {
            console.error(`[BoardCallInterop] Max retry attempts reached for ${remoteUserId}`);
            if (dotNetObjRef) {
                dotNetObjRef.invokeMethodAsync('UpdateUserConnectionStatus', remoteUserId, 'failed');
            }
        }
    }
}

function createPeerConnection(remoteUserId) {
    
    const pc = new RTCPeerConnection({
        iceServers: [{ urls: 'stun:stun.l.google.com:19302' }]
    });
    
    peerConnections[remoteUserId] = pc;
    
    const streamToAdd = screenSharing ? screenStream : localStream;
    if (streamToAdd) {
        streamToAdd.getTracks().forEach(track => {
            pc.addTrack(track, streamToAdd);
        });
    }
    
    pc.onicecandidate = (event) => {
        if (event.candidate) {
            sendSignal({
                type: 'ice-candidate',
                userId: userId,
                candidate: event.candidate,
                board: boardId
            });
        }
    };
    
    pc.ontrack = (event) => {
        if (event.streams && event.streams[0]) {
            const videoId = `remoteVideo_${remoteUserId}`;
            
            const setStreamWithRetry = (attempt = 0) => {
                const videoElement = document.getElementById(videoId);
                if (videoElement) {
                    videoElement.srcObject = event.streams[0];
                    
                    if (dotNetObjRef) {
                        dotNetObjRef.invokeMethodAsync('UpdateUserConnectionStatus', remoteUserId, 'connected');
                    }
                } else if (attempt < 10) {
                    setTimeout(() => setStreamWithRetry(attempt + 1), 500);
                } else {
                    console.warn('[BoardCallInterop] Remote video element not found after retries:', videoId);
                }
            };
            
            setStreamWithRetry();
        }
    };
    
    pc.onconnectionstatechange = () => {
        
        if (dotNetObjRef) {
            switch (pc.connectionState) {
                case 'connecting':
                    dotNetObjRef.invokeMethodAsync('UpdateUserConnectionStatus', remoteUserId, 'connecting');
                    break;
                case 'connected':
                    dotNetObjRef.invokeMethodAsync('UpdateUserConnectionStatus', remoteUserId, 'connected');
                    break;
                case 'disconnected':
                case 'failed':
                    dotNetObjRef.invokeMethodAsync('UpdateUserConnectionStatus', remoteUserId, 'failed');
                    setTimeout(() => createPeerConnectionWithRetry(remoteUserId, 0), RETRY_DELAY);
                    break;
                case 'closed':
                    handleUserLeft({ userId: remoteUserId });
                    break;
            }
        }
    };
}

function createOffer(remoteUserId) {
    const pc = peerConnections[remoteUserId];
    if (!pc) return;
    
    
    pc.createOffer()
        .then(offer => pc.setLocalDescription(offer))
        .then(() => {
            sendSignal({
                type: 'offer',
                userId: userId,
                sdp: pc.localDescription,
                board: boardId
            });
        })
        .catch(err => {
            console.error('[BoardCallInterop] Error creating offer:', err);
            setTimeout(() => createOffer(remoteUserId), RETRY_DELAY);
        });
}