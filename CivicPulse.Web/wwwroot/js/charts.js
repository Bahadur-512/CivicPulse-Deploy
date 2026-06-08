window.CivicCharts = {
    charts: {},

    renderDonut: function (canvasId, labels, data, colors, title) {
        this.destroyChart(canvasId);
        var ctx = document.getElementById(canvasId);
        if (!ctx) return;
        this.charts[canvasId] = new Chart(ctx, {
            type: 'doughnut',
            data: { labels: labels, datasets: [{ data: data, backgroundColor: colors, borderWidth: 0 }] },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: {
                    legend: { position: 'bottom', labels: { padding: 16, font: { size: 12 } } },
                    title: { display: !!title, text: title, font: { size: 14, weight: 'bold' }, padding: { bottom: 12 } }
                }
            }
        });
    },

    renderBar: function (canvasId, labels, datasets, title) {
        this.destroyChart(canvasId);
        var ctx = document.getElementById(canvasId);
        if (!ctx) return;
        this.charts[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: { labels: labels, datasets: datasets },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: {
                    legend: { position: 'bottom', labels: { padding: 16, font: { size: 12 } } },
                    title: { display: !!title, text: title, font: { size: 14, weight: 'bold' }, padding: { bottom: 12 } }
                },
                scales: { y: { beginAtZero: true, grid: { color: 'rgba(0,0,0,0.05)' }, ticks: { stepSize: 1, precision: 0 } }, x: { grid: { display: false } } }
            }
        });
    },

    renderLine: function (canvasId, labels, datasets, title) {
        this.destroyChart(canvasId);
        var ctx = document.getElementById(canvasId);
        if (!ctx) return;
        this.charts[canvasId] = new Chart(ctx, {
            type: 'line',
            data: { labels: labels, datasets: datasets },
            options: {
                responsive: true, maintainAspectRatio: false,
                plugins: {
                    legend: { position: 'bottom', labels: { padding: 16, font: { size: 12 } } },
                    title: { display: !!title, text: title, font: { size: 14, weight: 'bold' }, padding: { bottom: 12 } }
                },
                scales: { y: { beginAtZero: true, grid: { color: 'rgba(0,0,0,0.05)' }, ticks: { stepSize: 1, precision: 0 } }, x: { grid: { display: false } } },
                elements: {
                    point: { radius: 4, hoverRadius: 6, backgroundColor: '#1B3A6B', borderColor: '#1B3A6B' },
                    line: { tension: 0.4 }
                }
            }
        });
    },

    renderHorizontalBar: function (canvasId, labels, data, colors, title) {
        this.destroyChart(canvasId);
        var ctx = document.getElementById(canvasId);
        if (!ctx) return;
        this.charts[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: { labels: labels, datasets: [{ data: data, backgroundColor: colors, borderWidth: 0 }] },
            options: {
                indexAxis: 'y', responsive: true, maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    title: { display: !!title, text: title, font: { size: 14, weight: 'bold' }, padding: { bottom: 12 } }
                },
                scales: { x: { beginAtZero: true, grid: { color: 'rgba(0,0,0,0.05)' }, ticks: { stepSize: 1, precision: 0 } }, y: { grid: { display: false } } }
            }
        });
    },

    destroyChart: function (canvasId) {
        if (this.charts[canvasId]) {
            this.charts[canvasId].destroy();
            delete this.charts[canvasId];
        }
    },

    updateChart: function (canvasId, labels, data) {
        var chart = this.charts[canvasId];
        if (!chart) return;
        chart.data.labels = labels;
        chart.data.datasets.forEach(function (ds, i) { ds.data = data[i] || data; });
        chart.update();
    }
};
