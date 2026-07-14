// Generic delete confirmation modal (paired with _DeleteTodoWarningPartial.cshtml).
//
// A delete trigger carries:
//   data-delete-trigger                 marks the element as a trigger
//   data-delete-item="To Do" | "Group"  what is being deleted (used in the wording)
//   data-delete-url="/..."              the POST url that actually performs the delete
//
// Flow:
//   - trigger click  -> blur the page + show the warning, wording set from the item
//   - confirm click  -> POST to the trigger's url (with the page antiforgery token)
//   - cancel / backdrop / Escape -> hide it again
(function () {
    var modal = document.querySelector('[data-delete-modal]');
    if (!modal) {
        return;
    }

    var titleEl = modal.querySelector('.delete-warning__title');
    var textEl = modal.querySelector('.delete-warning__text');
    var pendingUrl = null;

    function open(trigger) {
        var item = trigger.getAttribute('data-delete-item');
        pendingUrl = trigger.getAttribute('data-delete-url');

        if (item) {
            if (titleEl) {
                titleEl.textContent = 'Delete this ' + item + '?';
            }
            if (textEl) {
                textEl.textContent = 'This action cannot be undone. The ' + item + ' will be permanently removed.';
            }
        }

        modal.classList.add('is-open');
        document.body.classList.add('is-modal-open');
    }

    function close() {
        modal.classList.remove('is-open');
        document.body.classList.remove('is-modal-open');
    }

    function submitDelete() {
        if (!pendingUrl) {
            return;
        }

        // Build a POST form on the fly so the delete stays a POST (only-delete-post).
        var form = document.createElement('form');
        form.method = 'post';
        form.action = pendingUrl;

        var token = document.querySelector('input[name="__RequestVerificationToken"]');
        if (token) {
            var hidden = document.createElement('input');
            hidden.type = 'hidden';
            hidden.name = '__RequestVerificationToken';
            hidden.value = token.value;
            form.appendChild(hidden);
        }

        document.body.appendChild(form);
        form.submit();
    }

    document.addEventListener('click', function (e) {
        var trigger = e.target.closest('[data-delete-trigger]');
        if (trigger) {
            e.preventDefault();
            open(trigger);
            return;
        }

        if (e.target.closest('[data-delete-confirm]')) {
            e.preventDefault();
            submitDelete();
            return;
        }

        // Cancel button and the backdrop both carry [data-delete-cancel].
        if (e.target.closest('[data-delete-cancel]')) {
            e.preventDefault();
            close();
        }
    });

    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && modal.classList.contains('is-open')) {
            close();
        }
    });
})();
