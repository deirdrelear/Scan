# Scan / MinerWatch

Ветка `feature/minerwatch-foundation` развивает **MinerWatch** — внешний наблюдатель
за несколькими окнами EVE. Он должен показывать заполнение трюма, состояние двух
харвестеров и GREEN / YELLOW / RED / UNKNOWN. Управления клиентом нет.

**Текущий результат: проверяемое ядро и инструменты разработки. Это ещё не работающий
монитор EVE.** Live capture, распознавание реальных кадров, scheduler и панель WPF
пока не реализованы. Старый LocalScan запускается и собирается отдельно.

## Что уже работает

- Реестр логических клиентов: постоянный ID из конфигурации, перепривязка окна,
  неоднозначные заголовки, исчезновение, минимизация, смена размера/DPI/процесса.
- Один JSON-профиль для одинаковых UI; шесть якорей ROI; строгая проверка геометрии.
- Типизированные границы capture → pixels → detectors → measurements → state.
- Автомат состояний с гистерезисом, подтверждением новыми кадрами, проверкой
  уверенности, возраста и согласованности измерений.
- Offline replay измерений и Windows-команда для чтения метаданных окон.

## Сборка и проверки

Для новых инструментов нужен **.NET 10 SDK**. Ядро собирается для `netstandard2.0`,
чтобы позже его можно было подключить к существующему WPF/.NET Framework 4.8.
При первой сборке restore загружает reference package NETStandard.Library с NuGet.

Из корня репозитория:

```powershell
dotnet build MinerWatch.slnx -c Release -m:1
dotnet run --project tests/MinerWatch.Tests -c Release --no-build -- .
dotnet run --project src/MinerWatch.Tool -c Release --no-build -- replay profiles/mining.example.json tests/fixtures/measurements/mining-session.jsonl
```

Тесты — обычное консольное приложение с ненулевым exit code при ошибке.
`dotnet test` эти проверки не запускает. Replay выдаёт JSONL и работает без EVE и Windows;
это прогон заранее заданных измерений, **не проверка CV**.
GitHub Actions запускает сборку и эти же проверки на Linux и Windows. Это не заменяет
проверку захвата на машине с EVE.

Windows discovery, не запускающее захват или управление окнами:

```powershell
dotnet run --project src/MinerWatch.Tool -c Release --no-build -- windows
```

Вывод содержит HWND, PID, время запуска процесса, заголовок, класс, размер клиентской
области, DPI, признаки минимизации/скрытия и имя персонажа, если оно распознано.
Геометрия читается в физических пикселях. Неудачный доступ к метаданным процесса
не обходит ограничения Windows: окно пропускается.

Проверка структуры профиля:

```powershell
dotnet run --project src/MinerWatch.Tool -c Release --no-build -- profile profiles/mining.example.json
```

Для примера ожидается `captureReady: false`, `ProfileNotCalibrated`, exit code **2**.
Координаты в нём демонстрационные. Флаг `calibrated` сам по себе не доказывает
правильность распознавания; настоящий профиль требует размеченных кадров.
Команда проверяет профиль на его reference geometry, а не совместимость с живым окном.

## Следующий этап

1. На Windows проверить discovery с 3–5 перекрывающимися, несвёрнутыми окнами,
   затем со всеми клиентами; сохранить вывод до/после перезапуска одного окна.
2. Собрать кадры трюма и колец модулей по [fixture checklist](tests/fixtures/images/README.md).
3. На этих кадрах реализовать детекторы и общий профиль с проверкой наличия нужного UI.
4. Подключить Windows capture и scheduler; сравнить WGC и DWM Atlas по
   [benchmark protocol](docs/CAPTURE_BENCHMARK.md).
5. После этого подключать панель WPF. Изменений старого интерфейса пока нет.

## Документы

- [Нормативный контракт](docs/MINERWATCH_CONTRACT.md)
- [Архитектура и текущая реализация](docs/MINERWATCH_ARCHITECTURE.md)
- [Решения и пересмотр предположений](docs/adr/0001-observer-core.md)
- [Правила для следующих агентов](AGENTS.md)

`EveProj.sln`, `LocalScan/` и `Common/` — прежнее приложение. Новый `MinerWatch.slnx`
не ссылается на эти проекты и не включает их PrintWindow/BitBlt/BringToFront helpers.
