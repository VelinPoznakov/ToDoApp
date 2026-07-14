// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Back controls: return the user to their previous location instead of a fixed page.
// Any element marked with the data-back attribute triggers browser history back on click,
// so this covers every "go back" button across the app (current and future).
document.addEventListener('click', function (e) {
    var back = e.target.closest('[data-back]');
    if (!back) {
        return;
    }

    e.preventDefault();

    if (window.history.length > 1) {
        window.history.back();
    } else {
        // Opened directly with no history to return to: fall back to home.
        window.location.href = '/';
    }
});
