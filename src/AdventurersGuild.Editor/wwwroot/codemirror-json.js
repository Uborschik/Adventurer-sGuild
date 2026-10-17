window.jsonEditor = (function () {
    var editors = {};
    var currentTheme = "default";
    var hintData = {}; 

    function jsonHint(cm) {
        var cur = cm.getCursor();
        var line = cm.getLine(cur.line);
        var start = cur.ch;
        
        // Ищем открывающую кавычку перед курсором
        while (start > 0 && line[start - 1] !== '"') {
            start--;
        }
        if (start === 0) return null; 
        start--;
        
        // Проверяем, что это значение (перед кавычкой должно быть двоеточие)
        var prefix = line.substring(0, start).trim();
        if (!prefix.endsWith(':')) return null;

        // Ищем ключ, к которому относится это значение
        var keyMatch = prefix.match(/"([^"]+)"\s*:$/);
        if (!keyMatch) return null;
        var key = keyMatch[1];
        
        var options = hintData[key];
        if (!options) return null;

        var word = line.substring(start + 1, cur.ch);
        var filtered = options.filter(function(o) { 
            return o.toLowerCase().startsWith(word.toLowerCase()); 
        });
        
        if (filtered.length === 0) return null;

        return {
            list: filtered,
            from: CodeMirror.Pos(cur.line, start + 1),
            to: CodeMirror.Pos(cur.line, cur.ch)
        };
    }

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
                theme: currentTheme,
                extraKeys: {
                    "Ctrl-Space": function(cm) { cm.showHint({ hint: jsonHint, completeSingle: false }); }
                }
            });

            editor.on("inputRead", function (cm, change) {
                if (change.origin === "+input") {
                    var cur = cm.getCursor();
                    var line = cm.getLine(cur.line);
                    var quoteIndex = line.lastIndexOf('"', cur.ch - 1);
                    var colonIndex = line.lastIndexOf(':', cur.ch - 1);
                    if (quoteIndex > colonIndex && quoteIndex !== -1) {
                        cm.showHint({ hint: jsonHint, completeSingle: false });
                    }
                }
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

        setTheme: function (theme) {
            currentTheme = theme;
            for (var id in editors) {
                if (editors.hasOwnProperty(id)) {
                    editors[id].setOption("theme", currentTheme);
                }
            }
        },

        setHints: function (data) {
            hintData = data;
        },

        destroy: function (id) {
            delete editors[id];
        }
    };
})();