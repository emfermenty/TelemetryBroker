CREATE TABLE logs
(
    time DateTime64(9),
    module_id String,
    level LowCardinality(String),
    line String,
    labels Map(String, String)
)
ENGINE = MergeTree
ORDER BY (module_id, time);
