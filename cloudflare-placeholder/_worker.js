const UNITY_BASE = "https://developer.unity3busercontent.com/webglproxy/orgs/4f3dc56a-4ea6-40f6-0000-12405d9a4898/projects/0620c155-d18e-466b-872f-f952f6577b33/buildtargets/default-webgl/builds/13/ef77c9b47ac0b945d9fcd49499098d6f/";

export default {
  async fetch(request) {
    const incoming = new URL(request.url);
    let path = incoming.pathname.replace(/^\/+/, "");
    if (!path) path = "index.html";

    const upstreamUrl = new URL(path + incoming.search, UNITY_BASE);
    const upstreamRequest = new Request(upstreamUrl.toString(), {
      method: request.method,
      headers: request.headers,
      redirect: "follow"
    });

    const upstream = await fetch(upstreamRequest);
    const headers = new Headers(upstream.headers);

    // Keep browser requests entirely on the Cloudflare hostname.
    headers.delete("content-security-policy");
    headers.delete("content-security-policy-report-only");
    headers.delete("x-frame-options");
    headers.set("Access-Control-Allow-Origin", "*");

    // Unity WebGL files need correct MIME types when served through a proxy.
    if (path.endsWith(".wasm")) headers.set("Content-Type", "application/wasm");
    else if (path.endsWith(".js")) headers.set("Content-Type", "application/javascript");
    else if (path.endsWith(".data")) headers.set("Content-Type", "application/octet-stream");
    else if (path.endsWith(".json")) headers.set("Content-Type", "application/json");
    else if (path.endsWith(".html")) headers.set("Content-Type", "text/html; charset=utf-8");

    return new Response(upstream.body, {
      status: upstream.status,
      statusText: upstream.statusText,
      headers
    });
  }
};
