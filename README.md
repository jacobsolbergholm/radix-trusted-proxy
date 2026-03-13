# Radix Trusted Proxy

A sample application that demonstrates **IP-based access control** for services running on [Radix](https://www.radix.equinor.com/) (or any Kubernetes platform). This example is implemented in C# / ASP.NET Core, but the same pattern applies to any language or framework.

## Concept

The application exposes two endpoints:

| Endpoint           | Description                                                                  |
| ------------------ | ---------------------------------------------------------------------------- |
| `GET /unfiltered`  | Returns a response with the caller's remote IP. **No IP filtering applied.** |
| `GET /filtered`    | Returns a response only if the caller's IP is on the allow list. Returns `403 Forbidden` otherwise. |

Incoming requests are checked against a configurable **IP allow list**. If the caller's IP is not on the list, the request is rejected.

### Extracting the Real Client IP

When running behind a load balancer or reverse proxy (as in Radix / Kubernetes), the direct connection IP is the proxy — not the original client. The application reads the **`X-Forwarded-For`** header to determine the real client IP. Only proxies in a trusted IP range are accepted.

In the Radix AKS cluster, the internal network uses the **`10.0.0.0/8`** CIDR range. This is a static range defined by the cluster infrastructure and should be set as the trusted proxy range in your application — it is not something end-users need to change.

This is a standard mechanism supported by virtually all web frameworks (Express, Flask, Spring Boot, ASP.NET Core, etc.).

## Configuration

The allow list is configured via the **`IP_ALLOW_LIST`** environment variable. Both IPv4 and IPv6 are supported. IPs are separated by semicolons:

```
IP_ALLOW_LIST="127.0.0.1;10.20.30.40;192.168.1.100"
```

In the Radix config (`radixconfig.yaml`) this is set as a component variable:

```yaml
variables:
  IP_ALLOW_LIST: "127.0.0.1"
```

## Inter-Component Communication (Same Namespace)

When a component within the **same Radix application / Kubernetes namespace** needs to call a component that has IP filtering enabled, two things are required:

1. **`X-Forwarded-For` header** — The calling component must include an `X-Forwarded-For` header containing its own IP address. Traffic between components in the same namespace does **not** pass through the external ingress/load balancer, so the header is not added automatically.

2. **IP allow listing** — The calling component's IP (or pod IP range) must be added to the `IP_ALLOW_LIST` environment variable. Only IPs in the allow list are permitted through the `/filtered` endpoint.

> **Note:** This only applies when the **target** component uses IP filtering middleware. If the target component does not perform IP-based access control, no `X-Forwarded-For` header or allow listing is needed.

> **Example:** If component **A** (pod IP `10.63.2.15`) needs to call a `/filtered` endpoint on component **B** (which has IP filtering enabled), you must:
>
> - Add `10.63.2.15` (or the relevant IP range) to `IP_ALLOW_LIST` on component **B**.
> - Have component **A** set the `X-Forwarded-For: 10.63.2.15` header on its requests.

## Running Locally

```bash
# Set the allow list
export IP_ALLOW_LIST="127.0.0.1"

# Run the application
dotnet run
```

Test the endpoints:

```bash
curl http://localhost:8080/unfiltered
curl http://localhost:8080/filtered
```

## Docker

```bash
docker build -t radix-trusted-proxy .
docker run -p 8080:8080 -e IP_ALLOW_LIST="127.0.0.1" radix-trusted-proxy:latest
```
