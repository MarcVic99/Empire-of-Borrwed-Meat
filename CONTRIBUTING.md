# Flujo de trabajo Git

- `main` contiene las versiones publicadas. Las versiones se integran mediante una pull request desde `develop` y se identifican con una etiqueta de versión.
- `develop` es la rama de integración. Las ramas de trabajo salen de ella y regresan mediante una pull request. La primera rama de trabajo es `first-prototype`.
- Para las siguientes tareas, usa `feature/<nombre>` o `fix/<nombre>`. Una corrección urgente de una versión publicada sale de `main` como `hotfix/<nombre>` y se integra también en `develop`.
- En cada pull request, resume los cambios y anota cómo los verificaste en Unity. Versiona los archivos `.meta` junto a sus recursos. Unity regenera `Library`, `Temp` y otros directorios que figuran en `.gitignore`.
