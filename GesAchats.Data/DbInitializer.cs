using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using GesAchats.Data.Context;

namespace GesAchats.Data;

public static class DbInitializer
{
    public static async Task SeedDataAsync(GesAchatsDbContext context)
    {
        // 1. Initialisation de la base (crée la base si elle n'existe pas)
        await context.Database.EnsureCreatedAsync();

        // Mettre à jour les statuts des bons de commande (seulement si la colonne Status existe)
        await context.Database.ExecuteSqlRawAsync(@"
            DO $$ 
            BEGIN 
                IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='PurchaseOrders' AND column_name='Status') THEN
                    BEGIN
                        -- Étape 1: Mettre à jour les statuts existants vers les nouveaux noms
                        UPDATE ""PurchaseOrders""
                        SET ""Status"" = 'En attente'
                        WHERE ""Status"" IN ('Issued', 'Pending', 'pending', 'issued');

                        UPDATE ""PurchaseOrders""
                        SET ""Status"" = 'Validé'
                        WHERE ""Status"" IN ('Validated', 'validated', 'Valide', 'valide', 'Accepted', 'accepted');

                        UPDATE ""PurchaseOrders""
                        SET ""Status"" = 'Annulé'
                        WHERE ""Status"" IN ('Cancelled', 'cancelled', 'Rejected', 'rejected');

                        -- Étape 2: Mettre à jour les bons de commande qui ont un BL lié à 'Validé'
                        UPDATE ""PurchaseOrders"" po
                        SET ""Status"" = 'Validé'
                        WHERE EXISTS (
                            SELECT 1 FROM bons_livraison bl
                            WHERE bl.bc_id = po.""Id""
                        );
                    EXCEPTION
                        WHEN undefined_column THEN
                            RAISE NOTICE 'Column Status does not exist in PurchaseOrders, skipping update';
                    END;
                END IF;
            END $$;
        ");

        // Mettre à jour les statuts des devis (seulement si la colonne Status existe)
        await context.Database.ExecuteSqlRawAsync(@"
            DO $$ 
            BEGIN 
                IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Quotations' AND column_name='Status') THEN
                    BEGIN
                        UPDATE ""Quotations""
                        SET ""Status"" = 'Validé'
                        WHERE ""Status"" IN ('Validated', 'Accepted', 'accepted', 'validated', 'Valide', 'valide', 'Sent');
                    EXCEPTION
                        WHEN undefined_column THEN
                            RAISE NOTICE 'Column Status does not exist in Quotations, skipping update';
                    END;
                END IF;
            END $$;
        ");

        // 1.1 Migration manuelle du schéma (FullName, etc.)
        await context.Database.ExecuteSqlRawAsync(@"
            -- Roles update
             ALTER TABLE ""Roles"" ADD COLUMN IF NOT EXISTS ""Code"" VARCHAR(50);
             ALTER TABLE ""Roles"" ADD COLUMN IF NOT EXISTS ""Label"" VARCHAR(100);
             
             -- On ne met à jour Code et Label que s'ils sont vides
             -- On utilise une constante par défaut si 'Name' n'existe pas
             DO $$ 
             BEGIN 
                 IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Roles' AND column_name='Name') THEN
                     UPDATE ""Roles"" SET ""Code"" = UPPER(""Name"") WHERE ""Code"" IS NULL;
                     UPDATE ""Roles"" SET ""Label"" = ""Name"" WHERE ""Label"" IS NULL;
                 END IF;
             END $$;
            
            -- Users update
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""FullName"" VARCHAR(100);
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""UpdatedAt"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP;
            ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""LastLoginAt"" TIMESTAMP;

            -- Needs update
            ALTER TABLE ""Needs"" ADD COLUMN IF NOT EXISTS ""Reason"" INTEGER DEFAULT 0;
            ALTER TABLE ""Needs"" ADD COLUMN IF NOT EXISTS ""Priority"" INTEGER DEFAULT 1;
            ALTER TABLE ""Needs"" ADD COLUMN IF NOT EXISTS ""DesiredUrgencyDate"" TIMESTAMP;
            ALTER TABLE ""Needs"" ADD COLUMN IF NOT EXISTS ""RequiredDelayDays"" INTEGER DEFAULT 0;
            ALTER TABLE ""Needs"" ADD COLUMN IF NOT EXISTS ""History"" TEXT;

            DO $$ 
            BEGIN 
                -- 1. RENOMMAGE DES TABLES (si nécessaire)
                IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Invoices') AND 
                   NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'factures') THEN
                    ALTER TABLE ""Invoices"" RENAME TO factures;
                END IF;

                IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Payments') AND 
                   NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'reglements') THEN
                    ALTER TABLE ""Payments"" RENAME TO reglements;
                END IF;

                IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'DeliveryNotes') AND 
                   NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'bons_livraison') THEN
                    ALTER TABLE ""DeliveryNotes"" RENAME TO bons_livraison;
                END IF;

                -- 2. RÉPARATION DE LA TABLE ""factures""
                IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'factures') THEN
                    -- Ajout de bc_id
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='bc_id') THEN
                        ALTER TABLE factures ADD COLUMN bc_id INTEGER;
                    END IF;

                    -- Ajout de bl_id
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='bl_id') THEN
                        ALTER TABLE factures ADD COLUMN bl_id INTEGER;
                    END IF;

                    -- Renommage/Ajout des autres colonnes
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='InvoiceNumber') THEN
                        ALTER TABLE factures RENAME COLUMN ""InvoiceNumber"" TO numero_facture;
                    END IF;
                    
                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='SupplierId') THEN
                        ALTER TABLE factures RENAME COLUMN ""SupplierId"" TO fournisseur_id;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='InvoiceDate') THEN
                        ALTER TABLE factures RENAME COLUMN ""InvoiceDate"" TO date_facture;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='AmountTTC') THEN
                        ALTER TABLE factures RENAME COLUMN ""AmountTTC"" TO montant_ttc;
                    END IF;

                    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='Status') THEN
                        ALTER TABLE factures RENAME COLUMN ""Status"" TO statut;
                    END IF;

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
                        ALTER TABLE factures ADD COLUMN cree_par INTEGER DEFAULT 1;
                    END IF;
                END IF;

                -- 3. RÉPARATION DE LA TABLE ""bons_livraison""
                IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'bons_livraison') THEN
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='bons_livraison' AND column_name='bc_id') THEN
                        IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='bons_livraison' AND column_name='PurchaseOrderId') THEN
                            ALTER TABLE bons_livraison RENAME COLUMN ""PurchaseOrderId"" TO bc_id;
                        ELSE
                            ALTER TABLE bons_livraison ADD COLUMN bc_id INTEGER;
                        END IF;
                    END IF;
                END IF;

                -- Needs table creation (Updated for History)
                IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Needs') THEN
                    CREATE TABLE ""Needs"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""NumeroBesoin"" VARCHAR(50) UNIQUE NOT NULL,
                        ""Description"" TEXT NOT NULL,
                        ""Status"" INTEGER DEFAULT 0,
                        ""Reason"" INTEGER DEFAULT 0,
                        ""Priority"" INTEGER DEFAULT 1,
                        ""DesiredUrgencyDate"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        ""RequiredDelayDays"" INTEGER DEFAULT 7,
                        ""Justification"" TEXT,
                        ""Notes"" TEXT,
                        ""RequestedById"" INTEGER REFERENCES ""Users""(""Id""),
                        ""RequestedAt"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        ""DateTransmission"" TIMESTAMP,
                        ""DateCompletion"" TIMESTAMP,
                        ""ValidatedById"" INTEGER REFERENCES ""Users""(""Id""),
                        ""CreatedAt"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        ""UpdatedAt"" TIMESTAMP,
                        ""IsDeleted"" BOOLEAN DEFAULT FALSE,
                        ""History"" TEXT,
                        ""ProductId"" INTEGER REFERENCES ""Products""(""Id""),
                        ""Quantity"" DECIMAL(18,2),
                        ""Unit"" VARCHAR(20)
                    );
                END IF;

                -- NeedDetails table for many-to-many relationship
                IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'NeedDetails') THEN
                    CREATE TABLE ""NeedDetails"" (
                        ""Id"" SERIAL PRIMARY KEY,
                        ""NeedId"" INTEGER REFERENCES ""Needs""(""Id"") ON DELETE CASCADE,
                        ""ProductId"" INTEGER REFERENCES ""Products""(""Id""),
                        ""Quantity"" DECIMAL(18,2) NOT NULL,
                        ""UnitPriceHT"" DECIMAL(18,2),
                        ""IsNewProduct"" BOOLEAN DEFAULT FALSE
                    );
                END IF;

                -- Ensure NumeroBesoin column exists if Needs table was old
                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Needs' AND column_name='NumeroBesoin') THEN
                    ALTER TABLE ""Needs"" ADD COLUMN ""NumeroBesoin"" VARCHAR(50);
                    UPDATE ""Needs"" SET ""NumeroBesoin"" = 'BES-' || LPAD(""Id""::text, 3, '0');
                    ALTER TABLE ""Needs"" ALTER COLUMN ""NumeroBesoin"" SET NOT NULL;
                    ALTER TABLE ""Needs"" ADD CONSTRAINT ""UQ_Needs_NumeroBesoin"" UNIQUE (""NumeroBesoin"");
                END IF;

                -- Fix Status column type if it's still VARCHAR
                IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Needs' AND column_name='Status' AND data_type='character varying') THEN
                    ALTER TABLE ""Needs"" ALTER COLUMN ""Status"" TYPE INTEGER USING (0);
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Needs' AND column_name='Reason') THEN
                    ALTER TABLE ""Needs"" ADD COLUMN ""Reason"" INTEGER DEFAULT 0;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Needs' AND column_name='Priority') THEN
                    ALTER TABLE ""Needs"" ADD COLUMN ""Priority"" INTEGER DEFAULT 1;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Needs' AND column_name='DesiredUrgencyDate') THEN
                    ALTER TABLE ""Needs"" ADD COLUMN ""DesiredUrgencyDate"" TIMESTAMP DEFAULT CURRENT_TIMESTAMP;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Needs' AND column_name='RequiredDelayDays') THEN
                    ALTER TABLE ""Needs"" ADD COLUMN ""RequiredDelayDays"" INTEGER DEFAULT 7;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Needs' AND column_name='Notes') THEN
                    ALTER TABLE ""Needs"" ADD COLUMN ""Notes"" TEXT;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Needs' AND column_name='ProductId') THEN
                    ALTER TABLE ""Needs"" ADD COLUMN ""ProductId"" INTEGER REFERENCES ""Products""(""Id"");
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Needs' AND column_name='Quantity') THEN
                    ALTER TABLE ""Needs"" ADD COLUMN ""Quantity"" DECIMAL(18,2);
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Needs' AND column_name='Unit') THEN
                    ALTER TABLE ""Needs"" ADD COLUMN ""Unit"" VARCHAR(20);
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Needs' AND column_name='DateTransmission') THEN
                    ALTER TABLE ""Needs"" ADD COLUMN ""DateTransmission"" TIMESTAMP;
                END IF;

                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Needs' AND column_name='DateCompletion') THEN
                    ALTER TABLE ""Needs"" ADD COLUMN ""DateCompletion"" TIMESTAMP;
                END IF;
            END $$;
        ");

        try 
        {
            // 2. Ajouter les rôles si inexistants
            if (!await context.Roles.AnyAsync())
            {
                var roles = new List<Role>
                {
                    new Role { Code = "ADMIN", Label = "Administrateur", Description = "Accès complet au système" },
                    new Role { Code = "ACHETEUR", Label = "Acheteur", Description = "Gestion des commandes et fournisseurs" },
                    new Role { Code = "MAGASINIER", Label = "Magasinier", Description = "Gestion des stocks et réceptions" },
                    new Role { Code = "COMPTABLE", Label = "Comptable", Description = "Gestion des factures et paiements" }
                };
                await context.Roles.AddRangeAsync(roles);
                await context.SaveChangesAsync();
            }

            // 3. Ajouter l'utilisateur admin si inexistant
            if (!await context.Users.AnyAsync(u => u.Login == "admin"))
            {
                var adminRole = await context.Roles.FirstAsync(r => r.Code == "ADMIN");
                var adminUser = new User
                {
                    Login = "admin",
                    FullName = "Administrateur Système",
                    Email = "admin@gesachats.com",
                    PasswordHash = "1234",
                    RoleId = adminRole.Id,
                    IsActive = true
                };
                await context.Users.AddAsync(adminUser);
                await context.SaveChangesAsync();
            }

            // 4. Ajouter des utilisateurs de test pour chaque rôle
            var rolesMap = await context.Roles.ToDictionaryAsync(r => r.Code);

            if (!await context.Users.AnyAsync(u => u.Login == "magasinier"))
            {
                await context.Users.AddAsync(new User
                {
                    Login = "magasinier",
                    FullName = "Jean Stock",
                    Email = "magasinier@gesachats.com",
                    PasswordHash = "1234",
                    RoleId = rolesMap["MAGASINIER"].Id,
                    IsActive = true
                });
            }

            if (!await context.Users.AnyAsync(u => u.Login == "acheteur"))
            {
                await context.Users.AddAsync(new User
                {
                    Login = "acheteur",
                    FullName = "Alice Achats",
                    Email = "acheteur@gesachats.com",
                    PasswordHash = "1234",
                    RoleId = rolesMap["ACHETEUR"].Id,
                    IsActive = true
                });
            }

            if (!await context.Users.AnyAsync(u => u.Login == "comptable"))
            {
                await context.Users.AddAsync(new User
                {
                    Login = "comptable",
                    FullName = "Marc Chiffre",
                    Email = "comptable@gesachats.com",
                    PasswordHash = "1234",
                    RoleId = rolesMap["COMPTABLE"].Id,
                    IsActive = true
                });
            }
            else
            {
                // Force update password to 1234 for test users if they already exist
                var usersToUpdate = await context.Users.Where(u => u.Login == "comptable" || u.Login == "acheteur" || u.Login == "magasinier" || u.Login == "admin").ToListAsync();
                foreach(var u in usersToUpdate) u.PasswordHash = "1234";
            }

            await context.SaveChangesAsync();

            // 4.5 Ajouter des magasins
            if (!await context.Magasins.AnyAsync())
            {
                var magasins = new List<Magasin>
                {
                    new Magasin { Nom = "Magasin Principal", IsActive = true },
                    new Magasin { Nom = "Magasin Annex", IsActive = true },
                    new Magasin { Nom = "Magasin Sud", IsActive = true }
                };
                await context.Magasins.AddRangeAsync(magasins);
                await context.SaveChangesAsync();
            }

            // 5. Articles de test (Ciment, Graviers, etc.)
            if (!await context.Products.AnyAsync())
            {
                var magasinPrincipal = await context.Magasins.FirstAsync(m => m.Nom == "Magasin Principal");
                var magasinAnnex = await context.Magasins.FirstAsync(m => m.Nom == "Magasin Annex");
                
                var products = new List<Product>
                {
                    new Product { Designation = "Ciment 35kg", CurrentStock = 10, MinimumStock = 50, Unit = "sac", Category = "Gros Oeuvre", MagasinId = magasinPrincipal.Id },
                    new Product { Designation = "Graviers 40mm", CurrentStock = 0, MinimumStock = 20, Unit = "m3", Category = "Granulats", MagasinId = magasinPrincipal.Id },
                    new Product { Designation = "Sable fin", CurrentStock = 100, MinimumStock = 30, Unit = "m3", Category = "Granulats", MagasinId = magasinAnnex.Id },
                    new Product { Designation = "Acier HA8", CurrentStock = 5, MinimumStock = 100, Unit = "barre", Category = "Ferraillage", MagasinId = magasinAnnex.Id },
                    new Product { Designation = "Tuiles", CurrentStock = 200, MinimumStock = 100, Unit = "pcs", Category = "Couverture", MagasinId = magasinPrincipal.Id }
                };
                await context.Products.AddRangeAsync(products);
                await context.SaveChangesAsync();
            }
            else
            {
                // Si des produits existent déjà, leur assigner un magasin par défaut
                var magasinPrincipal = await context.Magasins.FirstAsync(m => m.Nom == "Magasin Principal");
                var productsWithoutMagasin = await context.Products.Where(p => p.MagasinId == null).ToListAsync();
                foreach (var product in productsWithoutMagasin)
                {
                    product.MagasinId = magasinPrincipal.Id;
                }
                await context.SaveChangesAsync();
            }

            // 6. Besoins de test
            if (!await context.Needs.AnyAsync())
            {
                var magasinier = await context.Users.FirstAsync(u => u.Login == "magasinier");
                var ciment = await context.Products.FirstAsync(p => p.Designation == "Ciment 35kg");
                var graviers = await context.Products.FirstAsync(p => p.Designation == "Graviers 40mm");
                var acier = await context.Products.FirstAsync(p => p.Designation == "Acier HA8");

                var needs = new List<Need>
                {
                    new Need { NumeroBesoin = "#BES-001", Unit = "sac", Description = "Réappro Ciment", ProductId = ciment.Id, Quantity = 100, Status = NeedStatus.Draft, RequestedById = magasinier.Id, Reason = NeedReason.RegularRestock, Priority = NeedPriority.Normal },
                    new Need { NumeroBesoin = "#BES-002", Unit = "barre", Description = "Besoin urgent Acier", ProductId = acier.Id, Quantity = 500, Status = NeedStatus.Draft, RequestedById = magasinier.Id, Reason = NeedReason.Urgency, Priority = NeedPriority.High, DesiredUrgencyDate = DateTime.UtcNow.AddDays(2) },
                    new Need { NumeroBesoin = "#BES-003", Unit = "m3", Description = "Rupture Graviers", ProductId = graviers.Id, Quantity = 20, Status = NeedStatus.TransmittedToPurchasing, RequestedById = magasinier.Id, Reason = NeedReason.Stockout, Priority = NeedPriority.High, DateTransmission = DateTime.UtcNow },
                    new Need { NumeroBesoin = "#BES-004", Unit = "pcs", Description = "Besoin Tuiles", ProductId = (await context.Products.FirstAsync(p => p.Designation == "Tuiles")).Id, Quantity = 300, Status = NeedStatus.TransmittedToPurchasing, RequestedById = magasinier.Id, Reason = NeedReason.RegularRestock, Priority = NeedPriority.Low, DateTransmission = DateTime.UtcNow }
                };
                await context.Needs.AddRangeAsync(needs);
                await context.SaveChangesAsync();
            }

            // 7. Fournisseurs et Devis de test
            if (!await context.Suppliers.AnyAsync())
            {
                var suppliers = new List<Supplier>
                {
                    new Supplier { CompanyName = "Société Matériaux SA", Email = "contact@societemat.com", City = "Casablanca", IsActive = true, Rating = 4.5m, AverageDeliveryDelay = 3 },
                    new Supplier { CompanyName = "Cimenterie du Nord", Email = "sales@cimentnord.com", City = "Tanger", IsActive = true, Rating = 4.0m, AverageDeliveryDelay = 5 },
                    new Supplier { CompanyName = "Aciers Modernes", Email = "info@aciersmod.com", City = "Rabat", IsActive = true, Rating = 3.5m, AverageDeliveryDelay = 2 }
                };
                await context.Suppliers.AddRangeAsync(suppliers);
                await context.SaveChangesAsync();
            }

            // 8. Commandes de test
            if (!await context.PurchaseOrders.AnyAsync())
            {
                var acheteur = await context.Users.FirstAsync(u => u.Login == "acheteur");
                var supplier1 = await context.Suppliers.FirstAsync(s => s.CompanyName == "Société Matériaux SA");
                var supplier2 = await context.Suppliers.FirstAsync(s => s.CompanyName == "Cimenterie du Nord");

                var ciment = await context.Products.FirstAsync(p => p.Designation == "Ciment 35kg");
                var graviers = await context.Products.FirstAsync(p => p.Designation == "Graviers 40mm");
                var acier = await context.Products.FirstAsync(p => p.Designation == "Acier HA8");

                // Récupérer un besoin existant pour lier les devis
                var firstNeed = await context.Needs.FirstOrDefaultAsync() ?? new Need { Id = 1 };

                // Devis pour simulation
                var devis1 = new Quotation 
                { 
                    ReferenceNumber = "DEV-SIM-001", 
                    SupplierId = supplier1.Id, 
                    NeedId = firstNeed.Id, 
                    Status = QuotationStatus.Pending, 
                    TotalAmountHT = 1200, 
                    TotalAmountTTC = 1440,
                    CreatedById = acheteur.Id // Ajout de l'ID utilisateur
                };
                devis1.Details.Add(new QuotationDetail { ProductId = graviers.Id, Quantity = 20, UnitPriceHT = 60, UnitPriceTTC = 72 });
                
                var devis2 = new Quotation 
                { 
                    ReferenceNumber = "DEV-SIM-002", 
                    SupplierId = supplier2.Id, 
                    NeedId = firstNeed.Id, 
                    Status = QuotationStatus.Pending, 
                    TotalAmountHT = 1300, 
                    TotalAmountTTC = 1560,
                    CreatedById = acheteur.Id // Ajout de l'ID utilisateur
                };
                devis2.Details.Add(new QuotationDetail { ProductId = graviers.Id, Quantity = 20, UnitPriceHT = 65, UnitPriceTTC = 78 });

                await context.Quotations.AddRangeAsync(devis1, devis2);
                await context.SaveChangesAsync();

                var order1 = new PurchaseOrder 
                { 
                    OrderNumber = "BC-2026-0001", 
                    OrderDate = DateTime.UtcNow.AddDays(-5), 
                    SupplierId = supplier1.Id, 
                    Status = PurchaseOrderStatus.Pending,
                    ExpectedDeliveryDate = DateTime.UtcNow.AddDays(2),
                    CreatedById = acheteur.Id,
                    TotalAmountHT = 850,
                    TotalAmountTTC = 1020,
                    TotalVAT = 170,
                    PaymentConditions = "Net 30"
                };

                order1.Details.Add(new PurchaseOrderDetail { ProductId = ciment.Id, Quantity = 100, UnitPriceHT = 8.5m, UnitPriceTTC = 10.2m });

                var order2 = new PurchaseOrder 
                { 
                    OrderNumber = "BC-2026-0002", 
                    OrderDate = DateTime.UtcNow.AddDays(-2), 
                    SupplierId = supplier1.Id, 
                    Status = PurchaseOrderStatus.Validated,
                    ExpectedDeliveryDate = DateTime.UtcNow.AddDays(1),
                    CreatedById = acheteur.Id,
                    TotalAmountHT = 2250,
                    TotalAmountTTC = 2700,
                    TotalVAT = 450,
                    PaymentConditions = "Net 30"
                };

                order2.Details.Add(new PurchaseOrderDetail { ProductId = acier.Id, Quantity = 500, UnitPriceHT = 4.5m, UnitPriceTTC = 5.4m });

                await context.PurchaseOrders.AddRangeAsync(order1, order2);
            }

            await context.SaveChangesAsync();

            // 5. Insertion des données pour l'historique des prix (5 achats par produit)
            if (await context.Set<PurchaseOrderDetail>().CountAsync() < 10)
            {
                // Nettoyage pour éviter les doublons partiels
                context.Set<PurchaseOrderDetail>().RemoveRange(context.Set<PurchaseOrderDetail>());
                context.PurchaseOrders.RemoveRange(context.PurchaseOrders);
                context.Set<QuotationDetail>().RemoveRange(context.Set<QuotationDetail>());
                context.Quotations.RemoveRange(context.Quotations);
                await context.SaveChangesAsync();

                var suppliers = await context.Suppliers.ToListAsync();
                var products = await context.Products.ToListAsync();
                var random = new Random();

                foreach (var product in products)
                {
                    decimal basePrice = product.Designation switch
                    {
                        "Ciment 35kg" => 8.00m,
                        "Graviers 40mm" => 25.00m,
                        "Sable fin" => 45.00m,
                        "Acier HA8" => 5.00m,
                        "Tuiles" => 120.00m,
                        _ => 10.00m
                    };

                    for (int i = 1; i <= 5; i++)
                    {
                        var supplier = suppliers[random.Next(suppliers.Count)];
                        // Variation de prix de +/- 10%
                        decimal priceVariation = (decimal)(random.NextDouble() * 0.2 - 0.1);
                        decimal unitPrice = Math.Round(basePrice * (1 + priceVariation), 2);
                        int quantity = random.Next(10, 100);
                        DateTime date = DateTime.UtcNow.AddDays(-random.Next(1, 180));

                        var q = new Quotation 
                        { 
                            ReferenceNumber = $"DEV-{product.Id}-{i}", 
                            SupplierId = supplier.Id, 
                            TotalAmountHT = unitPrice * quantity, 
                            Status = QuotationStatus.Pending, 
                            Date = date.AddDays(-1), 
                            CreatedById = 1 
                        };
                        await context.Quotations.AddAsync(q);
                        await context.SaveChangesAsync();

                        var qd = new QuotationDetail 
                        { 
                            QuotationId = q.Id, 
                            ProductId = product.Id, 
                            Quantity = quantity, 
                            UnitPriceHT = unitPrice 
                        };
                        await context.Set<QuotationDetail>().AddAsync(qd);

                        var bc = new PurchaseOrder 
                        { 
                            OrderNumber = $"BC-{product.Id}-{i}", 
                            SupplierId = supplier.Id, 
                            QuotationId = q.Id, 
                            TotalAmountHT = unitPrice * quantity, 
                            Status = "Issued", 
                            OrderDate = date, 
                            CreatedById = 1 
                        };
                        await context.PurchaseOrders.AddAsync(bc);
                        await context.SaveChangesAsync();

                        var bcd = new PurchaseOrderDetail 
                        { 
                            PurchaseOrderId = bc.Id, 
                            ProductId = product.Id, 
                            Quantity = quantity, 
                            UnitPriceHT = unitPrice 
                        };
                        await context.Set<PurchaseOrderDetail>().AddAsync(bcd);
                    }
                }
                await context.SaveChangesAsync();
            }

            // 7. Initialisation des tables Bons de Livraison (BL)
            await context.Database.ExecuteSqlRawAsync(@"
                DO $$ 
                BEGIN 
                    -- Table bons_livraison
                    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'bons_livraison') THEN
                        CREATE TABLE bons_livraison (
                            id SERIAL PRIMARY KEY,
                            numero_bl VARCHAR(50) NOT NULL UNIQUE,
                            date_reception TIMESTAMP WITHOUT TIME ZONE NOT NULL,
                            fournisseur_id INTEGER NOT NULL REFERENCES ""Suppliers""(""Id""),
                            bc_id INTEGER NOT NULL REFERENCES ""PurchaseOrders""(""Id"") ON DELETE RESTRICT,
                            fichier_pdf TEXT,
                            observations TEXT,
                            ""Status"" VARCHAR(50) DEFAULT 'Pending',
                            ""ReceivedQuantity"" DECIMAL(18,2) DEFAULT 0,
                            ""CompliantQuantity"" DECIMAL(18,2) DEFAULT 0,
                            ""DefectiveQuantity"" DECIMAL(18,2) DEFAULT 0,
                            ""ReceivedById"" INTEGER REFERENCES ""Users""(""Id""),
                            ""CreatedAt"" TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,
                            ""UpdatedAt"" TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
                        );
                    END IF;

                    -- Table bl_details
                    IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'bl_details') THEN
                        CREATE TABLE bl_details (
                            id SERIAL PRIMARY KEY,
                            bl_id INTEGER NOT NULL REFERENCES bons_livraison(id) ON DELETE CASCADE,
                            produit_id INTEGER NOT NULL REFERENCES ""Products""(""Id""),
                            quantite_commandee DECIMAL(18,2) NOT NULL,
                            quantite_livree DECIMAL(18,2) NOT NULL,
                            prix_ht DECIMAL(18,2) NOT NULL,
                            prix_ttc DECIMAL(18,2) NOT NULL,
                            total DECIMAL(18,2) NOT NULL,
                            valide BOOLEAN NOT NULL DEFAULT FALSE
                        );
                    END IF;
                END $$;
            ");
            
            // --- 8. Ajout des Bons de Commande validés pour le graphique ---
            // Vérifier qu'on a assez de fournisseurs et produits
            var testAcheteur = await context.Users.FirstOrDefaultAsync(u => u.Login == "acheteur");
            var testSuppliers = await context.Suppliers.ToListAsync();
            var testProducts = await context.Products.ToListAsync();
            
            if (testAcheteur != null && testSuppliers.Any() && testProducts.Any())
            {
                // Vérifier si on a déjà nos BC de test (éviter les doublons)
                var testBcExists = await context.PurchaseOrders
                    .AnyAsync(po => po.OrderNumber.StartsWith("BC-TEST-"));
                
                if (!testBcExists)
                {
                    var random = new Random();
                    
                    // Données mensuelles cibles
                    var monthlyTestData = new List<(int year, int month, decimal totalTtc)>
                    {
                        (2025, 9, 85000m),
                        (2025, 10, 120000m),
                        (2025, 11, 95000m),
                        (2025, 12, 160000m),
                        (2026, 1, 130000m),
                        (2026, 2, 210000m),
                        (2026, 3, 175000m),
                        (2026, 4, 240000m),
                        (2026, 5, 300000m)
                    };
                    
                    int bcIndex = 1;
                    foreach (var (year, month, totalTtc) in monthlyTestData)
                    {
                        var supplier = testSuppliers[random.Next(testSuppliers.Count)];
                        var orderDate = new DateTime(year, month, 15, 10, 0, 0, DateTimeKind.Utc);
                        decimal tvaRate = 0.20m;
                        decimal totalHt = Math.Round(totalTtc / (1 + tvaRate), 2);
                        decimal totalVat = Math.Round(totalTtc - totalHt, 2);
                        
                        // Créer le BC
                        var bc = new PurchaseOrder
                        {
                            OrderNumber = $"BC-TEST-{bcIndex:D4}",
                            OrderDate = orderDate,
                            SupplierId = supplier.Id,
                            Status = PurchaseOrderStatus.Validated,
                            CreatedById = testAcheteur.Id,
                            TotalAmountHT = totalHt,
                            TotalAmountTTC = totalTtc,
                            TotalVAT = totalVat,
                            PaymentConditions = "Net 30 jours",
                            CreatedAt = orderDate,
                            UpdatedAt = orderDate
                        };
                        
                        // Ajouter un détail minimal pour le BC
                        var product = testProducts[random.Next(testProducts.Count)];
                        decimal basePrice = product.Designation switch
                        {
                            "Ciment 35kg" => 8.00m,
                            "Graviers 40mm" => 25.00m,
                            "Sable fin" => 45.00m,
                            "Acier HA8" => 5.00m,
                            "Tuiles" => 120.00m,
                            _ => 10.00m
                        };
                        int qty = Math.Max(1, (int)(totalHt / basePrice));
                        decimal unitPriceHt = Math.Round(totalHt / qty, 2);
                        decimal unitPriceTtc = Math.Round(unitPriceHt * (1 + tvaRate), 2);
                        
                        bc.Details.Add(new PurchaseOrderDetail
                        {
                            ProductId = product.Id,
                            Quantity = qty,
                            UnitPriceHT = unitPriceHt,
                            UnitPriceTTC = unitPriceTtc
                        });
                        
                        await context.PurchaseOrders.AddAsync(bc);
                        bcIndex++;
                    }
                    
                    await context.SaveChangesAsync();

                    // --- 9. Ajout des Bons de Livraison de test ---
                    var testMagasinier = await context.Users.FirstOrDefaultAsync(u => u.Login == "magasinier");
                    if (testMagasinier != null)
                    {
                        // Check if test BLs already exist
                        var testBlExists = await context.DeliveryNotes
                            .AnyAsync(bl => bl.DeliveryNumber.StartsWith("BL-TEST-"));

                        if (!testBlExists)
                        {
                            var randomBl = new Random();
                            // Get all test BCs
                            var testBcs = await context.PurchaseOrders
                                .Where(po => po.OrderNumber.StartsWith("BC-TEST-"))
                                .ToListAsync();

                            if (testBcs.Any())
                            {
                                // Create 3 test BLs with recent dates
                                var blDates = new List<DateTime>
                                {
                                    DateTime.UtcNow.AddDays(-1),
                                    DateTime.UtcNow.AddDays(-3),
                                    DateTime.UtcNow.AddDays(-7)
                                };

                                for (int i = 1; i <= 3; i++)
                                {
                                    var bc = testBcs[randomBl.Next(testBcs.Count)];
                                    var firstDetail = bc.Details.FirstOrDefault();
                                    var receivedQty = firstDetail != null ? firstDetail.Quantity : 100;

                                    var bl = new DeliveryNote
                                    {
                                        DeliveryNumber = $"BL-TEST-{i:D4}",
                                        ReceptionDate = blDates[i - 1],
                                        PurchaseOrderId = bc.Id,
                                        SupplierId = bc.SupplierId,
                                        ReceivedQuantity = receivedQty,
                                        CompliantQuantity = receivedQty,
                                        DefectiveQuantity = 0,
                                        Status = i == 1 ? "Validé" : "En attente",
                                        ReceivedById = testMagasinier.Id,
                                        CreatedAt = blDates[i - 1],
                                        UpdatedAt = blDates[i - 1]
                                    };

                                    if (firstDetail != null)
                                    {
                                        bl.Details.Add(new DeliveryNoteDetail
                                        {
                                            ProductId = firstDetail.ProductId,
                                            QuantityOrdered = firstDetail.Quantity,
                                            QuantityReceived = receivedQty,
                                            UnitPriceHT = firstDetail.UnitPriceHT,
                                            UnitPriceTTC = firstDetail.UnitPriceTTC,
                                            Total = receivedQty * firstDetail.UnitPriceTTC,
                                            IsValidated = true
                                        });
                                    }

                                    await context.DeliveryNotes.AddAsync(bl);
                                }

                                await context.SaveChangesAsync();
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var message = $"Erreur lors de l'initialisation de la base de données : {ex.Message}";
            if (ex.InnerException != null) message += $"\n\nInner Exception : {ex.InnerException.Message}";
            
            throw new Exception(message + "\n\nSi vous avez récemment mis à jour l'application, vous pouvez essayer de supprimer la base de données PostgreSQL pour qu'elle soit recréée proprement.", ex);
        }
    }
}
