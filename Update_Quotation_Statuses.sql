-- Script pour mettre à jour les statuts des devis
-- Remplace "Validated", "Accepted", et autres variantes par "Validé"
-- Et laisse "En attente" tel quel

UPDATE "Quotations"
SET "Status" = 'Validé'
WHERE "Status" IN ('Validated', 'Accepted', 'accepted', 'validated', 'Valide', 'valide');

-- Vérification des modifications
SELECT "Id", "ReferenceNumber", "Status"
FROM "Quotations"
ORDER BY "Id";
