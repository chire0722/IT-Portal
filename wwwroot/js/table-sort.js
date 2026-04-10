// Table-sort: click column headers to sort asc/desc
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('th[data-sort]').forEach(function (th) {
        th.style.cursor = 'pointer';
        th.style.userSelect = 'none';
        th.title = 'Click to sort';
        th.addEventListener('click', function () {
            var table = th.closest('table');
            var tbody = table.querySelector('tbody');
            var rows = Array.from(tbody.querySelectorAll('tr'));
            var colIdx = Array.from(th.parentElement.children).indexOf(th);
            var type = th.getAttribute('data-sort'); // text, date, number
            var asc = th.getAttribute('data-dir') !== 'asc';
            th.setAttribute('data-dir', asc ? 'asc' : 'desc');

            // Reset all sort indicators
            th.parentElement.querySelectorAll('th[data-sort]').forEach(function (h) {
                h.classList.remove('sort-asc', 'sort-desc');
            });
            th.classList.add(asc ? 'sort-asc' : 'sort-desc');

            rows.sort(function (a, b) {
                var aText = a.children[colIdx]?.textContent.trim() || '';
                var bText = b.children[colIdx]?.textContent.trim() || '';

                if (type === 'date') {
                    // Parse dd/MM/yyyy
                    var pa = aText.split('/'); var pb = bText.split('/');
                    var da = pa.length === 3 ? new Date(pa[2], pa[1] - 1, pa[0]) : new Date(0);
                    var db = pb.length === 3 ? new Date(pb[2], pb[1] - 1, pb[0]) : new Date(0);
                    return asc ? da - db : db - da;
                }
                if (type === 'number') {
                    return asc ? (parseFloat(aText) || 0) - (parseFloat(bText) || 0) : (parseFloat(bText) || 0) - (parseFloat(aText) || 0);
                }
                // text
                return asc ? aText.localeCompare(bText) : bText.localeCompare(aText);
            });

            rows.forEach(function (row) { tbody.appendChild(row); });
        });
    });
});
