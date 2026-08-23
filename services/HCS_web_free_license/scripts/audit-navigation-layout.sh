#!/usr/bin/env bash
set -euo pipefail

script_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
solution_root=$(CDPATH= cd -- "$script_dir/.." && pwd)
layout_css="$solution_root/src/HCS.Blazor.Client/Layouts/HCSMainLayout.razor.css"

required_rules=(
    '.hcs-top-nav > .hcs-nav-menu {'
    'align-self: stretch;'
    'display: flex;'
    '.hcs-top-nav ::deep .hcs-top-nav__link {'
    'align-self: stretch;'
    'height: auto;'
    'flex-direction: column;'
)

for rule in "${required_rules[@]}"; do
    if ! grep -Fq "$rule" "$layout_css"; then
        printf 'ERROR: missing desktop navigation alignment guard: %s\n' "$rule" >&2
        exit 1
    fi
done

if grep -Fq 'justify-content: flex-center;' "$layout_css"; then
    printf 'ERROR: invalid flex alignment value remains in navigation CSS\n' >&2
    exit 1
fi

printf 'Desktop navigation alignment audit passed.\n'
