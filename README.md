# F1RaceIoTSimulationPlatform

Trenutno aktivni deo projekta je `f1-feed-replay-service`.

Servis radi sledece:
- prima `Index.json` URL za konkretnu F1 sesiju
- opciono filtrira koje feedove zelis da skine
- skida feed fajlove u lokalni storage po sesiji
- generise replay konfiguraciju
- ucitava i pokrece replay
- objavljuje raw evente na MQTT topic-e

Najbitniji endpointi:
- `POST /api/replay/bootstrap`
- `POST /api/replay/load`
- `POST /api/replay/start`
- `POST /api/replay/pause`
- `POST /api/replay/resume`
- `POST /api/replay/stop`
- `POST /api/replay/speed`
- `GET /api/replay/status`
- `GET /health`

Primer `bootstrap` zahteva:

```json
{
  "indexUrl": "https://livetiming.formula1.com/static/2021/2021-09-12_Italian_Grand_Prix/2021-09-12_Race/Index.json",
  "feedNames": ["TimingData", "TrackStatus", "WeatherData"],
  "downloadFeeds": true,
  "forceDownload": false,
  "loadAfterDownload": true,
  "startAfterLoad": false
}
```

Lokalno pokretanje:

```bash
docker compose up --build
```

Posle toga servis slusa na `http://localhost:8080`.
