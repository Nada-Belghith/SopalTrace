# SopalTrace — Installation sur Windows Server avec IIS (sans Docker)

Ce dossier contient une version **déjà compilée** de SopalTrace, prête à être installée sur un serveur Windows
avec IIS et SQL Server. Aucun outil de développement (SDK .NET, Node.js) n'est nécessaire sur le serveur.

| Dossier | Contenu |
|---|---|
| `api/` | Back-end compilé (API ASP.NET Core **.NET 8**) |
| `web/` | Front-end compilé (Vue 3) + `web.config` (redirection `/api` et routes du site) |
| `base/` | `SopalTraceDB.bak` (sauvegarde de la base, données de référence et de démonstration) + `creer_compte_api.sql` |

Architecture une fois installée :

```
Navigateur → http://<serveur>:8080        site IIS « SopalTrace »    (dossier web)
               ├── /         → pages du site
               └── /api/...  → relayé vers http://localhost:5000   site IIS « SopalTraceApi » (dossier api)
                                                                     └── SQL Server → base SopalTraceDB
```

---

## 1. Prérequis à installer sur le serveur

À installer **dans cet ordre** (le Hosting Bundle doit être installé *après* IIS) :

1. **IIS** : *Gestionnaire de serveur → Ajouter des rôles → Serveur Web (IIS)*
   (sur Windows 10/11 : `optionalfeatures` → *Internet Information Services*).
2. **SQL Server 2022** (Express, Developer ou Standard) + **SSMS**.
   La sauvegarde a été faite avec SQL Server 2022 : elle ne se restaure **pas** sur une version antérieure (2019, 2017…).
3. **ASP.NET Core 8 Hosting Bundle** : <https://dotnet.microsoft.com/download/dotnet/8.0>
   → colonne *ASP.NET Core Runtime 8.0.x* → ligne Windows → **Hosting Bundle**.
4. **URL Rewrite 2.1** pour IIS.
5. **Application Request Routing (ARR) 3.0** pour IIS.

Puis, dans une invite de commandes **administrateur** : `iisreset`.

> Si le Hosting Bundle a été installé avant IIS : *Programmes et fonctionnalités* → Hosting Bundle → *Modifier* → **Réparer**.

## 2. Base de données

1. Copier `base\SopalTraceDB.bak` dans le dossier de sauvegarde de SQL Server, par exemple
   `C:\Program Files\Microsoft SQL Server\MSSQL16.<INSTANCE>\MSSQL\Backup\`
   (le service SQL Server n'a pas accès aux dossiers personnels).
2. SSMS → clic droit **Bases de données** → **Restaurer la base de données…**
   - **Support** → `…` → **Ajouter** → `SopalTraceDB.bak`
   - page **Fichiers** : cocher **Déplacer tous les fichiers vers le dossier** (la sauvegarde provient d'un serveur Linux)
   - **OK**
3. Ouvrir `base\creer_compte_api.sql`, **remplacer le mot de passe**, exécuter (F5).
4. Clic droit sur le serveur → **Propriétés** → **Sécurité** → *Mode d'authentification SQL Server et Windows* → OK,
   puis clic droit sur le serveur → **Redémarrer**.

## 3. Copier les fichiers

Copier `api` et `web` sur le serveur, par exemple dans `C:\SopalTrace\api` et `C:\SopalTrace\web`.

## 4. Configurer l'API

Dans `C:\SopalTrace\api`, copier `appsettings.Production.exemple.json` en **`appsettings.Production.json`**
(le nom exact est important) et le compléter :

| Clé | Valeur |
|---|---|
| `ConnectionStrings:SopalTraceConnection` | `Server=` nom de l'instance (`localhost\SQLEXPRESS`, `localhost`…), mot de passe de l'étape 2.3 |
| `Jwt:Secret` | chaîne aléatoire d'**au moins 32 caractères** (64 conseillés) — sinon l'API refuse de démarrer |
| `Cors:AllowedOrigins` et `Auth:FrontendBaseUrl` | adresse du site telle que tapée dans le navigateur, ex. `http://srv-sopal:8080` |
| `EmailSettings:*` | serveur SMTP de Sopal (activation des comptes, mot de passe oublié) |
| `Rapports:GotenbergUrl` | service de conversion PDF (optionnel ; sans lui, l'export PDF est indisponible) |
| `DocumentsExternes:RepertoirePartage` | dossier des documents joints (optionnel) |

Générer une clé JWT (PowerShell) :
```powershell
-join ((48..57)+(65..90)+(97..122) | Get-Random -Count 62 | ForEach-Object {[char]$_})
```

> `appsettings.json` contient les valeurs par défaut et est remplacé à chaque nouvelle version : ne pas le modifier.

## 5. Créer les sites IIS

Ouvrir le Gestionnaire IIS (`inetmgr`).

**a. Activer le proxy (ARR)** — clic sur le nom du serveur → *Application Request Routing Cache* →
*Server Proxy Settings…* → cocher **Enable proxy** → *Apply*.

**b. API**
- *Pools d'applications* → *Ajouter* : nom `SopalTraceApi`, version CLR **Aucun code managé**.
- *Sites* → *Ajouter un site web* : nom `SopalTraceApi`, pool `SopalTraceApi`, chemin `C:\SopalTrace\api`, port **5000**.
- Vérifier : `http://localhost:5000/api/health` → **Healthy**.

**c. Front**
- *Sites* → *Ajouter un site web* : nom `SopalTrace`, chemin `C:\SopalTrace\web`, port **8080**.

> Ports différents : changer la liaison du site dans IIS. Si le port de l'API change, modifier aussi
> `http://localhost:5000` dans `web\web.config`. Si le port ou l'adresse du site change, mettre à jour
> `Cors:AllowedOrigins` et `Auth:FrontendBaseUrl`, puis recycler le pool `SopalTraceApi`.

## 6. Tester

Ouvrir `http://<serveur>:8080`, puis se connecter avec un compte de démonstration :

| Matricule | Mot de passe |
|---|---|
| 88881 – 88884 | `Passw0rdE2E!` |

Vérifier aussi qu'un rafraîchissement (F5) sur une page interne ne donne pas d'erreur 404.

## 7. Dépannage

| Symptôme | Cause probable |
|---|---|
| **HTTP 500.19** | URL Rewrite absent, ou Hosting Bundle installé avant IIS (le réparer, puis `iisreset`) |
| **HTTP 500.30 / 500.31** | l'API plante au démarrage : chaîne SQL ou clé JWT. Voir l'erreur en lançant `dotnet SopalTrace.Api.dll` dans `C:\SopalTrace\api` |
| **HTTP 502** sur `/api/...` | proxy ARR non activé (étape 5a) ou site `SopalTraceApi` arrêté |
| **Login failed for user 'sopaltrace_app'** | authentification SQL non activée / serveur non redémarré (étape 2.4), ou mot de passe différent |
| **Erreur CORS dans le navigateur** | `Cors:AllowedOrigins` ne correspond pas exactement à l'adresse du site |
| Configuration modifiée sans effet | recycler le pool `SopalTraceApi` |
