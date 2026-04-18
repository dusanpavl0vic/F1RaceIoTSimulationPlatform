# f1-dashboard

Next.js dashboard organizovan po feature-folder obrascu.

## Pokretanje

```bash
npm install
npm run dev
```

## Sta je vec podeseno

- Redux Toolkit store
- RTK Query `baseApi`
- feature-folder struktura za `app` i `race-state`
- UI komponente izdvojene u `components/`
- osnovni SignalR hook i dashboard normalizeri u okviru `race-state` feature-a
- provider u root layout-u
- MUI tema i `styled-components` theme
- globalni breakpoint helper-i i `useDevice` hook
- device alias-i kroz `theme.devices.mobile`, `theme.devices.tablet`, `theme.devices.desktop`

Struktura:

```text
components/
  layout/
  race-state/
  providers/
features/
  app/
    appUiSlice.ts
    appSelectors.ts
  race-state/
    raceStateApi.ts
    raceStateUiSlice.ts
    raceStateSelectors.ts
    raceStateTypes.ts
    raceStateDashboard.ts
    useRaceDashboardLive.ts
    useSignalR.ts
    liveDashboardState.ts
store/
  baseApi.ts
  index.ts
hooks/
theme/
```

Env primer:

```bash
cp .env.example .env.local
```
