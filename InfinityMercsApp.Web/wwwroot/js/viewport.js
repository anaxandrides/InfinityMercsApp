// Tells a component the window's width now and whenever it changes, for the view models that lay themselves out by
// width (AdaptiveLayoutModes). Resizes are settled for a moment before they are reported.
const watchers = new Map();

export function watch(id, component) {
    let timer = 0;
    const report = () => {
        clearTimeout(timer);
        timer = setTimeout(() => component.invokeMethodAsync("WidthChanged", window.innerWidth), 100);
    };
    window.addEventListener("resize", report);
    watchers.set(id, report);
    return window.innerWidth;
}

export function unwatch(id) {
    const report = watchers.get(id);
    if (report) {
        window.removeEventListener("resize", report);
        watchers.delete(id);
    }
}
