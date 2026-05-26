-- 📊 SCRIPT D'INITIALISATION DE L'HISTORIQUE DES PRIX ET ACHATS
-- Base de données : GesAchatsDb
-- Objectif : Valider l'interface "Historique des Prix et Achats"

-- 1. Nettoyage des données existantes (Optionnel, à utiliser avec prudence)
-- TRUNCATE "PurchaseOrders", "Quotations", "Products", "Suppliers" RESTART IDENTITY CASCADE;

-- 2. Insertion des Fournisseurs (Suppliers)
INSERT INTO "Suppliers" ("CompanyName", "Email", "IsActive") VALUES 
('BatiPro Solutions', 'contact@batipro.com', true),
('Global Matériaux', 'sales@globalmat.fr', true),
('Direct Quincaillerie', 'info@directquin.com', true)
ON CONFLICT DO NOTHING;

-- 3. Insertion des Produits (Products)
-- On s'aligne sur les désignations de la capture d'écran
INSERT INTO "Products" ("Designation", "Unit", "CurrentStock", "MinimumStock", "IsActive") VALUES 
('Ciment 35kg', 'Sac', 100, 20, true),
('Graviers 40mm', 'Tonne', 50, 10, true),
('Sable fin', 'm3', 30, 5, true),
('Acier HA8', 'Barre', 200, 50, true),
('Tuiles', 'Palette', 15, 5, true)
ON CONFLICT DO NOTHING;

-- 4. Insertion de l'Historique des Prix (via Quotations et PurchaseOrders)
-- Pour "Ciment 35kg" (Id 1 si base vide)
INSERT INTO "Quotations" ("ReferenceNumber", "SupplierId", "ProductId", "Quantity", "UnitPriceHT", "Status", "Date") VALUES 
('DEV-CIM-001', 1, 1, 50, 8.50, 'Validated', '2026-04-15 10:00:00'),
('DEV-CIM-002', 2, 1, 50, 9.20, 'Pending', '2026-04-20 11:00:00');

INSERT INTO "PurchaseOrders" ("OrderNumber", "SupplierId", "QuotationId", "TotalAmount", "Status", "OrderDate") VALUES 
('BC-CIM-001', 1, 1, 425.00, 'Issued', '2026-04-16 09:00:00');

-- Pour "Acier HA8" (Id 4 si base vide)
INSERT INTO "Quotations" ("ReferenceNumber", "SupplierId", "ProductId", "Quantity", "UnitPriceHT", "Status", "Date") VALUES 
('DEV-ACIER-001', 3, 4, 100, 4.50, 'Validated', '2026-04-10 14:00:00');

INSERT INTO "PurchaseOrders" ("OrderNumber", "SupplierId", "QuotationId", "TotalAmount", "Status", "OrderDate") VALUES 
('BC-ACIER-001', 3, 2, 450.00, 'Issued', '2026-04-11 15:00:00');

-- Pour "Tuiles" (Id 5 si base vide) - Cas limite : Prix à 0 ou non encore commandé
INSERT INTO "Quotations" ("ReferenceNumber", "SupplierId", "ProductId", "Quantity", "UnitPriceHT", "Status", "Date") VALUES 
('DEV-TUILE-001', 1, 5, 20, 0.00, 'Validated', '2026-05-01 08:30:00');

INSERT INTO "PurchaseOrders" ("OrderNumber", "SupplierId", "QuotationId", "TotalAmount", "Status", "OrderDate") VALUES 
('BC-TUILE-001', 1, 3, 0.00, 'Issued', '2026-05-02 10:00:00');

-- Cas Nominal : Plusieurs achats pour un même article pour voir l'évolution
INSERT INTO "Quotations" ("ReferenceNumber", "SupplierId", "ProductId", "Quantity", "UnitPriceHT", "Status", "Date") VALUES 
('DEV-CIM-003', 1, 1, 40, 8.75, 'Validated', '2026-05-01 10:00:00');

INSERT INTO "PurchaseOrders" ("OrderNumber", "SupplierId", "QuotationId", "TotalAmount", "Status", "OrderDate") VALUES 
('BC-CIM-002', 1, 4, 350.00, 'Issued', '2026-05-02 11:00:00');
