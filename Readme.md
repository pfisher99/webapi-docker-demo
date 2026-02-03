# webapi-docker-demo (.NET 10 Minimal API + Kubernetes)

A tiny .NET Minimal API designed for Kubernetes/SRE practice:
- Kubernetes-style health and readiness probes
- Prometheus `/metrics` endpoint (via `prometheus-net`)
- A `/work` endpoint to generate CPU load (useful for HPA demos)
- Returns the running pod hostname so you can see load-balancing across replicas

## Endpoints

| Endpoint | Purpose |
|---|---|
| `GET /` | Returns JSON with a message, pod hostname, and UTC timestamp |
| `GET /healthz` | Liveness probe endpoint (`200 ok`) |
| `GET /readyz` | Readiness probe endpoint (`200 ready`) |
| `GET /metrics` | Prometheus scrape endpoint |
| `GET /work?ms=250` | Busy-loop CPU work for `ms` milliseconds (1–5000) |

Example `/` response:
```json
{
  "message": "Hello from .NET 10 in Kubernetes!",
  "pod": "webapi-demo-b7cfcc96d-xxxxx",
  "utc": "2026-02-03T22:30:00.000Z"
}
