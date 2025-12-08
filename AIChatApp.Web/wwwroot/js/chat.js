(function () {
    // --- CONFIGURATION & STATE ---
    const config = {
        currentUserId: document.getElementById('currentUserId').value,
        chatUserId: document.getElementById('chatUserId').value,
        elements: {
            messageInput: document.getElementById('messageInput'),
            sendButton: document.getElementById('sendButton'),
            messagesContainer: document.getElementById('chatMessages'),
            typingIndicator: document.getElementById('typingIndicator'),
            userStatus: document.getElementById('userStatus')
        }
    };

    let connection = null;
    let typingTimeout = null;
    let isConnected = false;

    // --- INITIALIZATION ---

    // Auto-resize textarea on input
    config.elements.messageInput.addEventListener('input', function () {
        this.style.height = 'auto';
        this.style.height = Math.min(this.scrollHeight, 120) + 'px';
        handleTyping();
    });

    // Scroll to bottom on page load
    scrollToBottom(true);

    // Initialize SignalR
    async function initializeConnection() {
        try {
            connection = new signalR.HubConnectionBuilder()
                .withUrl("/chatHub")
                .withAutomaticReconnect()
                .build();

            setupSignalREvents();

            await connection.start();
            isConnected = true;
            console.log("SignalR Connected");

            // Mark existing messages as read immediately on load
            await connection.invoke("MarkAsRead", config.chatUserId);

        } catch (err) {
            console.error("SignalR Connection Error:", err);
            setTimeout(initializeConnection, 5000); // Retry logic
        }
    }

    // --- SIGNALR EVENTS ---

    function setupSignalREvents() {
        // 1. Receive Message (From other user)
        connection.on("ReceiveMessage", (data) => {
            if (data.senderId === config.chatUserId) {
                appendMessage(data.message, false, data.sentAt, data.id);
                scrollToBottom();

                // Mark as read immediately since we are viewing the chat
                connection.invoke("MarkAsRead", config.chatUserId).catch(console.error);
            }
        });

        // 2. Message Sent Confirmation (Update our temp message)
        connection.on("MessageSent", (data) => {
            if (data.senderId === config.currentUserId) {
                finalizeSentMessage(data);
            }
        });

        // 3. Message Edited (Real-time update)
        connection.on("MessageEdited", (messageId, newContent) => {
            const el = document.querySelector(`[data-message-id='${messageId}']`);
            if (el) {
                const textEl = el.querySelector(".message-text");
                if (textEl) textEl.textContent = newContent;

                // Add "edited" label if not present
                if (!el.querySelector(".edited-indicator")) {
                    const meta = el.querySelector(".flex.items-center");
                    if (meta) {
                        const span = document.createElement("span");
                        span.className = "edited-indicator text-xs text-gray-400 ml-2";
                        span.textContent = "(edited)";
                        meta.appendChild(span);
                    }
                }
            }
        });

        // 4. Message Deleted (Real-time update)
        connection.on("MessageDeleted", (messageId) => {
            const el = document.querySelector(`[data-message-id='${messageId}']`);
            if (el) {
                el.style.transition = "opacity 0.3s";
                el.style.opacity = "0";
                setTimeout(() => el.remove(), 300);
            }
        });

        // 5. Status & Typing
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

    // --- SEND MESSAGE LOGIC ---

    config.elements.sendButton.addEventListener("click", sendMessage);
    config.elements.messageInput.addEventListener("keypress", (e) => {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
            sendMessage();
        }
    });

    async function sendMessage() {
        const text = config.elements.messageInput.value.trim();
        if (!text) return;

        if (!isConnected) {
            alert("Connection lost. Reconnecting...");
            await initializeConnection();
            return;
        }

        try {
            // 1. Optimistic UI: Show message immediately
            const tempId = "temp-" + Date.now();
            appendMessage(text, true, new Date().toISOString(), tempId, false, true);

            // 2. Reset Input
            config.elements.messageInput.value = "";
            config.elements.messageInput.style.height = 'auto';
            scrollToBottom();

            // 3. Send to Server
            await connection.invoke("SendMessage", config.chatUserId, text);

        } catch (err) {
            console.error("Send failed:", err);
            // Optionally remove the temp message or show error icon
        }
    }

    function finalizeSentMessage(data) {
        // Find the temporary message by matching content and temp attribute
        const tempEls = config.elements.messagesContainer.querySelectorAll("[data-client-temp='true']");

        for (let i = tempEls.length - 1; i >= 0; i--) {
            const el = tempEls[i];
            const textEl = el.querySelector(".message-text");

            // Match content (simple heuristic)
            if (textEl && textEl.textContent === data.message) {
                el.setAttribute("data-message-id", data.id);
                el.removeAttribute("data-client-temp");

                // **FIX: Update the data-message-id on the buttons too**
                const editBtn = el.querySelector(".edit-message-btn");
                const deleteBtn = el.querySelector(".delete-message-btn");
                const optionsBtn = el.querySelector(".message-options-btn");

                if (editBtn) editBtn.setAttribute("data-message-id", data.id);
                if (deleteBtn) deleteBtn.setAttribute("data-message-id", data.id);
                if (optionsBtn) optionsBtn.setAttribute("data-message-id", data.id);

                // Update timestamp
                const timeSpan = el.querySelector(".text-xs.text-gray-500");
                if (timeSpan) timeSpan.textContent = new Date(data.sentAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

                // Update Status Icon
                const status = el.querySelector(".message-status");
                if (status) status.innerHTML = getStatusIconHtml('sent');

                break;
            }
        }
    }

    // --- UI INTERACTION (Edit/Delete/Menu) ---

    config.elements.messagesContainer.addEventListener('click', function (e) {

        // 1. Toggle "Three Dots" Menu
        const optionsBtn = e.target.closest('.message-options-btn');
        if (optionsBtn) {
            e.stopPropagation();
            const container = optionsBtn.closest('.group');
            const menu = container.querySelector('.message-actions');

            // Close all other menus
            document.querySelectorAll('.message-actions').forEach(m => {
                if (m !== menu) m.classList.add('hidden');
            });

            // Toggle current
            menu.classList.toggle('hidden');
            return;
        }

        // 2. Edit Button
        const editBtn = e.target.closest('.edit-message-btn');
        if (editBtn) {
            const container = editBtn.closest('.group');
            const id = editBtn.getAttribute('data-message-id');

            // Skip if temp message
            if (id && id.startsWith('temp-')) {
                console.log('Cannot edit message that is still being sent');
                return;
            }

            // Hide menu
            editBtn.closest('.message-actions').classList.add('hidden');
            startInlineEdit(container, id);
            return;
        }

        // 3. Delete Button
        const deleteBtn = e.target.closest('.delete-message-btn');
        if (deleteBtn) {
            const id = deleteBtn.getAttribute('data-message-id');

            // Skip if temp message
            if (id && id.startsWith('temp-')) {
                console.log('Cannot delete message that is still being sent');
                return;
            }

            // Hide menu
            deleteBtn.closest('.message-actions').classList.add('hidden');
            if (confirm("Delete this message?")) {
                connection.invoke("DeleteMessage", parseInt(id)).catch(console.error);
            }
            return;
        }

        // 4. Click outside to close menus
        if (!e.target.closest('.message-actions')) {
            document.querySelectorAll('.message-actions').forEach(m => m.classList.add('hidden'));
        }
    });

    // Close menus when clicking anywhere else on document
    document.addEventListener('click', (e) => {
        if (!e.target.closest('.message-options-btn') && !e.target.closest('.message-actions')) {
            document.querySelectorAll('.message-actions').forEach(m => m.classList.add('hidden'));
        }
    });

    // --- INLINE EDITING LOGIC ---

    function startInlineEdit(container, messageId) {
        if (container.classList.contains('message-editing')) return;

        const textEl = container.querySelector('.message-text');
        const currentText = textEl.textContent;
        const bubble = textEl.parentElement;

        container.classList.add('message-editing');
        textEl.style.display = 'none';

        // Create Textarea
        const textarea = document.createElement('textarea');
        textarea.className = 'w-full rounded bg-blue-800 text-white placeholder-blue-300 border-none px-2 py-1 text-sm resize-none focus:ring-2 focus:ring-white/50';
        textarea.value = currentText;
        textarea.rows = 2;

        // Action Buttons
        const btnRow = document.createElement('div');
        btnRow.className = 'flex justify-end gap-2 mt-2';
        btnRow.innerHTML = `
            <button class="cancel-edit text-xs text-blue-200 hover:text-white font-medium">Cancel</button>
            <button class="save-edit px-3 py-1 bg-white text-blue-700 text-xs font-bold rounded hover:bg-gray-100">Save</button>
        `;

        bubble.appendChild(textarea);
        bubble.appendChild(btnRow);
        textarea.focus();

        // Handlers
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
                    // Optimistic update
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

        // Clear existing timeout
        if (typingTimeout) clearTimeout(typingTimeout);

        // Only send "I am typing" if we haven't sent it recently (simple throttling)
        if (!config.elements.messageInput.getAttribute('data-typing')) {
            connection.invoke("UserTyping", config.chatUserId).catch(console.error);
            config.elements.messageInput.setAttribute('data-typing', 'true');
        }

        // Reset after 3 seconds of inactivity
        typingTimeout = setTimeout(() => {
            config.elements.messageInput.removeAttribute('data-typing');
        }, 3000);
    }

    function showTypingIndicator() {
        config.elements.typingIndicator.classList.remove("hidden");
        // Hide automatically after 3.5s just in case offline event is missed
        setTimeout(() => config.elements.typingIndicator.classList.add("hidden"), 3500);
    }

    function updateUserStatus(isOnline) {
        config.elements.userStatus.className = isOnline
            ? "absolute bottom-0 right-0 w-3.5 h-3.5 bg-green-500 border-2 border-white rounded-full"
            : "absolute bottom-0 right-0 w-3.5 h-3.5 bg-gray-400 border-2 border-white rounded-full";
    }

    function appendMessage(text, isSent, timestamp, messageId, isRead, isTemp) {
        const div = document.createElement("div");
        div.className = `flex ${isSent ? "justify-end" : "justify-start"} animate-fadeIn mb-4`;
        if (messageId) div.setAttribute("data-message-id", messageId);
        if (isTemp) div.setAttribute("data-client-temp", "true");

        const time = new Date(timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });

        if (isSent) {
            // Sent Message Bubble
            div.innerHTML = `
                <div class="max-w-[70%] group relative">
                    <div class="bg-gradient-to-br from-blue-600 to-blue-700 text-white rounded-2xl rounded-tr-sm px-4 py-2.5 shadow-md">
                        <p class="text-sm leading-relaxed break-words message-text">${escapeHtml(text)}</p>
                    </div>
                    
                    <button type="button" class="message-options-btn absolute -top-2 -right-2 w-8 h-8 rounded-full bg-white text-gray-600 flex items-center justify-center shadow-sm opacity-0 group-hover:opacity-100 transition-opacity z-10" data-message-id="${messageId}">
                        <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 6v.01M12 12v.01M12 18v.01" /></svg>
                    </button>
                    
                    <div class="message-actions hidden absolute top-full right-0 mt-1 w-32 bg-white rounded-lg shadow-xl border border-gray-100 z-20 overflow-hidden">
                        <button class="w-full text-left px-4 py-2 hover:bg-gray-50 text-sm text-gray-700 edit-message-btn" data-message-id="${messageId}">Edit</button>
                        <button class="w-full text-left px-4 py-2 hover:bg-gray-50 text-sm text-red-600 delete-message-btn" data-message-id="${messageId}">Delete</button>
                    </div>

                    <div class="flex items-center justify-end gap-1.5 mt-1 px-2">
                        <span class="text-xs text-gray-500">${time}</span>
                        <span class="message-status">${getStatusIconHtml(isRead ? 'seen' : 'sent')}</span>
                    </div>
                </div>`;
        } else {
            // Received Message Bubble
            div.innerHTML = `
                <div class="max-w-[70%]">
                    <div class="bg-white border border-gray-200 rounded-2xl rounded-tl-sm px-4 py-2.5 shadow-sm">
                        <p class="text-sm text-gray-800 leading-relaxed break-words message-text">${escapeHtml(text)}</p>
                    </div>
                    <div class="flex items-center gap-1.5 mt-1 px-2">
                        <span class="text-xs text-gray-500">${time}</span>
                    </div>
                </div>`;
        }

        config.elements.messagesContainer.appendChild(div);
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
            // Immediate scroll (for page load)
            config.elements.messagesContainer.scrollTop = config.elements.messagesContainer.scrollHeight;
        } else {
            // Delayed scroll (for new messages with animation)
            setTimeout(() => {
                config.elements.messagesContainer.scrollTop = config.elements.messagesContainer.scrollHeight;
            }, 350); // Wait for fadeIn animation (300ms) + small buffer
        }
    }

    // Start
    initializeConnection();
})();