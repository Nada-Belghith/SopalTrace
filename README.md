# SopalTrace

Application web de **traçabilité qualité** de la production Sopal (robinets de bouteilles de gaz) : plans de contrôle,
exécution des contrôles par les opérateurs, préparation et expédition par le magasin, documents qualité générés en PDF,
documents externes des services et traçabilité complète de chaque ordre de fabrication (OF).

---

## 1. Ce que fait l'application

| Rôle | Ce qu'il fait |
|---|---|
| `ADMIN` | Comptes et habilitations (validation des inscriptions), modèles Excel des documents |
| `RESPONSABLE_DI` | Plans de fabrication (opérations FAB), import Excel des plans |
| `SUPERVISEUR_QUALITE` | Formulaires, plans d'assemblage, produit fini, vérification machine, échantillonnage, contrôle au poste |
| `RESPONSABLE_QUALITE` | Validation des documents qualité, **traçabilité par OF** (seul rôle qui y a accès) |
| `OPERATEUR` | Exécution des contrôles d'un OF à son poste (vérifications machine, fiches, résultats) |
| `MAGASINIER` | Préparation des OF (scan des lots), expédition (scan des BL) |
| `SERVICE_QUALITE` / `SERVICE_ACHATS` / `SERVICE_COMMERCIAL` / `SERVICE_DI` | Dépôt des documents externes (rapports de contrôle, certificats, BC fournisseur, BL, BC client, plans de définition) |

Les données de l'ERP (articles, OF, utilisateurs, BL, commandes) viennent de **copies des tables Sage X3** dans la base
(`AUTILIS`, `MFGHEAD_OrdreFabrication`, `SDELIVERY`, `SORDER`, `PORDER`…). Un utilisateur ne peut s'inscrire que si son
matricule existe dans `AUTILIS` ; chaque inscription est validée par un administrateur.

## 2. Architecture

```
navigateur ──► nginx (front Vue 3, SPA) ──/api──► API .NET 8 ──► SQL Server 2022
                                                     └──────────► Gotenberg (Excel → PDF)
```

| Dossier | Contenu |
|---|---|
| `SopalTraceApp/` | API .NET 8 en *Clean Architecture* : `SopalTrace.Api` (contrôleurs, sécurité JWT), `SopalTrace.Application` (services métier), `SopalTrace.Domain` (entités, constantes), `SopalTrace.Infrastructure` (EF Core *Database First*, dépôts, moteur Excel) |
| `SopalTraceApp/SopalTrace.Infrastructure/ScriptsSQL/` | Scripts de la base (voir §4) |
| `SopalTraceApp/*.Tests/` | Tests unitaires et d'intégration (xUnit) |
| `SopalTraceApp/tests/e2e/` | Recette automatique de bout en bout (Python) : base neuve + API + scénarios par rôle |
| `SopalTraceApp/tests/fixtures/modeles/` | Modèles Excel d'exemple de Sopal (utilisés par les tests) |
| `SopalTraceApp/deploy/` | Installation serveur : `docker-compose.prod.yml`, HTTPS, modèle `.env`, [INSTALLATION.md](SopalTraceApp/deploy/INSTALLATION.md) |
| `SopalTraceWeb/` | Front Vue 3 (Vite, Tailwind, vue-router) ; servi par nginx en production |
| `.github/workflows/ci.yml` | Intégration continue (build, tests, E2E, recette Docker, publication des images, livraison) |

Points techniques :
- **Base de données d'abord** : le schéma est défini par les scripts SQL ; les règles critiques (unicité, états,
  documents figés) sont garanties par la base (contraintes, index filtrés, déclencheurs).
- **Heure** : Tunisie (`TZ=Africa/Tunis`) partout.
- **PDF** : produits à la demande depuis les modèles Excel par Gotenberg, puis conservés en base. Les fichiers (PDF,
  modèles, documents externes) sont **stockés dans la base** : la sauvegarde de la base suffit.
- **Sécurité** : JWT (rôle revérifié en base à chaque requête), cookie de renouvellement `Secure`, verrouillage après
  échecs de connexion, limite de requêtes d'authentification, en-têtes de sécurité nginx.

## 3. Prérequis

| Usage | À installer |
|---|---|
| Serveur (test ou production) | **Docker** + Docker Compose uniquement |
| Développement | .NET SDK 10 (cible net8.0, voir `global.json`), Node.js 20, SQL Server ou LocalDB, Docker (Gotenberg), Python 3.12 + `openpyxl` pour les E2E |

## 4. Base de données : quels scripts exécuter

Dossier `SopalTraceApp/SopalTrace.Infrastructure/ScriptsSQL/`.

| Script | Rôle | Production | Test / recette |
|---|---|---|---|
| `01_Create_SopalTraceDB.sql` | Crée la base et tout le schéma | ✅ | ✅ |
| `02_ReferenceData_SopalTraceDB.sql` | Référentiels : rôles, opérations, machines, familles, formulaires, ISO 2859 | ✅ | ✅ |
| `03_SageMirrorData_SopalTraceDB.sql` | Exemples de données Sage (articles, OF, utilisateurs) | ❌ | ✅ |
| `04_DemoData_SopalTraceDB.sql` | Données de démonstration | ❌ | ✅ |
| `Demo_Comptes_Services.sql` | Comptes actifs des 4 services (88881–88884) | ❌ | ✅ (après 01–04) |
| `Demo_Commandes_Sage.sql` | Commandes fournisseurs/clients et BL d'exemple, traçabilité de l'OF `OF2607ASS02` | ❌ | ✅ (après 01–04) |
| `Outils/Vider_Formulaires_Et_Modeles.sql` | Vide formulaires et modèles d'une base de test | ❌ jamais | si besoin |

> **Base neuve = 01 → 02 (→ 03 → 04 en test).** Première version : il n'y a aucune migration, 01/02 contiennent
> tout le schéma. Quand une version ultérieure changera une base déjà installée, ses scripts de mise à jour seront
> livrés dans `ScriptsSQL/Migrations/` (à appliquer dans l'ordre de leur date, rejouables, en tout ou rien).

## 5. Installer sur un serveur (Docker)

Procédure complète : [SopalTraceApp/deploy/INSTALLATION.md](SopalTraceApp/deploy/INSTALLATION.md). Résumé :

**Étape 1 — Récupérer la livraison.** Le paquet `sopaltrace-vX.Y.Z.zip` de la *release* GitHub (créé par la CI à chaque
étiquette `v*`) contient les fichiers de `deploy/` et le dossier `sql/`.

**Étape 2 — Configurer.**
```bash
cp .env.prod.example .env
```
Renseigner dans `.env` :
| Variable | Valeur |
|---|---|
| `SOPALTRACE_VERSION` | version à installer (`v1.0.0`) |
| `DB_PASSWORD` | mot de passe SQL Server fort (12 caractères min., complexe) |
| `Jwt__Secret` | clé aléatoire d'au moins 32 caractères (64 conseillés) — l'API refuse de démarrer sinon |
| `Auth__FrontendBaseUrl` | adresse du site (liens des e-mails d'activation) |
| `SMTP_*`, `EMAIL_*` | serveur d'envoi des e-mails (serveur Sopal, ou Brevo) |
| `INIT_SCRIPTS` | production : `01… 02…` ; test : `01… 02… 03… 04…` |

**Étape 3 — Télécharger les images** (privées sur GitHub Container Registry) :
```bash
docker login ghcr.io        # utilisateur GitHub + jeton avec le droit read:packages
docker compose -f docker-compose.prod.yml pull
```

**Étape 4 — Créer la base** (une seule fois) **puis démarrer** :
```bash
docker compose -f docker-compose.prod.yml --profile init run --rm init-db
docker compose -f docker-compose.prod.yml up -d
```

**Étape 5 — Vérifier.** `http://<serveur>/api/health` répond `Healthy`, puis la page de connexion s'affiche.

**Étape 6 — Comptes.**
- *Test* : exécuter `Demo_Comptes_Services.sql` et `Demo_Commandes_Sage.sql` dans le conteneur SQL
  (`docker compose -f docker-compose.prod.yml exec sqlserver /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "<DB_PASSWORD>" -d SopalTraceDB -i /chemin/script.sql`,
  ou SSMS). Les comptes 88881–88884 se connectent avec le mot de passe de démonstration communiqué à part.
- *Premier administrateur* (production comme test) : pré-attribuer le rôle à son matricule Sage, puis cette personne
  s'inscrit depuis la page d'inscription et reçoit directement son lien d'activation :
  ```sql
  INSERT INTO dbo.PreassignedRole (Matricule, RoleCode, AssignedBy) VALUES ('<matricule>', 'ADMIN', 'INSTALLATION');
  ```
  Les autres comptes se gèrent ensuite dans « Gestion des habilitations ».

**Étape 7 — Modèles Excel.** L'application ne livre aucun modèle : l'administrateur envoie chaque modèle depuis la page
« Modèles Excel » (exemples Sopal dans `tests/fixtures/modeles`).

**HTTPS (obligatoire en production)** : certificat dans `./certs` (`sopaltrace.crt`, `sopaltrace.key`) puis
```bash
docker compose -f docker-compose.prod.yml -f docker-compose.https.yml up -d
```

**Mise à jour** : sauvegarder la base, changer `SOPALTRACE_VERSION`, `pull` puis `up -d`, et appliquer les scripts de
`Migrations/` livrés avec la nouvelle version s'il y en a.

## 6. Lancer en développement

**Base** (une fois) : exécuter `01` à `04` sur LocalDB ou SQL Server (SSMS ou `sqlcmd -b -i <script>`).

**Conversion PDF** :
```bash
docker run -d --name gotenberg -p 3000:3000 gotenberg/gotenberg:8.20.1
```

**API** (`http://localhost:5125`, Swagger sur `/swagger`) :
```bash
cd SopalTraceApp
dotnet user-secrets --project SopalTrace.Api set "ConnectionStrings:SopalTraceConnection" "Server=(localdb)\MSSQLLocalDB;Database=SopalTraceDB;Trusted_Connection=True;TrustServerCertificate=True"
dotnet user-secrets --project SopalTrace.Api set "Jwt:Secret" "<clé aléatoire de 32 caractères minimum>"
dotnet run --project SopalTrace.Api --launch-profile http
```
(Ou par variables d'environnement : `ConnectionStrings__SopalTraceConnection`, `Jwt__Secret`.)

**Front** (`http://localhost:5173`) :
```bash
cd SopalTraceWeb
npm ci
# sans Docker, faire pointer le front vers l'API locale :
echo VITE_API_URL=http://localhost:5125/api > .env.local
npm run dev
```

**Tout en Docker** (alternative) : depuis `SopalTraceApp/`, copier `.env.example` en `.env`, le compléter, puis
`docker compose up -d --build` (site sur `http://localhost`).

## 7. Tests

| Commande | Ce qui est testé |
|---|---|
| `dotnet test SopalTraceApp/SopalTraceApp.slnx` | Tests unitaires et d'intégration de l'API |
| `npm run lint:check && npm test && npm run build` (dans `SopalTraceWeb`) | Lint, tests unitaires et build du front |
| `python tests/e2e/run_e2e.py` (dans `SopalTraceApp`, après `dotnet build`) | Recette de bout en bout sur une base neuve `SopalTraceDB_E2E` (voir [tests/e2e/README.md](SopalTraceApp/tests/e2e/README.md)) |

La CI (`.github/workflows/ci.yml`) exécute tout cela à chaque envoi, plus la recherche de secrets et une recette de la
pile Docker de production. Sur `main` elle publie les images ; sur une étiquette `vX.Y.Z` elle crée aussi la *release*
avec le paquet d'installation :
```bash
git tag v1.0.0 && git push origin v1.0.0
```

## 8. Avant la mise en production réelle

- Certificat HTTPS fourni par la DSI.
- Compte SQL applicatif à droits limités (`DB_USER`) au lieu de `sa`.
- Sauvegardes planifiées de la base et test de restauration.
- Alimentation des tables Sage (articles, OF, utilisateurs, BL, commandes `SORDER` / `PORDER`) depuis Sage X3.
- Sur la base de production : **uniquement 01 et 02**, jamais 03, 04 ni les scripts `Demo_*`.
