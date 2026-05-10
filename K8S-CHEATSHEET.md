# Kubernetes Cheat Sheet

## Common commands

- `kubectl apply -f <archivo>.yaml`
  - Create or update resources from a manifest file.
- `kubectl get pods`
  - List pods in the current namespace.
- `kubectl get svc`
  - List services in the current namespace.
- `kubectl describe pod <nombre-pod>`
  - Show detailed pod information and events.
- `kubectl logs -f <nombre-pod>`
  - Tail logs from a pod.
- `kubectl port-forward svc/<servicio> 8080:80`
  - Forward a local port to a service in the cluster.
- `kubectl delete -f <archivo>.yaml`
  - Delete resources defined in a manifest.
- `kubectl rollout status deployment/<nombre>`
  - Check deployment rollout progress.
- `kubectl get all`
  - List common resources in the namespace.
- `kubectl config use-context <contexto>`
  - Switch the active cluster/context.

## Quick notes

- `apply` is idempotent: it creates or updates resources as needed.
- `describe` is the first command to use when a pod fails.
- `logs` is the first tool to inspect runtime failures.
- `port-forward` lets you access cluster services locally without ingress.

## Recommended usage

1. Deploy resources:
   - `kubectl apply -f infra/k8s/`
2. Check pod status:
   - `kubectl get pods`
3. Inspect failures:
   - `kubectl describe pod <pod-name>`
4. Tail logs:
   - `kubectl logs -f <pod-name>`
