-- name: Abilities unlocked
-- description: Sessions that unlocked each movement ability (syringes) and the median minute.
-- chart: bar x=ability y=sessions
SELECT toString(properties.ability) AS ability,
       count(DISTINCT properties.session_id) AS sessions,
       round(quantile(0.5)(toFloat(properties.time_sec)) / 60, 1) AS median_min
FROM events
WHERE {build} AND {filters} AND event = 'ability_unlocked'
GROUP BY ability
ORDER BY median_min
