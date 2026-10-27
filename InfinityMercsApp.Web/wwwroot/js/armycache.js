// Keeps the army data's payloads in the browser between visits, each under its hash, so a later visit loads armies from
// here rather than the API. Payloads no longer in the current snapshot are pruned. Any failure (private browsing, no
// storage) just means nothing is kept.
const cacheName = "mercs-army-data-v1";
const keyFor = hash => `/army-cache/${hash}`;

export async function get(hash) {
    try {
        const cache = await caches.open(cacheName);
        const response = await cache.match(keyFor(hash));
        return response ? await response.text() : null;
    } catch {
        return null;
    }
}

export async function put(hash, text) {
    try {
        const cache = await caches.open(cacheName);
        await cache.put(keyFor(hash), new Response(text, { headers: { "content-type": "application/json" } }));
    } catch {
        // Not kept; it is downloaded again next time.
    }
}

export async function prune(hashes) {
    try {
        const keep = new Set(hashes.map(keyFor));
        const cache = await caches.open(cacheName);
        for (const request of await cache.keys()) {
            if (!keep.has(new URL(request.url).pathname)) {
                await cache.delete(request);
            }
        }
    } catch {
        // Nothing to prune.
    }
}
