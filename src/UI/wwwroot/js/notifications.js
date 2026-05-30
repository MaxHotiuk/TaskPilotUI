window.notificationHelpers = {
    updateBadge: function (count) {
        const items = document.querySelectorAll('.ant-menu-item');
        let notifItem = null;

        for (const item of items) {
            const menuId = item.getAttribute('data-menu-id') || '';
            if (menuId.endsWith('-notifications') || menuId === 'notifications') {
                notifItem = item;
                break;
            }
        }

        if (!notifItem) return;

        let badge = notifItem.querySelector('.notification-count-badge');

        if (count > 0) {
            if (!badge) {
                badge = document.createElement('span');
                badge.className = 'notification-count-badge';
                notifItem.appendChild(badge);
            }
            badge.textContent = count > 99 ? '99+' : String(count);
        } else if (badge) {
            badge.remove();
        }
    }
};
