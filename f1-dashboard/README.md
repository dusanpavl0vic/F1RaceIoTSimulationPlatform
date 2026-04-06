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
store/
```

Env primer:

```bash
cp .env.example .env.local
```
