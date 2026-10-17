#!/usr/bin/env bash
# One-time scaffolding for k8s/base/apps/*.yaml. Afterwards edit the YAML directly.
set -euo pipefail
cd "$(git rev-parse --show-toplevel)"
mkdir -p k8s/base/apps

env_db() {   # secret key holding this service's connection string
  cat <<EOF
            - name: ConnectionStrings__DefaultConnection
              valueFrom:
                secretKeyRef:
                  name: commerce-secrets
                  key: $1
EOF
}

env_mq() {
  cat <<'EOF'
            - name: RabbitMq__Password
              valueFrom:
                secretKeyRef:
                  name: commerce-secrets
                  key: RABBITMQ_PASSWORD
EOF
}

env_gateway() {
  cat <<'EOF'
            - name: ASPNETCORE_FORWARDEDHEADERS_ENABLED
              value: "true"   # trust X-Forwarded-For; the network policy lets only the ingress reach this pod
            - name: ReverseProxy__Clusters__ordering__Destinations__primary__Address
              value: http://ordering-api:8080/
            - name: ReverseProxy__Clusters__catalog__Destinations__primary__Address
              value: http://catalog-api:8080/
            - name: ReverseProxy__Clusters__payments__Destinations__primary__Address
              value: http://payments-api:8080/
            - name: ReverseProxy__Clusters__notifications__Destinations__primary__Address
              value: http://notifications-api:8080/
EOF
}

app() {   # name, env block, pod labels block, replicas line ("" when an HPA owns the count), hpa (yes|no)
  local name="$1" env_block="$2" labels_block="$3" replicas="$4" hpa="$5"

  cat > "k8s/base/apps/$name.yaml" <<EOF
apiVersion: apps/v1
kind: Deployment
metadata:
  name: $name
  labels:
    app.kubernetes.io/name: $name
    app.kubernetes.io/part-of: commerce-core
    commerce.io/stage: apps
spec:
${replicas}  revisionHistoryLimit: 3
  strategy:
    type: RollingUpdate
    rollingUpdate:
      maxSurge: 1
      maxUnavailable: 0
  selector:
    matchLabels:
      app.kubernetes.io/name: $name
  template:
    metadata:
      labels:
        app.kubernetes.io/name: $name
        app.kubernetes.io/part-of: commerce-core
${labels_block}
    spec:
      automountServiceAccountToken: false
      terminationGracePeriodSeconds: 30
      securityContext:
        runAsNonRoot: true
        runAsUser: 1654
        runAsGroup: 1654
        seccompProfile:
          type: RuntimeDefault
      containers:
        - name: app
          image: $name
          ports:
            - name: http
              containerPort: 8080
          envFrom:
            - configMapRef:
                name: commerce-config
          env:
${env_block}
          resources:
            requests:
              cpu: 100m
              memory: 128Mi
            limits:
              memory: 384Mi
          startupProbe:
            httpGet:
              path: /health/live
              port: http
            periodSeconds: 2
            failureThreshold: 30
          readinessProbe:
            httpGet:
              path: /health/ready
              port: http
            periodSeconds: 10
            timeoutSeconds: 3
          livenessProbe:
            httpGet:
              path: /health/live
              port: http
            periodSeconds: 20
            timeoutSeconds: 3
            failureThreshold: 3
          lifecycle:
            preStop:            # let the endpoint list catch up before the pod stops accepting work
              exec:
                command: [sleep, "5"]
          securityContext:
            allowPrivilegeEscalation: false
            readOnlyRootFilesystem: true
            capabilities:
              drop: [ALL]
          volumeMounts:
            - name: tmp
              mountPath: /tmp
      volumes:
        - name: tmp
          emptyDir: {}
---
apiVersion: v1
kind: Service
metadata:
  name: $name
  labels:
    app.kubernetes.io/name: $name
    app.kubernetes.io/part-of: commerce-core
    commerce.io/stage: apps
spec:
  selector:
    app.kubernetes.io/name: $name
  ports:
    - name: http
      port: 8080
      targetPort: http
EOF

  if [ "$hpa" = "yes" ]; then
    cat >> "k8s/base/apps/$name.yaml" <<EOF
---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: $name
  labels:
    app.kubernetes.io/name: $name
    app.kubernetes.io/part-of: commerce-core
    commerce.io/stage: apps
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: $name
  minReplicas: 1
  maxReplicas: 4
  metrics:
    - type: Resource
      resource:
        name: cpu
        target:
          type: Utilization
          averageUtilization: 70
EOF
  fi
}

db_label='        commerce.io/needs-db: "true"'
mq_label='        commerce.io/needs-mq: "true"'
db_mq_labels="$db_label"$'\n'"$mq_label"
one_replica=$'  replicas: 1\n'

#   name               env                                              pod labels      replicas        hpa
app ordering-api      "$(env_db ORDERS_DB_CONN)"$'\n'"$(env_mq)"        "$db_mq_labels" ""              yes
app catalog-api       "$(env_db CATALOG_DB_CONN)"$'\n'"$(env_mq)"       "$db_mq_labels" "$one_replica"  no
app payments-api      "$(env_db PAYMENTS_DB_CONN)"$'\n'"$(env_mq)"      "$db_mq_labels" "$one_replica"  no
app notifications-api "$(env_db NOTIFICATIONS_DB_CONN)"$'\n'"$(env_mq)" "$db_mq_labels" "$one_replica"  no
app gateway           "$(env_gateway)"                                  ""              ""              yes

echo "Wrote k8s/base/apps/*.yaml"