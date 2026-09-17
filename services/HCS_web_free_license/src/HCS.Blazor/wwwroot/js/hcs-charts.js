(function () {
    window.hcsCharts = window.hcsCharts || {};
    window.chartInstances = window.chartInstances || {};

    function convertToRgba(color, opacity) {
        if (!color) return "rgba(52, 152, 219, " + opacity + ")";
        if (color.indexOf("rgba") === 0) return color.replace(/[\d.]+\)$/g, opacity + ")");
        if (color.indexOf("rgb") === 0) return color.replace("rgb", "rgba").replace(")", ", " + opacity + ")");
        var hex = color.replace("#", "");
        if (hex.length === 3) hex = hex[0] + hex[0] + hex[1] + hex[1] + hex[2] + hex[2];
        var r = parseInt(hex.substring(0, 2), 16);
        var g = parseInt(hex.substring(2, 4), 16);
        var b = parseInt(hex.substring(4, 6), 16);
        return "rgba(" + r + ", " + g + ", " + b + ", " + opacity + ")";
    }

    function loadChartJs() {
        return new Promise(function (resolve, reject) {
            if (typeof Chart !== "undefined") {
                resolve();
                return;
            }
            var script = document.createElement("script");
            script.src = "https://cdn.jsdelivr.net/npm/chart.js@4.4.0/dist/chart.umd.min.js";
            script.onload = function () { resolve(); };
            script.onerror = function () { reject(new Error("Failed to load Chart.js")); };
            document.head.appendChild(script);
        });
    }

    function destroy(canvasId) {
        if (window.chartInstances[canvasId]) {
            window.chartInstances[canvasId].destroy();
            delete window.chartInstances[canvasId];
        }
    }

    window.hcsCharts.destroy = destroy;

    function formatChartNumber(value) {
        var n = Number(value);
        if (!isFinite(n)) return "0";
        return Math.round(n).toLocaleString("vi-VN");
    }

    function extraFlag(extras, name) {
        if (!extras) return undefined;
        if (extras[name] !== undefined) return extras[name];
        var pascal = name.charAt(0).toUpperCase() + name.slice(1);
        return extras[pascal];
    }

    var hcsPieLabelsPlugin = {
        id: "hcsPieLabels",
        afterDatasetsDraw: function (chart) {
            var opts = (chart.options.plugins || {}).hcsPieLabels || {};
            if (!opts.display) return;
            var dataset = chart.data.datasets[0];
            if (!dataset) return;
            var meta = chart.getDatasetMeta(0);
            var values = dataset.data || [];
            var total = values.reduce(function (sum, item) { return sum + Number(item || 0); }, 0);
            if (total <= 0) return;
            var ctx = chart.ctx;
            meta.data.forEach(function (arc, index) {
                var value = Number(values[index] || 0);
                if (value <= 0) return;
                var percent = value * 100 / total;
                if (percent < 4) return;
                var point = typeof arc.getCenterPoint === "function" ? arc.getCenterPoint() : arc.tooltipPosition();
                ctx.save();
                ctx.fillStyle = "#ffffff";
                ctx.textAlign = "center";
                ctx.textBaseline = "middle";
                ctx.font = "700 12px Inter, system-ui, sans-serif";
                ctx.fillText(percent.toLocaleString("vi-VN", { maximumFractionDigits: 1 }) + "%", point.x, point.y - 7);
                ctx.font = "600 10px Inter, system-ui, sans-serif";
                ctx.fillStyle = "rgba(255,255,255,.92)";
                ctx.fillText(formatChartNumber(value), point.x, point.y + 8);
                ctx.restore();
            });
        }
    };

    var hcsCenterTextPlugin = {
        id: "hcsCenterText",
        afterDraw: function (chart) {
            var opts = (chart.options.plugins || {}).hcsCenter || {};
            if (!opts.label) return;
            var meta = chart.getDatasetMeta(0);
            if (!meta.data.length) return;
            var center = meta.data[0];
            var ctx = chart.ctx;
            ctx.save();
            ctx.textAlign = "center";
            ctx.textBaseline = "middle";
            ctx.fillStyle = "#12263a";
            ctx.font = "800 22px Inter, system-ui, sans-serif";
            ctx.fillText(String(opts.label), center.x, center.y - (opts.subLabel ? 8 : 0));
            if (opts.subLabel) {
                ctx.fillStyle = "#5b7484";
                ctx.font = "600 11px Inter, system-ui, sans-serif";
                ctx.fillText(String(opts.subLabel), center.x, center.y + 12);
            }
            ctx.restore();
        }
    };

    function pieOptions(isDoughnut, title, showLegend, extras) {
        extras = extras || {};
        var showDataLabels = !!extraFlag(extras, "showDataLabels");
        var centerLabel = extraFlag(extras, "centerLabel") || "";
        var centerSubLabel = extraFlag(extras, "centerSubLabel") || "";
        return {
            responsive: true,
            maintainAspectRatio: false,
            cutout: isDoughnut ? "58%" : 0,
            layout: { padding: showDataLabels ? 8 : 0 },
            plugins: {
                legend: {
                    display: !!showLegend,
                    position: "bottom",
                    labels: {
                        boxWidth: 10,
                        boxHeight: 10,
                        padding: 10,
                        font: { size: 12, weight: "600" },
                        generateLabels: function (chart) {
                            var dataset = chart.data.datasets[0] || {};
                            var values = dataset.data || [];
                            var total = values.reduce(function (sum, item) { return sum + Number(item || 0); }, 0);
                            var colors = dataset.backgroundColor || [];
                            return (chart.data.labels || []).map(function (label, index) {
                                var value = Number(values[index] || 0);
                                var percent = total > 0 ? (value * 100 / total) : 0;
                                return {
                                    text: label + " · " + formatChartNumber(value) + " (" + percent.toLocaleString("vi-VN", { maximumFractionDigits: 1 }) + "%)",
                                    fillStyle: colors[index],
                                    strokeStyle: colors[index],
                                    hidden: false,
                                    index: index
                                };
                            });
                        }
                    }
                },
                title: { display: !!title, text: title || "" },
                tooltip: {
                    callbacks: {
                        label: function (context) {
                            var value = Number(context.raw || 0);
                            var total = (context.dataset.data || []).reduce(function (sum, item) { return sum + Number(item || 0); }, 0);
                            var percent = total > 0 ? (value * 100 / total) : 0;
                            return " " + formatChartNumber(value) + " hồ sơ (" + percent.toLocaleString("vi-VN", { maximumFractionDigits: 1 }) + "%)";
                        }
                    }
                },
                hcsPieLabels: { display: showDataLabels },
                hcsCenter: { label: centerLabel, subLabel: centerSubLabel }
            }
        };
    }

    window.hcsCharts.createPie = async function (canvasId, labels, data, colors, isDoughnut, title, showLegend, extras) {
        await loadChartJs();
        var canvas = document.getElementById(canvasId);
        if (!canvas) return;
        destroy(canvasId);
        var background = (colors || []).map(function (c) { return convertToRgba(c, 1); });
        window.chartInstances[canvasId] = new Chart(canvas.getContext("2d"), {
            type: isDoughnut ? "doughnut" : "pie",
            data: {
                labels: labels,
                datasets: [{ data: data, backgroundColor: background, borderColor: "#fff", borderWidth: 2, hoverOffset: 4 }]
            },
            options: pieOptions(isDoughnut, title, showLegend, extras),
            plugins: [hcsPieLabelsPlugin, hcsCenterTextPlugin]
        });
    };

    window.hcsCharts.updatePie = function (canvasId, labels, data, colors, extras) {
        var chart = window.chartInstances[canvasId];
        if (!chart) return;
        chart.data.labels = labels;
        chart.data.datasets[0].data = data;
        chart.data.datasets[0].backgroundColor = (colors || []).map(function (c) { return convertToRgba(c, 1); });
        if (extras) {
            var isDoughnut = chart.config.type === "doughnut";
            var showLegend = !!(chart.options.plugins && chart.options.plugins.legend && chart.options.plugins.legend.display);
            var title = chart.options.plugins && chart.options.plugins.title ? chart.options.plugins.title.text : "";
            chart.options = pieOptions(isDoughnut, title, showLegend, extras);
        }
        chart.update();
    };

    window.hcsCharts.createBar = async function (canvasId, labels, data, colors, title, xAxisLabel, yAxisLabel, horizontal, maxValue) {
        await loadChartJs();
        var canvas = document.getElementById(canvasId);
        if (!canvas) return;
        destroy(canvasId);
        var background = (colors || []).map(function (c) { return convertToRgba(c, 1); });
        window.chartInstances[canvasId] = new Chart(canvas.getContext("2d"), {
            type: "bar",
            data: {
                labels: labels,
                datasets: [{
                    label: yAxisLabel || "Count",
                    data: data,
                    backgroundColor: background,
                    borderColor: background,
                    borderWidth: 2
                }]
            },
            options: {
                indexAxis: horizontal ? "y" : "x",
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    title: { display: !!title, text: title || "" }
                },
                scales: horizontal ? {
                    x: { beginAtZero: true, max: maxValue || undefined },
                    y: { ticks: { autoSkip: false } }
                } : {
                    y: { beginAtZero: true, title: { display: !!yAxisLabel, text: yAxisLabel || "" } },
                    x: { title: { display: !!xAxisLabel, text: xAxisLabel || "" } }
                }
            }
        });
    };

    window.hcsCharts.updateBar = function (canvasId, labels, data, colors) {
        var chart = window.chartInstances[canvasId];
        if (!chart) return;
        chart.data.labels = labels;
        chart.data.datasets[0].data = data;
        chart.data.datasets[0].backgroundColor = (colors || []).map(function (c) { return convertToRgba(c, 1); });
        chart.update();
    };

    window.hcsCharts.createLine = async function (canvasId, labels, data, title, yAxisLabel, maxValue) {
        await loadChartJs();
        var canvas = document.getElementById(canvasId);
        if (!canvas) return;
        destroy(canvasId);
        window.chartInstances[canvasId] = new Chart(canvas.getContext("2d"), {
            type: "line",
            data: {
                labels: labels,
                datasets: [{
                    label: yAxisLabel || "Average",
                    data: data,
                    borderColor: "#2563eb",
                    backgroundColor: convertToRgba("#2563eb", 0.14),
                    pointBackgroundColor: "#2563eb",
                    pointRadius: 4,
                    tension: 0.25,
                    fill: true
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    title: { display: !!title, text: title || "" }
                },
                scales: {
                    y: { beginAtZero: true, max: maxValue || undefined, title: { display: !!yAxisLabel, text: yAxisLabel || "" } },
                    x: { ticks: { autoSkip: false } }
                }
            }
        });
    };

    window.hcsCharts.updateLine = function (canvasId, labels, data) {
        var chart = window.chartInstances[canvasId];
        if (!chart) return;
        chart.data.labels = labels;
        chart.data.datasets[0].data = data;
        chart.update();
    };
})();
