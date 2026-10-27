# database/seed

Sample data scripts (SQL). Written after the EF Core migration has created the tables.

`01_sample_data.sql` clears the business data (the admin account and the seeded container types stay) and loads 8 shipping lines,
10 customers, three physical blocks A, B, C (41 bays x 6 rows x 4 tiers each), a virtual block V, 118 containers
(82 in the yard, 40 of them for 0-9 days and 42 for 10+ days), 41 stays that already left, and the release orders they left on.
Times are relative to the day the script runs. It is safe to run again; it starts by clearing the data.

```bash
docker exec -i depot-postgres psql -U depot -d depot -v ON_ERROR_STOP=1 < database/seed/01_sample_data.sql
```
