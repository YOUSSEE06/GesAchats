-- 🐘 Script de création de la base de données GesAchats (PostgreSQL)
-- Généré le 30/04/2026

-- 1. Création des tables de base (Rôles et Utilisateurs)
CREATE TABLE "Roles" (
    "Id" SERIAL PRIMARY KEY,
    "Code" VARCHAR(50) NOT NULL UNIQUE,
    "Label" VARCHAR(100) NOT NULL,
    "Description" TEXT,
    "PermissionsJson" TEXT
);

CREATE TABLE "Users" (
    "Id" SERIAL PRIMARY KEY,
    "Login" VARCHAR(50) NOT NULL UNIQUE,
    "FullName" VARCHAR(100),
    "Email" VARCHAR(100),
    "PasswordHash" TEXT NOT NULL,
    "RoleId" INTEGER REFERENCES "Roles"("Id"),
    "IsActive" BOOLEAN DEFAULT TRUE,
    "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "LastLoginAt" TIMESTAMP
);

-- 2. Catalogue (Produits et Fournisseurs)
CREATE TABLE "Suppliers" (
    "Id" SERIAL PRIMARY KEY,
    "CompanyName" VARCHAR(100) NOT NULL,
    "Email" VARCHAR(100),
    "IsActive" BOOLEAN DEFAULT TRUE
);

CREATE TABLE "Products" (
    "Id" SERIAL PRIMARY KEY,
    "Designation" VARCHAR(200) NOT NULL,
    "Unit" VARCHAR(20) NOT NULL,
    "CurrentStock" DECIMAL(18,2) DEFAULT 0,
    "MinimumStock" DECIMAL(18,2) DEFAULT 0,
    "IsActive" BOOLEAN DEFAULT TRUE
);

-- 2.1 Besoins (Techniciens / Magasiniers)
CREATE TABLE "Needs" (
    "Id" SERIAL PRIMARY KEY,
    "Description" TEXT NOT NULL,
    "ProductId" INTEGER REFERENCES "Products"("Id"),
    "Quantity" DECIMAL(18,2),
    "Status" VARCHAR(20),
    "Justification" TEXT,
    "RequestedById" INTEGER REFERENCES "Users"("Id"),
    "RequestedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "ValidatedById" INTEGER REFERENCES "Users"("Id"),
    "CreatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP,
    "IsDeleted" BOOLEAN DEFAULT FALSE
);

-- 3. Cycle d'Achat (Devis, Commandes, Livraisons)
CREATE TABLE "Quotations" (
    "Id" SERIAL PRIMARY KEY,
    "ReferenceNumber" VARCHAR(50) NOT NULL,
    "SupplierId" INTEGER REFERENCES "Suppliers"("Id"),
    "ProductId" INTEGER REFERENCES "Products"("Id"),
    "Quantity" DECIMAL(18,2),
    "UnitPriceHT" DECIMAL(18,2),
    "Status" VARCHAR(20),
    "Date" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE "PurchaseOrders" (
    "Id" SERIAL PRIMARY KEY,
    "OrderNumber" VARCHAR(50) NOT NULL,
    "SupplierId" INTEGER REFERENCES "Suppliers"("Id"),
    "QuotationId" INTEGER REFERENCES "Quotations"("Id"),
    "TotalAmount" DECIMAL(18,2),
    "Status" VARCHAR(20),
    "OrderDate" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE "DeliveryNotes" (
    "Id" SERIAL PRIMARY KEY,
    "DeliveryNumber" VARCHAR(50) NOT NULL,
    "PurchaseOrderId" INTEGER REFERENCES "PurchaseOrders"("Id"),
    "SupplierId" INTEGER REFERENCES "Suppliers"("Id"),
    "ReceivedQuantity" DECIMAL(18,2),
    "CompliantQuantity" DECIMAL(18,2),
    "Status" VARCHAR(20),
    "ReceptionDate" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- 4. Finance (Factures et Paiements)
CREATE TABLE "Invoices" (
    "Id" SERIAL PRIMARY KEY,
    "InvoiceNumber" VARCHAR(50) NOT NULL,
    "SupplierId" INTEGER REFERENCES "Suppliers"("Id"),
    "AmountTTC" DECIMAL(18,2),
    "Status" VARCHAR(20),
    "InvoiceDate" TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE "Payments" (
    "Id" SERIAL PRIMARY KEY,
    "InvoiceId" INTEGER REFERENCES "Invoices"("Id"),
    "AmountPaid" DECIMAL(18,2),
    "PaymentDate" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    "PaymentMethod" VARCHAR(50)
);

-- 5. Seed Initial
INSERT INTO "Roles" ("Code", "Label", "Description") VALUES ('ADMIN', 'Administrateur', 'Administrateur Système');
INSERT INTO "Users" ("Login", "FullName", "Email", "PasswordHash", "RoleId") VALUES ('admin', 'Administrateur Système', 'admin@gesachats.com', '0000', 1);
