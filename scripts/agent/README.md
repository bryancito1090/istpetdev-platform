# Agente logístico local (n8n)

`local-up.sh` construye los workflows versionados, los importa y los publica en
la instancia local de n8n 2.39.5 (perfil `agent` de
`infra/compose/docker-compose.local.yml`). Funciona tanto con un volumen nuevo
como con Bash en Linux/macOS o Git Bash en Windows.

## Requisitos

- Docker Engine o Docker Desktop con Compose v2.
- Python 3 disponible como `python3` o, en Windows, mediante el lanzador `py -3`.
- Un archivo `.env` completo, copiado desde `.env.example`.

## `ISTPETDEV_API_BASE_URL`

Indica la URL de la API tal como la ve n8n desde dentro del contenedor:

- **Windows/macOS (Docker Desktop, incluido Git Bash):**
  `http://host.docker.internal:5000/api/v1`.
- **Linux con Docker Engine:** el gateway del bridge, por ejemplo
  `http://172.18.0.1:5000/api/v1`. `host.docker.internal` también resuelve
  porque el Compose local declara `host-gateway`.

## Uso

```bash
scripts/agent/local-up.sh
```

El script tolera una exportación vacía (instalación nueva), copia los workflows
con `MSYS_NO_PATHCONV=1` y `cygpath` en Git Bash, y al terminar deja los cuatro
workflows publicados y los webhooks `watch` e `incident` rechazando firmas
inválidas con HTTP 401.
