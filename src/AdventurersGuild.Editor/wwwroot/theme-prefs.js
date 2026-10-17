window.themePrefs = {
    get: function () {
        try { return localStorage.getItem('ag-theme') || 'light'; }
        catch { return 'light'; }
    },
    set: function (value) {
        try { localStorage.setItem('ag-theme', value); }
        catch { }
    }
};