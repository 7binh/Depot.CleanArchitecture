-- Sample data for the Depot database (BR_DEPOT.md; used to check the reports of BR-RP).
--
-- Clears the business data first (keeps the admin account and the seeded container types), then loads:
--   * 8 shipping lines, 10 customers
--   * 3 physical blocks A, B, C of 41 bays x 6 rows x 4 tiers, and one virtual block V
--   * 118 containers: 82 in the yard (40 for 0-9 days, 42 for 10+ days), 41 stays that already left (5 containers came back)
--   * release orders that cover the gate outs, plus 2 fresh orders to try a gate out
-- Gate in and gate out times are relative to the day the script runs, so the two age groups stay filled.
-- Block A holds 20ft containers only (odd bays), block B 40ft only (even bays 4 apart),
-- block C both: 20ft in bays 1-21, 40ft in bays 26-38.
--
-- Run:  docker exec -i depot-postgres psql -U depot -d depot -v ON_ERROR_STOP=1 < database/seed/01_sample_data.sql
-- Customer names and tax codes are made up; shipping lines are real.

SET client_encoding = 'UTF8';
BEGIN;

TRUNCATE "ContainerStays", "ReleaseOrderItems", "ReleaseOrders", "Containers", "YardRows", "Bays", "Blocks",
         "Customers", "LineOperators", "RefreshTokens";
DELETE FROM "ContainerTypes" WHERE "Code" NOT IN
    ('BUNKER', 'DRY', 'FLATRACK', 'OPENTOP', 'REEFER', 'SPECIALIZED', 'TANKX', 'VENTILATED');

-- A moment on the depot calendar (Vietnam time), N days before today; never in the future.
CREATE FUNCTION pg_temp.depot_time(days_ago int, hh int, mm int) RETURNS timestamptz AS $$
    SELECT LEAST(
        (((now() AT TIME ZONE 'Asia/Ho_Chi_Minh')::date - days_ago) + make_time(hh, mm, 0)) AT TIME ZONE 'Asia/Ho_Chi_Minh',
        now() - interval '5 minutes');
$$ LANGUAGE sql;

INSERT INTO "LineOperators" ("Id", "Code", "Name", "CreatedAt", "CreatedBy") VALUES
    ('fe6fc428-18fb-500f-a9a6-282e693ef218', 'MSK', 'Maersk Line', now(), 'seed'),
    ('002054bf-5459-5b3b-a1dd-1a51353ee8d0', 'MSC', 'Mediterranean Shipping Company', now(), 'seed'),
    ('44f060b8-1c5e-5b6e-af6a-ecc7833d06a4', 'CMA', 'CMA CGM', now(), 'seed'),
    ('1df94171-244e-54d5-8aac-8d4b0245a7ce', 'COS', 'COSCO Shipping Lines', now(), 'seed'),
    ('3b4ef251-4d7d-53fa-8227-be22beb2c6e7', 'EGH', 'Evergreen Marine', now(), 'seed'),
    ('85543a90-fa4b-5e7f-b670-affb2b18537d', 'ONE', 'Ocean Network Express', now(), 'seed'),
    ('14093399-eea6-5221-9f49-ddc076f09576', 'HLX', 'Hapag-Lloyd', now(), 'seed'),
    ('09ce94ba-271b-5d68-8315-e1a96e16612c', 'OOL', 'Orient Overseas Container Line', now(), 'seed');

INSERT INTO "Customers" ("Id", "TaxCode", "Name", "CreatedAt", "CreatedBy") VALUES
    ('b9f067a2-96fe-5f5b-8017-070362278038', '0312458796', 'Công ty TNHH Xuất nhập khẩu Hải Long', now(), 'seed'),
    ('44b5acbe-407d-5bc2-b86f-08334519baaa', '0309876543', 'Công ty Cổ phần Thủy sản Đại Dương', now(), 'seed'),
    ('f6074a36-5f89-51ea-9739-68853325fe57', '0305551234', 'Công ty TNHH Gỗ Nội Thất Phú Mỹ', now(), 'seed'),
    ('fda2feea-d450-5828-951c-fd5079edc4fa', '0314447788', 'Công ty Cổ phần Nông sản Tây Nguyên', now(), 'seed'),
    ('8ea7b7b5-1b93-5bb5-8822-abb349b78fa9', '0301122334', 'Công ty TNHH Dệt May Sài Gòn', now(), 'seed'),
    ('bceed250-169f-55b0-b2b2-243f3e5b446e', '0316669900', 'Công ty TNHH Điện tử Việt Tiến', now(), 'seed'),
    ('396be773-e276-5ab3-9154-f5152f120365', '0302468135', 'Công ty Cổ phần Cà phê Đắk Lắk Xanh', now(), 'seed'),
    ('3e36f733-9565-5aec-9cbb-04f46dbabfd1', '0307713579', 'Công ty TNHH Thực phẩm An Phát', now(), 'seed'),
    ('4584127c-b6e5-5191-980e-8fde640b917c', '0310024680', 'Công ty Cổ phần Logistics Nam Bộ', now(), 'seed'),
    ('77433a38-157e-5f19-a418-0d6e2cd15d0d', '0313692581', 'Công ty TNHH Giày da Thành Công', now(), 'seed');

INSERT INTO "Blocks" ("Id", "Code", "Name", "IsVirtual", "CreatedAt", "CreatedBy") VALUES
    ('2437a77e-eb0f-53c8-b462-abb4ae5ef24b', 'A', 'Bãi A', false, now(), 'seed'),
    ('388a3b83-b075-538e-8406-3e6ef0d1c251', 'B', 'Bãi B', false, now(), 'seed'),
    ('28e49d9b-70c4-5b27-aab1-202214c1b438', 'C', 'Bãi C', false, now(), 'seed'),
    ('019e6955-5866-5594-be09-4cf8262460d7', 'V', 'Bãi ảo (chưa xếp vị trí)', true, now(), 'seed');

-- 41 bays and 6 rows of 4 tiers in each physical block.
INSERT INTO "Bays" ("Id", "BlockId", "BayNo", "CreatedAt", "CreatedBy")
SELECT gen_random_uuid(), b."Id", n, now(), 'seed'
FROM "Blocks" b CROSS JOIN generate_series(1, 41) AS n
WHERE NOT b."IsVirtual";

INSERT INTO "YardRows" ("Id", "BayId", "RowNo", "MaxTier", "CreatedAt", "CreatedBy")
SELECT gen_random_uuid(), y."Id", n, 4, now(), 'seed'
FROM "Bays" y CROSS JOIN generate_series(1, 6) AS n;

INSERT INTO "Containers" ("Id", "Number", "LineOperatorId", "ContainerTypeId", "IsoCode", "SizeFeet", "MaxWeightKg", "TareWeightKg", "ManufactureDate", "CreatedAt", "CreatedBy")
SELECT gen_random_uuid(), v.number, o."Id", t."Id", v.iso, v.size, v.max_kg, v.tare_kg, v.made::date, now(), 'seed'
FROM (VALUES
    ('COSU5624551', 'COS', 'DRY', '22G1', 20, 30480, 2250, '2015-01-27'),
    ('EGHU4498342', 'EGH', 'DRY', '22G1', 20, 30480, 2250, '2015-05-05'),
    ('ONEU5959165', 'ONE', 'DRY', '22G1', 20, 30480, 2250, '2023-10-23'),
    ('EGHU6556495', 'EGH', 'DRY', '22G1', 20, 30480, 2250, '2016-01-08'),
    ('CMAU8543903', 'CMA', 'DRY', '22G1', 20, 30480, 2250, '2022-11-17'),
    ('MSKU0728239', 'MSK', 'DRY', '22G1', 20, 30480, 2250, '2018-10-07'),
    ('OOLU4937199', 'OOL', 'DRY', '22G1', 20, 30480, 2250, '2022-06-27'),
    ('COSU6665160', 'COS', 'DRY', '22G1', 20, 30480, 2250, '2019-05-16'),
    ('COSU8011830', 'COS', 'DRY', '22G1', 20, 30480, 2250, '2022-08-12'),
    ('CMAU9240521', 'CMA', 'DRY', '22G1', 20, 30480, 2250, '2017-02-17'),
    ('MSCU3987424', 'MSC', 'DRY', '22G1', 20, 30480, 2250, '2024-09-22'),
    ('CMAU3908423', 'CMA', 'DRY', '22G1', 20, 30480, 2250, '2024-06-17'),
    ('EGHU2380350', 'EGH', 'DRY', '22G1', 20, 30480, 2250, '2019-02-06'),
    ('ONEU8902190', 'ONE', 'DRY', '22G1', 20, 30480, 2250, '2024-11-27'),
    ('CMAU1052847', 'CMA', 'DRY', '22G1', 20, 30480, 2250, '2016-04-06'),
    ('MSKU0448270', 'MSK', 'DRY', '22G1', 20, 30480, 2250, '2016-03-13'),
    ('EGHU2262213', 'EGH', 'DRY', '22G1', 20, 30480, 2250, '2020-12-15'),
    ('MSKU0152143', 'MSK', 'DRY', '22G1', 20, 30480, 2250, '2015-02-11'),
    ('CMAU3823542', 'CMA', 'DRY', '22G1', 20, 30480, 2250, '2015-08-16'),
    ('MSKU6181215', 'MSK', 'DRY', '22G1', 20, 30480, 2250, '2023-11-03'),
    ('COSU3702260', 'COS', 'DRY', '22G1', 20, 30480, 2250, '2016-07-12'),
    ('HLXU3093858', 'HLX', 'DRY', '22G1', 20, 30480, 2250, '2024-03-19'),
    ('MSKU0627845', 'MSK', 'DRY', '22G1', 20, 30480, 2250, '2019-09-10'),
    ('OOLU7699284', 'OOL', 'DRY', '22G1', 20, 30480, 2250, '2021-08-24'),
    ('HLXU1307204', 'HLX', 'DRY', '22G1', 20, 30480, 2250, '2016-01-08'),
    ('CMAU6230258', 'CMA', 'DRY', '22G1', 20, 30480, 2250, '2019-02-22'),
    ('CMAU0701047', 'CMA', 'DRY', '22G1', 20, 30480, 2250, '2014-12-03'),
    ('ONEU0404863', 'ONE', 'DRY', '22G1', 20, 30480, 2250, '2016-06-15'),
    ('EGHU8368645', 'EGH', 'DRY', '22G1', 20, 30480, 2250, '2021-04-19'),
    ('EGHU0978446', 'EGH', 'DRY', '22G1', 20, 30480, 2250, '2017-03-17'),
    ('HLXU4569337', 'HLX', 'DRY', '22G1', 20, 30480, 2250, '2017-12-04'),
    ('HLXU6531270', 'HLX', 'DRY', '22G1', 20, 30480, 2250, '2014-02-23'),
    ('CMAU9639023', 'CMA', 'DRY', '22G1', 20, 30480, 2250, '2019-02-17'),
    ('ONEU0358691', 'ONE', 'DRY', '22G1', 20, 30480, 2250, '2014-07-03'),
    ('MSKU7849865', 'MSK', 'DRY', '22G1', 20, 30480, 2250, '2021-10-07'),
    ('OOLU4258393', 'OOL', 'DRY', '22G1', 20, 30480, 2250, '2020-08-14'),
    ('COSU4773864', 'COS', 'DRY', '22G1', 20, 30480, 2250, '2017-03-20'),
    ('MSCU0566381', 'MSC', 'DRY', '22G1', 20, 30480, 2250, '2024-12-23'),
    ('MSKU3144012', 'MSK', 'DRY', '22G1', 20, 30480, 2250, '2019-09-07'),
    ('EGHU8461969', 'EGH', 'DRY', '22G1', 20, 30480, 2250, '2022-08-21'),
    ('EGHU6567329', 'EGH', 'DRY', '22G1', 20, 30480, 2250, '2015-06-03'),
    ('MSKU7050890', 'MSK', 'DRY', '22G1', 20, 30480, 2250, '2018-01-17'),
    ('EGHU5317433', 'EGH', 'DRY', '22G1', 20, 30480, 2250, '2021-11-06'),
    ('OOLU2262537', 'OOL', 'DRY', '22G1', 20, 30480, 2250, '2015-03-04'),
    ('MSKU2612633', 'MSK', 'DRY', '22G1', 20, 30480, 2250, '2024-08-08'),
    ('CMAU2671511', 'CMA', 'DRY', '22G1', 20, 30480, 2250, '2020-12-19'),
    ('MSKU2033462', 'MSK', 'DRY', '42G1', 40, 30480, 3750, '2017-09-21'),
    ('COSU3189291', 'COS', 'DRY', '42G1', 40, 30480, 3750, '2015-10-10'),
    ('MSCU6785026', 'MSC', 'DRY', '45G1', 40, 30480, 3900, '2018-10-27'),
    ('COSU8360581', 'COS', 'REEFER', '42R1', 40, 34000, 4400, '2016-04-05'),
    ('MSKU0647630', 'MSK', 'REEFER', '42R1', 40, 34000, 4400, '2016-02-10'),
    ('HLXU9628330', 'HLX', 'DRY', '45G1', 40, 30480, 3900, '2020-12-10'),
    ('EGHU6569553', 'EGH', 'DRY', '45G1', 40, 30480, 3900, '2022-04-26'),
    ('MSKU6746728', 'MSK', 'DRY', '42G1', 40, 30480, 3750, '2015-01-12'),
    ('COSU2649875', 'COS', 'DRY', '45G1', 40, 30480, 3900, '2024-01-13'),
    ('HLXU8322582', 'HLX', 'DRY', '42G1', 40, 30480, 3750, '2018-01-20'),
    ('MSKU2116335', 'MSK', 'DRY', '42G1', 40, 30480, 3750, '2023-04-01'),
    ('OOLU8853352', 'OOL', 'DRY', '45G1', 40, 30480, 3900, '2019-06-13'),
    ('HLXU4183128', 'HLX', 'DRY', '45G1', 40, 30480, 3900, '2015-03-21'),
    ('COSU7043432', 'COS', 'DRY', '42G1', 40, 30480, 3750, '2015-09-13'),
    ('MSCU4614019', 'MSC', 'DRY', '45G1', 40, 30480, 3900, '2021-10-16'),
    ('HLXU3797760', 'HLX', 'DRY', '42G1', 40, 30480, 3750, '2015-10-10'),
    ('MSCU6633065', 'MSC', 'DRY', '42G1', 40, 30480, 3750, '2017-04-19'),
    ('CMAU7271307', 'CMA', 'DRY', '42G1', 40, 30480, 3750, '2022-11-09'),
    ('MSCU9890149', 'MSC', 'DRY', '42G1', 40, 30480, 3750, '2023-04-11'),
    ('MSKU5962677', 'MSK', 'DRY', '22G1', 20, 30480, 2250, '2024-06-28'),
    ('EGHU8912996', 'EGH', 'DRY', '22G1', 20, 30480, 2250, '2018-02-22'),
    ('CMAU3270180', 'CMA', 'DRY', '22G1', 20, 30480, 2250, '2014-06-14'),
    ('MSCU4242673', 'MSC', 'DRY', '22G1', 20, 30480, 2250, '2018-08-10'),
    ('CMAU4256130', 'CMA', 'DRY', '22G1', 20, 30480, 2250, '2024-09-21'),
    ('ONEU3045089', 'ONE', 'DRY', '22G1', 20, 30480, 2250, '2021-03-05'),
    ('MSKU2318275', 'MSK', 'DRY', '22G1', 20, 30480, 2250, '2023-01-15'),
    ('MSCU1019964', 'MSC', 'DRY', '22G1', 20, 30480, 2250, '2024-09-21'),
    ('MSCU4909370', 'MSC', 'DRY', '45G1', 40, 30480, 3900, '2024-09-15'),
    ('MSKU0060617', 'MSK', 'DRY', '42G1', 40, 30480, 3750, '2021-09-07'),
    ('MSKU8519024', 'MSK', 'DRY', '42G1', 40, 30480, 3750, '2017-06-28'),
    ('HLXU3463931', 'HLX', 'DRY', '42G1', 40, 30480, 3750, '2022-02-14'),
    ('EGHU6547641', 'EGH', 'DRY', '42G1', 40, 30480, 3750, '2020-04-01'),
    ('COSU2403639', 'COS', 'DRY', '42G1', 40, 30480, 3750, '2018-04-07'),
    ('OOLU4159765', 'OOL', 'DRY', '22G1', 20, 30480, 2250, '2014-03-28'),
    ('HLXU1628565', 'HLX', 'DRY', '22G1', 20, 30480, 2250, '2018-02-10'),
    ('MSKU0532816', 'MSK', 'DRY', '22G1', 20, 30480, 2250, '2020-10-21'),
    ('OOLU6805376', 'OOL', 'DRY', '22G1', 20, 30480, 2250, '2022-06-13'),
    ('MSKU5752731', 'MSK', 'DRY', '42G1', 40, 30480, 3750, '2022-09-27'),
    ('CMAU3032135', 'CMA', 'DRY', '22G1', 20, 30480, 2250, '2024-02-13'),
    ('OOLU2789929', 'OOL', 'DRY', '22G1', 20, 30480, 2250, '2020-09-22'),
    ('COSU1088574', 'COS', 'DRY', '42G1', 40, 30480, 3750, '2016-11-26'),
    ('EGHU9237090', 'EGH', 'DRY', '42G1', 40, 30480, 3750, '2022-06-20'),
    ('MSKU5244915', 'MSK', 'DRY', '22G1', 20, 30480, 2250, '2019-01-08'),
    ('EGHU9938336', 'EGH', 'DRY', '22G1', 20, 30480, 2250, '2017-04-07'),
    ('COSU0261556', 'COS', 'DRY', '22G1', 20, 30480, 2250, '2022-06-12'),
    ('CMAU6547140', 'CMA', 'DRY', '42G1', 40, 30480, 3750, '2015-12-17'),
    ('HLXU0820362', 'HLX', 'DRY', '22G1', 20, 30480, 2250, '2023-10-06'),
    ('OOLU4083715', 'OOL', 'DRY', '22G1', 20, 30480, 2250, '2019-06-01'),
    ('CMAU6655400', 'CMA', 'DRY', '22G1', 20, 30480, 2250, '2021-08-08'),
    ('EGHU0016020', 'EGH', 'DRY', '42G1', 40, 30480, 3750, '2016-05-23'),
    ('MSKU0594607', 'MSK', 'DRY', '22G1', 20, 30480, 2250, '2020-01-08'),
    ('MSCU1784114', 'MSC', 'DRY', '42G1', 40, 30480, 3750, '2024-12-03'),
    ('MSCU8199615', 'MSC', 'DRY', '22G1', 20, 30480, 2250, '2015-10-12'),
    ('CMAU5553549', 'CMA', 'DRY', '22G1', 20, 30480, 2250, '2015-09-12'),
    ('HLXU1656269', 'HLX', 'DRY', '22G1', 20, 30480, 2250, '2020-05-14'),
    ('COSU2549428', 'COS', 'DRY', '22G1', 20, 30480, 2250, '2015-06-20'),
    ('HLXU5987820', 'HLX', 'DRY', '22G1', 20, 30480, 2250, '2022-07-27'),
    ('HLXU6555610', 'HLX', 'DRY', '22G1', 20, 30480, 2250, '2021-03-25'),
    ('MSCU5481517', 'MSC', 'DRY', '42G1', 40, 30480, 3750, '2021-03-06'),
    ('COSU1778836', 'COS', 'DRY', '22G1', 20, 30480, 2250, '2015-05-14'),
    ('COSU4938917', 'COS', 'DRY', '22G1', 20, 30480, 2250, '2015-12-27'),
    ('EGHU3413401', 'EGH', 'DRY', '42G1', 40, 30480, 3750, '2017-08-25'),
    ('CMAU9680958', 'CMA', 'DRY', '22G1', 20, 30480, 2250, '2022-05-16'),
    ('MSKU1639060', 'MSK', 'DRY', '42G1', 40, 30480, 3750, '2015-01-20'),
    ('MSKU2894434', 'MSK', 'DRY', '22G1', 20, 30480, 2250, '2014-08-16'),
    ('ONEU1737771', 'ONE', 'DRY', '22G1', 20, 30480, 2250, '2019-08-26'),
    ('MSCU0490840', 'MSC', 'DRY', '22G1', 20, 30480, 2250, '2023-11-05'),
    ('EGHU6329172', 'EGH', 'DRY', '22G1', 20, 30480, 2250, '2014-07-08'),
    ('MSKU9301931', 'MSK', 'DRY', '42G1', 40, 30480, 3750, '2023-08-20'),
    ('MSCU6219081', 'MSC', 'DRY', '42G1', 40, 30480, 3750, '2019-11-05'),
    ('HLXU3866618', 'HLX', 'DRY', '22G1', 20, 30480, 2250, '2021-09-10'),
    ('EGHU6582489', 'EGH', 'DRY', '42G1', 40, 30480, 3750, '2014-04-14')
) AS v(number, owner, ctype, iso, size, max_kg, tare_kg, made)
JOIN "LineOperators" o ON o."Code" = v.owner
JOIN "ContainerTypes" t ON t."Code" = v.ctype;

INSERT INTO "ReleaseOrders" ("Id", "LineOperatorId", "CustomerId", "OrderNo", "IssuedDate", "ExpiryDate", "ExportVessel", "CreatedAt", "CreatedBy")
SELECT v.id::uuid, o."Id", c."Id", v.order_no, (now() AT TIME ZONE 'Asia/Ho_Chi_Minh')::date - v.issued_ago, (now() AT TIME ZONE 'Asia/Ho_Chi_Minh')::date + v.expiry_in, v.vessel, now(), 'seed'
FROM (VALUES
    ('19c75ce3-3868-5ede-bd48-ebed6a294f18', 'MSK', 'MSK2610001', '0310024680', 'MAERSK EDINBURGH V.2610E', 48, 17),
    ('62e7b8ac-56f5-5e3f-bfff-342a19908f20', 'MSC', 'MSC2610001', '0305551234', 'MSC ISABELLA V.2610E', 48, 25),
    ('1aa99d5c-96d3-56da-b3f5-0013e359e127', 'CMA', 'CMA2610001', '0305551234', 'CMA CGM MARCO POLO V.2610E', 48, 14),
    ('65760536-eda6-5ee6-8222-b9f9995aaeaf', 'COS', 'COS2610001', '0302468135', 'COSCO SHIPPING UNIVERSE V.2610E', 48, 15),
    ('7a2e7565-4a84-5d34-b6a4-2ee3abd9d268', 'EGH', 'EGH2610001', '0310024680', 'EVER ACE V.2610E', 48, 15),
    ('33f4ed79-4ba1-5cfc-88f8-e4707ad53acf', 'ONE', 'ONE2610001', '0310024680', 'ONE APUS V.2610E', 48, 25),
    ('81c0671f-cb93-5303-a0ba-69e04ae272e7', 'HLX', 'HLX2610001', '0309876543', 'HAMBURG EXPRESS V.2610E', 48, 12),
    ('31aba63e-fdaa-54a4-a4aa-1d9a0dbba3f5', 'OOL', 'OOL2610001', '0301122334', 'OOCL HONG KONG V.2610E', 48, 19),
    ('fc69ddd6-ca04-5d26-8685-4d59cf4dd327', 'MSK', 'MSK2610002', '0305551234', 'MAERSK EDINBURGH V.2611E', 3, 18),
    ('c19e1ead-4778-5465-82ed-43e8208b736b', 'CMA', 'CMA2610002', '0302468135', 'CMA CGM MARCO POLO V.2611E', 3, 25)
) AS v(id, owner, order_no, tax_code, vessel, issued_ago, expiry_in)
JOIN "LineOperators" o ON o."Code" = v.owner
JOIN "Customers" c ON c."TaxCode" = v.tax_code;

INSERT INTO "ReleaseOrderItems" ("Id", "ReleaseOrderId", "ContainerTypeId", "SizeFeet", "Quantity", "CreatedAt", "CreatedBy")
SELECT v.id::uuid, v.order_id::uuid, t."Id", v.size, v.qty, now(), 'seed'
FROM (VALUES
    ('6b2b9cb6-a82e-576d-a211-b1be71629bcb', '19c75ce3-3868-5ede-bd48-ebed6a294f18', 20, 5),
    ('51107406-b971-5da6-8d5b-b7214f930e72', '19c75ce3-3868-5ede-bd48-ebed6a294f18', 40, 8),
    ('e8a42b32-c1b2-5bbb-9910-11b135166b0b', '62e7b8ac-56f5-5e3f-bfff-342a19908f20', 20, 4),
    ('d16c7f9e-3bb8-5256-9840-3cdf02dfe65b', '62e7b8ac-56f5-5e3f-bfff-342a19908f20', 40, 5),
    ('6092885f-6005-5fcd-8eb5-3b391d5daffc', '1aa99d5c-96d3-56da-b3f5-0013e359e127', 20, 8),
    ('caad554c-3d07-5303-91c5-e8b7cd182b9c', '1aa99d5c-96d3-56da-b3f5-0013e359e127', 40, 5),
    ('95ab2481-02a9-5cec-a1d6-880fa6add8fe', '65760536-eda6-5ee6-8222-b9f9995aaeaf', 20, 9),
    ('06455bec-8ca1-5bbf-8135-baf5df10cdd9', '65760536-eda6-5ee6-8222-b9f9995aaeaf', 40, 5),
    ('32bbef75-a639-53fe-8768-93d3a565e0cc', '7a2e7565-4a84-5d34-b6a4-2ee3abd9d268', 20, 3),
    ('1ef776c0-5635-5abe-bcfb-2d73a76dac74', '7a2e7565-4a84-5d34-b6a4-2ee3abd9d268', 40, 6),
    ('2b9c3f2a-661c-5908-91bf-97787fbcd2f0', '33f4ed79-4ba1-5cfc-88f8-e4707ad53acf', 20, 3),
    ('ec5e4d93-045a-5084-8b58-60549b9a6eaa', '81c0671f-cb93-5303-a0ba-69e04ae272e7', 20, 8),
    ('7f3c6af2-ec95-5544-906d-4cc74dcb215b', '31aba63e-fdaa-54a4-a4aa-1d9a0dbba3f5', 20, 6),
    ('f6b400f5-4dfc-5e93-86d3-c9e0a85e7457', 'fc69ddd6-ca04-5d26-8685-4d59cf4dd327', 20, 6),
    ('c2abf278-3dfa-596b-9201-251097201cb4', 'c19e1ead-4778-5465-82ed-43e8208b736b', 40, 4)
) AS v(id, order_id, size, qty)
CROSS JOIN "ContainerTypes" t WHERE t."Code" = 'DRY';

-- Every stay: where the container stood (block, bay, row, tier), when it came in and, if it left, on which order line.
INSERT INTO "ContainerStays" ("Id", "ContainerId", "GateInAt", "Grade", "Condition", "DamageNote", "InVehicleType", "InVehicleNumber",
    "BlockId", "RowId", "Tier", "GateOutAt", "OutVehicleType", "OutVehicleNumber", "ReleaseOrderItemId", "CreatedAt", "CreatedBy")
SELECT gen_random_uuid(), c."Id", pg_temp.depot_time(v.in_days, v.in_h, v.in_m), v.grade, v.cond, v.note, v.in_vtype, v.in_vnum,
    b."Id", r."Id", v.tier,
    CASE WHEN v.out_days IS NULL THEN NULL ELSE pg_temp.depot_time(v.out_days, v.out_h, v.out_m) END,
    v.out_vtype, v.out_vnum, v.item::uuid, now(), 'seed'
FROM (VALUES
    ('OOLU6805376', 'A', 31, 6, 1, 44, 7, 0, 'A', 'Normal', NULL, 'Truck', '50H-497.99', 11, 9, 20, 'Truck', '60C-159.32', '7f3c6af2-ec95-5544-906d-4cc74dcb215b'),
    ('MSKU5752731', 'B', 22, 5, 1, 37, 10, 30, 'A', 'Normal', NULL, 'Truck', '29C-951.33', 34, 15, 40, 'Truck', '51C-634.45', '51107406-b971-5da6-8d5b-b7214f930e72'),
    ('CMAU3032135', 'A', 17, 6, 1, 42, 11, 15, 'A', 'Normal', NULL, 'Truck', '50H-573.29', 8, 11, 0, 'Truck', '51D-689.32', '6092885f-6005-5fcd-8eb5-3b391d5daffc'),
    ('OOLU2789929', 'A', 21, 4, 2, 29, 9, 30, 'B', 'Normal', NULL, 'Truck', '50H-106.69', 26, 13, 0, 'Truck', '51C-958.40', '7f3c6af2-ec95-5544-906d-4cc74dcb215b'),
    ('COSU1088574', 'B', 30, 1, 1, 22, 10, 15, 'B', 'Normal', NULL, 'Truck', '51C-528.98', 7, 15, 0, 'Barge', 'SG-7038', '06455bec-8ca1-5bbf-8135-baf5df10cdd9'),
    ('EGHU9237090', 'B', 2, 5, 1, 9, 10, 0, 'A', 'Normal', NULL, 'Truck', '61C-233.26', 6, 11, 40, 'Truck', '51D-653.10', '1ef776c0-5635-5abe-bcfb-2d73a76dac74'),
    ('MSKU5244915', 'A', 27, 1, 1, 26, 10, 45, 'A', 'Normal', NULL, 'Truck', '60C-117.67', 7, 7, 20, 'Truck', '50H-867.24', '6b2b9cb6-a82e-576d-a211-b1be71629bcb'),
    ('EGHU9938336', 'A', 17, 4, 2, 27, 8, 15, 'C', 'Normal', NULL, 'Truck', '51D-802.48', 19, 12, 0, 'Truck', '60C-128.62', '32bbef75-a639-53fe-8768-93d3a565e0cc'),
    ('COSU0261556', 'A', 5, 1, 1, 38, 7, 0, 'A', 'Normal', NULL, 'Truck', '50H-739.15', 3, 8, 0, 'Truck', '50H-382.30', '95ab2481-02a9-5cec-a1d6-880fa6add8fe'),
    ('CMAU6547140', 'B', 34, 4, 2, 32, 12, 45, 'B', 'Normal', NULL, 'Truck', '51D-422.94', 11, 7, 20, 'Truck', '60C-356.35', 'caad554c-3d07-5303-91c5-e8b7cd182b9c'),
    ('HLXU0820362', 'A', 3, 6, 2, 8, 10, 15, 'A', 'Normal', NULL, 'Truck', '72C-337.52', 6, 9, 20, 'Truck', '60C-518.55', 'ec5e4d93-045a-5084-8b58-60549b9a6eaa'),
    ('OOLU4083715', 'A', 13, 6, 1, 27, 14, 15, 'B', 'Normal', NULL, 'Truck', '60C-168.67', 25, 12, 20, 'Truck', '51D-826.56', '7f3c6af2-ec95-5544-906d-4cc74dcb215b'),
    ('CMAU6655400', 'A', 1, 1, 1, 41, 12, 0, 'A', 'Normal', NULL, 'Truck', '29C-218.72', 28, 9, 20, 'Truck', '29C-967.65', '6092885f-6005-5fcd-8eb5-3b391d5daffc'),
    ('EGHU0016020', 'B', 2, 6, 1, 10, 15, 0, 'A', 'Normal', NULL, 'Truck', '51C-618.16', 7, 10, 0, 'Truck', '51C-821.16', '1ef776c0-5635-5abe-bcfb-2d73a76dac74'),
    ('MSKU0594607', 'A', 31, 1, 1, 39, 11, 15, 'C', 'Normal', NULL, 'Truck', '72C-489.94', 35, 10, 40, 'Truck', '29C-877.92', '6b2b9cb6-a82e-576d-a211-b1be71629bcb'),
    ('MSCU1784114', 'B', 6, 5, 1, 8, 9, 45, 'A', 'Normal', NULL, 'Truck', '51C-489.37', 6, 13, 20, 'Truck', '51D-861.60', 'd16c7f9e-3bb8-5256-9840-3cdf02dfe65b'),
    ('MSCU8199615', 'A', 17, 4, 1, 37, 17, 0, 'A', 'Normal', NULL, 'Truck', '72C-988.26', 17, 17, 40, 'Truck', '50H-470.45', 'e8a42b32-c1b2-5bbb-9910-11b135166b0b'),
    ('CMAU5553549', 'A', 15, 6, 2, 16, 11, 15, 'A', 'Normal', NULL, 'Truck', '72C-905.27', 6, 11, 0, 'Truck', '50H-146.15', '6092885f-6005-5fcd-8eb5-3b391d5daffc'),
    ('HLXU1656269', 'A', 25, 5, 2, 16, 13, 45, 'A', 'Normal', NULL, 'Truck', '51C-993.27', 8, 11, 20, 'Truck', '61C-454.51', 'ec5e4d93-045a-5084-8b58-60549b9a6eaa'),
    ('COSU2549428', 'A', 31, 1, 2, 9, 6, 15, 'A', 'Normal', NULL, 'Truck', '61C-978.99', 6, 11, 40, 'Truck', '60C-583.26', '95ab2481-02a9-5cec-a1d6-880fa6add8fe'),
    ('HLXU5987820', 'A', 37, 1, 2, 10, 10, 45, 'A', 'Normal', NULL, 'Truck', '29C-142.92', 8, 10, 20, 'Truck', '29C-932.22', 'ec5e4d93-045a-5084-8b58-60549b9a6eaa'),
    ('HLXU6555610', 'A', 29, 2, 2, 17, 11, 15, 'A', 'Normal', NULL, 'Truck', '61C-930.84', 14, 7, 40, 'Truck', '51C-718.25', 'ec5e4d93-045a-5084-8b58-60549b9a6eaa'),
    ('MSCU5481517', 'B', 30, 4, 1, 28, 10, 15, 'A', 'Normal', NULL, 'Truck', '72C-682.76', 3, 16, 20, 'Truck', '61C-801.47', 'd16c7f9e-3bb8-5256-9840-3cdf02dfe65b'),
    ('COSU1778836', 'A', 41, 1, 2, 23, 6, 0, 'A', 'Normal', NULL, 'Barge', 'SG-5117', 15, 17, 0, 'Truck', '61C-483.83', '95ab2481-02a9-5cec-a1d6-880fa6add8fe'),
    ('COSU4938917', 'A', 13, 1, 2, 41, 13, 45, 'B', 'Normal', NULL, 'Truck', '29C-674.26', 17, 14, 40, 'Truck', '61C-403.40', '95ab2481-02a9-5cec-a1d6-880fa6add8fe'),
    ('EGHU3413401', 'B', 2, 6, 2, 12, 8, 0, 'A', 'Normal', NULL, 'Truck', '72C-750.71', 10, 15, 0, 'Truck', '50H-418.43', '1ef776c0-5635-5abe-bcfb-2d73a76dac74'),
    ('CMAU9680958', 'A', 9, 5, 1, 24, 8, 0, 'A', 'Normal', NULL, 'Truck', '51C-705.15', 13, 9, 20, 'Truck', '51C-359.36', '6092885f-6005-5fcd-8eb5-3b391d5daffc'),
    ('MSKU1639060', 'B', 2, 6, 1, 12, 10, 0, 'A', 'Normal', NULL, 'Barge', 'SG-5152', 1, 7, 20, 'Truck', '29C-198.46', '51107406-b971-5da6-8d5b-b7214f930e72'),
    ('MSKU2894434', 'A', 33, 3, 1, 8, 15, 15, 'C', 'Normal', NULL, 'Truck', '51C-187.49', 3, 7, 20, 'Truck', '50H-967.23', '6b2b9cb6-a82e-576d-a211-b1be71629bcb'),
    ('ONEU1737771', 'A', 31, 2, 1, 24, 7, 0, 'B', 'Normal', NULL, 'Truck', '60C-192.40', 19, 13, 0, 'Truck', '72C-617.88', '2b9c3f2a-661c-5908-91bf-97787fbcd2f0'),
    ('MSCU0490840', 'A', 13, 4, 2, 43, 16, 0, 'B', 'Normal', NULL, 'Truck', '51C-145.17', 31, 10, 0, 'Truck', '60C-651.39', 'e8a42b32-c1b2-5bbb-9910-11b135166b0b'),
    ('EGHU6329172', 'A', 33, 2, 2, 33, 6, 15, 'A', 'Normal', NULL, 'Truck', '72C-406.75', 18, 10, 20, 'Truck', '29C-121.48', '32bbef75-a639-53fe-8768-93d3a565e0cc'),
    ('MSKU9301931', 'B', 22, 5, 1, 42, 17, 45, 'A', 'Normal', NULL, 'Truck', '51D-381.36', 0, 16, 20, 'Truck', '51C-649.68', '51107406-b971-5da6-8d5b-b7214f930e72'),
    ('MSCU6219081', 'B', 38, 4, 1, 44, 7, 15, 'A', 'Normal', NULL, 'Truck', '60C-429.65', 19, 8, 0, 'Truck', '50H-664.37', 'd16c7f9e-3bb8-5256-9840-3cdf02dfe65b'),
    ('HLXU3866618', 'A', 27, 2, 1, 28, 15, 45, 'C', 'Normal', NULL, 'Truck', '61C-279.22', 14, 9, 0, 'Truck', '72C-346.64', 'ec5e4d93-045a-5084-8b58-60549b9a6eaa'),
    ('EGHU6582489', 'B', 34, 6, 1, 32, 7, 15, 'A', 'Normal', NULL, 'Truck', '61C-986.67', 30, 13, 0, 'Truck', '60C-737.46', '1ef776c0-5635-5abe-bcfb-2d73a76dac74'),
    ('EGHU6567329', 'A', 35, 1, 1, 10, 7, 15, 'A', 'Normal', NULL, 'Truck', '29C-910.57', 7, 13, 40, 'Truck', '60C-665.22', '32bbef75-a639-53fe-8768-93d3a565e0cc'),
    ('COSU3702260', 'A', 9, 3, 1, 12, 6, 15, 'A', 'Normal', NULL, 'Truck', '72C-114.73', 9, 9, 0, 'Barge', 'SG-3961', '95ab2481-02a9-5cec-a1d6-880fa6add8fe'),
    ('MSKU0060617', 'B', 2, 5, 2, 16, 12, 45, 'A', 'Normal', NULL, 'Truck', '50H-382.77', 12, 12, 40, 'Truck', '50H-293.33', '51107406-b971-5da6-8d5b-b7214f930e72'),
    ('HLXU3093858', 'A', 3, 6, 1, 14, 15, 30, 'B', 'Normal', NULL, 'Truck', '51D-871.74', 7, 13, 0, 'Truck', '29C-728.21', 'ec5e4d93-045a-5084-8b58-60549b9a6eaa'),
    ('CMAU3823542', 'A', 35, 1, 1, 7, 11, 15, 'A', 'Normal', NULL, 'Truck', '61C-789.72', 4, 15, 0, 'Truck', '50H-771.91', '6092885f-6005-5fcd-8eb5-3b391d5daffc'),
    ('COSU5624551', 'A', 7, 2, 1, 29, 14, 30, 'A', 'Damaged', 'Rách sơn, gỉ ở góc đáy', 'Truck', '51D-348.74', NULL, NULL, NULL, NULL, NULL, NULL),
    ('EGHU4498342', 'A', 3, 1, 1, 41, 16, 15, 'A', 'Normal', NULL, 'Truck', '61C-386.33', NULL, NULL, NULL, NULL, NULL, NULL),
    ('ONEU5959165', 'A', 5, 5, 1, 1, 7, 0, 'B', 'Normal', NULL, 'Truck', '61C-659.20', NULL, NULL, NULL, NULL, NULL, NULL),
    ('EGHU6556495', 'A', 5, 5, 2, 29, 13, 40, 'A', 'Damaged', 'Móp thành container bên trái', 'Truck', '51C-810.81', NULL, NULL, NULL, NULL, NULL, NULL),
    ('CMAU8543903', 'A', 5, 5, 3, 30, 17, 40, 'B', 'Normal', NULL, 'Truck', '50H-830.90', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSKU0728239', 'A', 9, 1, 1, 7, 8, 45, 'B', 'Damaged', 'Cửa container bị cong nhẹ', 'Truck', '50H-238.89', NULL, NULL, NULL, NULL, NULL, NULL),
    ('OOLU4937199', 'A', 9, 1, 2, 32, 17, 10, 'B', 'Normal', NULL, 'Barge', 'SG-7937', NULL, NULL, NULL, NULL, NULL, NULL),
    ('COSU6665160', 'A', 9, 1, 3, 7, 9, 0, 'A', 'Normal', NULL, 'Truck', '60C-321.81', NULL, NULL, NULL, NULL, NULL, NULL),
    ('COSU8011830', 'A', 25, 5, 1, 10, 14, 25, 'A', 'Normal', NULL, 'Truck', '50H-583.68', NULL, NULL, NULL, NULL, NULL, NULL),
    ('CMAU9240521', 'A', 25, 5, 2, 9, 11, 30, 'B', 'Normal', NULL, 'Truck', '51C-440.88', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSCU3987424', 'A', 25, 5, 3, 23, 9, 40, 'A', 'Normal', NULL, 'Truck', '61C-770.32', NULL, NULL, NULL, NULL, NULL, NULL),
    ('CMAU3908423', 'A', 27, 3, 1, 5, 8, 30, 'B', 'Normal', NULL, 'Truck', '60C-832.84', NULL, NULL, NULL, NULL, NULL, NULL),
    ('EGHU2380350', 'A', 27, 3, 2, 2, 14, 40, 'C', 'Normal', NULL, 'Truck', '29C-948.82', NULL, NULL, NULL, NULL, NULL, NULL),
    ('ONEU8902190', 'A', 17, 5, 1, 1, 11, 55, 'A', 'Normal', NULL, 'Truck', '50H-221.72', NULL, NULL, NULL, NULL, NULL, NULL),
    ('CMAU1052847', 'A', 17, 5, 2, 2, 13, 0, 'A', 'Normal', NULL, 'Truck', '51C-348.82', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSKU0448270', 'A', 5, 1, 1, 24, 8, 0, 'C', 'Normal', NULL, 'Truck', '72C-561.33', NULL, NULL, NULL, NULL, NULL, NULL),
    ('EGHU2262213', 'A', 5, 1, 2, 4, 9, 10, 'A', 'Normal', NULL, 'Truck', '72C-422.26', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSKU0152143', 'A', 9, 6, 1, 39, 8, 45, 'C', 'Normal', NULL, 'Truck', '61C-112.90', NULL, NULL, NULL, NULL, NULL, NULL),
    ('CMAU3823542', 'A', 9, 6, 2, 2, 11, 10, 'A', 'Normal', NULL, 'Truck', '50H-126.24', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSKU6181215', 'A', 1, 4, 1, 26, 12, 0, 'A', 'Normal', NULL, 'Truck', '50H-365.89', NULL, NULL, NULL, NULL, NULL, NULL),
    ('COSU3702260', 'A', 1, 4, 2, 4, 8, 10, 'A', 'Normal', NULL, 'Truck', '72C-396.34', NULL, NULL, NULL, NULL, NULL, NULL),
    ('HLXU3093858', 'A', 25, 3, 1, 5, 13, 45, 'C', 'Normal', NULL, 'Truck', '51C-685.90', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSKU0627845', 'A', 25, 3, 2, 5, 8, 45, 'B', 'Normal', NULL, 'Truck', '61C-637.12', NULL, NULL, NULL, NULL, NULL, NULL),
    ('OOLU7699284', 'A', 25, 3, 3, 20, 14, 30, 'A', 'Normal', NULL, 'Truck', '29C-317.91', NULL, NULL, NULL, NULL, NULL, NULL),
    ('HLXU1307204', 'A', 21, 4, 1, 22, 17, 55, 'A', 'Normal', NULL, 'Truck', '51D-776.77', NULL, NULL, NULL, NULL, NULL, NULL),
    ('CMAU6230258', 'A', 21, 4, 2, 35, 14, 25, 'A', 'Normal', NULL, 'Truck', '51C-346.65', NULL, NULL, NULL, NULL, NULL, NULL),
    ('CMAU0701047', 'A', 21, 4, 3, 32, 7, 55, 'A', 'Normal', NULL, 'Truck', '60C-104.98', NULL, NULL, NULL, NULL, NULL, NULL),
    ('ONEU0404863', 'A', 17, 6, 1, 26, 11, 15, 'A', 'Normal', NULL, 'Truck', '51D-270.79', NULL, NULL, NULL, NULL, NULL, NULL),
    ('EGHU8368645', 'A', 17, 6, 2, 9, 6, 0, 'A', 'Normal', NULL, 'Truck', '61C-935.35', NULL, NULL, NULL, NULL, NULL, NULL),
    ('EGHU0978446', 'A', 23, 6, 1, 25, 11, 15, 'A', 'Normal', NULL, 'Truck', '50H-355.58', NULL, NULL, NULL, NULL, NULL, NULL),
    ('HLXU4569337', 'A', 23, 6, 2, 14, 11, 55, 'A', 'Normal', NULL, 'Truck', '60C-491.56', NULL, NULL, NULL, NULL, NULL, NULL),
    ('HLXU6531270', 'A', 23, 6, 3, 3, 12, 10, 'C', 'Normal', NULL, 'Truck', '60C-788.35', NULL, NULL, NULL, NULL, NULL, NULL),
    ('CMAU9639023', 'A', 1, 6, 1, 9, 9, 55, 'A', 'Normal', NULL, 'Truck', '72C-166.29', NULL, NULL, NULL, NULL, NULL, NULL),
    ('ONEU0358691', 'A', 3, 5, 1, 23, 10, 30, 'B', 'Normal', NULL, 'Truck', '50H-376.78', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSKU7849865', 'A', 19, 2, 1, 26, 14, 25, 'B', 'Normal', NULL, 'Barge', 'SG-9290', NULL, NULL, NULL, NULL, NULL, NULL),
    ('OOLU4258393', 'A', 25, 4, 1, 2, 16, 10, 'B', 'Normal', NULL, 'Truck', '51C-149.41', NULL, NULL, NULL, NULL, NULL, NULL),
    ('COSU4773864', 'A', 25, 1, 1, 3, 12, 25, 'A', 'Normal', NULL, 'Barge', 'SG-3213', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSCU0566381', 'A', 9, 3, 1, 1, 17, 30, 'A', 'Normal', NULL, 'Truck', '51C-257.55', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSKU3144012', 'A', 11, 3, 1, 26, 13, 30, 'A', 'Normal', NULL, 'Truck', '60C-429.51', NULL, NULL, NULL, NULL, NULL, NULL),
    ('EGHU8461969', 'A', 11, 3, 2, 1, 13, 25, 'A', 'Normal', NULL, 'Truck', '61C-477.85', NULL, NULL, NULL, NULL, NULL, NULL),
    ('EGHU6567329', 'A', 11, 3, 3, 2, 11, 30, 'A', 'Normal', NULL, 'Truck', '29C-979.15', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSKU7050890', 'A', 27, 1, 1, 3, 13, 55, 'A', 'Normal', NULL, 'Truck', '51D-737.38', NULL, NULL, NULL, NULL, NULL, NULL),
    ('EGHU5317433', 'A', 25, 6, 1, 1, 14, 15, 'A', 'Normal', NULL, 'Truck', '61C-609.38', NULL, NULL, NULL, NULL, NULL, NULL),
    ('OOLU2262537', 'A', 25, 6, 2, 7, 10, 30, 'B', 'Normal', NULL, 'Truck', '72C-241.68', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSKU2612633', 'A', 3, 6, 1, 5, 17, 15, 'A', 'Normal', NULL, 'Truck', '72C-554.58', NULL, NULL, NULL, NULL, NULL, NULL),
    ('CMAU2671511', 'A', 3, 6, 2, 17, 6, 30, 'A', 'Normal', NULL, 'Truck', '51D-862.53', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSKU2033462', 'B', 6, 3, 1, 3, 10, 15, 'C', 'Normal', NULL, 'Truck', '29C-540.59', NULL, NULL, NULL, NULL, NULL, NULL),
    ('COSU3189291', 'B', 6, 3, 2, 6, 9, 30, 'A', 'Normal', NULL, 'Truck', '50H-674.26', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSCU6785026', 'B', 14, 6, 1, 15, 13, 45, 'B', 'Damaged', 'Sàn container thủng nhỏ', 'Truck', '61C-634.55', NULL, NULL, NULL, NULL, NULL, NULL),
    ('COSU8360581', 'B', 22, 5, 1, 5, 13, 10, 'C', 'Normal', NULL, 'Truck', '50H-300.12', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSKU0647630', 'B', 22, 5, 2, 0, 6, 45, 'A', 'Damaged', 'Cửa container bị cong nhẹ', 'Truck', '51C-810.21', NULL, NULL, NULL, NULL, NULL, NULL),
    ('HLXU9628330', 'B', 14, 1, 1, 7, 10, 25, 'A', 'Normal', NULL, 'Truck', '29C-602.69', NULL, NULL, NULL, NULL, NULL, NULL),
    ('EGHU6569553', 'B', 22, 2, 1, 12, 9, 40, 'A', 'Normal', NULL, 'Truck', '51D-695.52', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSKU6746728', 'B', 22, 2, 2, 35, 17, 15, 'B', 'Normal', NULL, 'Truck', '61C-691.47', NULL, NULL, NULL, NULL, NULL, NULL),
    ('COSU2649875', 'B', 2, 1, 1, 41, 9, 10, 'B', 'Normal', NULL, 'Truck', '29C-504.70', NULL, NULL, NULL, NULL, NULL, NULL),
    ('HLXU8322582', 'B', 2, 1, 2, 5, 8, 40, 'C', 'Normal', NULL, 'Truck', '51C-681.47', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSKU2116335', 'B', 6, 5, 1, 25, 6, 25, 'A', 'Normal', NULL, 'Truck', '51D-859.23', NULL, NULL, NULL, NULL, NULL, NULL),
    ('OOLU8853352', 'B', 6, 5, 2, 36, 8, 55, 'A', 'Normal', NULL, 'Truck', '51D-399.11', NULL, NULL, NULL, NULL, NULL, NULL),
    ('HLXU4183128', 'B', 2, 3, 1, 41, 15, 15, 'A', 'Normal', NULL, 'Truck', '51D-279.60', NULL, NULL, NULL, NULL, NULL, NULL),
    ('COSU7043432', 'B', 2, 3, 2, 5, 13, 40, 'A', 'Normal', NULL, 'Truck', '72C-358.90', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSCU4614019', 'B', 26, 3, 1, 41, 13, 40, 'A', 'Normal', NULL, 'Truck', '50H-978.38', NULL, NULL, NULL, NULL, NULL, NULL),
    ('HLXU3797760', 'B', 26, 3, 2, 32, 6, 15, 'B', 'Normal', NULL, 'Truck', '29C-108.11', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSCU6633065', 'B', 14, 2, 1, 8, 13, 45, 'A', 'Normal', NULL, 'Truck', '60C-339.67', NULL, NULL, NULL, NULL, NULL, NULL),
    ('CMAU7271307', 'B', 38, 1, 1, 36, 13, 10, 'C', 'Normal', NULL, 'Truck', '50H-166.32', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSCU9890149', 'B', 38, 1, 2, 14, 15, 30, 'A', 'Normal', NULL, 'Truck', '51C-218.87', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSKU5962677', 'C', 13, 3, 1, 7, 12, 30, 'A', 'Normal', NULL, 'Truck', '72C-991.70', NULL, NULL, NULL, NULL, NULL, NULL),
    ('EGHU8912996', 'C', 5, 4, 1, 1, 17, 55, 'A', 'Normal', NULL, 'Truck', '60C-705.25', NULL, NULL, NULL, NULL, NULL, NULL),
    ('CMAU3270180', 'C', 7, 2, 1, 31, 9, 25, 'A', 'Normal', NULL, 'Truck', '29C-174.29', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSCU4242673', 'C', 7, 2, 2, 12, 12, 40, 'A', 'Normal', NULL, 'Truck', '72C-285.34', NULL, NULL, NULL, NULL, NULL, NULL),
    ('CMAU4256130', 'C', 19, 4, 1, 7, 11, 55, 'A', 'Normal', NULL, 'Truck', '51D-402.82', NULL, NULL, NULL, NULL, NULL, NULL),
    ('ONEU3045089', 'C', 1, 3, 1, 40, 13, 40, 'B', 'Normal', NULL, 'Truck', '29C-790.76', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSKU2318275', 'C', 15, 3, 1, 5, 9, 30, 'A', 'Normal', NULL, 'Truck', '51D-292.25', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSCU1019964', 'C', 7, 3, 1, 1, 7, 30, 'A', 'Normal', NULL, 'Truck', '50H-433.71', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSCU4909370', 'C', 30, 1, 1, 15, 9, 25, 'B', 'Normal', NULL, 'Truck', '72C-601.31', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSKU0060617', 'C', 26, 6, 1, 6, 6, 55, 'A', 'Normal', NULL, 'Truck', '29C-160.62', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSKU8519024', 'C', 26, 6, 2, 13, 16, 55, 'B', 'Normal', NULL, 'Truck', '29C-492.71', NULL, NULL, NULL, NULL, NULL, NULL),
    ('HLXU3463931', 'C', 38, 6, 1, 28, 11, 15, 'B', 'Normal', NULL, 'Truck', '51C-763.11', NULL, NULL, NULL, NULL, NULL, NULL),
    ('EGHU6547641', 'C', 26, 2, 1, 24, 12, 0, 'A', 'Normal', NULL, 'Truck', '29C-973.72', NULL, NULL, NULL, NULL, NULL, NULL),
    ('COSU2403639', 'V', NULL, NULL, NULL, 10, 17, 30, 'A', 'Damaged', 'Móp thành container bên trái', 'Truck', '51D-113.76', NULL, NULL, NULL, NULL, NULL, NULL),
    ('OOLU4159765', 'V', NULL, NULL, NULL, 5, 6, 0, 'A', 'Normal', NULL, 'Truck', '29C-128.89', NULL, NULL, NULL, NULL, NULL, NULL),
    ('HLXU1628565', 'V', NULL, NULL, NULL, 13, 16, 10, 'A', 'Normal', NULL, 'Truck', '51D-926.58', NULL, NULL, NULL, NULL, NULL, NULL),
    ('MSKU0532816', 'V', NULL, NULL, NULL, 25, 10, 0, 'C', 'Normal', NULL, 'Barge', 'SG-1062', NULL, NULL, NULL, NULL, NULL, NULL)
) AS v(number, block, bay, row_no, tier, in_days, in_h, in_m, grade, cond, note, in_vtype, in_vnum,
    out_days, out_h, out_m, out_vtype, out_vnum, item)
JOIN "Containers" c ON c."Number" = v.number
JOIN "Blocks" b ON b."Code" = v.block
LEFT JOIN "Bays" y ON y."BlockId" = b."Id" AND y."BayNo" = v.bay
LEFT JOIN "YardRows" r ON r."BayId" = y."Id" AND r."RowNo" = v.row_no;

COMMIT;
