CREATE TABLE modules (
    id         TEXT PRIMARY KEY,
    kind       TEXT NOT NULL,
    first_seen TIMESTAMPTZ NOT NULL DEFAULT now(),
    last_seen  TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE metric_points (
    time        TIMESTAMPTZ NOT NULL,
    module_id   TEXT NOT NULL REFERENCES modules(id),
    metric_name TEXT NOT NULL,
    attributes  JSONB NOT NULL DEFAULT '{}',
    value       DOUBLE PRECISION NOT NULL
);

SELECT create_hypertable('metric_points', 'time');

CREATE INDEX ON metric_points (module_id, metric_name, time DESC);