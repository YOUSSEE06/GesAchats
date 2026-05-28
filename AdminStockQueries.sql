-- Admin Stock Dashboard Queries (PostgreSQL)

-- 1. Dashboard Statistics Queries
-- Total number of products
SELECT COUNT(*) AS total_products FROM "Products";

-- Total stock quantity (sum of all current stocks)
SELECT COALESCE(SUM("CurrentStock"), 0) AS total_stock FROM "Products";

-- Number of products in OK state (stock > seuil_min)
SELECT COUNT(*) AS products_ok 
FROM "Products" 
WHERE "CurrentStock" > "MinimumStock";

-- Number of products in ALERT state (stock <= seuil_min AND stock > 0)
SELECT COUNT(*) AS products_alert 
FROM "Products" 
WHERE "CurrentStock" <= "MinimumStock" AND "CurrentStock" > 0;

-- Number of products in OUT OF STOCK state (stock = 0)
SELECT COUNT(*) AS products_out_of_stock 
FROM "Products" 
WHERE "CurrentStock" = 0;


-- 2. Products Table Query (with Magasin name)
SELECT 
    p."Id" AS product_id,
    p."Designation" AS designation,
    m."Nom" AS magasin_name,
    p."CurrentStock" AS current_stock,
    p."MinimumStock" AS minimum_threshold,
    p."Unit" AS unit,
    CASE 
        WHEN p."CurrentStock" = 0 THEN 'OUT OF STOCK'
        WHEN p."CurrentStock" <= p."MinimumStock" THEN 'ALERT'
        ELSE 'OK'
    END AS state
FROM "Products" p
LEFT JOIN "Magasins" m ON p."MagasinId" = m."Id"
ORDER BY p."Designation";


-- 3. Filtered by Magasin Query
SELECT 
    p."Id" AS product_id,
    p."Designation" AS designation,
    m."Nom" AS magasin_name,
    p."CurrentStock" AS current_stock,
    p."MinimumStock" AS minimum_threshold,
    p."Unit" AS unit,
    CASE 
        WHEN p."CurrentStock" = 0 THEN 'OUT OF STOCK'
        WHEN p."CurrentStock" <= p."MinimumStock" THEN 'ALERT'
        ELSE 'OK'
    END AS state
FROM "Products" p
LEFT JOIN "Magasins" m ON p."MagasinId" = m."Id"
WHERE m."Id" = @magasin_id  -- Replace @magasin_id with actual magasin ID
ORDER BY p."Designation";


-- 4. Get All Magasins
SELECT "Id", "Nom" FROM "Magasins" ORDER BY "Nom";
