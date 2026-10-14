// SerpApi 키를 서버에만 두기 위한 프록시. Unity는 query만 보낸다.
const MAX_QUERY_LENGTH = 100;
const CACHE_SECONDS = 3600;

export default {
  async fetch(request, env, ctx) {
    if (request.method !== "GET") return text("method not allowed", 405);

    // IP별 호출 횟수를 제한한다. 캐시 적중 여부와 상관없이 먼저 확인한다.
    const ip = request.headers.get("CF-Connecting-IP") ?? "unknown";
    const { success } = await env.RATE_LIMITER.limit({ key: ip });
    if (!success) return text("too many requests", 429);

    const query = new URL(request.url).searchParams.get("query")?.trim();
    if (!query || query.length > MAX_QUERY_LENGTH) return text("bad request", 400);

    // 같은 검색어는 캐시로 응답해 SerpApi 호출 한도를 아낀다.
    const cacheKey = new Request(`https://cache.local/?q=${encodeURIComponent(query)}`);
    const cache = caches.default;
    const cached = await cache.match(cacheKey);
    if (cached) return cached;

    const upstream = new URL("https://serpapi.com/search.json");
    upstream.searchParams.set("engine", "naver");
    upstream.searchParams.set("where", "nexearch");
    upstream.searchParams.set("query", query);
    upstream.searchParams.set("api_key", env.SERPAPI_API_KEY);

    const res = await fetch(upstream);
    const body = await res.text();
    const response = new Response(body, {
      status: res.status,
      headers: {
        "Content-Type": "application/json",
        "Cache-Control": `public, max-age=${CACHE_SECONDS}`,
      },
    });

    if (res.ok) ctx.waitUntil(cache.put(cacheKey, response.clone()));
    return response;
  },
};

function text(message, status) {
  return new Response(message, { status });
}
