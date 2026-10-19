// charts.js
let chartInstances = {};

// Helper to create a sleek gradient
function getGradient(ctx, colorHex) {
    if (!colorHex.startsWith('#')) return colorHex; // Fallback if not hex
    const gradient = ctx.createLinearGradient(0, 0, 0, 350);
    gradient.addColorStop(0, colorHex);
    
    // Parse hex to rgba for the fading effect
    let r = parseInt(colorHex.substring(1,3), 16);
    let g = parseInt(colorHex.substring(3,5), 16);
    let b = parseInt(colorHex.substring(5,7), 16);
    gradient.addColorStop(1, `rgba(${r},${g},${b},0.1)`);
    
    return gradient;
}

function getChartColors() {
    return {
        textMain: getComputedStyle(document.body).getPropertyValue('--text-main').trim() || '#0f172a',
        textMuted: getComputedStyle(document.body).getPropertyValue('--text-muted').trim() || '#64748b',
        cardBg: getComputedStyle(document.body).getPropertyValue('--card-bg').trim() || '#ffffff',
        borderColor: getComputedStyle(document.body).getPropertyValue('--border-color').trim() || '#e2e8f0',
        fontFamily: "'Inter', sans-serif"
    };
}

function createDoughnutChart(canvasId, data, labels, colors) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext('2d');

    if (chartInstances[canvasId]) {
        chartInstances[canvasId].destroy();
    }

    const theme = getChartColors();
    const gradients = colors.map(c => getGradient(ctx, c));

    chartInstances[canvasId] = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: labels,
            datasets: [{
                data: data,
                backgroundColor: gradients,
                borderColor: theme.cardBg,
                borderWidth: 4,
                hoverOffset: 8,
                borderRadius: 5,
                spacing: 2
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            layout: {
                padding: { top: 10, bottom: 10, left: 10, right: 10 }
            },
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: {
                        color: theme.textMain,
                        padding: 20,
                        font: { family: theme.fontFamily, size: 13, weight: '500' },
                        usePointStyle: true,
                        pointStyle: 'circle'
                    }
                },
                tooltip: {
                    backgroundColor: theme.cardBg,
                    titleColor: theme.textMain,
                    bodyColor: theme.textMuted,
                    borderColor: theme.borderColor,
                    borderWidth: 1,
                    padding: 12,
                    boxPadding: 8,
                    usePointStyle: true,
                    titleFont: { family: theme.fontFamily, size: 14, weight: '600' },
                    bodyFont: { family: theme.fontFamily, size: 13 }
                }
            },
            cutout: '75%',
            animation: {
                animateScale: true,
                animateRotate: true,
                duration: 1000,
                easing: 'easeOutQuart'
            }
        }
    });
}

function createBarChart(canvasId, data, labels, colors, labelName) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext('2d');

    if (chartInstances[canvasId]) {
        chartInstances[canvasId].destroy();
    }

    const theme = getChartColors();
    const gradients = colors.map(c => getGradient(ctx, c));

    chartInstances[canvasId] = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: labels,
            datasets: [{
                label: labelName,
                data: data,
                backgroundColor: gradients,
                borderColor: colors,
                borderWidth: { top: 1, right: 1, left: 1, bottom: 0 },
                borderRadius: { topLeft: 6, topRight: 6, bottomLeft: 0, bottomRight: 0 },
                borderSkipped: false,
                barThickness: 32,
                maxBarThickness: 40
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            layout: {
                padding: { top: 20 }
            },
            plugins: {
                legend: { display: false },
                tooltip: {
                    backgroundColor: theme.cardBg,
                    titleColor: theme.textMain,
                    bodyColor: theme.textMuted,
                    borderColor: theme.borderColor,
                    borderWidth: 1,
                    padding: 12,
                    titleFont: { family: theme.fontFamily, size: 14, weight: '600' },
                    bodyFont: { family: theme.fontFamily, size: 13 },
                    displayColors: false
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    grid: {
                        color: theme.borderColor,
                        drawBorder: false,
                        borderDash: [4, 4]
                    },
                    ticks: {
                        color: theme.textMuted,
                        font: { family: theme.fontFamily, size: 12 },
                        padding: 10
                    },
                    border: { display: false }
                },
                x: {
                    grid: { display: false, drawBorder: false },
                    ticks: {
                        color: theme.textMuted,
                        font: { family: theme.fontFamily, size: 12, weight: '500' },
                        padding: 10
                    },
                    border: { display: false }
                }
            },
            animation: {
                duration: 1000,
                easing: 'easeOutQuart'
            }
        }
    });
}

function createLineChart(canvasId, data, labels, color, labelName) {
    const canvas = document.getElementById(canvasId);
    if (!canvas) return;
    const ctx = canvas.getContext('2d');

    if (chartInstances[canvasId]) {
        chartInstances[canvasId].destroy();
    }

    const theme = getChartColors();
    const gradient = getGradient(ctx, color);

    chartInstances[canvasId] = new Chart(ctx, {
        type: 'line',
        data: {
            labels: labels,
            datasets: [{
                label: labelName,
                data: data,
                borderColor: color,
                backgroundColor: gradient,
                fill: true,
                tension: 0.4,
                borderWidth: 3,
                pointBackgroundColor: theme.cardBg,
                pointBorderColor: color,
                pointBorderWidth: 2,
                pointRadius: 4,
                pointHoverRadius: 6
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            layout: {
                padding: { top: 20 }
            },
            plugins: {
                legend: { display: false },
                tooltip: {
                    backgroundColor: theme.cardBg,
                    titleColor: theme.textMain,
                    bodyColor: theme.textMuted,
                    borderColor: theme.borderColor,
                    borderWidth: 1,
                    padding: 12,
                    titleFont: { family: theme.fontFamily, size: 14, weight: '600' },
                    bodyFont: { family: theme.fontFamily, size: 13 },
                    displayColors: false
                }
            },
            scales: {
                y: {
                    beginAtZero: true,
                    grid: {
                        color: theme.borderColor,
                        drawBorder: false,
                        borderDash: [4, 4]
                    },
                    ticks: {
                        color: theme.textMuted,
                        font: { family: theme.fontFamily, size: 12 },
                        padding: 10
                    },
                    border: { display: false }
                },
                x: {
                    grid: { display: false, drawBorder: false },
                    ticks: {
                        color: theme.textMuted,
                        font: { family: theme.fontFamily, size: 12, weight: '500' },
                        padding: 10
                    },
                    border: { display: false }
                }
            },
            animation: {
                duration: 1000,
                easing: 'easeOutQuart'
            }
        }
    });
}

