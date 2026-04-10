# f1-dashboard

Osnovni Next.js frontend projekat.

## Pokretanje

```bash
npm install
npm run dev
```

## Sta je vec podeseno

- Redux Toolkit store
- feature `race-state` Redux slice za UI i WebSocket state
- RTK Query base API
- feature struktura za `race-state`
- osnovni WebSocket hook
- provider u root layout-u
- MUI tema i `styled-components` theme
- globalni breakpoint helper-i i `useDevice` hook
- device alias-i kroz `theme.devices.mobile`, `theme.devices.tablet`, `theme.devices.desktop`

Struktura:

```text
components/
  layout/
  providers/
features/
  race-state/
    api/
    components/
    hooks/
    store/
hooks/
store/
theme/
```

Env primer:

```bash
cp .env.example .env.local
```
