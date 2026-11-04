-- name: Buildings
-- description: For each building: times it was built, sessions that built it and the median minute.
-- chart: bar x=building y=sessions
SELECT toString(properties.building_id) AS building,
       count() AS built,
       count(DISTINCT properties.session_id) AS sessions,
       round(quantile(0.5)(toFloat(properties.time_sec)) / 60, 1) AS median_min
FROM events
WHERE {build} AND {filters} AND event = 'building_built'
GROUP BY building
ORDER BY sessions DESC
