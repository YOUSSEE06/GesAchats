-- 🛠️ SCRIPT DE RÉPARATION DU SCHÉMA DE LA BASE DE DONNÉES
-- Ce script corrige les colonnes manquantes et les noms de tables suite à la mise à jour

DO $$ 
BEGIN 
    -- 1. RENOMMAGE DES TABLES (si nécessaire)
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Invoices') AND 
       NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'factures') THEN
        ALTER TABLE "Invoices" RENAME TO factures;
        RAISE NOTICE 'Table Invoices renommée en factures';
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Payments') AND 
       NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'reglements') THEN
        ALTER TABLE "Payments" RENAME TO reglements;
        RAISE NOTICE 'Table Payments renommée en reglements';
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'DeliveryNotes') AND 
       NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'bons_livraison') THEN
        ALTER TABLE "DeliveryNotes" RENAME TO bons_livraison;
        RAISE NOTICE 'Table DeliveryNotes renommée en bons_livraison';
    END IF;

    -- 2. RÉPARATION DE LA TABLE "factures"
    -- Ajout de bc_id (Bon de Commande)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='bc_id') THEN
        ALTER TABLE factures ADD COLUMN bc_id INTEGER REFERENCES "PurchaseOrders"("Id");
        RAISE NOTICE 'Colonne bc_id ajoutée à factures';
    END IF;

    -- Ajout de bl_id (Bon de Livraison)
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='bl_id') THEN
        ALTER TABLE factures ADD COLUMN bl_id INTEGER;
        RAISE NOTICE 'Colonne bl_id ajoutée à factures';
    END IF;

    -- Renommage/Ajout des autres colonnes de factures si nécessaire
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='InvoiceNumber') THEN
        ALTER TABLE factures RENAME COLUMN "InvoiceNumber" TO numero_facture;
    END IF;
    
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='SupplierId') THEN
        ALTER TABLE factures RENAME COLUMN "SupplierId" TO fournisseur_id;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='InvoiceDate') THEN
        ALTER TABLE factures RENAME COLUMN "InvoiceDate" TO date_facture;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='AmountTTC') THEN
        ALTER TABLE factures RENAME COLUMN "AmountTTC" TO montant_ttc;
    END IF;

    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='Status') THEN
        ALTER TABLE factures RENAME COLUMN "Status" TO statut;
    END IF;

    -- Ajout des colonnes HT et TVA si manquantes
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='montant_ht') THEN
        ALTER TABLE factures ADD COLUMN montant_ht NUMERIC(18,2) DEFAULT 0;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='montant_tva') THEN
        ALTER TABLE factures ADD COLUMN montant_tva NUMERIC(18,2) DEFAULT 0;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='taux_tva') THEN
        ALTER TABLE factures ADD COLUMN taux_tva NUMERIC(18,2) DEFAULT 20.00;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='date_reception') THEN
        ALTER TABLE factures ADD COLUMN date_reception TIMESTAMP DEFAULT CURRENT_TIMESTAMP;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='cree_par') THEN
        -- Par défaut l'admin (Id=1)
        ALTER TABLE factures ADD COLUMN cree_par INTEGER REFERENCES "Users"("Id") DEFAULT 1;
    END IF;

    -- 3. RÉPARATION DE LA TABLE "bons_livraison"
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='bons_livraison' AND column_name='bc_id') THEN
        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='bons_livraison' AND column_name='PurchaseOrderId') THEN
            ALTER TABLE bons_livraison RENAME COLUMN "PurchaseOrderId" TO bc_id;
        ELSE
            ALTER TABLE bons_livraison ADD COLUMN bc_id INTEGER REFERENCES "PurchaseOrders"("Id");
        END IF;
    END IF;

    RAISE NOTICE '✅ Schéma de base de données réparé avec succès !';
END $$;
