// The telemetry setting kept between visits (the install id and whether telemetry is on), and a word to the site when
// the tab is hidden, so waiting telemetry goes before the tab may close.
const settingsKey = "mercs.telemetry-settings";

export function loadSettings() {
    try {
        return localStorage.getItem(settingsKey);
    } catch {
        return null;
    }
}

export function saveSettings(json) {
    try {
        localStorage.setItem(settingsKey, json);
    } catch {
        // Not kept; the next visit is a new install.
    }
}

export function watchHidden(component) {
    document.addEventListener("visibilitychange", () => {
        if (document.visibilityState === "hidden") {
            component.invokeMethodAsync("PageHidden");
        }
    });
}
