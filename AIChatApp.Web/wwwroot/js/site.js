/**
 * Closes the generic action modal.
 */
function closeActionModal() {
    const modal = document.getElementById('action-modal');
    if (modal) {
        modal.classList.add('hidden');

        // Restore cancel button visibility in case it was hidden by showSimpleToast
        const cancelButton = document.getElementById('modal-cancel-button');
        if (cancelButton) {
            cancelButton.classList.remove('hidden');
        }
    }
}

/**
 * Displays a simple toast/modal notification based on TempData.
 * Success = Green, Warning = Red.
 */
function showSimpleToast(title, message, type) {
    // 1. Get Elements
    const modal = document.getElementById('action-modal');
    const modalTitle = document.getElementById('modal-title');
    const modalMessage = document.getElementById('modal-message-content');
    const actionBtn = document.getElementById('modal-action-button');
    const iconContainer = document.getElementById('modal-icon-container');
    const cancelButton = document.getElementById('modal-cancel-button');

    // Safety check
    if (!modal || !modalTitle || !modalMessage || !actionBtn || !iconContainer || !cancelButton) {
        console.error("SimpleToast: One or more modal components missing.");
        return;
    }

    // 2. Set Content
    modalTitle.textContent = title;
    modalMessage.textContent = message;
    actionBtn.textContent = "OK"; // Default text for information

    // 3. Hide Cancel Button (Since this is just a notification toast)
    cancelButton.classList.add('hidden');

    // 4. Reset Classes (Remove previous colors)
    actionBtn.classList.remove('bg-indigo-600', 'hover:bg-indigo-500', 'bg-red-600', 'hover:bg-red-500');
    iconContainer.classList.remove('bg-green-100', 'bg-red-100');

    // 5. Apply Visual Style based on Type
    if (type === 'warning') {
        // Red Icon / Button
        iconContainer.innerHTML = `
            <svg class="h-6 w-6 text-red-600" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v3.75m-9.303 3.375h18.606c1.026 0 1.554 1.29.808 2.036l-9.303 9.303c-.63.63-1.63.63-2.26 0L2.14 17.584c-.746-.746-.218-2.036.808-2.036z" />
            </svg>`;
        iconContainer.classList.add('bg-red-100');
        actionBtn.classList.add('bg-red-600', 'hover:bg-red-500');
    } else {
        // Default: Success (Green)
        iconContainer.innerHTML = `
            <svg class="h-6 w-6 text-green-600" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 12.75l6 6 9-13.5" />
            </svg>`;
        iconContainer.classList.add('bg-green-100');
        actionBtn.classList.add('bg-indigo-600', 'hover:bg-indigo-500');
    }

    // 6. Action Handler: Just close
    actionBtn.onclick = closeActionModal;

    // 7. Show Modal
    modal.classList.remove('hidden');
}

/**
 * Handles interactive confirmations (like Logout).
 */
function showActionModal(options) {
    const modal = document.getElementById('action-modal');
    const title = document.getElementById('modal-title');
    const message = document.getElementById('modal-message-content');
    const actionBtn = document.getElementById('modal-action-button');
    const iconContainer = document.getElementById('modal-icon-container');
    const cancelButton = document.getElementById('modal-cancel-button');

    // Set Content
    title.textContent = options.title;
    message.textContent = options.message;
    actionBtn.textContent = options.actionText;

    // Ensure Cancel is visible for interactive modals
    cancelButton.classList.remove('hidden');

    // Reset Styles
    actionBtn.classList.remove('bg-indigo-600', 'hover:bg-indigo-500', 'bg-red-600', 'hover:bg-red-500');
    iconContainer.classList.remove('bg-green-100', 'bg-red-100');

    if (options.type === 'warning') {
        iconContainer.innerHTML = `<svg class="h-6 w-6 text-red-600" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M12 9v3.75m-9.303 3.375h18.606c1.026 0 1.554 1.29.808 2.036l-9.303 9.303c-.63.63-1.63.63-2.26 0L2.14 17.584c-.746-.746-.218-2.036.808-2.036z" /></svg>`;
        iconContainer.classList.add('bg-red-100');
        actionBtn.classList.add('bg-red-600', 'hover:bg-red-500');
    } else {
        iconContainer.innerHTML = `<svg class="h-6 w-6 text-green-600" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor"><path stroke-linecap="round" stroke-linejoin="round" d="M4.5 12.75l6 6 9-13.5" /></svg>`;
        iconContainer.classList.add('bg-green-100');
        actionBtn.classList.add('bg-indigo-600', 'hover:bg-indigo-500');
    }

    actionBtn.onclick = function () {
        closeActionModal();
        if (options.onAction) options.onAction();
    };

    modal.classList.remove('hidden');
}

function confirmLogout(event) {
    event.preventDefault();
    const form = document.getElementById('logoutForm');
    showActionModal({
        title: 'Confirm Logout',
        message: 'Are you sure you want to sign out?',
        type: 'warning',
        actionText: 'Yes, Logout',
        onAction: function () {
            form.submit();
        }
    });
}