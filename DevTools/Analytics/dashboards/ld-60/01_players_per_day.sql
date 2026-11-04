-- name: Players and sessions per day
-- description: Players (installs) and game launches that sent any event.
-- chart: bar x=day y=players,sessions
SELECT toDate(timestamp) AS day,
       count(DISTINCT distinct_id) AS players,
       count(DISTINCT properties.session_id) AS sessions
FROM events
WHERE {build} AND {filters}
GROUP BY day
ORDER BY day
