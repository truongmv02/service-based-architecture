# Changelog

All notable changes to this project will be documented in this file.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

---

## [1.0.0] - 2026-05-20

### Added

**Core types (`TMV.Service`)**

- `IServiceContainer` — service registry contract. Methods: `Add<T>`, `Replace<T>`, `Remove<T>`, `Clear`, `Get<T>`, `TryGet<T>`, `IsExists<T>`. Extends `IDisposable`.
- `ServiceContainer` — thread-safe concrete implementation backed by `ReaderWriterLockSlim`. No static state; create one per scope (app session, level, or test case).
  - Fast path for already-cached singletons uses a read lock only, so concurrent `Get` calls do not block each other.
  - Factory invocation happens outside the lock to prevent `LockRecursionException` when a factory resolves other services from the same container.
  - Double-checked locking on singleton creation; duplicate instances created under race conditions are disposed immediately.
- `ServiceLifetime` — enum controlling instance caching: `Singleton` (one per container, lazy) and `Transient` (new instance per `Get`).
- `ServiceDescriptor` — internal record that stores the factory delegate, lifetime, and cached singleton instance.
- `IService` — opt-in marker interface extending `IDisposable`. Services that implement it are automatically disposed when removed via `Remove<T>`, `Replace<T>`, or `Clear`.
- `ServiceLog` — internal debug logger. Emits warnings in Editor and Development builds only; stripped in production.

**Static facade (`TMV.Service`)**

- `ServiceLocator` — optional static entry point exposing the same `Add`, `Replace`, `Remove`, `Clear`, `Get`, `TryGet`, and `IsExists` API. Projects using explicit DI can ignore it entirely.
  - `ResetForTesting()` — swaps in a fresh container; call in `[TearDown]` when using the static facade in tests.
  - `Container` property — exposes the underlying `IServiceContainer` for scope injection or advanced testing.
- `ServiceLocatorScope` — internal wrapper that allows the container to be hot-swapped without changing the public API surface.

**Samples (`TMV.Service.Samples`)**

- `Service Sample` — importable sample demonstrating `Add`, `Get`, `Replace`, and auto-dispose via `IService`.
  - `GameBootstrap` — `MonoBehaviour` that registers `IAudioService` and `IScoreService` on `Awake`.
  - `SampleRunner` — `OnGUI` runner that exercises all major container operations at runtime.
  - `AudioService` / `IAudioService` — stub audio service implementing `IService` to demonstrate auto-dispose.
  - `ScoreService` / `IScoreService` — in-memory score accumulator demonstrating transient-friendly design.

**Tests (`TMV.Service.Tests`)**

- `ServiceContainerTests` (EditMode, 14 tests) — covers instance registration and equality, singleton and transient factory behavior, duplicate registration rejection, `Replace` on existing and missing registrations, `Remove` with auto-dispose, `Clear` and re-registration, unregistered `Get` error, `TryGet` safe pattern, and 32-task concurrent singleton resolution (no duplicates).
- `ServiceLocatorTests` (EditMode, 6 tests) — covers static facade `Add`/`Get` routing, `ResetForTesting` isolation, `Container` property exposure, transient via static facade, `Remove` via static facade, and `TryGet` safety.
- `ServiceContainerPlayModeTests` (PlayMode, 1 test) — verifies that a registered singleton persists across frame boundaries.
