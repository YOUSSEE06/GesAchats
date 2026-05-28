-- =============================================
-- Base de données : GesAchatsDb
-- Système de gestion des achats
-- Généré pour PostgreSQL
-- =============================================

-- =============================================
-- Création des enums (types personnalisés)
-- =============================================
CREATE TYPE stock_state AS ENUM ('Ok', 'Alert', 'OutOfStock');
CREATE TYPE need_status AS ENUM ('Draft', 'ToValidate', 'TransmittedToPurchasing', 'Validated', 'InPurchase', 'Cancelled', 'Rejected', 'Relaunched');
CREATE TYPE need_reason AS ENUM ('RegularRestock', 'Urgency', 'Stockout', 'CriticalStock', 'SpecificProject');
CREATE TYPE need_priority AS ENUM ('Low', 'Normal', 'High');

-- =============================================
-- Table : Roles
-- =============================================
CREATE TABLE Roles (
    Id SERIAL PRIMARY KEY,
    Code VARCHAR(50) NOT NULL UNIQUE,
    Label VARCHAR(100) NOT NULL,
    Description TEXT,
    PermissionsJson TEXT
);

-- =============================================
-- Table : Users
-- =============================================
CREATE TABLE Users (
    Id SERIAL PRIMARY KEY,
    Login VARCHAR(50) NOT NULL UNIQUE,
    FullName VARCHAR(100),
    Email VARCHAR(100) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL,
    RoleId INT NOT NULL,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    LastLoginAt TIMESTAMP,
    FOREIGN KEY (RoleId) REFERENCES Roles(Id)
);

-- =============================================
-- Table : Suppliers
-- =============================================
CREATE TABLE Suppliers (
    Id SERIAL PRIMARY KEY,
    CompanyName VARCHAR(200) NOT NULL,
    ContactName VARCHAR(100),
    Email VARCHAR(100),
    Phone VARCHAR(20),
    Address TEXT,
    PostalCode VARCHAR(20),
    City VARCHAR(100),
    Country VARCHAR(100),
    PaymentConditions VARCHAR(100),
    AverageDeliveryDelay INT,
    Rating NUMERIC(3,2),
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- =============================================
-- Table : Magasins
-- =============================================
CREATE TABLE Magasins (
    Id SERIAL PRIMARY KEY,
    Nom VARCHAR(200) NOT NULL,
    IsActive BOOLEAN DEFAULT TRUE
);

-- =============================================
-- Table : Products
-- =============================================
CREATE TABLE Products (
    Id SERIAL PRIMARY KEY,
    Designation VARCHAR(200) NOT NULL,
    Unit VARCHAR(20) DEFAULT 'pcs',
    CurrentStock NUMERIC(18,2) NOT NULL,
    MinimumStock NUMERIC(18,2) NOT NULL,
    Category VARCHAR(100),
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    LastPurchaseDate TIMESTAMP,
    DailyConsumption NUMERIC(18,2) DEFAULT 1,
    IsNew BOOLEAN DEFAULT FALSE,
    CreatedBy VARCHAR(255),
    MagasinId INT,
    FOREIGN KEY (MagasinId) REFERENCES Magasins(Id)
);

-- =============================================
-- Table : Needs (Besoins)
-- =============================================
CREATE TABLE Needs (
    Id SERIAL PRIMARY KEY,
    NumeroBesoin VARCHAR(50) NOT NULL,
    Description TEXT NOT NULL,
    ProductId INT NOT NULL,
    Quantity NUMERIC(18,2) NOT NULL,
    Unit VARCHAR(20),
    Reason need_reason DEFAULT 'RegularRestock',
    Priority need_priority DEFAULT 'Normal',
    DesiredUrgencyDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    RequiredDelayDays INT DEFAULT 7,
    Notes TEXT,
    Status need_status DEFAULT 'Draft',
    Justification TEXT,
    RequestedById INT NOT NULL,
    RequestedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    ValidatedById INT,
    DateTransmission TIMESTAMP,
    DateCompletion TIMESTAMP,
    MotifRejet TEXT,
    DateRejet TIMESTAMP,
    History TEXT,
    FOREIGN KEY (ProductId) REFERENCES Products(Id),
    FOREIGN KEY (RequestedById) REFERENCES Users(Id),
    FOREIGN KEY (ValidatedById) REFERENCES Users(Id)
);

-- =============================================
-- Table : NeedDetails
-- =============================================
CREATE TABLE NeedDetails (
    Id SERIAL PRIMARY KEY,
    NeedId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity NUMERIC(18,2) NOT NULL,
    UnitPriceHT NUMERIC(18,2),
    IsNewProduct BOOLEAN DEFAULT FALSE,
    FOREIGN KEY (NeedId) REFERENCES Needs(Id) ON DELETE CASCADE,
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

-- =============================================
-- Table : Quotations (Devis)
-- =============================================
CREATE TABLE Quotations (
    Id SERIAL PRIMARY KEY,
    ReferenceNumber VARCHAR(50) NOT NULL UNIQUE,
    Date TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    SupplierId INT NOT NULL,
    NeedId INT,
    TotalAmountHT NUMERIC(18,2) NOT NULL,
    TotalAmountTTC NUMERIC(18,2) NOT NULL,
    ResponseDate TIMESTAMP,
    Observations TEXT,
    Status VARCHAR(50) DEFAULT 'En attente',
    CreatedById INT NOT NULL,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id),
    FOREIGN KEY (NeedId) REFERENCES Needs(Id),
    FOREIGN KEY (CreatedById) REFERENCES Users(Id)
);

-- =============================================
-- Table : QuotationDetails
-- =============================================
CREATE TABLE QuotationDetails (
    Id SERIAL PRIMARY KEY,
    QuotationId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity NUMERIC(18,2) NOT NULL,
    UnitPriceHT NUMERIC(18,2) NOT NULL,
    UnitPriceTTC NUMERIC(18,2) NOT NULL,
    DeliveryDelayDays INT,
    FOREIGN KEY (QuotationId) REFERENCES Quotations(Id) ON DELETE CASCADE,
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

-- =============================================
-- Table : PurchaseOrders (Bons de Commande)
-- =============================================
CREATE TABLE PurchaseOrders (
    Id SERIAL PRIMARY KEY,
    OrderNumber VARCHAR(50) NOT NULL UNIQUE,
    OrderDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    SupplierId INT NOT NULL,
    QuotationId INT,
    NeedId INT,
    TotalAmountHT NUMERIC(18,2) NOT NULL,
    TotalAmountTTC NUMERIC(18,2) NOT NULL,
    TotalVAT NUMERIC(18,2) NOT NULL,
    PaymentConditions VARCHAR(100),
    RequestedDeliveryDelay INT,
    Status VARCHAR(50) DEFAULT 'Draft',
    ExpectedDeliveryDate TIMESTAMP,
    CreatedById INT NOT NULL,
    Observations TEXT,
    CreatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id),
    FOREIGN KEY (QuotationId) REFERENCES Quotations(Id),
    FOREIGN KEY (NeedId) REFERENCES Needs(Id),
    FOREIGN KEY (CreatedById) REFERENCES Users(Id)
);

-- =============================================
-- Table : PurchaseOrderDetails
-- =============================================
CREATE TABLE PurchaseOrderDetails (
    Id SERIAL PRIMARY KEY,
    PurchaseOrderId INT NOT NULL,
    ProductId INT NOT NULL,
    Quantity NUMERIC(18,2) NOT NULL,
    UnitPriceHT NUMERIC(18,2) NOT NULL,
    UnitPriceTTC NUMERIC(18,2) NOT NULL,
    FOREIGN KEY (PurchaseOrderId) REFERENCES PurchaseOrders(Id) ON DELETE CASCADE,
    FOREIGN KEY (ProductId) REFERENCES Products(Id)
);

-- =============================================
-- Table : bons_livraison (Bons de Livraison)
-- =============================================
CREATE TABLE bons_livraison (
    id SERIAL PRIMARY KEY,
    numero_bl VARCHAR(50) NOT NULL UNIQUE,
    date_reception TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    fournisseur_id INT NOT NULL,
    bc_id INT NOT NULL,
    quantite_recue NUMERIC(18,2),
    quantite_conforme NUMERIC(18,2),
    quantite_defectueuse NUMERIC(18,2),
    observations TEXT,
    statut VARCHAR(50) DEFAULT 'Recu',
    fichier_pdf TEXT,
    recu_par INT NOT NULL,
    date_creation TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    date_maj TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (fournisseur_id) REFERENCES Suppliers(Id),
    FOREIGN KEY (bc_id) REFERENCES PurchaseOrders(Id) ON DELETE RESTRICT,
    FOREIGN KEY (recu_par) REFERENCES Users(Id)
);

-- =============================================
-- Table : bl_details (Détails des BL)
-- =============================================
CREATE TABLE bl_details (
    id SERIAL PRIMARY KEY,
    bl_id INT NOT NULL,
    produit_id INT NOT NULL,
    quantite_commandee NUMERIC(18,2) NOT NULL,
    quantite_livree NUMERIC(18,2) NOT NULL,
    prix_ht NUMERIC(18,2) NOT NULL,
    prix_ttc NUMERIC(18,2) NOT NULL,
    total NUMERIC(18,2) NOT NULL,
    valide BOOLEAN,
    FOREIGN KEY (bl_id) REFERENCES bons_livraison(id) ON DELETE CASCADE,
    FOREIGN KEY (produit_id) REFERENCES Products(Id)
);

-- =============================================
-- Table : factures (Factures)
-- =============================================
CREATE TABLE factures (
    id SERIAL PRIMARY KEY,
    numero_facture VARCHAR(50) NOT NULL UNIQUE,
    numero_facture_fournisseur VARCHAR(100),
    date_facture TIMESTAMP NOT NULL,
    date_reception TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    fournisseur_id INT NOT NULL,
    bc_id INT,
    bl_id INT,
    montant_ht NUMERIC(18,2) NOT NULL,
    taux_tva NUMERIC(18,2) DEFAULT 20.00,
    montant_tva NUMERIC(18,2) NOT NULL,
    montant_ttc NUMERIC(18,2) NOT NULL,
    statut VARCHAR(50) DEFAULT 'EnAttente',
    conformite VARCHAR(50) DEFAULT 'NonVerifiee',
    justification_conformite TEXT,
    observations TEXT,
    fichier_pdf TEXT,
    date_echeance TIMESTAMP,
    cree_par INT NOT NULL,
    date_creation TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    date_maj TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (fournisseur_id) REFERENCES Suppliers(Id),
    FOREIGN KEY (bc_id) REFERENCES PurchaseOrders(Id),
    FOREIGN KEY (bl_id) REFERENCES bons_livraison(id),
    FOREIGN KEY (cree_par) REFERENCES Users(Id)
);

-- =============================================
-- Table : facture_details (Détails des factures)
-- =============================================
CREATE TABLE facture_details (
    id SERIAL PRIMARY KEY,
    facture_id INT NOT NULL,
    produit_id INT NOT NULL,
    quantite NUMERIC(18,3) NOT NULL,
    pu_ht NUMERIC(18,2) NOT NULL,
    total_ht NUMERIC(18,2) NOT NULL,
    taux_tva NUMERIC(18,2) DEFAULT 20.00,
    total_ttc NUMERIC(18,2) NOT NULL,
    FOREIGN KEY (facture_id) REFERENCES factures(id) ON DELETE CASCADE,
    FOREIGN KEY (produit_id) REFERENCES Products(Id)
);

-- =============================================
-- Table : reglements (Paiements)
-- =============================================
CREATE TABLE reglements (
    id SERIAL PRIMARY KEY,
    numero_reglement VARCHAR(50) NOT NULL UNIQUE,
    facture_id INT NOT NULL,
    fournisseur_id INT NOT NULL,
    date_paiement TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    montant NUMERIC(18,2) NOT NULL,
    mode_paiement VARCHAR(50),
    reference VARCHAR(255),
    banque VARCHAR(100),
    fichier_preuve TEXT,
    type_fichier VARCHAR(20),
    fichier_recu TEXT,
    observations TEXT,
    cree_par INT NOT NULL,
    date_creation TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    date_maj TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (facture_id) REFERENCES factures(id),
    FOREIGN KEY (fournisseur_id) REFERENCES Suppliers(Id),
    FOREIGN KEY (cree_par) REFERENCES Users(Id)
);

-- =============================================
-- Table : AuditLogs
-- =============================================
CREATE TABLE AuditLogs (
    Id SERIAL PRIMARY KEY,
    UserId INT,
    Action VARCHAR(50) NOT NULL,
    EntityName VARCHAR(100) NOT NULL,
    EntityId INT NOT NULL,
    OldValues TEXT,
    NewValues TEXT,
    ActionDate TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    IpAddress VARCHAR(50),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- =============================================
-- Ajout des index
-- =============================================
CREATE INDEX idx_needs_status ON Needs(Status);
CREATE INDEX idx_needs_requested_at ON Needs(RequestedAt);
CREATE INDEX idx_products_is_active ON Products(IsActive);
CREATE INDEX idx_products_magasin_id ON Products(MagasinId);
CREATE INDEX idx_audit_logs_action_date ON AuditLogs(ActionDate);

-- =============================================
-- Fin du script
-- =============================================
