# Telemetry Prediction Service

FastAPI mikroservis za predikciju vremena sledeceg kruga.

Podrazumevani workflow:

1. Offline generises dataset iz canonical `.jsonl` fajlova.
2. Offline istreniras model i sacuvas `.joblib` artifact.
3. Runtime servis samo ucita model i radi inference.

Primer treninga iz kontejnera:

```bash
docker compose run --rm telemetry-prediction-service \
  python -m app.training.train \
  --canonical-dir /data/canonical \
  --output-dataset /models/training/next_lap_training.csv \
  --output-model /models/next_lap_model.joblib
```

Posle toga normalan runtime:

```bash
docker compose up telemetry-prediction-service
```

Opcioni demo mode:

- `TRAIN_ON_STARTUP=true`
- servis ce pri startup-u sam pokusati da napravi dataset i model

To je zgodno za demo, ali nije preporuceno za production runtime.
