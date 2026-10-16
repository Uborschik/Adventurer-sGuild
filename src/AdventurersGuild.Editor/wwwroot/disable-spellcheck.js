(function () {
    function disableSpellcheck(root) {
        root.querySelectorAll('textarea, input[type="text"]').forEach(function (el) {
            if (el.closest('.json-editor')) {
                el.setAttribute('spellcheck', 'false');
                el.setAttribute('autocomplete', 'off');
                el.setAttribute('autocorrect', 'off');
                el.setAttribute('autocapitalize', 'off');
            }
        });
    }

    // При загрузке
    document.addEventListener('DOMContentLoaded', function () {
        disableSpellcheck(document);
    });

    // Следим за изменениями DOM (Blazor может пересоздавать элементы)
    var observer = new MutationObserver(function (mutations) {
        disableSpellcheck(document);
    });

    observer.observe(document.body || document.documentElement, {
        childList: true,
        subtree: true
    });
})();