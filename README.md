# TMV Service
![Unity](https://img.shields.io/badge/made%20with-Unity-000000?logo=unity)
![Last Commit](https://img.shields.io/github/last-commit/truongmv02/service-based-architecture)
![Repo Size](https://img.shields.io/github/repo-size/truongmv02/service-based-architecture)
![Release](https://img.shields.io/github/v/release/truongmv02/service-based-architecture)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)

Thread-safe service container for Unity. Supports Singleton and Transient lifetimes, auto-dispose via IService, and an optional static ServiceLocator facade.

---

## Features

- **`ServiceContainer`** — concrete, scoped container; one per app session, level, or test case
- **`ServiceLocator`** — optional static facade for projects that do not need explicit DI
- **Singleton & Transient** lifetimes with lazy factory support
- **Auto-dispose** — services implementing `IService` are disposed on `Remove` or `Clear`
- **Thread-safe** reads via `ReaderWriterLockSlim`; concurrent singleton resolution without duplicate creation

---

## Install

**Option A — `Packages/manifest.json`**

```json
{
  "dependencies": {
    "com.tmv.service": "https://github.com/truongmv02/service-based-architecture.git"
  }
}
```

**Option B — Package Manager UI**

*Window → Package Manager → + → Add package from git URL*

```
https://github.com/truongmv02/service-based-architecture.git
```

To track `main` instead of a tag, omit the `#1.0.0` suffix. Unity does not auto-update git packages — run *Package Manager → Update* manually when a new version ships.

---

## Quick Start

### Static ServiceLocator (opt-in)

```csharp
// GameBootstrap.cs — runs before all scenes
void Awake()
{
    ServiceLocator.Add<IAudioService>(new AudioService());
    ServiceLocator.Add<IStorageService>(new PlayerPrefsStorage());

    // Factory: AnalyticsService depends on IAudioService
    ServiceLocator.Add<IAnalyticsService>(
        c => new AnalyticsService(c.Get<IAudioService>()),
        ServiceLifetime.Singleton
    );
}
```

```csharp
// Anywhere in gameplay code
var audio = ServiceLocator.Get<IAudioService>();

if (ServiceLocator.TryGet<IAudioService>(out var audio))
    audio.Play("sfx_coin");
```

### Explicit DI — no statics

```csharp
public class LevelController
{
    private readonly IAudioService _audio;

    public LevelController(IServiceContainer container)
    {
        _audio = container.Get<IAudioService>();
    }
}

// Scoped container per level — dispose on level exit
var levelContainer = new ServiceContainer();
levelContainer.Add<IAudioService>(ServiceLocator.Get<IAudioService>());
var controller = new LevelController(levelContainer);
```

---

## API Reference

| Method | Description |
|--------|-------------|
| `Add<T>(instance)` | Register a pre-created singleton. Throws if `T` is already registered. |
| `Add<T>(factory, lifetime)` | Register via factory. `Singleton` (default) or `Transient`. |
| `Replace<T>(instance)` | Overwrite an existing registration. Throws if `T` is not registered. |
| `Replace<T>(factory, lifetime)` | Overwrite with a factory. Disposes the previous singleton if it implements `IService`. |
| `Remove<T>()` | Unregister and dispose if `IService`. Throws if not registered. |
| `Clear()` | Remove all registrations, disposing all `IService` instances. |
| `Get<T>()` | Resolve. Throws `InvalidOperationException` if not registered. |
| `TryGet<T>(out T)` | Safe resolve. Returns `false` instead of throwing. |
| `IsExists<T>()` | Returns `true` if `T` is registered. |

All members above exist on both `ServiceContainer` (instance) and `ServiceLocator` (static facade).

---

## Lifetimes

**`Singleton`** (default) — the factory is called once; the instance is cached and reused on every `Get`.

**`Transient`** — the factory is called on every `Get`. The container does not track Transient instances; the caller owns their lifecycle.

---

## Auto-Dispose with IService

Implement `IService` (which extends `IDisposable`) to opt in to automatic cleanup:

```csharp
public class AudioService : IAudioService, IService
{
    public void Play(string id) { ... }

    // Called automatically when Remove<AudioService>() or Clear() is called
    public void Dispose() { /* release native audio sources */ }
}
```

---

## Testing

### Without the static facade

```csharp
[Test]
public void WhenLevelStarts_AudioIsPlayed()
{
    var container = new ServiceContainer();
    container.Add<IAudioService>(new FakeAudioService());

    var sut = new LevelController(container);
    sut.Start();

    Assert.That(container.Get<IAudioService>() is FakeAudioService fake
        && fake.PlayCallCount == 1);

    container.Dispose();
}
```

### With the static facade

Call `ServiceLocator.ResetForTesting()` in `[TearDown]` to restore a clean container between tests:

```csharp
[TestFixture]
public class BootstrapTests
{
    [TearDown]
    public void TearDown() => ServiceLocator.ResetForTesting();

    [Test]
    public void AfterBootstrap_AudioServiceIsResolvable()
    {
        GameBootstrap.Initialize();
        Assert.That(ServiceLocator.IsExists<IAudioService>(), Is.True);
    }
}
```

---

## Design Notes

**Transient disposal** — Transient instances are not tracked by the container. If a Transient service implements `IService`, the caller is responsible for calling `Dispose`.

**Level scoping** — create a `new ServiceContainer()` at level start and call `container.Dispose()` on exit. No named-scope API is needed.

---

## Links

- [Design document & full source listing](service.md)
- [Repository](https://github.com/truongmv02/service-based-architecture)

---

## License

MIT © truongmv — see [LICENSE](LICENSE).
