-- =============================================================================
-- GESACHATS - SEED COMPLET DE DEMONSTRATION
-- PostgreSQL / Supabase - Schema EF Core (PascalCase pour les tables standard,
-- snake_case pour bons_livraison / bl_details / factures / facture_details / reglements)
--
-- CHAINE DE RELATIONS (strictement respectee) :
--   Besoin (Needs + NeedDetails)
--     -> Devis (Quotations + QuotationDetails)
--     -> Bon de Commande (PurchaseOrders + PurchaseOrderDetails)
--     -> Bon de Livraison (bons_livraison + bl_details)
--     -> Facture (factures + facture_details)
--     -> Reglements (reglements)
--
-- CONTENU : 10 fournisseurs, 20 produits, 5 besoins, 5 devis,
--           5 BC, 5 BL, 5 factures, 10 reglements.
--
-- STATUTS METIER UTILISES PAR L APPLICATION :
--   Devis      : 'En attente' | 'Valide'
--   BC         : 'En attente' | 'Valide' | 'Annule'
--   BL         : 'EnAttente'  | 'Valide'
--   Facture    : 'EnAttente' | 'Verifiee' | 'Partiellement payee' | 'Payee' | 'Rejetee'
--   Conformite : 'NonVerifiee' | 'Conforme' | 'EcartMineur' | 'NonConforme'
--   Reglement  : 'EnAttente' | 'Valide' | 'Rejete'
--   Besoin     : int enum NeedStatus (0=Draft 1=ToValidate 2=TransmittedToPurchasing
--                3=Validated 4=InPurchase 5=Cancelled 6=Rejected 7=Relaunched)
--
-- EXECUTION : a lancer dans le SQL Editor Supabase (ou psql), sur une base vide
-- ou existante. Le script est IDEMPOTENT (ON CONFLICT DO NOTHING) : les lignes
-- deja presentes ne sont pas modifiees. Les sequences sont resynchronisees a la
-- fin pour eviter tout conflit d ID avec l application.
-- =============================================================================

BEGIN;

-- =============================================================================
-- 0. ROLES + UTILISATEURS DE SEED (references FK des chaines)
--    Ces comptes ne servent qu a l integrite referentielle. L authentification
--    reelle passe par Supabase Auth (SupabaseAuthId).
-- =============================================================================

INSERT INTO public."Roles" ("Code", "Label", "Description")
VALUES
    ('ADMIN',      'Administrateur', 'Administration complete du systeme'),
    ('ACHETEUR',   'Acheteur',       'Gestion des besoins, devis et bons de commande'),
    ('MAGASINIER', 'Magasinier',     'Reception des livraisons et gestion des stocks'),
    ('COMPTABLE',  'Comptable',      'Factures et reglements')
ON CONFLICT ("Code") DO NOTHING;

INSERT INTO public."Users" ("Login", "FullName", "Email", "PasswordHash", "RoleId", "IsActive", "CreatedAt", "UpdatedAt")
SELECT 'seed.admin',      'Admin Seed',      'seed.admin@gesachats.demo', 'SEED_DEMO_PASSWORD_HASH_DISABLED',
       r."Id", FALSE, '2026-05-01 08:00:00+00', '2026-05-01 08:00:00+00'
FROM public."Roles" r WHERE r."Code" = 'ADMIN'
ON CONFLICT ("Login") DO NOTHING;

INSERT INTO public."Users" ("Login", "FullName", "Email", "PasswordHash", "RoleId", "IsActive", "CreatedAt", "UpdatedAt")
SELECT 'seed.acheteur',   'Acheteur Seed',   'seed.acheteur@gesachats.demo', 'SEED_DEMO_PASSWORD_HASH_DISABLED',
       r."Id", FALSE, '2026-05-01 08:00:00+00', '2026-05-01 08:00:00+00'
FROM public."Roles" r WHERE r."Code" = 'ACHETEUR'
ON CONFLICT ("Login") DO NOTHING;

INSERT INTO public."Users" ("Login", "FullName", "Email", "PasswordHash", "RoleId", "IsActive", "CreatedAt", "UpdatedAt")
SELECT 'seed.magasinier', 'Magasinier Seed', 'seed.magasinier@gesachats.demo', 'SEED_DEMO_PASSWORD_HASH_DISABLED',
       r."Id", FALSE, '2026-05-01 08:00:00+00', '2026-05-01 08:00:00+00'
FROM public."Roles" r WHERE r."Code" = 'MAGASINIER'
ON CONFLICT ("Login") DO NOTHING;

INSERT INTO public."Users" ("Login", "FullName", "Email", "PasswordHash", "RoleId", "IsActive", "CreatedAt", "UpdatedAt")
SELECT 'seed.comptable',  'Comptable Seed',  'seed.comptable@gesachats.demo', 'SEED_DEMO_PASSWORD_HASH_DISABLED',
       r."Id", FALSE, '2026-05-01 08:00:00+00', '2026-05-01 08:00:00+00'
FROM public."Roles" r WHERE r."Code" = 'COMPTABLE'
ON CONFLICT ("Login") DO NOTHING;

-- =============================================================================
-- 1. FOURNISSEURS (10)
-- =============================================================================

INSERT INTO public."Suppliers"
    ("Id", "CompanyName", "ContactName", "Email", "Phone", "Address", "PostalCode",
     "City", "Country", "PaymentConditions", "AverageDeliveryDelay", "Rating", "IsActive", "CreatedAt", "UpdatedAt")
VALUES
    (1, 'ATLAS DISTRIBUTION SARL', 'Karim El Fassi',  'k.elfassi@atlasdistribution.ma', '0522-451230', '12 Rue des Orangers, Zone Industrielle Sidi Bernoussi', '20240', 'Casablanca', 'Maroc', '30 jours fin de mois', 12, 4.5, TRUE, '2026-05-01 08:00:00+00', '2026-05-01 08:00:00+00'),
    (2, 'MAROC ELECTRO SAS',       'Youssef Benali',  'y.benali@maroclectro.ma',        '0522-887456', '45 Bd Moulay Slimane, Quartier Ain Sebaa',           '20250', 'Casablanca', 'Maroc', '45 jours',             15, 4.2, TRUE, '2026-05-01 08:00:00+00', '2026-05-01 08:00:00+00'),
    (3, 'PLOMB & CO',              'Rachid Amrani',   'r.amrani@plombeco.ma',            '0537-669812', '8 Av de la Victoire',                                  '10030', 'Rabat',      'Maroc', '30 jours',             10, 4.0, TRUE, '2026-05-01 08:00:00+00', '2026-05-01 08:00:00+00'),
    (4, 'OUTILS MODERNES SA',      'Mehdi Tazi',      'm.tazi@outilsmodernes.ma',        '0539-335677', 'Zone Franche, Lot 14',                                  '90000', 'Tanger',     'Maroc', '60 jours',             20, 4.7, TRUE, '2026-05-01 08:00:00+00', '2026-05-01 08:00:00+00'),
    (5, 'TECHNO FOURNITURES',      'Salma Idrissi',   's.idrissi@technofournitures.ma',  '0522-901234', '3 Rue Ibn Sina, Quartier Maarif',                       '20100', 'Casablanca', 'Maroc', '30 jours',              7, 4.6, TRUE, '2026-05-01 08:00:00+00', '2026-05-01 08:00:00+00'),
    (6, 'BATIR PLUS',              'Omar Chraibi',    'o.chraibi@batirplus.ma',          '0524-445566', 'Route de l Ourika, Km 5',                              '40000', 'Marrakech',  'Maroc', '45 jours',             18, 3.9, TRUE, '2026-05-01 08:00:00+00', '2026-05-01 08:00:00+00'),
    (7, 'LUMIERE MAROC',           'Nadia Berrada',   'n.berrada@lumiere-maroc.ma',      '0535-778899', 'Av Hassan II',                                        '30000', 'Fes',        'Maroc', '30 jours',              9, 4.1, TRUE, '2026-05-01 08:00:00+00', '2026-05-01 08:00:00+00'),
    (8, 'ACIER & METAL',           'Hassan Bouzidi',  'h.bouzidi@acier-metal.ma',        '0539-554433', 'Port de Tanger Med, Zone Logistique',                  '93000', 'Tanger',     'Maroc', '60 jours',             25, 4.3, TRUE, '2026-05-01 08:00:00+00', '2026-05-01 08:00:00+00'),
    (9, 'SANI MAROC',              'Fatima Alaoui',   'fz.alaoui@sanimaroc.ma',          '0537-112233', '21 Rue Al Fath, Agdal',                                '10080', 'Rabat',      'Maroc', '30 jours',              8, 4.4, TRUE, '2026-05-01 08:00:00+00', '2026-05-01 08:00:00+00'),
    (10, 'HYGIENE PRO',            'Imad Lahlou',     'i.lahlou@hygienepro.ma',          '0522-667788', '78 Av Al Massira, Hay Hassani',                        '20410', 'Casablanca', 'Maroc', '30 jours',              6, 4.0, TRUE, '2026-05-01 08:00:00+00', '2026-05-01 08:00:00+00')
ON CONFLICT ("Id") DO NOTHING;

-- =============================================================================
-- 2. PRODUITS (20)
-- =============================================================================

INSERT INTO public."Products"
    ("Id", "Category", "CreatedAt", "CreatedBy", "CurrentStock", "DailyConsumption", "Designation",
     "IsActive", "IsNew", "LastPurchaseDate", "MagasinId", "MinimumStock", "Unit", "UpdatedAt")
VALUES
    (1,  'Bureautique',  '2026-05-01 08:00:00+00', 'seed', 120, 2, 'Papier A4 (ramette 500 feuilles)',  TRUE, FALSE, '2026-05-22 10:00:00+00', NULL, 50,  'ramette', '2026-05-01 08:00:00+00'),
    (2,  'Bureautique',  '2026-05-01 08:00:00+00', 'seed', 30,  1, 'Cartouche encre noire HP 305',      TRUE, FALSE, '2026-05-22 10:00:00+00', NULL, 15,  'pcs',     '2026-05-01 08:00:00+00'),
    (3,  'Bureautique',  '2026-05-01 08:00:00+00', 'seed', 18,  1, 'Cartouche encre couleur HP 305',    TRUE, FALSE, '2026-05-22 10:00:00+00', NULL, 10,  'pcs',     '2026-05-01 08:00:00+00'),
    (4,  'Bureautique',  '2026-05-01 08:00:00+00', 'seed', 400, 5, 'Classeur carton dossier',           TRUE, FALSE, '2026-05-22 10:00:00+00', NULL, 100, 'pcs',     '2026-05-01 08:00:00+00'),
    (5,  'Bureautique',  '2026-05-01 08:00:00+00', 'seed', 60,  1, 'Marqueur permanent noir (lot 10)',  TRUE, FALSE, '2026-05-22 10:00:00+00', NULL, 20,  'lot',     '2026-05-01 08:00:00+00'),
    (6,  'Informatique', '2026-05-01 08:00:00+00', 'seed', 25,  1, 'Clavier USB AZERTY',                TRUE, FALSE, '2026-06-02 11:00:00+00', NULL, 10,  'pcs',     '2026-05-01 08:00:00+00'),
    (7,  'Informatique', '2026-05-01 08:00:00+00', 'seed', 40,  1, 'Souris optique USB',                TRUE, FALSE, '2026-06-02 11:00:00+00', NULL, 15,  'pcs',     '2026-05-01 08:00:00+00'),
    (8,  'Informatique', '2026-05-01 08:00:00+00', 'seed', 12,  1, 'Disque dur externe 1 To',           TRUE, FALSE, '2026-06-02 11:00:00+00', NULL, 5,   'pcs',     '2026-05-01 08:00:00+00'),
    (9,  'Informatique', '2026-05-01 08:00:00+00', 'seed', 8,   1, 'Ecran LED 24 pouces',               TRUE, FALSE, '2026-06-02 11:00:00+00', NULL, 3,   'pcs',     '2026-05-01 08:00:00+00'),
    (10, 'Electricite',  '2026-05-01 08:00:00+00', 'seed', 15,  1, 'Cable electrique 2.5 mm2 (rouleau 100 m)', TRUE, FALSE, '2026-06-10 09:00:00+00', NULL, 5,  'rouleau', '2026-05-01 08:00:00+00'),
    (11, 'Electricite',  '2026-05-01 08:00:00+00', 'seed', 200, 2, 'Prise murale double 16 A',          TRUE, FALSE, '2026-06-10 09:00:00+00', NULL, 50,  'pcs',     '2026-05-01 08:00:00+00'),
    (12, 'Electricite',  '2026-05-01 08:00:00+00', 'seed', 80,  1, 'Disjoncteur 16 A',                  TRUE, FALSE, '2026-06-10 09:00:00+00', NULL, 20,  'pcs',     '2026-05-01 08:00:00+00'),
    (13, 'Electricite',  '2026-05-01 08:00:00+00', 'seed', 90,  2, 'Spot LED encastrable 9 W',          TRUE, FALSE, '2026-06-10 09:00:00+00', NULL, 30,  'pcs',     '2026-05-01 08:00:00+00'),
    (14, 'Plomberie',    '2026-05-01 08:00:00+00', 'seed', 100, 2, 'Tuyau PVC 32 mm (barre 3 m)',       TRUE, FALSE, '2026-06-20 14:00:00+00', NULL, 40,  'barre',   '2026-05-01 08:00:00+00'),
    (15, 'Plomberie',    '2026-05-01 08:00:00+00', 'seed', 22,  1, 'Robinet mitigeur lavabo',           TRUE, FALSE, '2026-06-20 14:00:00+00', NULL, 8,   'pcs',     '2026-05-01 08:00:00+00'),
    (16, 'Plomberie',    '2026-05-01 08:00:00+00', 'seed', 75,  1, 'Flexible inox 60 cm',               TRUE, FALSE, '2026-06-20 14:00:00+00', NULL, 25,  'pcs',     '2026-05-01 08:00:00+00'),
    (17, 'Outillage',    '2026-05-01 08:00:00+00', 'seed', 6,   1, 'Perceuse sans fil 18 V',            TRUE, FALSE, '2026-06-25 15:00:00+00', NULL, 2,   'pcs',     '2026-05-01 08:00:00+00'),
    (18, 'Outillage',    '2026-05-01 08:00:00+00', 'seed', 20,  1, 'Jeu de tournevis 12 pieces',        TRUE, FALSE, '2026-06-25 15:00:00+00', NULL, 6,   'jeu',     '2026-05-01 08:00:00+00'),
    (19, 'Outillage',    '2026-05-01 08:00:00+00', 'seed', 15,  1, 'Masque FFP2 (boite de 50)',         TRUE, FALSE, '2026-06-25 15:00:00+00', NULL, 10,  'boite',   '2026-05-01 08:00:00+00'),
    (20, 'Outillage',    '2026-05-01 08:00:00+00', 'seed', 300, 3, 'Gants de travail cuir (paire)',     TRUE, FALSE, '2026-06-25 15:00:00+00', NULL, 60,  'paire',   '2026-05-01 08:00:00+00')
ON CONFLICT ("Id") DO NOTHING;

-- =============================================================================
-- 3. LISTES DE BESOINS (5) + DETAILS
--    RequestedById  = seed.acheteur ; ValidatedById = seed.admin
--    Status = 3 (Validated) car chaque chaine est completement traitee.
-- =============================================================================

INSERT INTO public."Needs"
    ("Id", "NumeroBesoin", "Description", "ProductId", "Quantity", "Unit", "Reason", "Priority",
     "DesiredUrgencyDate", "RequiredDelayDays", "Notes", "Status", "Justification", "RequestedById",
     "RequestedAt", "UpdatedAt", "ValidatedById", "DateTransmission", "DateCompletion", "History")
VALUES
    (1, 'BES-2026-001', 'Reapprovisionnement fournitures de bureau',  1, 50, 'ramette', 0, 1,
     '2026-05-18 09:00:00+00', 14, 'Stock de papier et encres faible. Commande trimestrielle habituelle.', 3,
     'Niveau minimum de stock atteint sur plusieurs articles.',
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.acheteur' LIMIT 1),
     '2026-05-04 09:30:00+00', '2026-06-02 16:00:00+00',
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.admin' LIMIT 1),
     '2026-05-05 10:00:00+00', '2026-06-02 16:00:00+00', NULL),
    (2, 'BES-2026-002', 'Equipement informatique pour nouveaux postes', 6, 10, 'pcs', 4, 2,
     '2026-05-25 09:00:00+00', 15, 'Equipement de 10 postes pour le service commercial.', 3,
     'Projet specifique de developpement du service commercial.',
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.acheteur' LIMIT 1),
     '2026-05-10 08:30:00+00', '2026-06-03 16:00:00+00',
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.admin' LIMIT 1),
     '2026-05-11 09:00:00+00', '2026-06-03 16:00:00+00', NULL),
    (3, 'BES-2026-003', 'Travaux electricite atelier maintenance',   10, 8, 'rouleau', 0, 1,
     '2026-06-02 09:00:00+00', 15, 'Remise aux normes de l installation electrique de l atelier.', 3,
     'Mise en conformite de l atelier de maintenance.',
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.acheteur' LIMIT 1),
     '2026-05-18 10:00:00+00', '2026-06-11 15:00:00+00',
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.admin' LIMIT 1),
     '2026-05-19 09:00:00+00', '2026-06-11 15:00:00+00', NULL),
    (4, 'BES-2026-004', 'Rehabilitation sanitaire batiment B',       14, 40, 'barre', 3, 2,
     '2026-06-08 09:00:00+00', 14, 'Stock critique : rupture imminente de tuyaux et accessoires.', 3,
     'Fuite importante constatee dans les sanitaires du batiment B.',
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.acheteur' LIMIT 1),
     '2026-05-25 11:00:00+00', '2026-06-21 17:00:00+00',
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.admin' LIMIT 1),
     '2026-05-26 09:00:00+00', '2026-06-21 17:00:00+00', NULL),
    (5, 'BES-2026-005', 'Equipement outillage atelier maintenance', 17, 2, 'pcs', 4, 1,
     '2026-06-15 09:00:00+00', 20, 'Renouvellement du parc outillage de l atelier.', 3,
     'Projet de modernisation du parc outillage.',
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.acheteur' LIMIT 1),
     '2026-06-01 08:30:00+00', '2026-06-26 16:00:00+00',
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.admin' LIMIT 1),
     '2026-06-02 09:00:00+00', '2026-06-26 16:00:00+00', NULL)
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO public."NeedDetails" ("Id", "IsNewProduct", "NeedId", "ProductId", "Quantity", "UnitPriceHT")
VALUES
    (1,  FALSE, 1, 1,  50,  NULL),
    (2,  FALSE, 1, 2,  12,  NULL),
    (3,  FALSE, 1, 3,  8,   NULL),
    (4,  FALSE, 1, 4,  100, NULL),
    (5,  FALSE, 1, 5,  15,  NULL),
    (6,  FALSE, 2, 6,  10,  NULL),
    (7,  FALSE, 2, 7,  15,  NULL),
    (8,  FALSE, 2, 8,  5,   NULL),
    (9,  FALSE, 2, 9,  3,   NULL),
    (10, FALSE, 3, 10, 8,   NULL),
    (11, FALSE, 3, 11, 60,  NULL),
    (12, FALSE, 3, 12, 25,  NULL),
    (13, FALSE, 3, 13, 40,  NULL),
    (14, FALSE, 4, 14, 40,  NULL),
    (15, FALSE, 4, 15, 10,  NULL),
    (16, FALSE, 4, 16, 30,  NULL),
    (17, FALSE, 5, 17, 2,   NULL),
    (18, FALSE, 5, 18, 8,   NULL),
    (19, FALSE, 5, 19, 10,  NULL),
    (20, FALSE, 5, 20, 50,  NULL)
ON CONFLICT ("Id") DO NOTHING;

-- =============================================================================
-- 4. DEVIS (5) + DETAILS
--    Chaque devis reprend les lignes de son besoin, fournisseur retenu.
--    TotalAmountHT = somme des lignes ; TotalAmountTTC = HT * 1.20
--    Tous les devis sont 'Valide' car chacun a genere un BC.
-- =============================================================================

INSERT INTO public."Quotations"
    ("Id", "ReferenceNumber", "Date", "SupplierId", "NeedId", "TotalAmountHT", "TotalAmountTTC",
     "ResponseDate", "Observations", "Status", "CreatedById", "CreatedAt", "UpdatedAt")
VALUES
    (1, 'DEV-2026-001', '2026-05-08 10:00:00+00', 1, 1, 7455.00, 8946.00,
     '2026-05-10 14:00:00+00', 'Offre remise 3% pour commande superieure a 5000 MAD.', 'Valide', (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.acheteur' LIMIT 1), '2026-05-08 10:00:00+00', '2026-05-10 14:00:00+00'),
    (2, 'DEV-2026-002', '2026-05-15 10:00:00+00', 5, 2, 6995.00, 8394.00,
     '2026-05-17 15:00:00+00', 'Garantie 24 mois incluse sur le materiel informatique.', 'Valide', (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.acheteur' LIMIT 1), '2026-05-15 10:00:00+00', '2026-05-17 15:00:00+00'),
    (3, 'DEV-2026-003', '2026-05-22 10:00:00+00', 2, 3, 7955.00, 9546.00,
     '2026-05-24 11:00:00+00', 'Cable livrable en rouleaux de 100 m.', 'Valide', (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.acheteur' LIMIT 1), '2026-05-22 10:00:00+00', '2026-05-24 11:00:00+00'),
    (4, 'DEV-2026-004', '2026-05-30 10:00:00+00', 9, 4, 4180.00, 5016.00,
     '2026-06-01 16:00:00+00', 'Demande de livraison prioritaire sous 8 jours.', 'Valide', (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.acheteur' LIMIT 1), '2026-05-30 10:00:00+00', '2026-06-01 16:00:00+00'),
    (5, 'DEV-2026-005', '2026-06-05 10:00:00+00', 4, 5, 5110.00, 6132.00,
     '2026-06-07 12:00:00+00', 'Outillage professionnel garanti 12 mois.', 'Valide', (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.acheteur' LIMIT 1), '2026-06-05 10:00:00+00', '2026-06-07 12:00:00+00')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO public."QuotationDetails" ("Id", "QuotationId", "ProductId", "Quantity", "UnitPriceHT", "UnitPriceTTC", "DeliveryDelayDays")
VALUES
    (1,  1, 1,  50,  45.00,  54.00,  10),
    (2,  1, 2,  12,  185.00, 222.00, 10),
    (3,  1, 3,  8,   220.00, 264.00, 10),
    (4,  1, 4,  100, 8.50,   10.20,  10),
    (5,  1, 5,  15,  25.00,  30.00,  10),
    (6,  2, 6,  10,  95.00,  114.00, 15),
    (7,  2, 7,  15,  65.00,  78.00,  15),
    (8,  2, 8,  5,   480.00, 576.00, 15),
    (9,  2, 9,  3,   890.00, 1068.00, 15),
    (10, 3, 10, 8,   320.00, 384.00, 15),
    (11, 3, 11, 60,  35.00,  42.00,  15),
    (12, 3, 12, 25,  55.00,  66.00,  15),
    (13, 3, 13, 40,  48.00,  57.60,  15),
    (14, 4, 14, 40,  28.00,  33.60,  8),
    (15, 4, 15, 10,  210.00, 252.00, 8),
    (16, 4, 16, 30,  32.00,  38.40,  8),
    (17, 5, 17, 2,   1150.00, 1380.00, 20),
    (18, 5, 18, 8,   145.00, 174.00, 20),
    (19, 5, 19, 10,  75.00,  90.00,  20),
    (20, 5, 20, 50,  18.00,  21.60,  20)
ON CONFLICT ("Id") DO NOTHING;

-- =============================================================================
-- 5. BONS DE COMMANDE (5) + DETAILS
--    Chaque BC reprend exactement les montants de son devis.
--    TotalVAT = HT * 0.20 ; TotalAmountTTC = HT * 1.20
-- =============================================================================

INSERT INTO public."PurchaseOrders"
    ("Id", "OrderNumber", "OrderDate", "SupplierId", "QuotationId", "NeedId", "TotalAmountHT",
     "TotalAmountTTC", "TotalVAT", "PaymentConditions", "RequestedDeliveryDelay", "Status",
     "ExpectedDeliveryDate", "CreatedById", "Observations", "CreatedAt", "UpdatedAt")
VALUES
    (1, 'BC-2026-001', '2026-05-12 09:00:00+00', 1, 1, 1, 7455.00, 8946.00, 1491.00, '30 jours fin de mois', 10, 'Valide',
     '2026-05-22 09:00:00+00', (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.acheteur' LIMIT 1), 'Commande trimestrielle fournitures.', '2026-05-12 09:00:00+00', '2026-05-12 09:00:00+00'),
    (2, 'BC-2026-002', '2026-05-18 09:00:00+00', 5, 2, 2, 6995.00, 8394.00, 1399.00, '30 jours', 15, 'Valide',
     '2026-06-02 09:00:00+00', (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.acheteur' LIMIT 1), 'Equipement complet des nouveaux postes.', '2026-05-18 09:00:00+00', '2026-05-18 09:00:00+00'),
    (3, 'BC-2026-003', '2026-05-25 09:00:00+00', 2, 3, 3, 7955.00, 9546.00, 1591.00, '45 jours', 15, 'Valide',
     '2026-06-10 09:00:00+00', (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.acheteur' LIMIT 1), 'Materiel pour l atelier.', '2026-05-25 09:00:00+00', '2026-05-25 09:00:00+00'),
    (4, 'BC-2026-004', '2026-06-02 09:00:00+00', 9, 4, 4, 4180.00, 5016.00, 836.00, '30 jours', 8, 'Valide',
     '2026-06-10 09:00:00+00', (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.acheteur' LIMIT 1), 'Livraison prioritaire demandee.', '2026-06-02 09:00:00+00', '2026-06-02 09:00:00+00'),
    (5, 'BC-2026-005', '2026-06-08 09:00:00+00', 4, 5, 5, 5110.00, 6132.00, 1022.00, '60 jours', 20, 'Valide',
     '2026-06-28 09:00:00+00', (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.acheteur' LIMIT 1), 'Renouvellement du parc outillage.', '2026-06-08 09:00:00+00', '2026-06-08 09:00:00+00')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO public."PurchaseOrderDetails" ("Id", "PurchaseOrderId", "ProductId", "Quantity", "UnitPriceHT", "UnitPriceTTC")
VALUES
    (1,  1, 1,  50,  45.00,  54.00),
    (2,  1, 2,  12,  185.00, 222.00),
    (3,  1, 3,  8,   220.00, 264.00),
    (4,  1, 4,  100, 8.50,   10.20),
    (5,  1, 5,  15,  25.00,  30.00),
    (6,  2, 6,  10,  95.00,  114.00),
    (7,  2, 7,  15,  65.00,  78.00),
    (8,  2, 8,  5,   480.00, 576.00),
    (9,  2, 9,  3,   890.00, 1068.00),
    (10, 3, 10, 8,   320.00, 384.00),
    (11, 3, 11, 60,  35.00,  42.00),
    (12, 3, 12, 25,  55.00,  66.00),
    (13, 3, 13, 40,  48.00,  57.60),
    (14, 4, 14, 40,  28.00,  33.60),
    (15, 4, 15, 10,  210.00, 252.00),
    (16, 4, 16, 30,  32.00,  38.40),
    (17, 5, 17, 2,   1150.00, 1380.00),
    (18, 5, 18, 8,   145.00, 174.00),
    (19, 5, 19, 10,  75.00,  90.00),
    (20, 5, 20, 50,  18.00,  21.60)
ON CONFLICT ("Id") DO NOTHING;

-- =============================================================================
-- 6. BONS DE LIVRAISON (5) + DETAILS
--    Statut 'Valide' : la facture a ete creee pour chaque BL.
--    BL-3 : ecart de 2 spots LED (ligne 13 commandee 40, recue 38)
--    -> DefectiveQuantity = 2, total recu = 131, conforme = 129.
--    total (bl_details) = quantite_livree * prix_ttc (regle de l application).
-- =============================================================================

INSERT INTO public.bons_livraison
    (id, numero_bl, date_reception, bc_id, fournisseur_id, "ReceivedQuantity", "CompliantQuantity",
     "DefectiveQuantity", observations, "Status", fichier_pdf, "ReceivedById", "CreatedAt", "UpdatedAt")
VALUES
    (1, 'BL-2026-001', '2026-05-22 10:00:00+00', 1, 1, 185, 185, 0,
     'Reception conforme, aucun ecart constate.', 'Valide', NULL, (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.magasinier' LIMIT 1), '2026-05-22 10:00:00+00', '2026-05-22 10:00:00+00'),
    (2, 'BL-2026-002', '2026-06-02 11:00:00+00', 2, 5, 33, 33, 0,
     'Materiel informatique conforme, cartons d origine.', 'Valide', NULL, (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.magasinier' LIMIT 1), '2026-06-02 11:00:00+00', '2026-06-02 11:00:00+00'),
    (3, 'BL-2026-003', '2026-06-10 09:00:00+00', 3, 2, 131, 129, 2,
     'Ecart : 2 spots LED manquants dans le colis n 4.', 'Valide', NULL, (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.magasinier' LIMIT 1), '2026-06-10 09:00:00+00', '2026-06-10 09:00:00+00'),
    (4, 'BL-2026-004', '2026-06-20 14:00:00+00', 4, 9, 80, 80, 0,
     'Reception conforme, livraison prioritaire honoree.', 'Valide', NULL, (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.magasinier' LIMIT 1), '2026-06-20 14:00:00+00', '2026-06-20 14:00:00+00'),
    (5, 'BL-2026-005', '2026-06-25 15:00:00+00', 5, 4, 70, 70, 0,
     'Outillage conforme, garanties jointes.', 'Valide', NULL, (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.magasinier' LIMIT 1), '2026-06-25 15:00:00+00', '2026-06-25 15:00:00+00')
ON CONFLICT (id) DO NOTHING;

INSERT INTO public.bl_details
    (id, bl_id, produit_id, quantite_commandee, quantite_livree, prix_ht, prix_ttc, total, valide)
VALUES
    (1,  1, 1,  50,  50,  45.00,  54.00,  2700.00, TRUE),
    (2,  1, 2,  12,  12,  185.00, 222.00, 2664.00, TRUE),
    (3,  1, 3,  8,   8,   220.00, 264.00, 2112.00, TRUE),
    (4,  1, 4,  100, 100, 8.50,   10.20,  1020.00, TRUE),
    (5,  1, 5,  15,  15,  25.00,  30.00,  450.00,  TRUE),
    (6,  2, 6,  10,  10,  95.00,  114.00, 1140.00, TRUE),
    (7,  2, 7,  15,  15,  65.00,  78.00,  1170.00, TRUE),
    (8,  2, 8,  5,   5,   480.00, 576.00, 2880.00, TRUE),
    (9,  2, 9,  3,   3,   890.00, 1068.00, 3204.00, TRUE),
    (10, 3, 10, 8,   8,   320.00, 384.00, 3072.00, TRUE),
    (11, 3, 11, 60,  60,  35.00,  42.00,  2520.00, TRUE),
    (12, 3, 12, 25,  25,  55.00,  66.00,  1650.00, TRUE),
    (13, 3, 13, 40,  38,  48.00,  57.60,  2188.80, FALSE),
    (14, 4, 14, 40,  40,  28.00,  33.60,  1344.00, TRUE),
    (15, 4, 15, 10,  10,  210.00, 252.00, 2520.00, TRUE),
    (16, 4, 16, 30,  30,  32.00,  38.40,  1152.00, TRUE),
    (17, 5, 17, 2,   2,   1150.00, 1380.00, 2760.00, TRUE),
    (18, 5, 18, 8,   8,   145.00, 174.00, 1392.00, TRUE),
    (19, 5, 19, 10,  10,  75.00,  90.00,  900.00,  TRUE),
    (20, 5, 20, 50,  50,  18.00,  21.60,  1080.00, TRUE)
ON CONFLICT (id) DO NOTHING;

-- =============================================================================
-- 7. FACTURES (5) + DETAILS
--    Facture = quantites livrees (BL). F3 : 38 spots au lieu de 40.
--    montant_ht = somme total_ht ; montant_tva = ht * 0.20 ; montant_ttc = ht * 1.20
--    Cas couverts : Payee (F1, F3) / Partiellement payee (F2, F5) / EnAttente (F4)
-- =============================================================================

INSERT INTO public.factures
    (id, numero_facture, numero_facture_fournisseur, date_facture, date_reception, fournisseur_id,
     bc_id, bl_id, montant_ht, taux_tva, montant_tva, montant_ttc, statut, conformite,
     justification_conformite, observations, date_echeance, "FilePath", cree_par, date_creation, date_maj)
VALUES
    (1, 'FAC-2026-001', 'FACT-2026-0789', '2026-05-24 10:00:00+00', '2026-05-24 10:00:00+00', 1,
     1, 1, 7455.00, 20.00, 1491.00, 8946.00, 'Payee', 'Conforme',
     NULL, 'Facture conforme au bon de livraison BL-2026-001.', '2026-06-23 10:00:00+00', NULL, (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.comptable' LIMIT 1), '2026-05-24 10:00:00+00', '2026-05-24 10:00:00+00'),
    (2, 'FAC-2026-002', 'TF-2026-3412', '2026-06-03 10:00:00+00', '2026-06-03 10:00:00+00', 5,
     2, 2, 6995.00, 20.00, 1399.00, 8394.00, 'Partiellement payee', 'Conforme',
     NULL, 'Deux reglements recus, solde en cours.', '2026-07-03 10:00:00+00', NULL, (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.comptable' LIMIT 1), '2026-06-03 10:00:00+00', '2026-06-03 10:00:00+00'),
    (3, 'FAC-2026-003', 'ME-2026-1154', '2026-06-12 10:00:00+00', '2026-06-12 10:00:00+00', 2,
     3, 3, 7859.00, 20.00, 1571.80, 9430.80, 'Payee', 'EcartMineur',
     '2 spots LED non recus (ecart BL-2026-003) : facture reduite en consequence.', 'Facture ajustee sur quantites livrees.', '2026-07-12 10:00:00+00', NULL, (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.comptable' LIMIT 1), '2026-06-12 10:00:00+00', '2026-06-12 10:00:00+00'),
    (4, 'FAC-2026-004', 'SM-2026-2290', '2026-06-22 10:00:00+00', '2026-06-22 10:00:00+00', 9,
     4, 4, 4180.00, 20.00, 836.00, 5016.00, 'EnAttente', 'Conforme',
     NULL, 'En attente de reglement, echeance sous 30 jours.', '2026-07-22 10:00:00+00', NULL, (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.comptable' LIMIT 1), '2026-06-22 10:00:00+00', '2026-06-22 10:00:00+00'),
    (5, 'FAC-2026-005', 'OM-2026-3311', '2026-06-27 10:00:00+00', '2026-06-27 10:00:00+00', 4,
     5, 5, 5110.00, 20.00, 1022.00, 6132.00, 'Partiellement payee', 'NonVerifiee',
     NULL, 'Conformite a verifier, reglements partiels recus.', '2026-07-27 10:00:00+00', NULL, (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.comptable' LIMIT 1), '2026-06-27 10:00:00+00', '2026-06-27 10:00:00+00')
ON CONFLICT (id) DO NOTHING;

INSERT INTO public.facture_details (id, facture_id, produit_id, quantite, pu_ht, taux_tva, total_ht, total_ttc)
VALUES
    (1,  1, 1,  50,  45.00,  20.00, 2250.00, 2700.00),
    (2,  1, 2,  12,  185.00, 20.00, 2220.00, 2664.00),
    (3,  1, 3,  8,   220.00, 20.00, 1760.00, 2112.00),
    (4,  1, 4,  100, 8.50,   20.00, 850.00,  1020.00),
    (5,  1, 5,  15,  25.00,  20.00, 375.00,  450.00),
    (6,  2, 6,  10,  95.00,  20.00, 950.00,  1140.00),
    (7,  2, 7,  15,  65.00,  20.00, 975.00,  1170.00),
    (8,  2, 8,  5,   480.00, 20.00, 2400.00, 2880.00),
    (9,  2, 9,  3,   890.00, 20.00, 2670.00, 3204.00),
    (10, 3, 10, 8,   320.00, 20.00, 2560.00, 3072.00),
    (11, 3, 11, 60,  35.00,  20.00, 2100.00, 2520.00),
    (12, 3, 12, 25,  55.00,  20.00, 1375.00, 1650.00),
    (13, 3, 13, 38,  48.00,  20.00, 1824.00, 2188.80),
    (14, 4, 14, 40,  28.00,  20.00, 1120.00, 1344.00),
    (15, 4, 15, 10,  210.00, 20.00, 2100.00, 2520.00),
    (16, 4, 16, 30,  32.00,  20.00, 960.00,  1152.00),
    (17, 5, 17, 2,   1150.00, 20.00, 2300.00, 2760.00),
    (18, 5, 18, 8,   145.00, 20.00, 1160.00, 1392.00),
    (19, 5, 19, 10,  75.00,  20.00, 750.00,  900.00),
    (20, 5, 20, 50,  18.00,  20.00, 900.00,  1080.00)
ON CONFLICT (id) DO NOTHING;

-- =============================================================================
-- 8. REGLEMENTS (10)
--    F1  (8946.00) : 40% + 40% + 20% = 100%  -> Payee
--    F2  (8394.00) : 30% + 20% + 15% =  65%  -> Partiellement payee
--    F3  (9430.80) : 60% + 40% = 100%        -> Payee
--    F4  (5016.00) : aucun reglement          -> EnAttente
--    F5  (6132.00) : 30% + 25% = 55%          -> Partiellement payee
-- =============================================================================

INSERT INTO public.reglements
    (id, numero_reglement, facture_id, fournisseur_id, mode_paiement, date_paiement, montant,
     statut, reference, banque, observations, fichier_preuve, type_fichier, fichier_recu,
     cree_par, date_creation, date_maj)
VALUES
    (1,  'REG-2026-001', 1, 1, 'Virement', '2026-05-26 09:00:00+00', 3578.40,
     'Valide', 'VIR-2026-1001', 'Attijariwafa Bank', 'Premier acompte 40% sur FAC-2026-001.', NULL, NULL, NULL,
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.comptable' LIMIT 1), '2026-05-26 09:00:00+00', '2026-05-26 09:00:00+00'),
    (2,  'REG-2026-002', 1, 1, 'Cheque',    '2026-06-15 09:00:00+00', 3578.40,
     'Valide', 'CHQ-04512',    'BMCE Bank',       'Second versement 40% sur FAC-2026-001.', NULL, NULL, NULL,
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.comptable' LIMIT 1), '2026-06-15 09:00:00+00', '2026-06-15 09:00:00+00'),
    (3,  'REG-2026-003', 1, 1, 'Virement', '2026-07-01 09:00:00+00', 1789.20,
     'Valide', 'VIR-2026-1002', 'CIH Bank',        'Solde 20% : facture FAC-2026-001 entierement payee.', NULL, NULL, NULL,
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.comptable' LIMIT 1), '2026-07-01 09:00:00+00', '2026-07-01 09:00:00+00'),
    (4,  'REG-2026-004', 2, 5, 'Virement', '2026-06-10 09:00:00+00', 2518.20,
     'Valide', 'VIR-2026-1003', 'Banque Populaire', 'Premier acompte 30% sur FAC-2026-002.', NULL, NULL, NULL,
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.comptable' LIMIT 1), '2026-06-10 09:00:00+00', '2026-06-10 09:00:00+00'),
    (5,  'REG-2026-005', 2, 5, 'Virement', '2026-07-05 09:00:00+00', 1678.80,
     'Valide', 'VIR-2026-1004', 'Attijariwafa Bank', 'Deuxieme versement 20% sur FAC-2026-002.', NULL, NULL, NULL,
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.comptable' LIMIT 1), '2026-07-05 09:00:00+00', '2026-07-05 09:00:00+00'),
    (6,  'REG-2026-006', 2, 5, 'Cheque',    '2026-07-20 09:00:00+00', 1259.10,
     'Valide', 'CHQ-04680',    'BMCE Bank',       'Versement supplementaire 15% sur FAC-2026-002.', NULL, NULL, NULL,
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.comptable' LIMIT 1), '2026-07-20 09:00:00+00', '2026-07-20 09:00:00+00'),
    (7,  'REG-2026-007', 3, 2, 'Virement', '2026-06-15 09:00:00+00', 5658.48,
     'Valide', 'VIR-2026-1005', 'CIH Bank',        'Premier versement 60% sur FAC-2026-003.', NULL, NULL, NULL,
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.comptable' LIMIT 1), '2026-06-15 09:00:00+00', '2026-06-15 09:00:00+00'),
    (8,  'REG-2026-008', 3, 2, 'Virement', '2026-06-30 09:00:00+00', 3772.32,
     'Valide', 'VIR-2026-1006', 'Banque Populaire', 'Solde 40% : facture FAC-2026-003 entierement payee.', NULL, NULL, NULL,
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.comptable' LIMIT 1), '2026-06-30 09:00:00+00', '2026-06-30 09:00:00+00'),
    (9,  'REG-2026-009', 5, 4, 'Cheque',    '2026-07-05 09:00:00+00', 1839.60,
     'Valide', 'CHQ-04722',    'Attijariwafa Bank', 'Premier versement 30% sur FAC-2026-005.', NULL, NULL, NULL,
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.comptable' LIMIT 1), '2026-07-05 09:00:00+00', '2026-07-05 09:00:00+00'),
    (10, 'REG-2026-010', 5, 4, 'Especes',   '2026-07-25 09:00:00+00', 1533.00,
     'Valide', 'ESP-2026-031', NULL,            'Versement especes 25% sur FAC-2026-005.', NULL, NULL, NULL,
     (SELECT "Id" FROM public."Users" WHERE "Login" = 'seed.comptable' LIMIT 1), '2026-07-25 09:00:00+00', '2026-07-25 09:00:00+00')
ON CONFLICT (id) DO NOTHING;

-- =============================================================================
-- 9. RESYNCHRONISATION DES SEQUENCES
--    Evite tout conflit d ID avec les prochains insertions de l application.
-- =============================================================================

SELECT setval(pg_get_serial_sequence('public."Suppliers"',        'Id'), GREATEST((SELECT COALESCE(MAX("Id"), 1) FROM public."Suppliers"),        1));
SELECT setval(pg_get_serial_sequence('public."Products"',         'Id'), GREATEST((SELECT COALESCE(MAX("Id"), 1) FROM public."Products"),         1));
SELECT setval(pg_get_serial_sequence('public."Needs"',            'Id'), GREATEST((SELECT COALESCE(MAX("Id"), 1) FROM public."Needs"),            1));
SELECT setval(pg_get_serial_sequence('public."NeedDetails"',      'Id'), GREATEST((SELECT COALESCE(MAX("Id"), 1) FROM public."NeedDetails"),      1));
SELECT setval(pg_get_serial_sequence('public."Quotations"',       'Id'), GREATEST((SELECT COALESCE(MAX("Id"), 1) FROM public."Quotations"),       1));
SELECT setval(pg_get_serial_sequence('public."QuotationDetails"', 'Id'), GREATEST((SELECT COALESCE(MAX("Id"), 1) FROM public."QuotationDetails"), 1));
SELECT setval(pg_get_serial_sequence('public."PurchaseOrders"',   'Id'), GREATEST((SELECT COALESCE(MAX("Id"), 1) FROM public."PurchaseOrders"),   1));
SELECT setval(pg_get_serial_sequence('public."PurchaseOrderDetails"', 'Id'), GREATEST((SELECT COALESCE(MAX("Id"), 1) FROM public."PurchaseOrderDetails"), 1));
SELECT setval(pg_get_serial_sequence('public.bons_livraison',     'id'), GREATEST((SELECT COALESCE(MAX(id), 1) FROM public.bons_livraison),     1));
SELECT setval(pg_get_serial_sequence('public.bl_details',         'id'), GREATEST((SELECT COALESCE(MAX(id), 1) FROM public.bl_details),         1));
SELECT setval(pg_get_serial_sequence('public.factures',           'id'), GREATEST((SELECT COALESCE(MAX(id), 1) FROM public.factures),           1));
SELECT setval(pg_get_serial_sequence('public.facture_details',    'id'), GREATEST((SELECT COALESCE(MAX(id), 1) FROM public.facture_details),    1));
SELECT setval(pg_get_serial_sequence('public.reglements',         'id'), GREATEST((SELECT COALESCE(MAX(id), 1) FROM public.reglements),         1));
SELECT setval(pg_get_serial_sequence('public."Users"',            'Id'), GREATEST((SELECT COALESCE(MAX("Id"), 1) FROM public."Users"),            1));

COMMIT;

-- =============================================================================
-- VERIFICATIONS (a lancer apres execution, doivent retourner 1 ligne chacune)
-- =============================================================================
-- 1) Toutes les factures doivent avoir un BL et un BC valides :
--    SELECT count(*) FROM public.factures f WHERE f.bl_id IS NULL OR f.bc_id IS NULL;
-- 2) Chaque BL doit avoir un BC :
--    SELECT count(*) FROM public.bons_livraison bl WHERE bl.bc_id IS NULL;
-- 3) Reglements ne doivent jamais depasser le TTC de leur facture :
--    SELECT count(*) FROM public.reglements r
--    JOIN public.factures f ON f.id = r.facture_id
--    WHERE (SELECT SUM(montant) FROM public.reglements WHERE facture_id = r.facture_id) > f.montant_ttc;
-- 4) Chaque BC doit avoir un devis et chaque devis un besoin :
--    SELECT count(*) FROM public."PurchaseOrders" po WHERE po."QuotationId" IS NULL;
--    SELECT count(*) FROM public."Quotations" q WHERE q."NeedId" IS NULL;
-- 5) Devis vs BC : montants identiques :
--    SELECT count(*) FROM public."PurchaseOrders" po
--    JOIN public."Quotations" q ON q."Id" = po."QuotationId"
--    WHERE po."TotalAmountHT" <> q."TotalAmountHT";



