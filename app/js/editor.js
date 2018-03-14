// Save Monaco's amd require and restore Node's require
var amdRequire = global.require;
global.require = nodeRequire;

// require node modules before loader.js comes in
var path = require('path');
function uriFromPath(_path) {
  var pathName = path.resolve(_path).replace(/\\/g, '/');
  if (pathName.length > 0 && pathName.charAt(0) !== '/') {
    pathName = '/' + pathName;
  }
  return encodeURI('file://' + pathName);
}
amdRequire.config({
  baseUrl: uriFromPath(path.join(__dirname, '../node_modules/monaco-editor/min'))
});
// workaround monaco-css not understanding the environment
self.module = undefined;
// workaround monaco-typescript not understanding the environment
self.process.browser = true;
amdRequire(['vs/editor/editor.main'], function () {

  monaco.languages.register({
    id: 'arm'
  });
  monaco.languages.setMonarchTokensProvider('arm', {
    // Set defaultToken to invalid to see what you do not tokenize yet
    defaultToken: 'invalid',

    ignoreCase: true,

    brackets: [
      ['{', '}', 'delimiter.curly'],
      ['[', ']', 'delimiter.square'],
      ['(', ')', 'delimiter.parenthesis'],
      ['<', '>', 'delimiter.angle']
    ],

    operators: [
      '=', '>', '!'
    ],

    keywords: [
      'MOV', 'ADD', 'ADC', 'MVN', 'ORR', 'AND', 'EOR', 'BIC', 'SUB', 'SBC',
      'RSB', 'RSC', 'CMP', 'CMN', 'TST', 'TEQ', 'LSL', 'LSR', 'ASR', 'LDR',
      'STR', 'ADR', 'LDM', 'STM', 'MVNS', 'MOVS', 'MOV', 'SUBS', 'BPL', 'MVNS',
      'ADD', 'SUBS', 'CMP', 'BNE', 'MOVS', 'ASR', 'CMPCS', 'RRX', 'ASR', 'ROR',
      'ADCS', 'BEQ', 'B'
    ],

    // we include these common regular expressions
    symbols: /[=><!~?:&|+\-*\/\^%]+/,

    // C# style strings
    escapes: /\\(?:[abfnrtv\\"']|x[0-9A-Fa-f]{1,4}|u[0-9A-Fa-f]{4}|U[0-9A-Fa-f]{8})/,

    // The main tokenizer for our languages
    tokenizer: {
      root: [
        // identifiers and keywords
        [/[a-z_$][\w$]*/, {
          cases: {
            '@keywords': 'keyword',
            '@default': 'identifier'
          }
        }],

        // whitespace
        { include: '@whitespace' },

        // delimiters and operators
        [/[{}()\[\]]/, '@brackets'],
        [/[<>](?!@symbols)/, '@brackets'],
        [/@symbols/, {
          cases: {
            '@operators': 'operator',
            '@default': ''
          }
        }],

        // @ annotations.
        // As an example, we emit a debugging log message on these tokens.
        // Note: message are supressed during the first load -- change some lines to see them.
        [/@\s*[a-zA-Z_\$][\w\$]*/, { token: 'annotation', log: 'annotation token: $0' }],

        // numbers
        [/\d*\.\d+([eE][\-+]?\d+)?/, 'number.float'],
        [/0[xX][0-9a-fA-F]+/, 'number.hex'],
        [/\d+/, 'number'],

        // delimiter: after number because of .\d floats
        [/[;,.]/, 'delimiter'],

        // strings
        [/"([^"\\]|\\.)*$/, 'string.invalid'],  // non-teminated string
        [/"/, { token: 'string.quote', bracket: '@open', next: '@string' }],

        // characters
        [/'[^\\']'/, 'string'],
        [/(')(@escapes)(')/, ['string', 'string.escape', 'string']],
        [/'/, 'string.invalid'],

      ],

      comment: [
        [/[^\/*]+/, 'comment'],
        [/\/\*/, 'comment', '@push'],    // nested comment
        ["\\*/", 'comment', '@pop'],
        [/[\/*]/, 'comment']
      ],

      string: [
        [/[^\\"]+/, 'string'],
        [/@escapes/, 'string.escape'],
        [/\\./, 'string.escape.invalid'],
        [/"/, { token: 'string.quote', bracket: '@close', next: '@pop' }]
      ],

      whitespace: [
        [/[ \t\r\n]+/, 'white'],
        //        [/\/\*/, 'comment', '@comment'],
        //        [/\/\/.*$/, 'comment'],
      ],
    }
  });

  monaco.editor.defineTheme('one-dark-pro', {
    base: 'vs-dark',
    inherit: true, // can also be false to completely replace the builtin rules
    rules: [
      { token: 'operators', foreground: '56b6c2'},
      { token: 'keywords', foreground: '56b6c2'},
      { token: 'symbols', foreground: '56b6c2'},
      { token: 'escape', foreground: '57b6c2'},
      { token: 'string', foreground: 'e06c75'}
    ],
    "colors": {
      "activityBar.background": "#2F333D",
      "activityBar.foreground": "#D7DAE0",
      "activityBarBadge.background": "#4D78CC",
      "activityBarBadge.foreground": "#F8FAFD",
      "badge.background": "#282c34",
      "button.background": "#404754",
      "debugToolBar.background": "#21252b",
      "dropdown.background": "#1d1f23",
      "diffEditor.insertedTextBackground": "#00809B33",
      "dropdown.border": "#181A1F",
      "editor.background": "#282c34",
      "editorError.foreground": "#c24038",
      "editorMarkerNavigation.background": "#21252b",
      "editorRuler.foreground": "#abb2bf26",
      "editor.lineHighlightBackground": "#2c313c",
      "editor.selectionBackground": "#67769660",
      "editor.selectionHighlightBackground": "#ffffff10",
      "editor.selectionHighlightBorder": "#ddd",
      "editorCursor.background": "#ffffffc9",
      "editorCursor.foreground": "#528bff",
      "editorBracketMatch.border": "#515a6b",
      "editorBracketMatch.background": "#515a6b",
      "editor.findMatchBackground": "#42557B",
      "editor.findMatchBorder": "#457dff",
      "editor.findMatchHighlightBackground": "#314365",
      "editor.wordHighlightBackground": "#484e5b",
      "editor.wordHighlightBorder": "#7f848e",
      "editor.wordHighlightStrongBackground": "#abb2bf26",
      "editor.wordHighlightStrongBorder": "#7f848e",
      "editorGroup.background": "#181A1F",
      "editorGroup.border": "#181A1F",
      "editorGroupHeader.tabsBackground": "#21252B",
      "editorIndentGuide.background": "#3B4048",
      "editorLineNumber.foreground": "#495162",
      "editorActiveLineNumber.foreground": "#737984",
      "editorWhitespace.foreground": "#3B4048",
      "editorHoverWidget.background": "#21252B",
      "editorHoverWidget.border": "#181A1F",
      "editorSuggestWidget.background": "#21252B",
      "editorSuggestWidget.border": "#181A1F",
      "editorSuggestWidget.selectedBackground": "#2c313a",
      "editorWidget.background": "#21252B",
      "input.background": "#1d1f23",
      "list.activeSelectionBackground": "#2c313a",
      "list.activeSelectionForeground": "#d7dae0",
      "list.focusBackground": "#383E4A",
      "list.hoverBackground": "#292d35",
      "list.highlightForeground": "#C5C5C5",
      "list.inactiveSelectionBackground": "#2c313a",
      "list.inactiveSelectionForeground": "#d7dae0",
      "peekViewEditor.matchHighlightBackground": "#29244b",
      "scrollbarSlider.background": "#4e566660",
      "scrollbarSlider.activeBackground": "#747D9180",
      "scrollbarSlider.hoverBackground": "#5A637580",
      "sideBar.background": "#21252b",
      "sideBarSectionHeader.background": "#282c34",
      "statusBar.background": "#21252B",
      "statusBar.foreground": "#9da5b4",
      "statusBarItem.hoverBackground": "#2c313a",
      "statusBar.noFolderBackground": "#21252B",
      "statusBar.debuggingBackground": "#7e0097",
      "statusBar.debuggingBorder": "#66017a",
      "statusBar.debuggingForeground": "#ffffff",
      "tab.activeBackground": "#2c313a",
      "tab.border": "#181A1F",
      "tab.inactiveBackground": "#21252B",
      "tab.hoverBackground": "#323842",
      "tab.unfocusedHoverBackground": "#323842",
      "terminal.foreground":"#C8C8C8",
      "terminal.ansiBlack": "#2D3139",
      "terminal.ansiBlue": "#2e8ccf",
      "terminal.ansiGreen": "#98c379cc",
      "terminal.ansiYellow": "#B4881D",
      "titleBar.activeBackground": "#282c34",
      "titleBar.activeForeground": "#9da5b4",
      "titleBar.inactiveBackground": "#282C34",
      "titleBar.inactiveForeground": "#6B717D",
    }
  });

  // window.code = monaco.editor.create(document.getElementById('editor'), {
  //   value: [
  //     'mov r0, #5',
  //     'mov r1, r0'
  //   ].join('\n'),
  //   language: 'arm',
  //   theme: 'vs-light',
  //   renderWhitespace: 'all',
  //   roundedSelection: false,
  //   scrollBeyondLastLine: false,
  //   automaticLayout: true
  // });

  var mevent = new CustomEvent("monaco-ready", { "detail": "ready now!" });

  // Dispatch/Trigger/Fire the event
  document.dispatchEvent(mevent);
});
