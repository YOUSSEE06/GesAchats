-- 🛠️ SCRIPT DE MIGRATION AUTOMATIQUE GESACHATS
DO $$ 
BEGIN 
    -- 1. MISE À JOUR DE LA TABLE "Roles"
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Roles' AND column_name='Code') THEN
        ALTER TABLE "Roles" ADD COLUMN "Code" VARCHAR(50);
        UPDATE "Roles" SET "Code" = UPPER("Name") WHERE "Code" IS NULL;
        ALTER TABLE "Roles" ALTER COLUMN "Code" SET NOT NULL;
        IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'UQ_Roles_Code') THEN
            ALTER TABLE "Roles" ADD CONSTRAINT "UQ_Roles_Code" UNIQUE ("Code");
        END IF;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Roles' AND column_name='Label') THEN
        ALTER TABLE "Roles" ADD COLUMN "Label" VARCHAR(100);
        UPDATE "Roles" SET "Label" = "Name" WHERE "Label" IS NULL;
    END IF;

    -- 2. MISE À JOUR DE LA TABLE "Users"
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Users' AND column_name='FullName') THEN
        ALTER TABLE "Users" ADD COLUMN "FullName" VARCHAR(100);
        UPDATE "Users" SET "FullName" = "Login" WHERE "FullName" IS NULL;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Users' AND column_name='UpdatedAt') THEN
        ALTER TABLE "Users" ADD COLUMN "UpdatedAt" TIMESTAMP DEFAULT CURRENT_TIMESTAMP;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Users' AND column_name='LastLoginAt') THEN
        ALTER TABLE "Users" ADD COLUMN "LastLoginAt" TIMESTAMP;
    END IF;

    -- 3. AJOUT DE LA TABLE "Needs" SI MANQUANTE
    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Needs') THEN
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
    END IF;

    RAISE NOTICE '✅ Migration terminée avec succès !';
END $$;
