(function () {
    // --- CONFIGURATION & STATE ---
    const config = {
        currentUserId: document.getElementById('currentUserId').value,
        chatUserId: document.getElementById('chatUserId').value,
        elements: {
            // Inputs
            messageInput: document.getElementById('messageInput'),
            fileInput: document.getElementById('fileInput'),
            sendButton: document.getElementById('sendButton'),
            attachButton: document.getElementById('attachButton'),

            // Audio Recording Elements (NEW)
            recordButton: document.getElementById('recordButton'),
            recordingArea: document.getElementById('recordingArea'),
            recordingTimer: document.getElementById('recordingTimer'),
            cancelRecordingBtn: document.getElementById('cancelRecordingBtn'),
            stopRecordingBtn: document.getElementById('stopRecordingBtn'),

            // Preview Area
            previewArea: document.getElementById('attachmentPreview'),
            previewImage: document.getElementById('previewImage'),
            previewFileIcon: document.getElementById('previewFileIcon'),
            previewFileName: document.getElementById('previewFileName'),
            removeAttachmentBtn: document.getElementById('removeAttachmentBtn'),

            // Chat Area
            messagesContainer: document.getElementById('chatMessages'),
            typingIndicator: document.getElementById('typingIndicator'),
            userStatus: document.getElementById('userStatus')
        }
    };

    let connection = null;
    let typingTimeout = null;
    let isConnected = false;

    // Audio State
    let mediaRecorder = null;
    let audioChunks = [];
    let recordingStartTime = null;
    let recordingInterval = null;
    let currentAudioBlob = null; // Stores the final recorded file

    // --- INITIALIZATION ---

    // Auto-resize textarea
    config.elements.messageInput.addEventListener('input', function () {
        this.style.height = 'auto';
        this.style.height = Math.min(this.scrollHeight, 120) + 'px';
        handleTyping();
    });

    // Scroll to bottom on load
    scrollToBottom(true);

    // Initialize SignalR
    async function initializeConnection() {
        try {
            connection = new signalR.HubConnectionBuilder()
                .withUrl("/chatHub")
                .withAutomaticReconnect()
                .build();

            window.signalRConnection = connection;
            setupSignalREvents();

            await connection.start();
            isConnected = true;
            console.log("SignalR Connected");

            // Mark existing messages as read
            await connection.invoke("MarkAsRead", config.chatUserId);

        } catch (err) {
            console.error("SignalR Connection Error:", err);
            setTimeout(initializeConnection, 5000);
        }
    }

    // --- FILE HANDLING LOGIC ---

    // 1. Trigger hidden file input
    config.elements.attachButton.addEventListener('click', () => {
        config.elements.fileInput.click();
    });

    // 2. Handle File Selection & Preview
    config.elements.fileInput.addEventListener('change', function () {
        const file = this.files[0];
        if (!file) return;

        // Reset any existing audio blob if user selects a file
        currentAudioBlob = null;

        showPreview(file);
    });

    // 3. Remove Attachment (Unified for Files & Audio)
    config.elements.removeAttachmentBtn.addEventListener('click', clearAttachment);

    function clearAttachment() {
        config.elements.fileInput.value = ''; // Reset input
        currentAudioBlob = null; // Reset audio

        config.elements.previewArea.classList.add('hidden');
        config.elements.previewImage.src = '';
    }

    function showPreview(fileObj) {
        config.elements.previewArea.classList.remove('hidden');
        config.elements.previewFileName.textContent = fileObj.name || "Voice Message";

        // Check if image
        if (fileObj.type.startsWith('image/')) {
            config.elements.previewImage.classList.remove('hidden');
            config.elements.previewFileIcon.classList.add('hidden');
            config.elements.previewImage.src = URL.createObjectURL(fileObj);
        } else {
            // Audio or generic file uses the Icon
            config.elements.previewImage.classList.add('hidden');
            config.elements.previewFileIcon.classList.remove('hidden');
        }
    }

    // --- AUDIO RECORDING LOGIC (NEW) ---

    // 1. Start Recording
    config.elements.recordButton.addEventListener('click', async () => {
        // Clear any existing attachments first
        clearAttachment();

        if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
            alert("Audio recording is not supported in this browser.");
            return;
        }

        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            mediaRecorder = new MediaRecorder(stream);
            audioChunks = [];

            mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    audioChunks.push(event.data);
                }
            };

            mediaRecorder.onstop = () => {
                // Create the Blob
                const blob = new Blob(audioChunks, { type: 'audio/webm' });
                currentAudioBlob = blob;

                // Show in Preview Area (DRY: reusing file preview)
                // We mock a file object with a name and type for the preview function
                const mockFile = {
                    name: "Voice_Message.webm",
                    type: "audio/webm"
                };
                showPreview(mockFile);

                // Stop all tracks to release microphone hardware
                stream.getTracks().forEach(track => track.stop());
            };

            mediaRecorder.start();
            startTimer();

            // Toggle UI: Show recording area, disable text input
            config.elements.recordingArea.classList.remove('hidden');
            config.elements.messageInput.disabled = true;

        } catch (err) {
            console.error("Error accessing microphone:", err);
            alert("Could not access microphone. Please ensure permissions are granted.");
        }
    });

    // 2. Stop Recording (Success)
    config.elements.stopRecordingBtn.addEventListener('click', () => {
        if (mediaRecorder && mediaRecorder.state !== 'inactive') {
            mediaRecorder.stop();
        }
        resetRecordingUI();
    });

    // 3. Cancel Recording
    config.elements.cancelRecordingBtn.addEventListener('click', () => {
        if (mediaRecorder && mediaRecorder.state !== 'inactive') {
            // Nullify onstop so we don't save the blob
            mediaRecorder.onstop = null;
            mediaRecorder.stop();
            // Stop streams manually
            mediaRecorder.stream.getTracks().forEach(track => track.stop());
        }
        resetRecordingUI();
    });

    function startTimer() {
        recordingStartTime = Date.now();
        config.elements.recordingTimer.innerText = "00:00";
        recordingInterval = setInterval(() => {
            const elapsed = Date.now() - recordingStartTime;
            const date = new Date(elapsed);
            config.elements.recordingTimer.innerText = date.toISOString().substr(14, 5); // mm:ss
        }, 1000);
    }

    function resetRecordingUI() {
        clearInterval(recordingInterval);
        config.elements.recordingArea.classList.add('hidden');
        config.elements.messageInput.disabled = false;
        config.elements.messageInput.focus();
    }


    // --- SEND MESSAGE LOGIC (HTTP POST) ---

    config.elements.sendButton.addEventListener("click", sendMessage);
    config.elements.messageInput.addEventListener("keypress", (e) => {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });

    async function sendMessage() {
        const text = config.elements.messageInput.value.trim();

        // Determine attachment: Either a File Selection OR a Recorded Audio
        const fileInputFile = config.elements.fileInput.files[0];
        const attachment = fileInputFile || currentAudioBlob;

        // Validation: Must have either text or an attachment
        if (!text && !attachment) return;

        if (!isConnected) {
            alert("Connection lost. Reconnecting...");
            await initializeConnection();
            return;
        }

        try {
            // 1. Prepare Data
            const formData = new FormData();
            formData.append("ReceiverId", config.chatUserId);
            if (text) formData.append("MessageContent", text);

            if (attachment) {
                // If it's a blob (audio), we must provide a filename
                if (attachment instanceof Blob && !attachment.name) {
                    const fileName = `voice-${Date.now()}.webm`;
                    formData.append("File", attachment, fileName);
                } else {
                    formData.append("File", attachment);
                }
            }

            // 2. Optimistic UI: Show message immediately
            const tempId = "temp-" + Date.now();
            appendMessage(text, true, new Date().toISOString(), tempId, false, true, attachment);

            // 3. Reset Inputs immediately
            config.elements.messageInput.value = "";
            config.elements.messageInput.style.height = 'auto';
            clearAttachment(); // Clears both file input and audio blob
            scrollToBottom();

            // 4. Send via HTTP API (Not SignalR Hub)
            const response = await fetch('/Chat/SendMessage', {
                method: 'POST',
                body: formData
            });

            if (!response.ok) throw new Error("Failed to send message");

            const savedMessage = await response.json();

            // 5. Finalize the message (update ID and Status)
            finalizeSentMessage(tempId, savedMessage);

        } catch (err) {
            console.error("Send failed:", err);
            alert("Failed to send message. Please try again.");
        }
    }

    function finalizeSentMessage(tempId, data) {
        // Find the temp message
        const el = document.querySelector(`[data-message-id='${tempId}']`);
        if (!el) return;

        // Update ID
        el.setAttribute("data-message-id", data.id);
        el.removeAttribute("data-client-temp");

        // Update buttons data-id
        const btns = el.querySelectorAll("button");
        btns.forEach(b => b.setAttribute("data-message-id", data.id));

        // Update Status Icon
        const status = el.querySelector(".message-status");
        if (status) status.innerHTML = getStatusIconHtml('sent');
    }

    // --- SIGNALR EVENTS ---

    function setupSignalREvents() {
        // 1. Receive Message (From other user)
        connection.on("ReceiveMessage", (data) => {
            if (data.senderId === config.chatUserId) {
                appendMessage(
                    data.message,
                    false,
                    data.sentAt,
                    data.id,
                    false,
                    false,
                    null,
                    data.attachmentUrl,
                    data.attachmentType, // 'image', 'audio', 'file'
                    data.originalFileName
                );

                scrollToBottom();
                connection.invoke("MarkAsRead", config.chatUserId).catch(console.error);
            }
        });

        // 2. Message Edited
        connection.on("MessageEdited", (messageId, newContent) => {
            const el = document.querySelector(`[data-message-id='${messageId}']`);
            if (el) {
                const textEl = el.querySelector(".message-text");
                if (textEl) textEl.textContent = newContent;
            }
        });

        // 3. Message Deleted
        connection.on("MessageDeleted", (messageId) => {
            const el = document.querySelector(`[data-message-id='${messageId}']`);
            if (el) {
                el.style.transition = "opacity 0.3s";
                el.style.opacity = "0";
                setTimeout(() => el.remove(), 300);
            }
        });

        // 4. Status & Typing
        connection.on("MessagesRead", (userId) => {
            if (userId === config.chatUserId) markAllAsSeen();
        });

        connection.on("UserTyping", (userId) => {
            if (userId === config.chatUserId) showTypingIndicator();
        });

        connection.on("UserOnline", (userId) => {
            if (userId === config.chatUserId) updateUserStatus(true);
        });

        connection.on("UserOffline", (userId) => {
            if (userId === config.chatUserId) updateUserStatus(false);
        });
    }

    // --- UI INTERACTION (Edit/Delete/Menu) ---

    config.elements.messagesContainer.addEventListener('click', function (e) {
        // Toggle Menu
        const optionsBtn = e.target.closest('.message-options-btn');
        if (optionsBtn) {
            e.stopPropagation();
            const container = optionsBtn.closest('.group');
            const menu = container.querySelector('.message-actions');

            // Hide others
            document.querySelectorAll('.message-actions').forEach(m => {
                if (m !== menu) m.classList.add('hidden');
            });

            menu.classList.toggle('hidden');
            return;
        }

        // Edit
        const editBtn = e.target.closest('.edit-message-btn');
        if (editBtn) {
            const container = editBtn.closest('.group');
            const id = editBtn.getAttribute('data-message-id');
            // Check if it's a temp ID
            if (id.startsWith('temp-')) return;

            editBtn.closest('.message-actions').classList.add('hidden');
            startInlineEdit(container, id);
            return;
        }

        // Delete
        const deleteBtn = e.target.closest('.delete-message-btn');
        if (deleteBtn) {
            const id = deleteBtn.getAttribute('data-message-id');
            if (id.startsWith('temp-')) return;

            deleteBtn.closest('.message-actions').classList.add('hidden');
            if (confirm("Delete this message?")) {
                connection.invoke("DeleteMessage", parseInt(id)).catch(console.error);
            }
            return;
        }

        // Close menus if clicking background
        if (!e.target.closest('.message-actions')) {
            document.querySelectorAll('.message-actions').forEach(m => m.classList.add('hidden'));
        }
    });

    document.addEventListener('click', (e) => {
        if (!e.target.closest('.message-options-btn') && !e.target.closest('.message-actions')) {
            document.querySelectorAll('.message-actions').forEach(m => m.classList.add('hidden'));
        }
    });

    // --- INLINE EDITING ---

    function startInlineEdit(container, messageId) {
        if (container.classList.contains('message-editing')) return;

        const textEl = container.querySelector('.message-text');
        if (!textEl) return; // Can't edit if only attachment

        const currentText = textEl.textContent;
        const bubble = textEl.parentElement;

        container.classList.add('message-editing');
        textEl.style.display = 'none';

        const textarea = document.createElement('textarea');
        textarea.className = 'w-full rounded bg-blue-800 text-white placeholder-blue-300 border-none px-2 py-1 text-sm resize-none focus:ring-2 focus:ring-white/50';
        textarea.value = currentText;
        textarea.rows = 2;

        const btnRow = document.createElement('div');
        btnRow.className = 'flex justify-end gap-2 mt-2';
        btnRow.innerHTML = `
            <button class="cancel-edit text-xs text-blue-200 hover:text-white font-medium">Cancel</button>
            <button class="save-edit px-3 py-1 bg-white text-blue-700 text-xs font-bold rounded hover:bg-gray-100">Save</button>
        `;

        bubble.appendChild(textarea);
        bubble.appendChild(btnRow);
        textarea.focus();

        const cancel = () => {
            textarea.remove();
            btnRow.remove();
            textEl.style.display = 'block';
            container.classList.remove('message-editing');
        };

        btnRow.querySelector('.cancel-edit').addEventListener('click', cancel);
        btnRow.querySelector('.save-edit').addEventListener('click', async () => {
            const newText = textarea.value.trim();
            if (newText && newText !== currentText) {
                try {
                    await connection.invoke("EditMessage", parseInt(messageId), newText);
                    textEl.textContent = newText;
                    cancel();
                } catch (err) {
                    alert("Failed to edit message");
                }
            } else {
                cancel();
            }
        });
    }

    // --- HELPER FUNCTIONS ---

    function handleTyping() {
        if (!isConnected) return;
        if (typingTimeout) clearTimeout(typingTimeout);

        if (!config.elements.messageInput.getAttribute('data-typing')) {
            connection.invoke("UserTyping", config.chatUserId).catch(console.error);
            config.elements.messageInput.setAttribute('data-typing', 'true');
        }

        typingTimeout = setTimeout(() => {
            config.elements.messageInput.removeAttribute('data-typing');
        }, 3000);
    }

    function showTypingIndicator() {
        config.elements.typingIndicator.classList.remove("hidden");
        setTimeout(() => config.elements.typingIndicator.classList.add("hidden"), 3500);
    }

    function updateUserStatus(isOnline) {
        config.elements.userStatus.className = isOnline
            ? "absolute bottom-0 right-0 w-3.5 h-3.5 bg-green-500 border-2 border-white rounded-full"
            : "absolute bottom-0 right-0 w-3.5 h-3.5 bg-gray-400 border-2 border-white rounded-full";
    }

    // UPDATED: Now supports files/images/AUDIO
    function appendMessage(text, isSent, timestamp, messageId, isRead, isTemp, fileObj = null, attachmentUrl = null, attachmentType = null, originalFileName = null) {
        const div = document.createElement("div");
        div.className = `flex ${isSent ? "justify-end" : "justify-start"} animate-fadeIn mb-4`;
        if (messageId) div.setAttribute("data-message-id", messageId);
        if (isTemp) div.setAttribute("data-client-temp", "true");

        const time = new Date(timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

        // Generate Attachment HTML
        let attachmentHtml = "";

        // Case A: Optimistic Local File/Blob
        if (fileObj) {
            const url = URL.createObjectURL(fileObj);
            const type = fileObj.type;
            const name = fileObj.name || "Voice Message";

            if (type.startsWith('image/')) {
                attachmentHtml = generateImageHtml(url, isSent);
            } else if (type.startsWith('audio/') || type === 'video/webm') { // Some browsers record audio as video/webm
                attachmentHtml = generateAudioHtml(url, isSent);
            } else {
                attachmentHtml = generateFileHtml(url, name, isSent);
            }
        }
        // Case B: Server URL
        else if (attachmentUrl) {
            if (attachmentType === 'image') {
                attachmentHtml = generateImageHtml(attachmentUrl, isSent);
            } else if (attachmentType === 'audio') {
                attachmentHtml = generateAudioHtml(attachmentUrl, isSent);
            } else {
                attachmentHtml = generateFileHtml(attachmentUrl, originalFileName || "File", isSent);
            }
        }

        // Generate Text HTML
        const textHtml = text ? `<p class="text-sm leading-relaxed break-words message-text ${isSent ? 'text-white' : 'text-gray-800'}">${escapeHtml(text)}</p>` : '';

        // Generate Full Bubble
        if (isSent) {
            div.innerHTML = `
                <div class="max-w-[70%] group relative">
                    <div class="bg-gradient-to-br from-blue-600 to-blue-700 text-white rounded-2xl rounded-tr-sm px-4 py-2.5 shadow-md">
                        ${attachmentHtml}
                        ${textHtml}
                    </div>
                    
                    <button type="button" class="message-options-btn absolute -top-2 -right-2 w-8 h-8 rounded-full bg-white text-gray-600 flex items-center justify-center shadow-sm opacity-0 group-hover:opacity-100 transition-opacity z-10" data-message-id="${messageId}">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v.01M12 12v.01M12 18v.01" /></svg>
                    </button>
                    
                    <div class="message-actions hidden absolute top-full right-0 mt-1 w-32 bg-white rounded-lg shadow-xl border border-gray-100 z-20 overflow-hidden">
                         ${!attachmentUrl && !fileObj ? `<button class="w-full text-left px-4 py-2 hover:bg-gray-50 text-sm text-gray-700 edit-message-btn" data-message-id="${messageId}">Edit</button>` : ''}
                        <button class="w-full text-left px-4 py-2 hover:bg-gray-50 text-sm text-red-600 delete-message-btn" data-message-id="${messageId}">Delete</button>
                    </div>

                    <div class="flex items-center justify-end gap-1.5 mt-1 px-2">
                        <span class="text-xs text-gray-500">${time}</span>
                        <span class="message-status">${getStatusIconHtml(isRead ? 'seen' : 'sent')}</span>
                    </div>
                </div>`;
        } else {
            div.innerHTML = `
                <div class="max-w-[70%]">
                    <div class="bg-white border border-gray-200 rounded-2xl rounded-tl-sm px-4 py-2.5 shadow-sm">
                        ${attachmentHtml}
                        ${textHtml}
                    </div>
                    <div class="flex items-center gap-1.5 mt-1 px-2">
                        <span class="text-xs text-gray-500">${time}</span>
                    </div>
                </div>`;
        }

        config.elements.messagesContainer.appendChild(div);
    }

    // --- HTML GENERATORS ---

    function generateImageHtml(url, isSent) {
        return `
            <div class="mb-2">
                <a href="${url}" target="_blank">
                    <img src="${url}" class="rounded-lg max-w-xs max-h-80 w-auto h-auto object-cover border ${isSent ? 'border-white/20' : 'border-gray-200'}" />
                </a>
            </div>`;
    }

    function generateAudioHtml(url, isSent) {
        // Tailwind styled audio player
        return `
            <div class="mb-2 min-w-[240px]">
                <audio controls class="w-full h-8 rounded opacity-90 focus:outline-none" controlsList="nodownload">
                    <source src="${url}" type="audio/webm">
                    <source src="${url}" type="audio/mp3"> 
                    Your browser does not support the audio element.
                </audio>
                <span class="text-[10px] ${isSent ? 'text-blue-100' : 'text-gray-400'} block mt-1 ml-1">Voice Message</span>
            </div>`;
    }

    function generateFileHtml(url, fileName, isSent) {
        const containerClass = isSent
            ? "bg-black/20 hover:bg-black/30 border-white/10"
            : "bg-gray-100 hover:bg-gray-200 border-gray-200";

        const iconBg = isSent ? "bg-white text-blue-600" : "bg-white text-gray-600 shadow-sm";
        const textClass = isSent ? "text-white" : "text-gray-800";
        const subTextClass = isSent ? "text-blue-100 opacity-75" : "text-gray-500";

        return `
            <div class="mb-2">
                <a href="${url}" target="_blank" class="flex items-center gap-3 p-2 rounded-lg border transition-colors ${containerClass}">
                    <div class="${iconBg} p-1.5 rounded">
                        <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"></path></svg>
                    </div>
                    <div class="flex flex-col overflow-hidden">
                        <span class="text-xs font-semibold truncate ${textClass}" title="${fileName}">${fileName}</span>
                        <span class="text-[10px] uppercase ${subTextClass}">Attachment</span>
                    </div>
                </a>
            </div>`;
    }

    function markAllAsSeen() {
        document.querySelectorAll(".message-status").forEach(el => {
            el.innerHTML = getStatusIconHtml('seen');
        });
    }

    function getStatusIconHtml(status) {
        if (status === 'seen') {
            return `<svg class="w-4 h-4 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M9 13l4 4L23 7"/></svg>`;
        }
        return `<svg class="w-4 h-4 text-gray-400" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7"/></svg>`;
    }

    function escapeHtml(text) {
        if (!text) return "";
        return text.replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#039;");
    }

    function scrollToBottom(immediate = false) {
        if (immediate) {
            config.elements.messagesContainer.scrollTop = config.elements.messagesContainer.scrollHeight;
        } else {
            setTimeout(() => {
                config.elements.messagesContainer.scrollTop = config.elements.messagesContainer.scrollHeight;
            }, 350);
        }
    }

    // Start
    initializeConnection();
})();