-- 🧪 Script de Données de Test pour GesAchats v2.0
-- Ce script peuple la base de données PostgreSQL pour tester toutes les interfaces

-- 1. Nettoyage
TRUNCATE "Payments", "Invoices", "DeliveryNotes", "PurchaseOrders", "Quotations", "Products", "Suppliers" RESTART IDENTITY CASCADE;

-- 2. Fournisseurs (Suppliers)
INSERT INTO "Suppliers" ("CompanyName", "Email", "IsActive", "CreatedAt", "UpdatedAt") VALUES 
('BatiConstruction SA', 'contact@baticonstruct.fr', true, NOW(), NOW()),
('Quincaillerie Pro', 'ventes@quincapro.com', true, NOW(), NOW()),
('ÉlecDirect', 'support@elecdirect.net', true, NOW(), NOW());

-- 3. Produits (Products)
INSERT INTO "Products" ("Designation", "Unit", "CurrentStock", "MinimumStock", "IsActive", "CreatedAt", "UpdatedAt") VALUES 
('Ciment Sac 35kg', 'Sac', 50.00, 20.00, true, NOW(), NOW()),
('Brique Rouge Standard', 'Unité', 1200.00, 500.00, true, NOW(), NOW()),
('Câble Électrique 2.5mm (100m)', 'Rouleau', 15.00, 5.00, true, NOW(), NOW()),
('Peinture Blanche 10L', 'Pot', 8.00, 10.00, true, NOW(), NOW()), -- En dessous du seuil
('Perceuse à Percussion 800W', 'Unité', 3.00, 2.00, true, NOW(), NOW());

-- 4. Devis (Quotations)
INSERT INTO "Quotations" ("ReferenceNumber", "SupplierId", "ProductId", "Quantity", "UnitPriceHT", "UnitPriceTTC", "TotalAmountTTC", "Status", "Date", "CreatedAt", "UpdatedAt", "CreatedById") VALUES 
('DEV-2026-001', 1, 1, 100.00, 7.50, 9.00, 900.00, 'Pending', NOW(), NOW(), NOW(), 1),
('DEV-2026-002', 2, 5, 5.00, 85.00, 102.00, 510.00, 'Validated', NOW(), NOW(), NOW(), 1),
('DEV-2026-003', 3, 3, 20.00, 45.00, 54.00, 1080.00, 'Pending', NOW(), NOW(), NOW(), 1);

-- 5. Bons de Commande (PurchaseOrders)
-- Note: Lié au Devis 2 (Validated)
INSERT INTO "PurchaseOrders" ("OrderNumber", "SupplierId", "QuotationId", "ProductId", "Quantity", "UnitPriceHT", "TotalAmount", "Status", "OrderDate", "CreatedAt", "UpdatedAt", "CreatedById") VALUES 
('BC-2026-001', 2, 2, 5, 5.00, 85.00, 425.00, 'Draft', NOW(), NOW(), NOW(), 1),
('BC-2026-002', 1, NULL, 1, 100.00, 7.50, 750.00, 'Issued', NOW(), NOW(), NOW(), 1); -- Commande directe sans devis

-- 6. Bons de Livraison (DeliveryNotes)
-- Note: Lié au BC 2
INSERT INTO "DeliveryNotes" ("DeliveryNumber", "PurchaseOrderId", "SupplierId", "ReceivedQuantity", "CompliantQuantity", "DefectiveQuantity", "Status", "ReceptionDate", "CreatedAt", "UpdatedAt", "ReceivedById") VALUES 
('BL-998877', 2, 1, 100.00, 98.00, 2.00, 'Pending', NOW(), NOW(), NOW(), 1);

-- 7. Factures (Invoices)
INSERT INTO "Invoices" ("InvoiceNumber", "SupplierId", "AmountHT", "TaxAmount", "AmountTTC", "Status", "InvoiceDate", "CreatedAt", "UpdatedAt", "CreatedById") VALUES 
('FAC-2026-450', 1, 750.00, 150.00, 900.00, 'Pending', NOW(), NOW(), NOW(), 1),
('FAC-2026-451', 3, 100.00, 20.00, 120.00, 'Pending', NOW(), NOW(), NOW(), 1);

-- 8. Message de confirmation
DO $$ BEGIN RAISE NOTICE '✅ Données de test insérées avec succès !'; END $$;
