window.chatHelpers = {
    scrollToBottom: function (element) {
        if (!element) return;
        element.scrollTop = element.scrollHeight;
    },
    isNearBottom: function (element, threshold) {
        if (!element) return false;
        const distance = element.scrollHeight - element.scrollTop - element.clientHeight;
        return distance <= (threshold ?? 0);
    },
    isNearTop: function (element, threshold) {
        if (!element) return false;
        return element.scrollTop <= (threshold ?? 0);
    },
    getScrollInfo: function (element) {
        if (!element) return { scrollTop: 0, scrollHeight: 0, clientHeight: 0 };
        return {
            scrollTop: element.scrollTop,
            scrollHeight: element.scrollHeight,
            clientHeight: element.clientHeight
        };
    },
    setScrollTop: function (element, scrollTop) {
        if (!element) return;
        element.scrollTop = scrollTop;
    }
};
