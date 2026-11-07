# Politique de sécurité

## Signaler une vulnérabilité

Si vous découvrez une vulnérabilité de sécurité dans Scaffolder, merci de **ne pas** l'ouvrir comme une issue publique.

Envoyez un e-mail à **koffilevis21@gmail.com** avec :

- Une description de la vulnérabilité
- Les étapes pour la reproduire
- L'impact potentiel
- Une suggestion de correction (optionnel)

Nous nous engageons à :

- Accuser réception sous 48 heures
- Fournir une évaluation initiale sous 7 jours
- Publier un correctif et une note de sécurité dès que possible

## Versions supportées

| Version | Supportée |
|---------|-----------|
| 2.3.x   | ✅        |
| 2.2.x   | ✅ (correctifs de sécurité) |
| < 2.2   | ❌        |

## Bonnes pratiques pour les utilisateurs

- Gardez Scaffolder à jour (`scaffold upgrade`).
- Ne partagez jamais votre clé API IA (`scaffold config set apiKey`).
- La base de données locale est chiffrée (AES-GCM) ; ne la partagez pas.
- Les mises à jour sont vérifiées via SHA-256 ; n'utilisez que les releases officielles.

## Périmètre

- Injection de commande : Scaffolder utilise `ArgumentList` (pas de shell).
- Configuration chiffrée avec permissions 0600.
- Extraction sécurisée des archives (anti path-traversal).
- Interface web locale : écoute sur localhost uniquement, liste blanche de commandes, anti-CSRF.
