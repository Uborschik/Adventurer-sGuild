window.jsonEditor = (function () {
    var editors = {};

    return {
        create: function (element, dotnetRef, initialText) {
            var id = "cm-" + Math.random().toString(36).slice(2);
            element.id = id;

            var editor = CodeMirror(element, {
                value: initialText || "",
                mode: { name: "javascript", json: true },
                lineNumbers: true,
                matchBrackets: true,
                indentUnit: 2,
                tabSize: 2,
                lineWrapping: false,
                viewportMargin: 20,
            });

            editor.on("change", function () {
                dotnetRef.invokeMethodAsync("OnTextChanged", editor.getValue());
            });

            editors[id] = editor;
            return id;
        },

        setText: function (id, text) {
            var editor = editors[id];
            if (!editor) return;
            if (editor.getValue() === text) return;
            editor.setValue(text);
        },

        destroy: function (id) {
            delete editors[id];
        }
    };
})();