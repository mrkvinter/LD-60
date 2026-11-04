-- name: Progress funnel
-- description: Sessions that got at least this far. The steps are not strictly in order: a player can build
--   before filling a rack.
-- chart: bar x=step y=sessions
SELECT tupleElement(row, 2) AS step, tupleElement(row, 3) AS sessions
FROM (
    SELECT arrayJoin([
        (1, 'Started', count()),
        (2, 'Filled a rack', countIf(filled_racks >= 1)),
        (3, 'Built something', countIf(buildings >= 1)),
        (4, 'Bought an upgrade', countIf(upgrades >= 1)),
        (5, 'Unlocked an ability', countIf(abilities >= 1)),
        (6, 'Read a note', countIf(notes >= 1)),
        (7, 'Fake ending', countIf(fake_ending)),
        (8, 'Real ending', countIf(real_ending))
    ]) AS row
    FROM ({sessions})
)
ORDER BY tupleElement(row, 1)
