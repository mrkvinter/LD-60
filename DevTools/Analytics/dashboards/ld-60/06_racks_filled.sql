-- name: Racks filled
-- description: For each kind of trash: sessions that filled its rack and the median minute they did it.
-- chart: bar x=item y=sessions
SELECT toString(properties.item_id) AS item,
       count(DISTINCT properties.session_id) AS sessions,
       round(quantile(0.5)(toFloat(properties.time_sec)) / 60, 1) AS median_min
FROM events
WHERE {build} AND {filters} AND event = 'rack_filled'
GROUP BY item
ORDER BY sessions DESC
