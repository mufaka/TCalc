// ============================================================
// TCalc — Shared JS Utilities
// ============================================================

// ---------- §6.6: Chart.js Theme Helper ----------

/**
 * Resolves CSS custom property values from the current theme.
 * Returns an object with commonly used Chart.js colour values.
 *
 * Usage:
 *   const t = tcChartTheme();
 *   // t.text, t.textMuted, t.border, t.primary, t.accent,
 *   // t.bg, t.bgElevated, t.gridColor, t.tickColor
 */
function tcChartTheme() {
    const s = getComputedStyle(document.documentElement);
    const v = (prop) => s.getPropertyValue(prop).trim();
    return {
        text:        v('--tc-text')        || '#1a1a2e',
        textMuted:   v('--tc-text-muted')  || '#6b7280',
        border:      v('--tc-border')      || '#e2e5ec',
        primary:     v('--tc-primary')     || '#4f6ef7',
        accent:      v('--tc-accent')      || '#6c8cff',
        bg:          v('--tc-bg')          || '#f4f6fb',
        bgElevated:  v('--tc-bg-elevated') || '#ffffff',
        gridColor:   v('--tc-border')      || '#e2e5ec',
        tickColor:   v('--tc-text-muted')  || '#6b7280'
    };
}

/**
 * Returns a standard Chart.js scales configuration that respects the current theme.
 * Merge into your chart options.scales.
 *
 * Usage:
 *   options: { scales: tcChartScales({ xTitle: 'Time', yTitle: 'Value' }) }
 */
function tcChartScales(opts) {
    opts = opts || {};
    const t = tcChartTheme();
    const scales = {};
    scales.x = {
        title: { display: !!opts.xTitle, text: opts.xTitle || '', color: t.textMuted },
        ticks: { color: t.tickColor },
        grid:  { color: opts.xGrid === false ? 'transparent' : t.gridColor }
    };
    scales.y = {
        title: { display: !!opts.yTitle, text: opts.yTitle || '', color: t.textMuted },
        ticks: { color: t.tickColor },
        grid:  { color: t.gridColor }
    };
    if (opts.yMin !== undefined) scales.y.min = opts.yMin;
    if (opts.yMax !== undefined) scales.y.max = opts.yMax;
    if (opts.xMin !== undefined) scales.x.min = opts.xMin;
    if (opts.xMax !== undefined) scales.x.max = opts.xMax;
    return scales;
}

/**
 * Refreshes all active Chart.js instances to pick up new theme colours
 * after a light/dark toggle. Call from the theme toggle handler.
 */
function tcRefreshAllCharts() {
    const t = tcChartTheme();
    if (typeof Chart === 'undefined') return;
    Object.values(Chart.instances || {}).forEach(function (chart) {
        if (!chart || !chart.options) return;
        var scales = chart.options.scales || {};
        ['x', 'y'].forEach(function (axis) {
            if (!scales[axis]) return;
            if (scales[axis].ticks) scales[axis].ticks.color = t.tickColor;
            if (scales[axis].title) scales[axis].title.color = t.textMuted;
            if (scales[axis].grid)  scales[axis].grid.color  = t.gridColor;
        });
        chart.update('none');
    });
}

// Watch for theme changes and refresh charts
(function () {
    var observer = new MutationObserver(function (mutations) {
        mutations.forEach(function (m) {
            if (m.attributeName === 'data-theme') {
                tcRefreshAllCharts();
            }
        });
    });
    observer.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
})();

// ---------- §6.2 + §6.3: KaTeX Auto-Render ----------

/**
 * Renders all elements with class "tc-formula" or "tc-formula-block" using KaTeX.
 * Call after HTMX swaps or dynamic content insertion.
 * Elements should contain LaTeX source text as their textContent.
 *
 * Inline:  <span class="tc-formula">E(X) = \sum x_i P(x_i)</span>
 * Block:   <div class="tc-formula-block">P(A|B) = \frac{P(B|A) P(A)}{P(B)}</div>
 */
function tcRenderFormulas(root) {
    if (typeof katex === 'undefined') return;
    root = root || document;
    root.querySelectorAll('.tc-formula:not([data-tc-rendered]), .tc-formula-block:not([data-tc-rendered])').forEach(function (el) {
        var latex = el.getAttribute('data-formula') || el.textContent;
        var displayMode = el.classList.contains('tc-formula-block');
        try {
            katex.render(latex, el, { displayMode: displayMode, throwOnError: false });
            el.setAttribute('data-tc-rendered', 'true');
        } catch (e) {
            // Leave original text on error
        }
    });
}

// Auto-render formulas when the page loads and after HTMX swaps
document.addEventListener('DOMContentLoaded', function () { tcRenderFormulas(); });
document.addEventListener('htmx:afterSwap', function (e) { tcRenderFormulas(e.detail.elt); });

// ---------- §6.4: Reusable Data Entry Grid (Alpine.js) ----------

/**
 * Alpine.js data factory for a reusable data-entry grid.
 * Provides text input parsing, validation, preset loading, and value count.
 *
 * Usage in Razor:
 *   <div x-data="tcDataGrid({ onCompute: myCallback })">
 *     <textarea class="form-control" x-model="rawData" ...></textarea>
 *     <button @@click="compute()">Compute</button>
 *     <button @@click="clear()">Clear</button>
 *     <span x-text="countLabel"></span>
 *     <span x-show="error" x-text="error" class="tc-data-grid-error"></span>
 *   </div>
 *
 * Options:
 *   onCompute(values):   called with the parsed number array when user clicks compute
 *   minValues:           minimum count of values required (default 2)
 *   placeholder:         textarea placeholder text
 */
document.addEventListener('alpine:init', function () {
    if (typeof Alpine === 'undefined') return;
    Alpine.data('tcDataGrid', function (opts) {
        opts = opts || {};
        return {
            rawData: '',
            error: '',
            values: [],
            minValues: opts.minValues || 2,

            get countLabel() {
                var n = this.values.length;
                return n === 0 ? 'No data' : n + ' value' + (n === 1 ? '' : 's');
            },

            parse() {
                this.error = '';
                var nums = this.rawData
                    .split(/[\s,;]+/)
                    .map(function (s) { return s.trim(); })
                    .filter(function (s) { return s.length > 0; })
                    .map(Number);
                var bad = nums.some(isNaN);
                if (bad) {
                    this.error = 'Non-numeric value detected. Use commas, spaces, or newlines to separate numbers.';
                    this.values = [];
                    return false;
                }
                if (nums.length < this.minValues) {
                    this.error = 'Please enter at least ' + this.minValues + ' values.';
                    this.values = [];
                    return false;
                }
                this.values = nums;
                return true;
            },

            compute() {
                if (!this.parse()) return;
                if (typeof opts.onCompute === 'function') {
                    opts.onCompute(this.values);
                }
            },

            clear() {
                this.rawData = '';
                this.error = '';
                this.values = [];
                if (typeof opts.onClear === 'function') {
                    opts.onClear();
                }
            },

            loadPreset(data) {
                if (Array.isArray(data)) {
                    this.rawData = data.join(', ');
                } else if (typeof data === 'string') {
                    this.rawData = data;
                }
                this.parse();
            }
        };
    });
});
