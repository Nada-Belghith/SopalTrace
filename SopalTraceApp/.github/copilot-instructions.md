# Copilot Instructions

## General Guidelines
- Préférence utilisateur : les méthodes doivent être courtes, lisibles, SOLID, et réutiliser les méthodes existantes plutôt que dupliquer la logique (ex: restauration/versioning).
- En cas d'erreurs de mise à jour récursive maximale (ex: 'Maximum recursive updates exceeded'), stringifiez l'état et comparez-le avant l'assignation ou l'émission pour éviter les mises à jour cycliques.

## Validation Rules
- Règles de validation pour les lignes de plan de fabrication : tous les champs sont optionnels sauf ValeurNominale, ToleranceSuperieure, ToleranceInferieure et TypeControleId. Si le type de contrôle est 'VISUEL', ValeurNominale, ToleranceSuperieure et ToleranceInferieure ne sont pas obligatoires. Il doit y avoir soit une ValeurNominale soit un TypeCaracteristiqueId (pas les deux vides). LibelleAffiche n'est plus obligatoire.