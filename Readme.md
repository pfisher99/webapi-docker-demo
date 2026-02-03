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
```

## Configuration

### Environment variables
- `MESSAGE` (optional): text returned by `GET /`
- `HOSTNAME`: injected automatically by Kubernetes (pod name/hostname)

## Run locally (no Docker)

```bash
dotnet restore
dotnet run
```

Then hit your app (port will be shown in console):

```bash
curl http://localhost:<port>/
curl http://localhost:<port>/healthz
curl http://localhost:<port>/readyz
curl http://localhost:<port>/metrics | head
curl "http://localhost:<port>/work?ms=500"
```

## Build and run with Docker

From the repo root (where the Dockerfile lives):

```bash
docker build -t webapi-demo:0.1 .
docker run --rm -p 8080:8080 -e MESSAGE="hello from docker" webapi-demo:0.1
```

Test:

```bash
curl http://localhost:8080/
curl http://localhost:8080/metrics | head
```

## Deploy to kind (local Kubernetes)

### Prereqs
- Docker running
- kind cluster created (example name: `sre`)
- `kubectl` configured for that cluster
- (Optional but recommended) metrics-server installed if you want HPA

### 1) Load the image into kind
This avoids needing a container registry:

```bash
kind load docker-image webapi-demo:0.1 --name sre
```

### 2) Create namespace

```bash
kubectl create namespace webdemo
```

### 3) Deploy (example manifest)
Create `webdemo.yaml`:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: webapi-demo
  namespace: webdemo
spec:
  replicas: 1
  selector:
    matchLabels:
      app: webapi-demo
  template:
    metadata:
      labels:
        app: webapi-demo
    spec:
      containers:
        - name: webapi-demo
          image: webapi-demo:0.1
          imagePullPolicy: IfNotPresent
          ports:
            - containerPort: 8080
          env:
            - name: MESSAGE
              value: "hello from kind"
          resources:
            requests:
              cpu: 50m
              memory: 64Mi
            limits:
              cpu: 300m
              memory: 256Mi
          readinessProbe:
            httpGet:
              path: /readyz
              port: 8080
            initialDelaySeconds: 3
            periodSeconds: 5
          livenessProbe:
            httpGet:
              path: /healthz
              port: 8080
            initialDelaySeconds: 10
            periodSeconds: 10
---
apiVersion: v1
kind: Service
metadata:
  name: webapi-demo
  namespace: webdemo
spec:
  selector:
    app: webapi-demo
  ports:
    - name: http
      port: 80
      targetPort: 8080
```

Apply it:

```bash
kubectl apply -f webdemo.yaml
kubectl -n webdemo rollout status deploy/webapi-demo
kubectl -n webdemo get pods -o wide
kubectl -n webdemo get svc
```

### 4) Access the service (port-forward)

```bash
kubectl -n webdemo port-forward svc/webapi-demo 8085:80
```

Then:

```bash
curl http://localhost:8085/
curl http://localhost:8085/healthz
curl http://localhost:8085/readyz
curl http://localhost:8085/metrics | head
```

## Scaling (manual)

```bash
kubectl -n webdemo scale deploy/webapi-demo --replicas=5
kubectl -n webdemo get pods -o wide
```

Hit `/` repeatedly and watch the `pod` field change to confirm load balancing.

## Autoscaling (HPA) demo (requires metrics-server)

Create an HPA:

```bash
kubectl -n webdemo autoscale deploy/webapi-demo --cpu-percent=50 --min=1 --max=10
kubectl -n webdemo get hpa -w
```

Generate load:

```bash
for i in {1..200}; do curl -s "http://localhost:8085/work?ms=200" >/dev/null; done
```

Watch replicas increase:

```bash
kubectl -n webdemo get pods
```

## Useful SRE commands

```bash
# See rollout and history
kubectl -n webdemo rollout status deploy/webapi-demo
kubectl -n webdemo rollout history deploy/webapi-demo

# Logs
kubectl -n webdemo logs deploy/webapi-demo --tail=200
kubectl -n webdemo logs deploy/webapi-demo -f

# Describe wiring
kubectl -n webdemo describe deploy webapi-demo
kubectl -n webdemo describe svc webapi-demo
kubectl -n webdemo get endpoints webapi-demo -o wide

# Metrics
kubectl top nodes
kubectl top pods -n webdemo
```