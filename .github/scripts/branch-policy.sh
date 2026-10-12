#!/usr/bin/env bash
# Política de ramas de IstpetDev (ver CONTRIBUTING.md).
#
#   main    ← develop | hotfix/*
#   develop ← feature/* | fix/* | chore/* | docs/* | hotfix/*
#
# Uso:
#   branch-policy.sh pr        HEAD_REF BASE_REF HEAD_REPO BASE_REPO
#   branch-policy.sh push      BRANCH SHA REPO          (requiere GH_TOKEN)
#   branch-policy.sh dispatch  BRANCH                   (ejecución manual: solo main)
#
# Los nombres de rama llegan como argumentos, nunca interpolados en el YAML.
set -euo pipefail

SUMMARY="${GITHUB_STEP_SUMMARY:-/dev/null}"
WORK_BRANCH='^(feature|fix|chore|docs|hotfix)/[a-z0-9][a-z0-9._-]*$'
HOTFIX_BRANCH='^hotfix/[a-z0-9][a-z0-9._-]*$'

fail() {
  echo "::error title=Política de ramas::$1"
  printf '### ❌ Política de ramas\n\n%s\n\nConsulta CONTRIBUTING.md para el flujo de ramas.\n' "$1" >> "$SUMMARY"
  exit 1
}

pass() {
  echo "$1"
  printf '### ✅ Política de ramas\n\n%s\n' "$1" >> "$SUMMARY"
}

# Valida que HEAD pueda integrarse en BASE.
check_source() {
  local head="$1" base="$2"
  case "$base" in
    main)
      [ "$head" = "develop" ] || [[ "$head" =~ $HOTFIX_BRANCH ]] ||
        fail "\`$head\` → \`main\`: main solo acepta cambios desde 'develop' o 'hotfix/<descripcion>'."
      ;;
    develop)
      [[ "$head" =~ $WORK_BRANCH ]] ||
        fail "\`$head\` → \`develop\`: develop solo acepta 'feature/', 'fix/', 'chore/', 'docs/' o 'hotfix/' seguidos de una descripción en minúsculas con guiones (ej. feature/planner-rutas)."
      ;;
    *)
      fail "\`$head\` → \`$base\`: destino no contemplado por la política."
      ;;
  esac
}

mode="${1:-}"
shift || true

case "$mode" in
  pr)
    head="$1" base="$2" head_repo="$3" base_repo="$4"
    [ "$head_repo" = "$base_repo" ] ||
      fail "Solo se aceptan PR desde ramas de este repositorio, no desde forks ($head_repo)."
    check_source "$head" "$base"
    pass "PR \`$head\` → \`$base\` permitido."
    ;;

  push)
    branch="$1" sha="$2" repo="$3"
    # Un push válido es el merge de un PR: su commit resultante es el merge_commit_sha
    # de un PR ya fusionado hacia esta rama. La asociación puede tardar unos segundos.
    # Se exige también que la rama de origen sea de este repositorio: un fork puede
    # llamar "develop" o "hotfix/x" a su rama y pasaría la validación solo por el nombre.
    head=""
    for attempt in 1 2 3 4 5 6; do
      head=$(gh api "repos/$repo/commits/$sha/pulls" --jq \
        "[.[] | select(.merged_at != null and .base.ref == \"$branch\" and .merge_commit_sha == \"$sha\" and .head.repo.full_name == \"$repo\")][0].head.ref // empty")
      [ -n "$head" ] && break
      echo "Sin PR asociado al commit todavía (intento $attempt/6)..."
      sleep "${POLICY_RETRY_DELAY:-5}"
    done
    [ -n "$head" ] ||
      fail "Push directo a \`$branch\` detectado (commit \`${sha:0:7}\` no proviene de un PR fusionado desde una rama de este repositorio). Todo cambio debe entrar por Pull Request."
    check_source "$head" "$branch"
    pass "Push a \`$branch\` proviene del PR de \`$head\`: permitido."
    ;;

  dispatch)
    branch="$1"
    [ "$branch" = "main" ] ||
      fail "Ejecución manual desde \`$branch\` rechazada: los workflows de producción solo se ejecutan sobre 'main'."
    pass "Ejecución manual sobre \`main\` permitida."
    ;;

  *)
    fail "Modo desconocido: '$mode' (usar pr, push o dispatch)."
    ;;
esac
