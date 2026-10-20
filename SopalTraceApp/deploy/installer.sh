#!/bin/bash
# =============================================================================
# SopalTrace - Script d'installation automatique
# Usage:
#   curl -fsSLO https://raw.githubusercontent.com/Nada-Belghith/SopalTrace/main/SopalTraceApp/deploy/installer.sh
#   bash installer.sh
# =============================================================================
set -e

# Couleurs ANSI universelles
VERT='\033[0;32m'
JAUNE='\033[1;33m'
ROUGE='\033[0;31m'
BLEU='\033[0;34m'
CYAN='\033[0;36m'
GRAS='\033[1m'
RESET='\033[0m'

# Helpers pour la saisie interactive (compatibles curl | bash via /dev/tty)
prompt_read() {
    local text="$1"
    local var_name="$2"
    if [ -e /dev/tty ]; then
        read -r -p "$text" "$var_name" < /dev/tty
    else
        read -r -p "$text" "$var_name"
    fi
}

prompt_secret() {
    local text="$1"
    local var_name="$2"
    if [ -e /dev/tty ]; then
        read -r -s -p "$text" "$var_name" < /dev/tty
    else
        read -r -s -p "$text" "$var_name"
    fi
    echo ""
}

echo -e "${BLEU}${GRAS}"
echo "+---------------------------------------------------+"
echo "|       SopalTrace - Installation automatique       |"
echo "+---------------------------------------------------+"
echo -e "${RESET}"

GITHUB_REPO="Nada-Belghith/SopalTrace"
BRANCHE="main"
DOSSIER_INSTALL="$(pwd)/sopaltrace"

# Verification Docker
if ! command -v docker &> /dev/null; then
    echo -e "${ROUGE}[ERREUR] Docker n'est pas installe. Veuillez installer Docker.${RESET}"
    exit 1
fi
if ! docker compose version &> /dev/null; then
    echo -e "${ROUGE}[ERREUR] Docker Compose n'est pas installe.${RESET}"
    exit 1
fi
echo -e "${VERT}[OK] Docker et Docker Compose detectes.${RESET}"

# =============================================================================
# 1. TYPE D'INSTALLATION
# =============================================================================
echo ""
echo -e "${GRAS}--- Type d'installation ---${RESET}"
echo "  1) Production   (scripts 01 + 02 uniquement)"
echo "  2) Demonstration (scripts 01 + 02 + 03 + 04 + comptes demo)"
prompt_read "Choix [1/2] : " TYPE_INSTALL
if [ "$TYPE_INSTALL" = "2" ]; then
    MODE="demo"
    INIT_SCRIPTS="01_Create_SopalTraceDB.sql 02_ReferenceData_SopalTraceDB.sql 03_SageMirrorData_SopalTraceDB.sql 04_DemoData_SopalTraceDB.sql"
    echo -e "${JAUNE}-> Mode demonstration selectionne.${RESET}"
else
    MODE="prod"
    INIT_SCRIPTS="01_Create_SopalTraceDB.sql 02_ReferenceData_SopalTraceDB.sql"
    echo -e "${VERT}-> Mode production selectionne.${RESET}"
fi

# =============================================================================
# 2. COLLECTE DES SECRETS
# =============================================================================
echo ""
echo -e "${GRAS}--- Configuration des secrets ---${RESET}"

# Mot de passe SQL Server
while true; do
    prompt_secret "Mot de passe SQL Server sa (12 car. min., complexe) : " DB_PASSWORD
    if [ ${#DB_PASSWORD} -ge 12 ]; then break; fi
    echo -e "${ROUGE}  Trop court (minimum 12 caracteres requis).${RESET}"
done

# Cle JWT
while true; do
    prompt_secret "Cle secrete JWT (32 car. min., aleatoire) : " JWT_SECRET
    if [ ${#JWT_SECRET} -ge 32 ]; then break; fi
    echo -e "${ROUGE}  Trop court (minimum 32 caracteres requis).${RESET}"
done

# URL du front
prompt_read "Adresse publique du site (ex: https://sopaltrace.sopal.tn) [http://localhost] : " FRONTEND_URL
FRONTEND_URL=${FRONTEND_URL:-http://localhost}

# SMTP
echo ""
echo -e "${GRAS}--- Configuration e-mail (SMTP) ---${RESET}"
prompt_read "Serveur SMTP (ex: smtp.sopal.tn) : " SMTP_SERVER
prompt_read "Port SMTP [587] : " SMTP_PORT
SMTP_PORT=${SMTP_PORT:-587}
prompt_read "Utilisateur SMTP : " SMTP_USERNAME
prompt_read "E-mail expediteur : " EMAIL_SENDER
prompt_secret "Mot de passe e-mail : " EMAIL_PASSWORD

# Premier administrateur
echo ""
echo -e "${GRAS}--- Premier administrateur ---${RESET}"
prompt_read "Matricule Sage du premier administrateur : " ADMIN_MATRICULE
prompt_read "Qui realise l'installation ? [INSTALLATION] : " ASSIGNED_BY
ASSIGNED_BY=${ASSIGNED_BY:-INSTALLATION}

# Acces aux images Docker (ghcr.io)
echo ""
echo -e "${GRAS}--- Acces aux images Docker (ghcr.io) ---${RESET}"
echo "  Les images Docker sont-elles publiques ou privees ?"
echo "  1) Publiques  (pas besoin de jeton GitHub)"
echo "  2) Privees    (jeton GitHub avec droit read:packages)"
prompt_read "Choix [1/2] : " IMAGES_ACCESS
if [ "$IMAGES_ACCESS" = "2" ]; then
    echo -e "${JAUNE}  Creez un PAT sur : GitHub > Settings > Developer settings > Personal access tokens${RESET}"
    echo -e "${JAUNE}  Droit necessaire : read:packages${RESET}"
    prompt_read "Nom d'utilisateur GitHub : " GITHUB_USER
    prompt_secret "Jeton GitHub (PAT) : " GITHUB_TOKEN
else
    echo -e "${VERT}-> Images publiques, aucune authentification requise.${RESET}"
    GITHUB_USER=""
    GITHUB_TOKEN=""
fi

# =============================================================================
# 3. CREATION DU DOSSIER ET TELECHARGEMENT
# =============================================================================
echo ""
echo -e "${BLEU}--- Telechargement des fichiers de deploiement ---${RESET}"

mkdir -p "$DOSSIER_INSTALL/sql"
cd "$DOSSIER_INSTALL"

BASE_URL="https://raw.githubusercontent.com/${GITHUB_REPO}/${BRANCHE}"

telecharger() {
    local url="$1"
    local dest="$2"
    echo -e "  [>] Telechargement de $(basename "$dest")..."
    if [ -n "$GITHUB_TOKEN" ]; then
        curl -sS -f -H "Authorization: token ${GITHUB_TOKEN}" -o "$dest" "$url"
    else
        curl -sS -f -o "$dest" "$url"
    fi
}

telecharger "${BASE_URL}/SopalTraceApp/deploy/docker-compose.prod.yml" "docker-compose.prod.yml"
telecharger "${BASE_URL}/SopalTraceApp/deploy/docker-compose.https.yml" "docker-compose.https.yml" 2>/dev/null || true
telecharger "${BASE_URL}/SopalTraceApp/deploy/nginx-https.conf" "nginx-https.conf" 2>/dev/null || true

SQL_DIR="SopalTraceApp/SopalTrace.Infrastructure/ScriptsSQL"
telecharger "${BASE_URL}/${SQL_DIR}/01_Create_SopalTraceDB.sql" "sql/01_Create_SopalTraceDB.sql"
telecharger "${BASE_URL}/${SQL_DIR}/02_ReferenceData_SopalTraceDB.sql" "sql/02_ReferenceData_SopalTraceDB.sql"

if [ "$MODE" = "demo" ]; then
    telecharger "${BASE_URL}/${SQL_DIR}/03_SageMirrorData_SopalTraceDB.sql" "sql/03_SageMirrorData_SopalTraceDB.sql"
    telecharger "${BASE_URL}/${SQL_DIR}/04_DemoData_SopalTraceDB.sql" "sql/04_DemoData_SopalTraceDB.sql"
    telecharger "${BASE_URL}/${SQL_DIR}/Demo_Comptes_Services.sql" "sql/Demo_Comptes_Services.sql" 2>/dev/null || true
    telecharger "${BASE_URL}/${SQL_DIR}/Demo_Commandes_Sage.sql" "sql/Demo_Commandes_Sage.sql" 2>/dev/null || true
fi

echo -e "${VERT}[OK] Fichiers telecharges avec succes dans $DOSSIER_INSTALL.${RESET}"

# =============================================================================
# 4. GENERATION DU FICHIER .env
# =============================================================================
echo ""
echo -e "${BLEU}--- Generation du fichier .env ---${RESET}"

cat > .env << EOF
# Genere automatiquement par installer.sh - $(date)
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
echo -e "${VERT}[OK] Fichier .env genere avec succes.${RESET}"

# =============================================================================
# 5. CONNEXION GHCR.IO (SI PRIVE) ET PULL DES IMAGES
# =============================================================================
echo ""
echo -e "${BLEU}--- Telechargement des images Docker ---${RESET}"

if [ -n "$GITHUB_TOKEN" ]; then
    echo "  Connexion a ghcr.io..."
    echo "$GITHUB_TOKEN" | docker login ghcr.io -u "$GITHUB_USER" --password-stdin
fi

docker compose -f docker-compose.prod.yml pull
echo -e "${VERT}[OK] Images Docker recuperees.${RESET}"

# =============================================================================
# 6. INITIALISATION BASE DE DONNEES
# =============================================================================
echo ""
echo -e "${BLEU}--- Initialisation de la base de donnees SQL Server ---${RESET}"

docker compose -f docker-compose.prod.yml --profile init run --rm init-db
echo -e "${VERT}[OK] Base de donnees initialisee.${RESET}"

# =============================================================================
# 7. DEMARRAGE DES SERVICES
# =============================================================================
echo ""
echo -e "${BLEU}--- Demarrage des services en arriere-plan ---${RESET}"

docker compose -f docker-compose.prod.yml up -d
echo -e "${VERT}[OK] Conteneurs demarres.${RESET}"

# =============================================================================
# 8. VERIFICATION DE DISPONIBILITE (HEALTH CHECK)
# =============================================================================
echo ""
echo -e "${BLEU}--- Verification de la disponibilite de l'API ---${RESET}"

MAX_ESSAIS=30
for i in $(seq 1 $MAX_ESSAIS); do
    if curl -sf http://localhost/api/health > /dev/null 2>&1; then
        echo -e "${VERT}[OK] L'API est operationnelle et repond (Healthy).${RESET}"
        break
    fi
    if [ $i -eq $MAX_ESSAIS ]; then
        echo -e "${JAUNE}[ATTENTION] L'API met du temps a demarrer. Consultez les logs avec :${RESET}"
        echo -e "  docker compose -f docker-compose.prod.yml logs sopaltrace.api"
    fi
    echo "  Attente de demarrage... ($i/$MAX_ESSAIS)"
    sleep 5
done

# =============================================================================
# 9. ATTRIBUTION DU ROLE ADMIN
# =============================================================================
echo ""
echo -e "${BLEU}--- Attribution du role ADMIN au matricule ${ADMIN_MATRICULE} ---${RESET}"

docker compose -f docker-compose.prod.yml exec -T sqlserver \
    /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "${DB_PASSWORD}" -d SopalTraceDB \
    -Q "INSERT INTO dbo.PreassignedRole (Matricule, RoleCode, AssignedBy) VALUES ('${ADMIN_MATRICULE}', 'ADMIN', '${ASSIGNED_BY}');"

echo -e "${VERT}[OK] Role ADMIN pre-attribue au matricule ${ADMIN_MATRICULE}.${RESET}"

# =============================================================================
# 10. SCRIPTS DE DEMONSTRATION (SI MODE DEMO)
# =============================================================================
if [ "$MODE" = "demo" ] && [ -f "sql/Demo_Comptes_Services.sql" ]; then
    echo ""
    echo -e "${BLEU}--- Injection des donnees de demonstration ---${RESET}"

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

    echo -e "${VERT}[OK] Donnees de demonstration injectees.${RESET}"
    echo -e "${JAUNE}  Comptes de test : 88881 (achats), 88882 (commercial), 88883 (qualite), 88884 (DI)${RESET}"
fi

# =============================================================================
# RESUME FINAL
# =============================================================================
echo ""
echo -e "${VERT}${GRAS}"
echo "+---------------------------------------------------+"
echo "|            INSTALLATION TERMINEE !                |"
echo "+---------------------------------------------------+"
echo -e "${RESET}"
echo -e "  Application : ${GRAS}http://localhost${RESET}"
echo -e "  Health check: ${GRAS}http://localhost/api/health${RESET}"
echo -e "  Admin       : Matricule ${GRAS}${ADMIN_MATRICULE}${RESET} (creez le compte depuis la page d'inscription)"
echo ""
echo -e "  Dossier     : ${DOSSIER_INSTALL}"
echo -e "  Logs API    : docker compose -f docker-compose.prod.yml logs -f sopaltrace.api"
echo -e "  Redemarrer  : docker compose -f docker-compose.prod.yml restart"
echo -e "  Arreter     : docker compose -f docker-compose.prod.yml down"
echo ""
if [ "$MODE" = "demo" ]; then
    echo -e "${JAUNE}[ATTENTION] Mode DEMONSTRATION actif. A n'utiliser qu'a des fins d'evaluation.${RESET}"
fi