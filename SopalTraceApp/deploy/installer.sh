#!/bin/bash
# =============================================================================
# SopalTrace â€” Script d'installation automatique
# Usage : bash installer.sh
# PrÃ©requis : Docker + Docker Compose installÃ©s sur le serveur
# =============================================================================
set -e

# â”€â”€ Couleurs â”€â”€
VERT='\033[0;32m'
JAUNE='\033[1;33m'
ROUGE='\033[0;31m'
BLEU='\033[0;34m'
GRAS='\033[1m'
RESET='\033[0m'

echo -e "${BLEU}${GRAS}"
echo "â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—"
echo "â•‘       SopalTrace â€” Installation automatique     â•‘"
echo "â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•"
echo -e "${RESET}"

# â”€â”€ Configuration â”€â”€
GITHUB_REPO="Nada-Belghith/SopalTrace"
BRANCHE="main"
DOSSIER_INSTALL="$(pwd)/sopaltrace"

# â”€â”€ VÃ©rification Docker â”€â”€
if ! command -v docker &> /dev/null; then
    echo -e "${ROUGE}âŒ Docker n'est pas installÃ©. Installez Docker puis relancez ce script.${RESET}"
    exit 1
fi
if ! docker compose version &> /dev/null; then
    echo -e "${ROUGE}âŒ Docker Compose n'est pas installÃ©.${RESET}"
    exit 1
fi
echo -e "${VERT}âœ… Docker et Docker Compose dÃ©tectÃ©s${RESET}"

# =============================================================================
# 1. TYPE D'INSTALLATION
# =============================================================================
echo ""
echo -e "${GRAS}â”€â”€ Type d'installation â”€â”€${RESET}"
echo "  1) Production  (scripts 01 + 02 uniquement)"
echo "  2) DÃ©monstration / Recette (scripts 01 + 02 + 03 + 04 + comptes dÃ©mo)"
read -p "Choix [1/2] : " TYPE_INSTALL
if [ "$TYPE_INSTALL" = "2" ]; then
    MODE="demo"
    INIT_SCRIPTS="01_Create_SopalTraceDB.sql 02_ReferenceData_SopalTraceDB.sql 03_SageMirrorData_SopalTraceDB.sql 04_DemoData_SopalTraceDB.sql"
    echo -e "${JAUNE}â†’ Mode dÃ©monstration${RESET}"
else
    MODE="prod"
    INIT_SCRIPTS="01_Create_SopalTraceDB.sql 02_ReferenceData_SopalTraceDB.sql"
    echo -e "${VERT}â†’ Mode production${RESET}"
fi

# =============================================================================
# 2. COLLECTE DES SECRETS
# =============================================================================
echo ""
echo -e "${GRAS}â”€â”€ Configuration des secrets â”€â”€${RESET}"

# Mot de passe SQL Server
while true; do
    read -sp "Mot de passe SQL Server (12 car. min., complexe) : " DB_PASSWORD
    echo ""
    if [ ${#DB_PASSWORD} -ge 12 ]; then break; fi
    echo -e "${ROUGE}  Trop court (minimum 12 caractÃ¨res)${RESET}"
done

# ClÃ© JWT
while true; do
    read -sp "ClÃ© secrÃ¨te JWT (32 car. min., alÃ©atoire) : " JWT_SECRET
    echo ""
    if [ ${#JWT_SECRET} -ge 32 ]; then break; fi
    echo -e "${ROUGE}  Trop court (minimum 32 caractÃ¨res)${RESET}"
done

# URL du front
read -p "Adresse du site (ex: https://sopaltrace.sopal.tn) : " FRONTEND_URL
FRONTEND_URL=${FRONTEND_URL:-https://sopaltrace.exemple.tn}

# SMTP
echo ""
echo -e "${GRAS}â”€â”€ Configuration e-mail (SMTP) â”€â”€${RESET}"
read -p "Serveur SMTP (ex: smtp.sopal.tn) : " SMTP_SERVER
read -p "Port SMTP [587] : " SMTP_PORT
SMTP_PORT=${SMTP_PORT:-587}
read -p "Utilisateur SMTP : " SMTP_USERNAME
read -p "E-mail expÃ©diteur : " EMAIL_SENDER
read -sp "Mot de passe e-mail : " EMAIL_PASSWORD
echo ""

# Premier administrateur
echo ""
echo -e "${GRAS}â”€â”€ Premier administrateur â”€â”€${RESET}"
read -p "Matricule Sage du premier administrateur : " ADMIN_MATRICULE
read -p "Qui installe ? (ex: votre nom) [INSTALLATION] : " ASSIGNED_BY
ASSIGNED_BY=${ASSIGNED_BY:-INSTALLATION}

# Token GitHub pour ghcr.io
echo ""
echo -e "${GRAS}â”€â”€ AccÃ¨s au registre d'images (ghcr.io) â”€â”€${RESET}"
echo -e "${JAUNE}  CrÃ©ez un token sur : GitHub â†’ Settings â†’ Developer settings â†’ Personal access tokens"
echo -e "  Droit nÃ©cessaire : read:packages${RESET}"
read -p "Utilisateur GitHub : " GITHUB_USER
read -sp "Token GitHub : " GITHUB_TOKEN
echo ""

# =============================================================================
# 3. CRÃ‰ATION DU DOSSIER ET TÃ‰LÃ‰CHARGEMENT
# =============================================================================
echo ""
echo -e "${BLEU}â”€â”€ TÃ©lÃ©chargement des fichiers â”€â”€${RESET}"

mkdir -p "$DOSSIER_INSTALL/sql"
cd "$DOSSIER_INSTALL"

BASE_URL="https://raw.githubusercontent.com/${GITHUB_REPO}/${BRANCHE}"

# Fonction de tÃ©lÃ©chargement
telecharger() {
    local url="$1"
    local dest="$2"
    echo -e "  â¬‡  $(basename "$dest")"
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

echo -e "${VERT}âœ… Fichiers tÃ©lÃ©chargÃ©s${RESET}"

# =============================================================================
# 4. GÃ‰NÃ‰RATION DU .env
# =============================================================================
echo ""
echo -e "${BLEU}â”€â”€ GÃ©nÃ©ration du fichier .env â”€â”€${RESET}"

cat > .env << EOF
# GÃ©nÃ©rÃ© automatiquement par installer.sh â€” $(date)
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
echo -e "${VERT}âœ… Fichier .env crÃ©Ã©${RESET}"

# =============================================================================
# 5. LOGIN GHCR.IO ET PULL DES IMAGES
# =============================================================================
echo ""
echo -e "${BLEU}â”€â”€ Connexion Ã  ghcr.io et tÃ©lÃ©chargement des images â”€â”€${RESET}"

echo "$GITHUB_TOKEN" | docker login ghcr.io -u "$GITHUB_USER" --password-stdin

docker compose -f docker-compose.prod.yml pull
echo -e "${VERT}âœ… Images tÃ©lÃ©chargÃ©es${RESET}"

# =============================================================================
# 6. CRÃ‰ATION DE LA BASE
# =============================================================================
echo ""
echo -e "${BLEU}â”€â”€ CrÃ©ation de la base de donnÃ©es â”€â”€${RESET}"

docker compose -f docker-compose.prod.yml --profile init run --rm init-db
echo -e "${VERT}âœ… Base de donnÃ©es crÃ©Ã©e${RESET}"

# =============================================================================
# 7. DÃ‰MARRAGE DES SERVICES
# =============================================================================
echo ""
echo -e "${BLEU}â”€â”€ DÃ©marrage des services â”€â”€${RESET}"

docker compose -f docker-compose.prod.yml up -d
echo -e "${VERT}âœ… Services dÃ©marrÃ©s${RESET}"

# =============================================================================
# 8. ATTENTE DU HEALTH CHECK
# =============================================================================
echo ""
echo -e "${BLEU}â”€â”€ VÃ©rification du dÃ©marrage â”€â”€${RESET}"

MAX_ESSAIS=30
for i in $(seq 1 $MAX_ESSAIS); do
    if curl -sf http://localhost/api/health > /dev/null 2>&1; then
        echo -e "${VERT}âœ… API en ligne (Healthy)${RESET}"
        break
    fi
    if [ $i -eq $MAX_ESSAIS ]; then
        echo -e "${JAUNE}âš  L'API ne rÃ©pond pas encore. VÃ©rifiez avec : docker compose -f docker-compose.prod.yml logs sopaltrace.api${RESET}"
    fi
    echo "  Attente... ($i/$MAX_ESSAIS)"
    sleep 5
done

# =============================================================================
# 9. INSERTION DU PREMIER ADMINISTRATEUR
# =============================================================================
echo ""
echo -e "${BLEU}â”€â”€ CrÃ©ation du premier administrateur (matricule: ${ADMIN_MATRICULE}) â”€â”€${RESET}"

docker compose -f docker-compose.prod.yml exec -T sqlserver \
    /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "${DB_PASSWORD}" -d SopalTraceDB \
    -Q "INSERT INTO dbo.PreassignedRole (Matricule, RoleCode, AssignedBy) VALUES ('${ADMIN_MATRICULE}', 'ADMIN', '${ASSIGNED_BY}');"

echo -e "${VERT}âœ… RÃ´le ADMIN prÃ©-attribuÃ© au matricule ${ADMIN_MATRICULE}${RESET}"

# =============================================================================
# 10. SCRIPTS DE DÃ‰MO (si mode dÃ©mo)
# =============================================================================
if [ "$MODE" = "demo" ] && [ -f "sql/Demo_Comptes_Services.sql" ]; then
    echo ""
    echo -e "${BLEU}â”€â”€ ExÃ©cution des scripts de dÃ©monstration â”€â”€${RESET}"

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

    echo -e "${VERT}âœ… DonnÃ©es de dÃ©monstration installÃ©es${RESET}"
    echo -e "${JAUNE}  Comptes dÃ©mo : 88881 (achats), 88882 (commercial), 88883 (qualitÃ©), 88884 (DI)${RESET}"
fi

# =============================================================================
# RÃ‰SUMÃ‰
# =============================================================================
echo ""
echo -e "${VERT}${GRAS}"
echo "â•”â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•—"
echo "â•‘         âœ… Installation terminÃ©e !               â•‘"
echo "â•šâ•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•â•"
echo -e "${RESET}"
echo -e "  ðŸŒ Site        : ${GRAS}http://localhost${RESET}"
echo -e "  ðŸ” Health check: ${GRAS}http://localhost/api/health${RESET}"
echo -e "  ðŸ‘¤ Admin       : matricule ${GRAS}${ADMIN_MATRICULE}${RESET} â†’ s'inscrire depuis la page d'inscription"
echo ""
echo -e "  ðŸ“ Dossier     : ${DOSSIER_INSTALL}"
echo -e "  ðŸ“‹ Logs API    : docker compose -f docker-compose.prod.yml logs -f sopaltrace.api"
echo -e "  ðŸ”„ RedÃ©marrer  : docker compose -f docker-compose.prod.yml restart"
echo -e "  â¬‡  Mise Ã  jour : modifier SOPALTRACE_VERSION dans .env, puis pull + up -d"
echo ""
if [ "$MODE" = "demo" ]; then
    echo -e "${JAUNE}  âš  Mode DÃ‰MONSTRATION â€” ne pas utiliser tel quel en production${RESET}"
fi
