// wwwroot/js/boardcall.js
// Multi-user WebRTC + SignalR for board calls

let localStream = null;
let peerConnections = {};
let boardId = null;
let userId = null;
let srConnection = null;
let dotNetObjRef = null;
let cameraOn = true;
let micOn = true;
let joinedBoardGroup = false;
let inCall = false;

const mediaConstraints = { video: true, audio: true };

window.BoardCallInterop = {
    init: function (board, localVideoId, dotNetRef) {
        console.log('[BoardCallInterop] Initializing for board:', board);
        boardId = board;
        dotNetObjRef = dotNetRef;
        userId = generateUserId();
        
        // Initialize SignalR connection
        srConnection = new signalR.HubConnectionBuilder()
            .withUrl("http://localhost:5071/webrtc")
            .configureLogging(signalR.LogLevel.Information)
            .build();
            
        srConnection.onclose(startSignalR);
        srConnection.on("Receive", onSignalReceived);
        
        // Get local video element
        const localVideo = document.getElementById(localVideoId);
        if (!localVideo) {
            console.error('[BoardCallInterop] Local video element not found:', localVideoId);
            return;
        }
        
        // Start media capture and SignalR connection
        navigator.mediaDevices.getUserMedia(mediaConstraints)
            .then(stream => {
                localStream = stream;
                localVideo.srcObject = stream;
                console.log('[BoardCallInterop] Local stream acquired');
                startSignalR();
            })
            .catch(err => {
                console.error('[BoardCallInterop] Error getting user media:', err);
            });
    },

    startCall: function () {
        console.log('[BoardCallInterop] Starting call');
        inCall = true;
        ensureJoinedBoardGroup().then(() => {
            // Announce presence to other users
            sendSignal({
                type: 'user-joined',
                userId: userId,
                displayName: `User ${userId.slice(-4)}`,
                board: boardId
            });
            
            // Also request existing users to announce themselves
            sendSignal({
                type: 'request-users',
                userId: userId,
                board: boardId
            });
        });
    },

    hangUp: function () {
        console.log('[BoardCallInterop] Hanging up');
        inCall = false;
        
        // Close all peer connections
        Object.keys(peerConnections).forEach(remoteUserId => {
            if (peerConnections[remoteUserId]) {
                peerConnections[remoteUserId].close();
                delete peerConnections[remoteUserId];
            }
        });
        
        // Announce leaving before stopping stream
        if (srConnection && srConnection.state === "Connected") {
            sendSignal({
                type: 'user-left',
                userId: userId,
                board: boardId
            });
        }
        
        // Don't stop the local stream, just remove it from peer connections
        // This keeps the camera running for the local video
        const localVideo = document.querySelector('#localVideo');
        if (localVideo && localStream) {
            // Keep the local video running
            localVideo.srcObject = localStream;
        }
    },

    toggleCamera: function (on) {
        cameraOn = on;
        console.log('[BoardCallInterop] Toggle camera:', cameraOn);
        if (localStream) {
            localStream.getVideoTracks().forEach(track => {
                track.enabled = cameraOn;
            });
        }
    },

    toggleMic: function (on) {
        micOn = on;
        console.log('[BoardCallInterop] Toggle mic:', micOn);
        if (localStream) {
            localStream.getAudioTracks().forEach(track => {
                track.enabled = micOn;
            });
        }
    }
};

function generateUserId() {
    return 'user-' + Math.random().toString(36).substr(2, 9);
}

function startSignalR() {
    if (!srConnection) return;
    
    srConnection.start().then(() => {
        console.log('[BoardCallInterop] SignalR Connected');
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
        console.log('[BoardCallInterop] Joined board group:', boardId);
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
        
        // Only handle messages for this board
        if (message.board !== boardId) return;
        
        // Don't handle our own messages
        if (message.userId === userId) return;
        
        console.log('[BoardCallInterop] Received message:', message.type, 'from', message.userId);
        
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
            default:
                console.warn('[BoardCallInterop] Unknown message type:', message.type);
        }
    } catch (err) {
        console.error('[BoardCallInterop] Error parsing signal message:', err);
    }
}

function handleUserJoined(message) {
    console.log('[BoardCallInterop] User joined:', message.userId);
    
    // Add user to UI
    if (dotNetObjRef) {
        dotNetObjRef.invokeMethodAsync('AddRemoteUser', message.userId, message.displayName);
    }
    
    // Create peer connection and send offer
    createPeerConnection(message.userId);
    createOffer(message.userId);
}

function handleUserLeft(message) {
    console.log('[BoardCallInterop] User left:', message.userId);
    
    // Close peer connection
    if (peerConnections[message.userId]) {
        peerConnections[message.userId].close();
        delete peerConnections[message.userId];
    }
    
    // Remove user from UI
    if (dotNetObjRef) {
        dotNetObjRef.invokeMethodAsync('RemoveRemoteUser', message.userId);
    }
}

function handleRequestUsers(message) {
    console.log('[BoardCallInterop] User requesting existing users:', message.userId);
    
    // If we're in a call, announce our presence to the new user
    if (inCall) {
        sendSignal({
            type: 'user-joined',
            userId: userId,
            displayName: `User ${userId.slice(-4)}`,
            board: boardId
        });
    }
}

function handleOffer(message) {
    console.log('[BoardCallInterop] Received offer from:', message.userId);
    
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
        });
}

function handleAnswer(message) {
    console.log('[BoardCallInterop] Received answer from:', message.userId);
    
    const pc = peerConnections[message.userId];
    if (pc && pc.signalingState !== 'stable') {
        pc.setRemoteDescription(new RTCSessionDescription(message.sdp))
            .catch(err => {
                console.error('[BoardCallInterop] Error setting remote answer:', err);
            });
    }
}

function handleIceCandidate(message) {
    console.log('[BoardCallInterop] Received ICE candidate from:', message.userId);
    
    const pc = peerConnections[message.userId];
    if (pc) {
        pc.addIceCandidate(new RTCIceCandidate(message.candidate))
            .catch(err => {
                console.error('[BoardCallInterop] Error adding ICE candidate:', err);
            });
    }
}

function createPeerConnection(remoteUserId) {
    console.log('[BoardCallInterop] Creating peer connection for:', remoteUserId);
    
    const pc = new RTCPeerConnection({
        iceServers: [{ urls: 'stun:stun.l.google.com:19302' }]
    });
    
    peerConnections[remoteUserId] = pc;
    
    // Add local stream tracks
    if (localStream) {
        localStream.getTracks().forEach(track => {
            pc.addTrack(track, localStream);
        });
    }
    
    // Handle ICE candidates
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
    
    // Handle remote stream
    pc.ontrack = (event) => {
        console.log('[BoardCallInterop] Received remote track for:', remoteUserId);
        if (event.streams && event.streams[0]) {
            const videoId = `remoteVideo_${remoteUserId}`;
            setTimeout(() => {
                const videoElement = document.getElementById(videoId);
                if (videoElement) {
                    videoElement.srcObject = event.streams[0];
                    console.log('[BoardCallInterop] Set remote stream for:', videoId);
                } else {
                    console.warn('[BoardCallInterop] Remote video element not found:', videoId);
                }
            }, 100);
        }
    };
    
    // Handle connection state changes
    pc.onconnectionstatechange = () => {
        console.log('[BoardCallInterop] Connection state for', remoteUserId, ':', pc.connectionState);
        if (pc.connectionState === 'disconnected' || 
            pc.connectionState === 'failed' || 
            pc.connectionState === 'closed') {
            handleUserLeft({ userId: remoteUserId });
        }
    };
}

function createOffer(remoteUserId) {
    const pc = peerConnections[remoteUserId];
    if (!pc) return;
    
    console.log('[BoardCallInterop] Creating offer for:', remoteUserId);
    
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
        });
}