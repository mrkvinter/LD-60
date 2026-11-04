-- name: Upgrades bought
-- description: Purchases of each upgrade level and the median minute they happened.
-- chart: bar x=upgrade y=purchases
SELECT concat(toString(properties.upgrade), ' ', toString(properties.level)) AS upgrade,
       count() AS purchases,
       round(quantile(0.5)(toFloat(properties.time_sec)) / 60, 1) AS median_min
FROM events
WHERE {build} AND {filters} AND event = 'upgrade_bought'
GROUP BY upgrade
ORDER BY upgrade
