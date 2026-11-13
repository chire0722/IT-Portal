/* Project comment: csp-events.js contains the application's unobtrusive event handlers. */
(function () {
    'use strict';

    function parseArguments(element) {
        var argumentsList = Object.keys(element.dataset)
            .filter(function (key) { return /^cspArg\d+$/.test(key); })
            .sort(function (left, right) {
                return Number(left.substring(6)) - Number(right.substring(6));
            })
            .map(function (key) {
                var index = key.substring(6);
                var value = element.dataset[key];
                if (element.dataset['cspEncodedArg' + index] === 'true') {
                    value = decodeURIComponent(value);
                }
                if (value === 'true') return true;
                if (value === 'false') return false;
                if (/^-?\d+(\.\d+)?$/.test(value)) return Number(value);
                return value;
            });
        argumentsList.push(element);
        return argumentsList;
    }

    function invoke(element, event, action) {
        if (!action) return;

        var result;
        switch (action) {
            case 'remove-parent':
                if (element.parentElement) element.parentElement.remove();
                break;
            case 'click-target':
                var target = document.getElementById(element.dataset.cspTarget);
                if (target) target.click();
                break;
            case 'reload':
                window.location.reload();
                break;
            case 'stop-propagation':
                event.stopPropagation();
                break;
            case 'submit-form':
                if (element.form) element.form.requestSubmit();
                break;
            case 'toggle-row-checkbox':
                var checkbox = element.querySelector('input[type="checkbox"]');
                if (checkbox && event.target !== checkbox) checkbox.checked = !checkbox.checked;
                break;
            case 'select-all':
                document.querySelectorAll(element.dataset.cspSelector || 'input[type="checkbox"]').forEach(function (checkbox) {
                    checkbox.checked = true;
                });
                break;
            case 'uppercase':
                element.value = element.value.toUpperCase();
                break;
            case 'navigate-template':
                window.location.href = element.dataset.cspUrlTemplate.replace('{value}', encodeURIComponent(element.value));
                break;
            case 'confirm':
                result = window.confirm(element.dataset.cspConfirm || 'Are you sure?');
                break;
            case 'add-key-on-enter':
                if (event.key === 'Enter') {
                    event.preventDefault();
                    var keyAction = element.dataset.cspAction;
                    if (typeof window[keyAction] === 'function') {
                        result = window[keyAction].apply(element, parseArguments(element));
                    }
                }
                break;
            default:
                if (typeof window[action] === 'function') {
                    result = window[action].apply(element, parseArguments(element));
                }
                break;
        }

        if (result === false) event.preventDefault();
    }

    function closestWithAttribute(target, attribute) {
        return target && target.closest ? target.closest('[' + attribute + ']') : null;
    }

    document.addEventListener('click', function (event) {
        var element = closestWithAttribute(event.target, 'data-csp-click');
        if (element) invoke(element, event, element.dataset.cspClick);
    });

    document.addEventListener('change', function (event) {
        var element = closestWithAttribute(event.target, 'data-csp-change');
        if (element) invoke(element, event, element.dataset.cspChange);
    });

    document.addEventListener('input', function (event) {
        var element = closestWithAttribute(event.target, 'data-csp-input');
        if (element) invoke(element, event, element.dataset.cspInput);
    });

    document.addEventListener('keypress', function (event) {
        var element = closestWithAttribute(event.target, 'data-csp-keypress');
        if (element) invoke(element, event, element.dataset.cspKeypress);
    });

    document.addEventListener('submit', function (event) {
        var element = closestWithAttribute(event.target, 'data-csp-confirm');
        if (element && !window.confirm(element.dataset.cspConfirm || 'Are you sure?')) {
            event.preventDefault();
        }
    });
}());

