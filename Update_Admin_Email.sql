-- Mettre à jour l'email de l'utilisateur admin
UPDATE "Users" 
SET "Email" = 'youssefoularbi628@gmail.com',
    "UpdatedAt" = CURRENT_TIMESTAMP
WHERE "Login" = 'admin';
