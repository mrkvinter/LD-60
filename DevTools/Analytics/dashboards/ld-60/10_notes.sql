-- name: Notes read
-- description: Sessions that read each note and the median minute they found it.
-- chart: bar x=note y=sessions
SELECT toString(properties.note_id) AS note,
       count(DISTINCT properties.session_id) AS sessions,
       round(quantile(0.5)(toFloat(properties.time_sec)) / 60, 1) AS median_min
FROM events
WHERE {build} AND {filters} AND event = 'note_read'
GROUP BY note
ORDER BY median_min
