// spreadsheetGrid.js — document-level keyboard bridge for BMOS Blazor component.
// Mirrors the document.addEventListener('keydown', ...) logic in bmos.html so that
// keyboard navigation works regardless of which element is focused.

let dotNetHelper = null;
let aiInputKeydownHandler = null;
let aiInputElement = null;
const LOCAL_SHEETS_KEY = 'bmos.savedSheets';

export function initialize(dotNetRef) {
    dotNetHelper = dotNetRef;
    document.addEventListener('keydown', onDocKeyDown);
    document.addEventListener('click', onDocClick);
    bindAiInputEnterGuard();
}

export function cleanup() {
    document.removeEventListener('keydown', onDocKeyDown);
    document.removeEventListener('click', onDocClick);

    if (aiInputElement && aiInputKeydownHandler) {
        aiInputElement.removeEventListener('keydown', aiInputKeydownHandler);
    }

    aiInputElement = null;
    aiInputKeydownHandler = null;
    dotNetHelper = null;
}

function onDocClick(e) {
    if (!dotNetHelper) return;

    // Only close dropdowns for clicks outside the menu bar.
    const target = e.target;
    if (target instanceof Element && target.closest('#menubar')) {
        return;
    }

    // Skip invoking .NET if nothing is open.
    if (!document.querySelector('#menubar .m-item.open')) {
        return;
    }

    dotNetHelper
        .invokeMethodAsync('CloseMenusFromJs')
        .catch(err => console.error('[BMOS] CloseMenusFromJs error:', err));
}

// Called after Blazor adds a cell-edit-input to the DOM so it receives focus.
export function focusCellInput() {
    const input = document.querySelector('.cell-edit-input');
    if (!input) return;
    input.focus();
    // Place cursor at end of any pre-filled text (e.g. when typing a char starts edit).
    const len = input.value.length;
    input.setSelectionRange(len, len);
}

// Auto-resizes the AI textarea to fit content (up to its CSS max-height).
export function autoResizeAiInput() {
    const input = document.getElementById('ai-input');
    if (!input) return;

    bindAiInputEnterGuard();

    input.style.height = 'auto';
    input.style.height = `${input.scrollHeight}px`;
}

function bindAiInputEnterGuard() {
    const input = document.getElementById('ai-input');
    if (!input) return;

    // If Blazor re-rendered and replaced the textarea node, rebind listener.
    if (aiInputElement === input && aiInputKeydownHandler) {
        return;
    }

    if (aiInputElement && aiInputKeydownHandler) {
        aiInputElement.removeEventListener('keydown', aiInputKeydownHandler);
    }

    aiInputKeydownHandler = (e) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
        }
    };

    aiInputElement = input;
    aiInputElement.addEventListener('keydown', aiInputKeydownHandler);
}

function readSavedSheets() {
    try {
        const raw = localStorage.getItem(LOCAL_SHEETS_KEY);
        if (!raw) return {};
        const parsed = JSON.parse(raw);
        return parsed && typeof parsed === 'object' ? parsed : {};
    } catch {
        return {};
    }
}

function writeSavedSheets(sheets) {
    localStorage.setItem(LOCAL_SHEETS_KEY, JSON.stringify(sheets));
}

export function saveSheetToLocal(name, jsonText) {
    const normalizedName = (name || 'Untitled').trim() || 'Untitled';
    const sheets = readSavedSheets();
    sheets[normalizedName] = {
        json: jsonText,
        savedAt: new Date().toISOString()
    };
    writeSavedSheets(sheets);
}

export function loadSheetFromLocal(name) {
    const sheets = readSavedSheets();
    return sheets[name]?.json ?? null;
}

export function deleteSheetFromLocal(name) {
    const sheets = readSavedSheets();
    delete sheets[name];
    writeSavedSheets(sheets);
}

export function listSheetsFromLocal() {
    const sheets = readSavedSheets();
    return Object.entries(sheets).map(([name, entry]) => ({
        name,
        savedAt: entry.savedAt || ''
    }));
}

export function downloadSheetFile(fileName, jsonText) {
    const blob = new Blob([jsonText], { type: 'application/json;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    document.body.removeChild(anchor);
    URL.revokeObjectURL(url);
}

function onDocKeyDown(e) {
    if (!dotNetHelper) return;

    // If the cell editor (inline input) is active, let Blazor's @onkeydown handle it.
    if (document.activeElement?.classList.contains('cell-edit-input')) return;

    // If another INPUT or TEXTAREA has focus (formula bar, doc-name, AI textarea, modal
    // inputs), let the browser / Blazor handle it normally.
    const tag = document.activeElement?.tagName;
    if (tag === 'INPUT' || tag === 'TEXTAREA') return;

    // Prevent the browser from scrolling the page on navigation keys.
    if (['ArrowUp', 'ArrowDown', 'ArrowLeft', 'ArrowRight', 'Tab'].includes(e.key)) {
        e.preventDefault();
    }

    // Prevent the browser "Save page" dialog from opening on Ctrl/Cmd+S.
    if ((e.ctrlKey || e.metaKey) && (e.key === 's' || e.key === 'S')) {
        e.preventDefault();
    }

    dotNetHelper
        .invokeMethodAsync('HandleKeyFromJs', e.key, e.ctrlKey || e.metaKey, e.shiftKey, e.altKey)
        .catch(err => console.error('[BMOS] HandleKeyFromJs error:', err));
}
