# Installation de SopalTrace sur un serveur

Les images sont construites et testées par la CI, puis publiées sur GitHub Container Registry.
Le serveur n'a besoin que de **Docker** (avec Docker Compose) : rien n'est compilé sur place.

## Contenu de la livraison

| Fichier | Rôle |
|---|---|
| `docker-compose.prod.yml` | SQL Server, API, front (nginx), conversion PDF (Gotenberg) et initialisation de la base |
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

## Documents générés (PDF)

Les documents qualité (plans) et les documents remplis par les opérateurs sont mis en forme en PDF à partir des
modèles Excel de Sopal : l'API remplit le modèle avec les données de la base, le service `gotenberg` (LibreOffice,
sans port publié) le convertit en PDF, puis le PDF et l'Excel rempli sont conservés en base (empreinte SHA-256).

- Un PDF est produit **à la demande** (consultation, impression), jamais en arrière-plan, puis réutilisé tant que
  les données du document ne changent pas.
- Chaque document est mis en forme avec le **modèle en vigueur à sa création** : la base numérote dans l'ordre réel
  d'enregistrement les documents et les publications de modèles (séquence `CreationOrderSequence`, sans dépendre des
  horloges). Les publications sont un historique jamais modifié (`ReportTemplateAssignment`).
- L'application ne livre aucun modèle Excel : l'administrateur envoie lui-même chaque fichier. Les modèles d'exemple
  de Sopal sont dans `tests/fixtures/modeles` (utilisés par les tests). Un fichier déjà envoyé n'est jamais dupliqué :
  sa version existante est reprise.
- Le superviseur qualité définit les formulaires (page « Formulaires ») ; l'administrateur leur rattache leur modèle
  Excel (page « Modèles Excel ») : le fichier est vérifié contre les colonnes du formulaire, enregistré en brouillon,
  prévisualisé vierge (identité du formulaire, jamais les données d'un document), puis rattaché. Les documents sans
  formulaire reçoivent un modèle par défaut (même page).
- Si `gotenberg` est arrêté, la consultation d'un PDF répond « service de conversion indisponible » ; rien n'est perdu.
- Les PDF et les modèles Excel rattachés sont stockés dans la base (aucun dossier) : la sauvegarde de la base les inclut.
  Un nouveau PDF est archivé chaque fois que le contenu du document change ; les précédents restent consultables.
- Nom d'un modèle : « désignation du formulaire — CODE.version.numéro du modèle » (donné par l'application ; la
  migration `2026-10-17_Fiches_De_Reponse.sql` renomme les modèles existants de la même façon).
- Mise en page du PDF : le texte trop long d'une case passe à la ligne (ligne agrandie) et la feuille tient toujours sur
  la largeur d'une page, même avec des colonnes ajoutées.
- Chaque formulaire reçoit son modèle de l'administrateur ; une version de formulaire dont les colonnes changent n'a plus de modèle tant que l'administrateur ne lui
  en rattache pas un : ses PDF sont refusés avec un message clair.
- Base existante : appliquer dans l'ordre `ScriptsSQL/Migrations/2026-10-14_Modeles_Rapport.sql`,
  `2026-10-14_Rapports_Execution.sql`, `2026-10-14_Modeles_Par_Formulaire.sql` puis
  `2026-10-15_Modeles_Rattaches_Aux_Formulaires.sql`, `2026-10-15_Champ_Application_Formulaires.sql` puis
  `2026-10-15_Identite_Formulaires.sql` puis `2026-10-15_Roles_Et_Type_Robinet.sql` puis
  `2026-10-16_Natures_Et_Modeles_Dedies.sql` puis `2026-10-17_Fiches_De_Reponse.sql` (chacun est rejouable), puis
  redémarrer l'API. `Identite_Formulaires` s'arrête en les nommant s'il reste plusieurs documents génériques actifs sur
  un même formulaire, ou un document générique actif sur un formulaire archivé ; `Roles_Et_Type_Robinet` s'arrête en
  nommant les formulaires dont le champ d'application ne convient pas à leur rôle.
- Le formulaire d'un document se trouve par son rôle et son champ d'application (famille, poste, machine…), jamais par
  son code : le superviseur qualité crée les formulaires et précise leurs codes (page « Formulaires »). À la création
  d'un document, le formulaire choisi impose son champ d'application (champs remplis et verrouillés).
- Version de départ : donnée une seule fois, par le superviseur qualité, à la création du code de formulaire (page
  « Formulaires », celle de la fiche Sopal) ; ensuite la version augmente toute seule. Les éditeurs de documents n'ont
  plus de champ « version de départ » ; le document prend la version de son formulaire (les plans de fabrication
  spécifiques, propres à chaque article, gardent leur propre numérotation).
- Modèle dédié : chaque formulaire a son propre modèle Excel. Une version de modèle appartient à un seul formulaire
  (`ReportTemplateVersion.FormGroupId`, qui suit ses versions et ses changements de code), porte un nom propre
  (« Plan de contrôle d'assemblage (FE0594) — FE-ASS-MANU ») et est numérotée dans ce formulaire. Un même fichier Excel
  est envoyé à nouveau pour chaque formulaire ; la base refuse de rattacher le modèle d'un autre formulaire. La
  migration copie à l'identique chaque modèle jusqu'ici partagé : les documents gardent exactement le même contenu de
  modèle, les documents générés ne changent pas.
- Nature des types (`FormRole.Nature`) : DOCUMENT (contenu défini par le superviseur qualité) ou RESPONSE_SHEET (fiche de
  réponse remplie par les opérateurs : résultats du contrôle en cours d'assemblage et de fabrication).
- Relation « code + version → modèle » : toujours écrite dans l'historique des rattachements, tout est conservé.
  - Nouvelle version aux mêmes colonnes, ou changement de code : le modèle est recopié (« Repris de CODE.version »).
  - Nouvelle version aux colonnes changées, ou nouveau code de formulaire : demande automatique à l'administrateur
    (`ReportTemplateRequest`, origine NEW_STRUCTURE / NEW_FORM).
  - Le superviseur qualité demande un nouveau modèle (page « Formulaires », bouton « Demander un nouveau modèle ») avec
    son choix : même version (les documents déjà créés gardent leur modèle, ceux créés ensuite prennent le nouveau) ou
    version suivante (formulaire en version suivante, documents actifs archivés tels quels puis recréés). L'administrateur
    traite la demande (page « Modèles Excel », partie « Demandes ») : le choix du superviseur s'applique tel quel.
  - Les modèles d'un formulaire sont numérotés M1, M2… ; le fichier téléchargé est nommé `CODE_M2.xlsx`.
- Fiches de réponse (remplies par les opérateurs, aucun document à gérer), chacune avec son formulaire (code, version,
  modèle dédié) fixé à sa création :
  - résultats du contrôle en cours d'assemblage (famille, type de robinet, poste) ;
  - résultats du contrôle en cours de fabrication (famille donc type de robinet, et poste s'il existe) : ses lignes
    viennent du plan suivi par l'OF (plan de fabrication, ou plan d'assemblage du poste) ; le document générique
    « Résultat Contrôle C.F. » n'est plus à créer. Son modèle Excel viendra avec la fiche vide de Sopal ;
  - registre de traçabilité (formulaire FE0129, type `REGISTRE_TRACABILITE`) : la migration fait de son ancien modèle
    par défaut le modèle dédié de ce formulaire et attache ce formulaire aux fiches déjà remplies.
  Contrôle au poste, vérification machine et échantillonnage : la fiche remplie a le même formulaire que le plan.
- Un nouveau logo change toujours de version (document ou fiche) : le superviseur choisit « Nouveau logo » dans sa
  demande ; seules une mise en page ou une autre correction peuvent garder la même version.
- Un OF garde, pour toute sa durée, le modèle en vigueur à la première utilisation de chaque formulaire (fiche
  d'exécution), même si un modèle change pendant l'OF ; les OF suivants prennent le nouveau.
- Rôles des formulaires : table `FormRole` (libellé, ordre, nature, critères permis : famille, poste, machine, nature,
  type de robinet). Un nouveau type de document s'y ajoute par une ligne (avec son écran de saisie et, s'il est mis en forme,
  son modèle Excel). La base refuse un critère que le rôle ne prévoit pas ; nature et type de robinet ne désignent un
  formulaire que sans famille. Avec une famille, le type de robinet est toujours celui de la famille (repris par la
  base, jamais saisi à part).
- Une ligne de formulaire = un code et une version, figés dès qu'un document l'utilise (garanti par la base). Changer
  de code garde la version : l'ancienne ligne et ses documents génériques sont archivés tels quels, une nouvelle ligne
  les remplace avec les mêmes documents recréés (journal `FormCodeChange`). Un document générique actif par formulaire
  (hors modèles de fabrication). Les OF déjà démarrés continuent avec leurs documents.
- Base existante où des modèles d'exemple avaient été importés automatiquement : pour repartir sans formulaires ni
  modèles (tout créer soi-même), exécuter `ScriptsSQL/Outils/Vider_Formulaires_Et_Modeles.sql`. Jamais en production.
- Tests : `Rapports:ImporterModelesLivres=true` et `Rapports:DossierModelesLivres=<dossier>` importent au démarrage
  les fichiers `{CODE}.xlsx` du dossier (réglé par `tests/e2e/run_e2e.py`). Désactivé par défaut.
- Nouveau logo ou nouvelle mise en page : demande du superviseur qualité (même version ou version suivante), traitée
  par l'administrateur. En version suivante, le formulaire passe à sa version suivante avec ce modèle et tous ses
  documents actifs sont archivés tels quels puis recréés, le tout d'un seul tenant ; les documents spécifiques ne sont
  pas recréés. Le même fichier que le modèle actuel est refusé.
- Historique (bouton « Historique » d'un formulaire) : tous les modèles rattachés au formulaire, toutes versions et
  anciens codes compris, avec date, motif, auteur, aperçu et fichier Excel.
- Pied de page des fiches et documents : « code.version » du formulaire (`{{CodeFormulaire}}.{{VersionFormulaire}}`),
  rempli par l'application. Base existante : les modèles déjà importés gardent leur pied de page figé ; envoyer un
  fichier mis à jour depuis la page « Modèles Excel » pour en publier une nouvelle version.

## Installation de démonstration (recette sur un poste)

Mettre dans `.env` :
`INIT_SCRIPTS=01_Create_SopalTraceDB.sql 02_ReferenceData_SopalTraceDB.sql 03_SageMirrorData_SopalTraceDB.sql 04_DemoData_SopalTraceDB.sql`

## Points restant à traiter avant la production

- Certificat HTTPS fourni par la DSI (voir la section HTTPS).
- Compte SQL applicatif à droits limités (`DB_USER`) au lieu de `sa`.
- Sauvegardes planifiées et test de restauration.
- Alimentation des tables Sage (articles, OF, besoins, annuaire, BL) depuis Sage X3.
