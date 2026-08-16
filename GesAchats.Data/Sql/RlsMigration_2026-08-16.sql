-- ============================================================================
-- GesAchats — Migration RLS : activation de Row-Level Security sur public
-- Date : 2026-08-16
--
-- Contexte :
--   * Le client WPF GesAchats se connecte via Npgsql avec le rôle superuser
--     `postgres` (BYPASSRLS). L'activation de RLS n'affecte donc PAS le
--     fonctionnement de l'application.
--   * Les rôles applicatifs (ADMIN, ACHETEUR, COMPTABLE, MAGASINIER) sont
--     stockés dans public."Roles", reliés à auth.users via
--     public."Users"."SupabaseAuthId".
--   * Aucune politique n'existait ; anon et authenticated disposaient de
--     privilèges complets (SELECT/INSERT/...) sur toutes les tables.
--
-- Stratégie :
--   * TO authenticated uniquement (le client WPF n'utilise JAMAIS la Data API
--     avec la clé anon ; anon n'a droit à rien).
--   * Rôles évalués via des fonctions SECURITY DEFINER placées dans le schéma
--     non-exposé `gs_rls` (évite l'auto-référence RLS Users -> Users).
--   * Principe du moindre privilège : ADMIN = accès complet ; ACHETEUR =
--     fournisseurs/devis/bons de commande ; MAGASINIER = stock/besoins/
--     bons de livraison ; COMPTABLE = factures/règlements uniquement.
--   * Aucune table n'est supprimée ; aucune donnée n'est modifiée ;
--     les clés étrangères et colonnes sont conservées.
--   * Toutes les instructions sont réversibles : `drop policy` /
--     `alter table ... disable row level security`.
-- ============================================================================

begin;

-- ---------------------------------------------------------------------------
-- 1. Schéma privé + fonctions d'aide (SECURITY DEFINER, non exposé au REST)
-- ---------------------------------------------------------------------------
create schema if not exists gs_rls;

create or replace function gs_rls.current_user_id()
returns integer
language sql
stable
security definer
set search_path = public, pg_temp
as $$
    select u."Id"
    from public."Users" u
    where u."SupabaseAuthId" = auth.uid()
      and u."IsActive" = true
    limit 1
$$;

create or replace function gs_rls.has_role(p_role text)
returns boolean
language sql
stable
security definer
set search_path = public, pg_temp
as $$
    select exists (
        select 1
        from public."Users" u
        join public."Roles" r on r."Id" = u."RoleId"
        where u."SupabaseAuthId" = auth.uid()
          and u."IsActive" = true
          and upper(r."Code") = upper(p_role)
    )
$$;

create or replace function gs_rls.is_admin()
returns boolean
language sql
stable
security definer
set search_path = public, pg_temp
as $$
    select gs_rls.has_role('ADMIN') or gs_rls.has_role('DIRECTEUR')
$$;

-- Exécution réservée à authenticated (comme exigé par l'évaluateur RLS).
revoke all on function gs_rls.current_user_id() from public;
revoke all on function gs_rls.has_role(text) from public;
revoke all on function gs_rls.is_admin() from public;
grant usage on schema gs_rls to authenticated;
grant execute on function gs_rls.current_user_id() to authenticated;
grant execute on function gs_rls.has_role(text) to authenticated;
grant execute on function gs_rls.is_admin() to authenticated;

-- ---------------------------------------------------------------------------
-- 2. Roles (catalogue) — lecture pour tous les connectés, écriture ADMIN
-- ---------------------------------------------------------------------------
alter table public."Roles" enable row level security;

drop policy if exists "roles_select_authenticated" on public."Roles";
create policy "roles_select_authenticated"
    on public."Roles" for select
    to authenticated
    using (true);

drop policy if exists "roles_insert_admin" on public."Roles";
create policy "roles_insert_admin"
    on public."Roles" for insert
    to authenticated
    with check (gs_rls.is_admin());

drop policy if exists "roles_update_admin" on public."Roles";
create policy "roles_update_admin"
    on public."Roles" for update
    to authenticated
    using (gs_rls.is_admin())
    with check (gs_rls.is_admin());

drop policy if exists "roles_delete_admin" on public."Roles";
create policy "roles_delete_admin"
    on public."Roles" for delete
    to authenticated
    using (gs_rls.is_admin());

-- ---------------------------------------------------------------------------
-- 3. Users — données personnelles + PasswordHash : soi-même ou ADMIN
-- ---------------------------------------------------------------------------
alter table public."Users" enable row level security;

drop policy if exists "users_select_self_or_admin" on public."Users";
create policy "users_select_self_or_admin"
    on public."Users" for select
    to authenticated
    using (gs_rls.is_admin() or gs_rls.current_user_id() = "Id");

drop policy if exists "users_insert_admin" on public."Users";
create policy "users_insert_admin"
    on public."Users" for insert
    to authenticated
    with check (gs_rls.is_admin());

drop policy if exists "users_update_admin" on public."Users";
create policy "users_update_admin"
    on public."Users" for update
    to authenticated
    using (gs_rls.is_admin())
    with check (gs_rls.is_admin());

drop policy if exists "users_delete_admin" on public."Users";
create policy "users_delete_admin"
    on public."Users" for delete
    to authenticated
    using (gs_rls.is_admin());

-- ---------------------------------------------------------------------------
-- 4. EmailVerificationCodes — codes sensibles : soi-même ou ADMIN
-- ---------------------------------------------------------------------------
alter table public."EmailVerificationCodes" enable row level security;

drop policy if exists "evc_select_self_or_admin" on public."EmailVerificationCodes";
create policy "evc_select_self_or_admin"
    on public."EmailVerificationCodes" for select
    to authenticated
    using (gs_rls.is_admin()
           or gs_rls.current_user_id() = "UserId");

drop policy if exists "evc_insert_self_or_admin" on public."EmailVerificationCodes";
create policy "evc_insert_self_or_admin"
    on public."EmailVerificationCodes" for insert
    to authenticated
    with check (gs_rls.is_admin()
                or gs_rls.current_user_id() = "UserId");

drop policy if exists "evc_update_self_or_admin" on public."EmailVerificationCodes";
create policy "evc_update_self_or_admin"
    on public."EmailVerificationCodes" for update
    to authenticated
    using (gs_rls.is_admin()
           or gs_rls.current_user_id() = "UserId")
    with check (gs_rls.is_admin()
                or gs_rls.current_user_id() = "UserId");

drop policy if exists "evc_delete_admin" on public."EmailVerificationCodes";
create policy "evc_delete_admin"
    on public."EmailVerificationCodes" for delete
    to authenticated
    using (gs_rls.is_admin());

-- ---------------------------------------------------------------------------
-- 5. AuditLogs — écriture par le connecté, lecture ADMIN uniquement
-- ---------------------------------------------------------------------------
alter table public."AuditLogs" enable row level security;

drop policy if exists "auditlogs_insert_self_or_admin" on public."AuditLogs";
create policy "auditlogs_insert_self_or_admin"
    on public."AuditLogs" for insert
    to authenticated
    with check (gs_rls.is_admin()
                or "UserId" is null
                or gs_rls.current_user_id() = "UserId");

drop policy if exists "auditlogs_select_admin" on public."AuditLogs";
create policy "auditlogs_select_admin"
    on public."AuditLogs" for select
    to authenticated
    using (gs_rls.is_admin());

-- Pas de politique UPDATE/DELETE sur AuditLogs : journal immuable côté API.

-- ---------------------------------------------------------------------------
-- 6. DashboardKpiSnapshots — lecture/écriture pour tous les connectés
-- ---------------------------------------------------------------------------
alter table public."DashboardKpiSnapshots" enable row level security;

drop policy if exists "kpi_select_authenticated" on public."DashboardKpiSnapshots";
create policy "kpi_select_authenticated"
    on public."DashboardKpiSnapshots" for select
    to authenticated
    using (true);

drop policy if exists "kpi_insert_authenticated" on public."DashboardKpiSnapshots";
create policy "kpi_insert_authenticated"
    on public."DashboardKpiSnapshots" for insert
    to authenticated
    with check (true);

-- ---------------------------------------------------------------------------
-- 7. Products — lecture tous, écriture ADMIN + MAGASINIER (gestion du stock)
-- ---------------------------------------------------------------------------
alter table public."Products" enable row level security;

drop policy if exists "products_select_authenticated" on public."Products";
create policy "products_select_authenticated"
    on public."Products" for select
    to authenticated
    using (true);

drop policy if exists "products_insert_admin_magasinier" on public."Products";
create policy "products_insert_admin_magasinier"
    on public."Products" for insert
    to authenticated
    with check (gs_rls.is_admin() or gs_rls.has_role('MAGASINIER'));

drop policy if exists "products_update_admin_magasinier" on public."Products";
create policy "products_update_admin_magasinier"
    on public."Products" for update
    to authenticated
    using (gs_rls.is_admin() or gs_rls.has_role('MAGASINIER'))
    with check (gs_rls.is_admin() or gs_rls.has_role('MAGASINIER'));

drop policy if exists "products_delete_admin" on public."Products";
create policy "products_delete_admin"
    on public."Products" for delete
    to authenticated
    using (gs_rls.is_admin());

-- ---------------------------------------------------------------------------
-- 8. Magasins — lecture tous, écriture ADMIN
-- ---------------------------------------------------------------------------
alter table public."Magasins" enable row level security;

drop policy if exists "magasins_select_authenticated" on public."Magasins";
create policy "magasins_select_authenticated"
    on public."Magasins" for select
    to authenticated
    using (true);

drop policy if exists "magasins_write_admin" on public."Magasins";
create policy "magasins_write_admin"
    on public."Magasins" for insert
    to authenticated
    with check (gs_rls.is_admin());

drop policy if exists "magasins_update_admin" on public."Magasins";
create policy "magasins_update_admin"
    on public."Magasins" for update
    to authenticated
    using (gs_rls.is_admin())
    with check (gs_rls.is_admin());

drop policy if exists "magasins_delete_admin" on public."Magasins";
create policy "magasins_delete_admin"
    on public."Magasins" for delete
    to authenticated
    using (gs_rls.is_admin());

-- ---------------------------------------------------------------------------
-- 9. Suppliers — lecture tous, écriture ADMIN + ACHETEUR (module Fournisseurs)
-- ---------------------------------------------------------------------------
alter table public."Suppliers" enable row level security;

drop policy if exists "suppliers_select_authenticated" on public."Suppliers";
create policy "suppliers_select_authenticated"
    on public."Suppliers" for select
    to authenticated
    using (true);

drop policy if exists "suppliers_insert_admin_acheteur" on public."Suppliers";
create policy "suppliers_insert_admin_acheteur"
    on public."Suppliers" for insert
    to authenticated
    with check (gs_rls.is_admin() or gs_rls.has_role('ACHETEUR'));

drop policy if exists "suppliers_update_admin_acheteur" on public."Suppliers";
create policy "suppliers_update_admin_acheteur"
    on public."Suppliers" for update
    to authenticated
    using (gs_rls.is_admin() or gs_rls.has_role('ACHETEUR'))
    with check (gs_rls.is_admin() or gs_rls.has_role('ACHETEUR'));

drop policy if exists "suppliers_delete_admin" on public."Suppliers";
create policy "suppliers_delete_admin"
    on public."Suppliers" for delete
    to authenticated
    using (gs_rls.is_admin());

-- ---------------------------------------------------------------------------
-- 10. Needs / NeedDetails — lecture tous, écriture ADMIN + ACHETEUR + MAGASINIER
-- ---------------------------------------------------------------------------
alter table public."Needs" enable row level security;

drop policy if exists "needs_select_authenticated" on public."Needs";
create policy "needs_select_authenticated"
    on public."Needs" for select
    to authenticated
    using (true);

drop policy if exists "needs_insert_admin_acheteur_magasinier" on public."Needs";
create policy "needs_insert_admin_acheteur_magasinier"
    on public."Needs" for insert
    to authenticated
    with check (gs_rls.is_admin()
                or gs_rls.has_role('ACHETEUR')
                or gs_rls.has_role('MAGASINIER'));

drop policy if exists "needs_update_admin_acheteur_magasinier" on public."Needs";
create policy "needs_update_admin_acheteur_magasinier"
    on public."Needs" for update
    to authenticated
    using (gs_rls.is_admin()
           or gs_rls.has_role('ACHETEUR')
           or gs_rls.has_role('MAGASINIER'))
    with check (gs_rls.is_admin()
                or gs_rls.has_role('ACHETEUR')
                or gs_rls.has_role('MAGASINIER'));

drop policy if exists "needs_delete_admin" on public."Needs";
create policy "needs_delete_admin"
    on public."Needs" for delete
    to authenticated
    using (gs_rls.is_admin());

alter table public."NeedDetails" enable row level security;

drop policy if exists "needdetails_select_authenticated" on public."NeedDetails";
create policy "needdetails_select_authenticated"
    on public."NeedDetails" for select
    to authenticated
    using (true);

drop policy if exists "needdetails_insert_admin_acheteur_magasinier" on public."NeedDetails";
create policy "needdetails_insert_admin_acheteur_magasinier"
    on public."NeedDetails" for insert
    to authenticated
    with check (gs_rls.is_admin()
                or gs_rls.has_role('ACHETEUR')
                or gs_rls.has_role('MAGASINIER'));

drop policy if exists "needdetails_update_admin_acheteur_magasinier" on public."NeedDetails";
create policy "needdetails_update_admin_acheteur_magasinier"
    on public."NeedDetails" for update
    to authenticated
    using (gs_rls.is_admin()
           or gs_rls.has_role('ACHETEUR')
           or gs_rls.has_role('MAGASINIER'))
    with check (gs_rls.is_admin()
                or gs_rls.has_role('ACHETEUR')
                or gs_rls.has_role('MAGASINIER'));

drop policy if exists "needdetails_delete_admin" on public."NeedDetails";
create policy "needdetails_delete_admin"
    on public."NeedDetails" for delete
    to authenticated
    using (gs_rls.is_admin());

-- ---------------------------------------------------------------------------
-- 11. Quotations / QuotationDetails — lecture tous, écriture ADMIN + ACHETEUR
-- ---------------------------------------------------------------------------
alter table public."Quotations" enable row level security;

drop policy if exists "quotations_select_authenticated" on public."Quotations";
create policy "quotations_select_authenticated"
    on public."Quotations" for select
    to authenticated
    using (true);

drop policy if exists "quotations_insert_admin_acheteur" on public."Quotations";
create policy "quotations_insert_admin_acheteur"
    on public."Quotations" for insert
    to authenticated
    with check (gs_rls.is_admin() or gs_rls.has_role('ACHETEUR'));

drop policy if exists "quotations_update_admin_acheteur" on public."Quotations";
create policy "quotations_update_admin_acheteur"
    on public."Quotations" for update
    to authenticated
    using (gs_rls.is_admin() or gs_rls.has_role('ACHETEUR'))
    with check (gs_rls.is_admin() or gs_rls.has_role('ACHETEUR'));

drop policy if exists "quotations_delete_admin" on public."Quotations";
create policy "quotations_delete_admin"
    on public."Quotations" for delete
    to authenticated
    using (gs_rls.is_admin());

alter table public."QuotationDetails" enable row level security;

drop policy if exists "quotationdetails_select_authenticated" on public."QuotationDetails";
create policy "quotationdetails_select_authenticated"
    on public."QuotationDetails" for select
    to authenticated
    using (true);

drop policy if exists "quotationdetails_insert_admin_acheteur" on public."QuotationDetails";
create policy "quotationdetails_insert_admin_acheteur"
    on public."QuotationDetails" for insert
    to authenticated
    with check (gs_rls.is_admin() or gs_rls.has_role('ACHETEUR'));

drop policy if exists "quotationdetails_update_admin_acheteur" on public."QuotationDetails";
create policy "quotationdetails_update_admin_acheteur"
    on public."QuotationDetails" for update
    to authenticated
    using (gs_rls.is_admin() or gs_rls.has_role('ACHETEUR'))
    with check (gs_rls.is_admin() or gs_rls.has_role('ACHETEUR'));

drop policy if exists "quotationdetails_delete_admin" on public."QuotationDetails";
create policy "quotationdetails_delete_admin"
    on public."QuotationDetails" for delete
    to authenticated
    using (gs_rls.is_admin());

-- ---------------------------------------------------------------------------
-- 12. PurchaseOrders / PurchaseOrderDetails
--     Lecture tous ; création ADMIN + ACHETEUR ; mise à jour également
--     MAGASINIER (réception, changement de statut) ; suppression ADMIN.
-- ---------------------------------------------------------------------------
alter table public."PurchaseOrders" enable row level security;

drop policy if exists "purchaseorders_select_authenticated" on public."PurchaseOrders";
create policy "purchaseorders_select_authenticated"
    on public."PurchaseOrders" for select
    to authenticated
    using (true);

drop policy if exists "purchaseorders_insert_admin_acheteur" on public."PurchaseOrders";
create policy "purchaseorders_insert_admin_acheteur"
    on public."PurchaseOrders" for insert
    to authenticated
    with check (gs_rls.is_admin() or gs_rls.has_role('ACHETEUR'));

drop policy if exists "purchaseorders_update_admin_acheteur_magasinier" on public."PurchaseOrders";
create policy "purchaseorders_update_admin_acheteur_magasinier"
    on public."PurchaseOrders" for update
    to authenticated
    using (gs_rls.is_admin()
           or gs_rls.has_role('ACHETEUR')
           or gs_rls.has_role('MAGASINIER'))
    with check (gs_rls.is_admin()
                or gs_rls.has_role('ACHETEUR')
                or gs_rls.has_role('MAGASINIER'));

drop policy if exists "purchaseorders_delete_admin" on public."PurchaseOrders";
create policy "purchaseorders_delete_admin"
    on public."PurchaseOrders" for delete
    to authenticated
    using (gs_rls.is_admin());

alter table public."PurchaseOrderDetails" enable row level security;

drop policy if exists "purchaseorderdetails_select_authenticated" on public."PurchaseOrderDetails";
create policy "purchaseorderdetails_select_authenticated"
    on public."PurchaseOrderDetails" for select
    to authenticated
    using (true);

drop policy if exists "purchaseorderdetails_insert_admin_acheteur" on public."PurchaseOrderDetails";
create policy "purchaseorderdetails_insert_admin_acheteur"
    on public."PurchaseOrderDetails" for insert
    to authenticated
    with check (gs_rls.is_admin() or gs_rls.has_role('ACHETEUR'));

drop policy if exists "purchaseorderdetails_update_admin_acheteur_magasinier" on public."PurchaseOrderDetails";
create policy "purchaseorderdetails_update_admin_acheteur_magasinier"
    on public."PurchaseOrderDetails" for update
    to authenticated
    using (gs_rls.is_admin()
           or gs_rls.has_role('ACHETEUR')
           or gs_rls.has_role('MAGASINIER'))
    with check (gs_rls.is_admin()
                or gs_rls.has_role('ACHETEUR')
                or gs_rls.has_role('MAGASINIER'));

drop policy if exists "purchaseorderdetails_delete_admin" on public."PurchaseOrderDetails";
create policy "purchaseorderdetails_delete_admin"
    on public."PurchaseOrderDetails" for delete
    to authenticated
    using (gs_rls.is_admin());

-- ---------------------------------------------------------------------------
-- 13. bons_livraison / bl_details
--     Lecture tous ; création ADMIN + MAGASINIER ; mise à jour également
--     COMPTABLE (validation conformité) ; suppression ADMIN + MAGASINIER.
-- ---------------------------------------------------------------------------
alter table public.bons_livraison enable row level security;

drop policy if exists "bl_select_authenticated" on public.bons_livraison;
create policy "bl_select_authenticated"
    on public.bons_livraison for select
    to authenticated
    using (true);

drop policy if exists "bl_insert_admin_magasinier" on public.bons_livraison;
create policy "bl_insert_admin_magasinier"
    on public.bons_livraison for insert
    to authenticated
    with check (gs_rls.is_admin() or gs_rls.has_role('MAGASINIER'));

drop policy if exists "bl_update_admin_magasinier_comptable" on public.bons_livraison;
create policy "bl_update_admin_magasinier_comptable"
    on public.bons_livraison for update
    to authenticated
    using (gs_rls.is_admin()
           or gs_rls.has_role('MAGASINIER')
           or gs_rls.has_role('COMPTABLE'))
    with check (gs_rls.is_admin()
                or gs_rls.has_role('MAGASINIER')
                or gs_rls.has_role('COMPTABLE'));

drop policy if exists "bl_delete_admin_magasinier" on public.bons_livraison;
create policy "bl_delete_admin_magasinier"
    on public.bons_livraison for delete
    to authenticated
    using (gs_rls.is_admin() or gs_rls.has_role('MAGASINIER'));

alter table public.bl_details enable row level security;

drop policy if exists "bldetails_select_authenticated" on public.bl_details;
create policy "bldetails_select_authenticated"
    on public.bl_details for select
    to authenticated
    using (true);

drop policy if exists "bldetails_insert_admin_magasinier" on public.bl_details;
create policy "bldetails_insert_admin_magasinier"
    on public.bl_details for insert
    to authenticated
    with check (gs_rls.is_admin() or gs_rls.has_role('MAGASINIER'));

drop policy if exists "bldetails_update_admin_magasinier_comptable" on public.bl_details;
create policy "bldetails_update_admin_magasinier_comptable"
    on public.bl_details for update
    to authenticated
    using (gs_rls.is_admin()
           or gs_rls.has_role('MAGASINIER')
           or gs_rls.has_role('COMPTABLE'))
    with check (gs_rls.is_admin()
                or gs_rls.has_role('MAGASINIER')
                or gs_rls.has_role('COMPTABLE'));

drop policy if exists "bldetails_delete_admin_magasinier" on public.bl_details;
create policy "bldetails_delete_admin_magasinier"
    on public.bl_details for delete
    to authenticated
    using (gs_rls.is_admin() or gs_rls.has_role('MAGASINIER'));

-- ---------------------------------------------------------------------------
-- 14. factures / facture_details — DONNÉES FINANCIÈRES :
--     ADMIN + COMPTABLE uniquement (SELECT, INSERT, UPDATE) ; DELETE ADMIN.
-- ---------------------------------------------------------------------------
alter table public.factures enable row level security;

drop policy if exists "factures_select_admin_comptable" on public.factures;
create policy "factures_select_admin_comptable"
    on public.factures for select
    to authenticated
    using (gs_rls.is_admin() or gs_rls.has_role('COMPTABLE'));

drop policy if exists "factures_insert_admin_comptable" on public.factures;
create policy "factures_insert_admin_comptable"
    on public.factures for insert
    to authenticated
    with check (gs_rls.is_admin() or gs_rls.has_role('COMPTABLE'));

drop policy if exists "factures_update_admin_comptable" on public.factures;
create policy "factures_update_admin_comptable"
    on public.factures for update
    to authenticated
    using (gs_rls.is_admin() or gs_rls.has_role('COMPTABLE'))
    with check (gs_rls.is_admin() or gs_rls.has_role('COMPTABLE'));

drop policy if exists "factures_delete_admin" on public.factures;
create policy "factures_delete_admin"
    on public.factures for delete
    to authenticated
    using (gs_rls.is_admin());

alter table public.facture_details enable row level security;

drop policy if exists "facturedetails_select_admin_comptable" on public.facture_details;
create policy "facturedetails_select_admin_comptable"
    on public.facture_details for select
    to authenticated
    using (gs_rls.is_admin() or gs_rls.has_role('COMPTABLE'));

drop policy if exists "facturedetails_insert_admin_comptable" on public.facture_details;
create policy "facturedetails_insert_admin_comptable"
    on public.facture_details for insert
    to authenticated
    with check (gs_rls.is_admin() or gs_rls.has_role('COMPTABLE'));

drop policy if exists "facturedetails_update_admin_comptable" on public.facture_details;
create policy "facturedetails_update_admin_comptable"
    on public.facture_details for update
    to authenticated
    using (gs_rls.is_admin() or gs_rls.has_role('COMPTABLE'))
    with check (gs_rls.is_admin() or gs_rls.has_role('COMPTABLE'));

drop policy if exists "facturedetails_delete_admin" on public.facture_details;
create policy "facturedetails_delete_admin"
    on public.facture_details for delete
    to authenticated
    using (gs_rls.is_admin());

-- ---------------------------------------------------------------------------
-- 15. reglements — DONNÉES FINANCIÈRES : ADMIN + COMPTABLE uniquement.
-- ---------------------------------------------------------------------------
alter table public.reglements enable row level security;

drop policy if exists "reglements_select_admin_comptable" on public.reglements;
create policy "reglements_select_admin_comptable"
    on public.reglements for select
    to authenticated
    using (gs_rls.is_admin() or gs_rls.has_role('COMPTABLE'));

drop policy if exists "reglements_insert_admin_comptable" on public.reglements;
create policy "reglements_insert_admin_comptable"
    on public.reglements for insert
    to authenticated
    with check (gs_rls.is_admin() or gs_rls.has_role('COMPTABLE'));

drop policy if exists "reglements_update_admin_comptable" on public.reglements;
create policy "reglements_update_admin_comptable"
    on public.reglements for update
    to authenticated
    using (gs_rls.is_admin() or gs_rls.has_role('COMPTABLE'))
    with check (gs_rls.is_admin() or gs_rls.has_role('COMPTABLE'));

drop policy if exists "reglements_delete_admin" on public.reglements;
create policy "reglements_delete_admin"
    on public.reglements for delete
    to authenticated
    using (gs_rls.is_admin());

-- ---------------------------------------------------------------------------
-- 16. StockExits — lecture tous ; création/annulation ADMIN + MAGASINIER.
-- ---------------------------------------------------------------------------
alter table public."StockExits" enable row level security;

drop policy if exists "stockexits_select_authenticated" on public."StockExits";
create policy "stockexits_select_authenticated"
    on public."StockExits" for select
    to authenticated
    using (true);

drop policy if exists "stockexits_insert_admin_magasinier" on public."StockExits";
create policy "stockexits_insert_admin_magasinier"
    on public."StockExits" for insert
    to authenticated
    with check (gs_rls.is_admin() or gs_rls.has_role('MAGASINIER'));

drop policy if exists "stockexits_delete_admin_magasinier" on public."StockExits";
create policy "stockexits_delete_admin_magasinier"
    on public."StockExits" for delete
    to authenticated
    using (gs_rls.is_admin() or gs_rls.has_role('MAGASINIER'));

-- Pas de politique UPDATE sur StockExits : les sorties sont créées ou annulées.

-- ---------------------------------------------------------------------------
-- 17. __EFMigrationsHistory — interne EF Core : RLS activé, aucune politique
--     (refus de tout accès via la Data API).
-- ---------------------------------------------------------------------------
alter table public."__EFMigrationsHistory" enable row level security;

commit;