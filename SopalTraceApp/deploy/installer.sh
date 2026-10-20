#!/bin/bash
# =============================================================================
# SopalTrace — Script d'installation automatique
# Usage : bash installer.sh
# Prérequis : Docker + Docker Compose installés sur le serveur
# =============================================================================
set -e

# ── Couleurs ──
VERT='\033[0;32m'
JAUNE='\033[1;33m'
ROUGE='\033[0;31m'
BLEU='\033[0;34m'
GRAS='\033[1m'
RESET='\033[0m'

echo -e "${BLEU}${GRAS}"
echo "╔══════════════════════════════════════════════════╗"
echo "║       SopalTrace — Installation automatique     ║"
echo "╚══════════════════════════════════════════════════╝"
echo -e "${RESET}"

# ── Configuration ──
GITHUB_REPO="Nada-Belghith/SopalTrace"
BRANCHE="main"
DOSSIER_INSTALL="$(pwd)/sopaltrace"

# ── Vérification Docker ──
if ! command -v docker &> /dev/null; then
    echo -e "${ROUGE}❌ Docker n'est pas installé. Installez Docker puis relancez ce script.${RESET}"
    exit 1
fi
if ! docker compose version &> /dev/null; then
    echo -e "${ROUGE}❌ Docker Compose n'est pas installé.${RESET}"
    exit 1
fi
echo -e "${VERT}✅ Docker et Docker Compose détectés${RESET}"

# =============================================================================
# 1. TYPE D'INSTALLATION
# =============================================================================
echo ""
echo -e "${GRAS}── Type d'installation ──${RESET}"
echo "  1) Production  (scripts 01 + 02 uniquement)"
echo "  2) Démonstration / Recette (scripts 01 + 02 + 03 + 04 + comptes démo)"
read -p "Choix [1/2] : " TYPE_INSTALL
if [ "$TYPE_INSTALL" = "2" ]; then
    MODE="demo"
    INIT_SCRIPTS="01_Create_SopalTraceDB.sql 02_ReferenceData_SopalTraceDB.sql 03_SageMirrorData_SopalTraceDB.sql 04_DemoData_SopalTraceDB.sql"
    echo -e "${JAUNE}→ Mode démonstration${RESET}"
else
    MODE="prod"
    INIT_SCRIPTS="01_Create_SopalTraceDB.sql 02_ReferenceData_SopalTraceDB.sql"
    echo -e "${VERT}→ Mode production${RESET}"
fi

# =============================================================================
# 2. COLLECTE DES SECRETS
# =============================================================================
echo ""
echo -e "${GRAS}── Configuration des secrets ──${RESET}"

# Mot de passe SQL Server
while true; do
    read -sp "Mot de passe SQL Server (12 car. min., complexe) : " DB_PASSWORD
    echo ""
    if [ ${#DB_PASSWORD} -ge 12 ]; then break; fi
    echo -e "${ROUGE}  Trop court (minimum 12 caractères)${RESET}"
done

# Clé JWT
while true; do
    read -sp "Clé secrète JWT (32 car. min., aléatoire) : " JWT_SECRET
    echo ""
    if [ ${#JWT_SECRET} -ge 32 ]; then break; fi
    echo -e "${ROUGE}  Trop court (minimum 32 caractères)${RESET}"
done

# URL du front
read -p "Adresse du site (ex: https://sopaltrace.sopal.tn) : " FRONTEND_URL
FRONTEND_URL=${FRONTEND_URL:-https://sopaltrace.exemple.tn}

# SMTP
echo ""
echo -e "${GRAS}── Configuration e-mail (SMTP) ──${RESET}"
read -p "Serveur SMTP (ex: smtp.sopal.tn) : " SMTP_SERVER
read -p "Port SMTP [587] : " SMTP_PORT
SMTP_PORT=${SMTP_PORT:-587}
read -p "Utilisateur SMTP : " SMTP_USERNAME
read -p "E-mail expéditeur : " EMAIL_SENDER
read -sp "Mot de passe e-mail : " EMAIL_PASSWORD
echo ""

# Premier administrateur
echo ""
echo -e "${GRAS}── Premier administrateur ──${RESET}"
read -p "Matricule Sage du premier administrateur : " ADMIN_MATRICULE
read -p "Qui installe ? (ex: votre nom) [INSTALLATION] : " ASSIGNED_BY
ASSIGNED_BY=${ASSIGNED_BY:-INSTALLATION}

# Token GitHub pour ghcr.io
echo ""
echo -e "${GRAS}── Accès au registre d'images (ghcr.io) ──${RESET}"
echo -e "${JAUNE}  Créez un token sur : GitHub → Settings → Developer settings → Personal access tokens"
echo -e "  Droit nécessaire : read:packages${RESET}"
read -p "Utilisateur GitHub : " GITHUB_USER
read -sp "Token GitHub : " GITHUB_TOKEN
echo ""

# =============================================================================
# 3. CRÉATION DU DOSSIER ET TÉLÉCHARGEMENT
# =============================================================================
echo ""
echo -e "${BLEU}── Téléchargement des fichiers ──${RESET}"

mkdir -p "$DOSSIER_INSTALL/sql"
cd "$DOSSIER_INSTALL"

BASE_URL="https://raw.githubusercontent.com/${GITHUB_REPO}/${BRANCHE}"

# Fonction de téléchargement
telecharger() {
    local url="$1"
    local dest="$2"
    echo -e "  ⬇  $(basename "$dest")"
    if [ -n "$GITHUB_TOKEN" ]; then
        curl -sS -f -H "Authorization: token ${GITHUB_TOKEN}" -o "$dest" "$url"
    else
        curl -sS -f -o "$dest" "$url"
    fi
}

# docker-compose.prod.yml
telecharger "${BASE_URL}/SopalTraceApp/deploy/docker-compose.prod.yml" "docker-compose.prod.yml"

# docker-compose.https.yml (pour plus tard)
telecharger "${BASE_URL}/SopalTraceApp/deploy/docker-compose.https.yml" "docker-compose.https.yml" 2>/dev/null || true

# nginx-https.conf
telecharger "${BASE_URL}/SopalTraceApp/deploy/nginx-https.conf" "nginx-https.conf" 2>/dev/null || true

# Scripts SQL
SQL_DIR="SopalTraceApp/SopalTrace.Infrastructure/ScriptsSQL"
telecharger "${BASE_URL}/${SQL_DIR}/01_Create_SopalTraceDB.sql" "sql/01_Create_SopalTraceDB.sql"
telecharger "${BASE_URL}/${SQL_DIR}/02_ReferenceData_SopalTraceDB.sql" "sql/02_ReferenceData_SopalTraceDB.sql"

if [ "$MODE" = "demo" ]; then
    telecharger "${BASE_URL}/${SQL_DIR}/03_SageMirrorData_SopalTraceDB.sql" "sql/03_SageMirrorData_SopalTraceDB.sql"
    telecharger "${BASE_URL}/${SQL_DIR}/04_DemoData_SopalTraceDB.sql" "sql/04_DemoData_SopalTraceDB.sql"
    telecharger "${BASE_URL}/${SQL_DIR}/Demo_Comptes_Services.sql" "sql/Demo_Comptes_Services.sql" 2>/dev/null || true
    telecharger "${BASE_URL}/${SQL_DIR}/Demo_Commandes_Sage.sql" "sql/Demo_Commandes_Sage.sql" 2>/dev/null || true
fi

echo -e "${VERT}✅ Fichiers téléchargés${RESET}"

# =============================================================================
# 4. GÉNÉRATION DU .env
# =============================================================================
echo ""
echo -e "${BLEU}── Génération du fichier .env ──${RESET}"

cat > .env << EOF
# Généré automatiquement par installer.sh — $(date)
SOPALTRACE_VERSION=latest
SOPALTRACE_REGISTRY=ghcr.io/nada-belghith
SOPALTRACE_HTTP_PORT=80
SOPALTRACE_BACKUP_DIR=./sauvegardes

MSSQL_PID=Express
DB_PASSWORD=${DB_PASSWORD}

INIT_SCRIPTS=${INIT_SCRIPTS}

Jwt__Secret=${JWT_SECRET}
Auth__FrontendBaseUrl=${FRONTEND_URL}

SMTP_SERVER=${SMTP_SERVER}
SMTP_PORT=${SMTP_PORT}
SMTP_USERNAME=${SMTP_USERNAME}
EMAIL_SENDER=${EMAIL_SENDER}
EMAIL_PASSWORD=${EMAIL_PASSWORD}
EOF

mkdir -p sauvegardes
echo -e "${VERT}✅ Fichier .env créé${RESET}"

# =============================================================================
# 5. LOGIN GHCR.IO ET PULL DES IMAGES
# =============================================================================
echo ""
echo -e "${BLEU}── Connexion à ghcr.io et téléchargement des images ──${RESET}"

echo "$GITHUB_TOKEN" | docker login ghcr.io -u "$GITHUB_USER" --password-stdin

docker compose -f docker-compose.prod.yml pull
echo -e "${VERT}✅ Images téléchargées${RESET}"

# =============================================================================
# 6. CRÉATION DE LA BASE
# =============================================================================
echo ""
echo -e "${BLEU}── Création de la base de données ──${RESET}"

docker compose -f docker-compose.prod.yml --profile init run --rm init-db
echo -e "${VERT}✅ Base de données créée${RESET}"

# =============================================================================
# 7. DÉMARRAGE DES SERVICES
# =============================================================================
echo ""
echo -e "${BLEU}── Démarrage des services ──${RESET}"

docker compose -f docker-compose.prod.yml up -d
echo -e "${VERT}✅ Services démarrés${RESET}"

# =============================================================================
# 8. ATTENTE DU HEALTH CHECK
# =============================================================================
echo ""
echo -e "${BLEU}── Vérification du démarrage ──${RESET}"

MAX_ESSAIS=30
for i in $(seq 1 $MAX_ESSAIS); do
    if curl -sf http://localhost/api/health > /dev/null 2>&1; then
        echo -e "${VERT}✅ API en ligne (Healthy)${RESET}"
        break
    fi
    if [ $i -eq $MAX_ESSAIS ]; then
        echo -e "${JAUNE}⚠ L'API ne répond pas encore. Vérifiez avec : docker compose -f docker-compose.prod.yml logs sopaltrace.api${RESET}"
    fi
    echo "  Attente... ($i/$MAX_ESSAIS)"
    sleep 5
done

# =============================================================================
# 9. INSERTION DU PREMIER ADMINISTRATEUR
# =============================================================================
echo ""
echo -e "${BLEU}── Création du premier administrateur (matricule: ${ADMIN_MATRICULE}) ──${RESET}"

docker compose -f docker-compose.prod.yml exec -T sqlserver \
    /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "${DB_PASSWORD}" -d SopalTraceDB \
    -Q "INSERT INTO dbo.PreassignedRole (Matricule, RoleCode, AssignedBy) VALUES ('${ADMIN_MATRICULE}', 'ADMIN', '${ASSIGNED_BY}');"

echo -e "${VERT}✅ Rôle ADMIN pré-attribué au matricule ${ADMIN_MATRICULE}${RESET}"

# =============================================================================
# 10. SCRIPTS DE DÉMO (si mode démo)
# =============================================================================
if [ "$MODE" = "demo" ] && [ -f "sql/Demo_Comptes_Services.sql" ]; then
    echo ""
    echo -e "${BLEU}── Exécution des scripts de démonstration ──${RESET}"

    docker compose -f docker-compose.prod.yml exec -T sqlserver \
        /opt/mssql-tools18/bin/sqlcmd -C -I -b -f 65001 -S localhost -U sa -P "${DB_PASSWORD}" -d SopalTraceDB \
        -i "/var/opt/mssql/backup/../../../scripts/Demo_Comptes_Services.sql" 2>/dev/null || \

    docker cp "sql/Demo_Comptes_Services.sql" "$(docker compose -f docker-compose.prod.yml ps -q sqlserver)":/tmp/demo1.sql && \
    docker compose -f docker-compose.prod.yml exec -T sqlserver \
        /opt/mssql-tools18/bin/sqlcmd -C -I -b -f 65001 -S localhost -U sa -P "${DB_PASSWORD}" -d SopalTraceDB \
        -i /tmp/demo1.sql

    if [ -f "sql/Demo_Commandes_Sage.sql" ]; then
        docker cp "sql/Demo_Commandes_Sage.sql" "$(docker compose -f docker-compose.prod.yml ps -q sqlserver)":/tmp/demo2.sql && \
        docker compose -f docker-compose.prod.yml exec -T sqlserver \
            /opt/mssql-tools18/bin/sqlcmd -C -I -b -f 65001 -S localhost -U sa -P "${DB_PASSWORD}" -d SopalTraceDB \
            -i /tmp/demo2.sql
    fi

    echo -e "${VERT}✅ Données de démonstration installées${RESET}"
    echo -e "${JAUNE}  Comptes démo : 88881 (achats), 88882 (commercial), 88883 (qualité), 88884 (DI)${RESET}"
fi

# =============================================================================
# RÉSUMÉ
# =============================================================================
echo ""
echo -e "${VERT}${GRAS}"
echo "╔══════════════════════════════════════════════════╗"
echo "║         ✅ Installation terminée !               ║"
echo "╚══════════════════════════════════════════════════╝"
echo -e "${RESET}"
echo -e "  🌐 Site        : ${GRAS}http://localhost${RESET}"
echo -e "  🔍 Health check: ${GRAS}http://localhost/api/health${RESET}"
echo -e "  👤 Admin       : matricule ${GRAS}${ADMIN_MATRICULE}${RESET} → s'inscrire depuis la page d'inscription"
echo ""
echo -e "  📁 Dossier     : ${DOSSIER_INSTALL}"
echo -e "  📋 Logs API    : docker compose -f docker-compose.prod.yml logs -f sopaltrace.api"
echo -e "  🔄 Redémarrer  : docker compose -f docker-compose.prod.yml restart"
echo -e "  ⬇  Mise à jour : modifier SOPALTRACE_VERSION dans .env, puis pull + up -d"
echo ""
if [ "$MODE" = "demo" ]; then
    echo -e "${JAUNE}  ⚠ Mode DÉMONSTRATION — ne pas utiliser tel quel en production${RESET}"
fi
