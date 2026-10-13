# Installation de SopalTrace sur un serveur

Les images sont construites et testées par la CI, puis publiées sur GitHub Container Registry.
Le serveur n'a besoin que de **Docker** (avec Docker Compose) : rien n'est compilé sur place.

## Contenu de la livraison

| Fichier | Rôle |
|---|---|
| `docker-compose.prod.yml` | SQL Server, API, front (nginx) et initialisation de la base |
| `.env.prod.example` | modèle de configuration à copier en `.env` |
| `docker-compose.https.yml` + `nginx-https.conf` | HTTPS (redirection HTTP → HTTPS, HSTS) quand le certificat est disponible |
| `sql/` | scripts de la base : `01` schéma, `02` référentiels (production), `03` copie Sage et `04` démonstration (recette uniquement) |

## Première installation

```bash
cp .env.prod.example .env          # puis compléter les secrets
docker login ghcr.io               # jeton GitHub avec le droit « read:packages »
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml --profile init run --rm init-db
docker compose -f docker-compose.prod.yml up -d
```

Vérification : `http://<serveur>/api/health` répond `Healthy`, puis la page de connexion s'affiche.

### Premier administrateur

Toute inscription est une demande validée par un administrateur. Le tout premier administrateur est donc
désigné à l'installation, en pré-attribuant le rôle à son matricule Sage :

Requête à exécuter sur la base `SopalTraceDB` (SSMS, ou `sqlcmd` dans le conteneur `sqlserver`
avec l'utilisateur `sa` et le mot de passe `DB_PASSWORD` du fichier `.env`) :

```sql
INSERT INTO dbo.PreassignedRole (Matricule, RoleCode, AssignedBy) VALUES ('<matricule>', 'ADMIN', 'INSTALLATION');
```

Cette personne s'inscrit ensuite depuis la page d'inscription (matricule + e-mail de l'ERP) et reçoit
directement son lien d'activation. Les autres comptes se gèrent ensuite dans « Gestion des habilitations » :
validation ou refus motivé des demandes, rôles pré-attribués avant l'inscription.

## HTTPS (obligatoire en production)

Le cookie de session est marqué `Secure` : sans HTTPS, les sessions ne se renouvellent pas.
Placer le certificat dans `./certs` (`sopaltrace.crt` = chaîne complète, `sopaltrace.key`), puis :

```bash
docker compose -f docker-compose.prod.yml -f docker-compose.https.yml up -d
```

## Mise à jour

```bash
# dans .env : SOPALTRACE_VERSION=v1.3.0
docker compose -f docker-compose.prod.yml pull
docker compose -f docker-compose.prod.yml up -d
```

Faire une sauvegarde de la base avant chaque mise à jour.

Si la version change le schéma, appliquer à la base existante les scripts de `ScriptsSQL/Migrations/`
plus récents que la base, dans l'ordre de leur date (SSMS ou `sqlcmd -b -i <script>`), puis redémarrer l'API.
Chaque script est rejouable et s'exécute en tout ou rien ; s'il signale des données incohérentes, il affiche
la liste à corriger et n'applique rien.

## Installation de démonstration (recette sur un poste)

Mettre dans `.env` :
`INIT_SCRIPTS=01_Create_SopalTraceDB.sql 02_ReferenceData_SopalTraceDB.sql 03_SageMirrorData_SopalTraceDB.sql 04_DemoData_SopalTraceDB.sql`

## Points restant à traiter avant la production

- Certificat HTTPS fourni par la DSI (voir la section HTTPS).
- Compte SQL applicatif à droits limités (`DB_USER`) au lieu de `sa`.
- Sauvegardes planifiées et test de restauration.
- Alimentation des tables Sage (articles, OF, besoins, annuaire, BL) depuis Sage X3.
