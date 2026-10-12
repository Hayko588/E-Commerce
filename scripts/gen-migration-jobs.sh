#!/usr/bin/env bash
# One-time scaffolding for the four migration Jobs in k8s/base/migrations/.
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"
mkdir -p k8s/base/migrations

job() {   # service (orders), image (ordering-migrator), secret prefix (ORDERS)
  cat > "k8s/base/migrations/$1.yaml" <<EOF
apiVersion: batch/v1
kind: Job
metadata:
  name: $1-migrate
  labels:
    app.kubernetes.io/name: $1-migrate
    app.kubernetes.io/part-of: commerce-core
    commerce.io/stage: migrate
spec:
  backoffLimit: 4
  ttlSecondsAfterFinished: 86400
  template:
    metadata:
      labels:
        app.kubernetes.io/name: $1-migrate
        app.kubernetes.io/part-of: commerce-core
        commerce.io/needs-db: "true"
    spec:
      restartPolicy: Never
      automountServiceAccountToken: false
      securityContext:
        runAsNonRoot: true
        runAsUser: 1654
        runAsGroup: 1654
        seccompProfile:
          type: RuntimeDefault
      containers:
        - name: efbundle
          image: $2
          envFrom:
            - configMapRef:
                name: commerce-config
          env:
            - name: DOTNET_BUNDLE_EXTRACT_BASE_DIR
              value: /tmp/bundle
            - name: ConnectionStrings__DefaultConnection
              valueFrom:
                secretKeyRef:
                  name: commerce-secrets
                  key: ${3}_MIGRATOR_CONN
          securityContext:
            allowPrivilegeEscalation: false
            capabilities:
              drop: [ALL]
          volumeMounts:
            - name: tmp
              mountPath: /tmp
      volumes:
        - name: tmp
          emptyDir: {}
EOF
}

job orders        ordering-migrator      ORDERS
job catalog       catalog-migrator       CATALOG
job payments      payments-migrator      PAYMENTS
job notifications notifications-migrator NOTIFICATIONS
echo "Wrote k8s/base/migrations/*.yaml"