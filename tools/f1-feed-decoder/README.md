# F1 Feed Decoder

Ovo je mali izolovan C# alat za proveru `Position.z.jsonStream` i `CarData.z.jsonStream` fajlova bez pokretanja mikroservisa.

Pokretanje:

```bash
dotnet run --project ./tools/f1-feed-decoder -- --mode position --file ./docker/replay-data/2021-09-12-italian-grand-prix-2021-09-12-race/feeds/Position.z.jsonStream --driver 3 --limit 30
```

Sa preskakanjem nultih koordinata:

```bash
dotnet run --project ./tools/f1-feed-decoder -- --mode position --file ./docker/replay-data/2021-09-12-italian-grand-prix-2021-09-12-race/feeds/Position.z.jsonStream --driver 3 --limit 30 --skip-zero
```

Za telemetriju:

```bash
dotnet run --project ./tools/f1-feed-decoder -- --mode telemetry --file ./docker/replay-data/2021-09-12-italian-grand-prix-2021-09-12-race/feeds/CarData.z.jsonStream --driver 44 --limit 20
```

Alat:

- sece timestamp prefix iz `jsonStream` reda
- skida navodnike oko base64 stringa
- radi base64 decode
- radi raw deflate decode
- parsira JSON i ispisuje podatke za proveru
