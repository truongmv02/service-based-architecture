# TMV.Service — UPM Package Design

> Design document for review before implementation.

---

## Class Diagram

```
┌──────────────────────────────────────────────────────────────┐
│                      «interface»                             │
│                   IServiceContainer                          │
│──────────────────────────────────────────────────────────────│
│ + Add<T>(service)                                            │
│ + Add<T>(factory, lifetime)                                  │
│ + Replace<T>(service)                                        │
│ + Replace<T>(factory, lifetime)                              │
│ + Remove<T>()                                                │
│ + Clear()                                                    │
│ + Get<T>() : T                                               │
│ + TryGet<T>(out T) : bool                                    │
│ + IsExists<T>() : bool                                       │
└──────────────────┬───────────────────────────────────────────┘
                   │ implements
        ┌──────────┴──────────┐
        │                     │
        ▼                     ▼
┌────────────────┐   ┌────────────────────┐
│ServiceContainer│   │ServiceLocatorScope │  «internal»
│────────────────│   │────────────────────│
│- _descriptors  │   │- _container        │
│- _lock         │   │+ Reset()           │
└───────┬────────┘   └────────┬───────────┘
        │ uses                │ wraps
        ▼                     ▼
┌───────────────────┐  ┌───────────────────────────┐
│  ServiceDescriptor│  │     ServiceLocator        │
│  «internal»       │  │   (static facade, opt-in) │
│───────────────────│  │───────────────────────────│
│+ Lifetime         │  │+ Add<T>(...)              │
│+ CachedInstance   │  │+ Get<T>()                 │
│+ Factory          │  │+ Clear()                  │
└───────────────────┘  │+ ResetForTesting()        │
                       └───────────────────────────┘

┌──────────────────┐
│   «interface»    │
│    IService      │   ← optional marker, opt-in Dispose lifecycle
│──────────────────│
│ + Dispose()      │   ← inherited from IDisposable
└──────────────────┘
        ▲
        │ implement (optional)
   MyGameService
```

**Relationships:**
- `ServiceContainer` is the core implementation, depends only on BCL.
- `ServiceLocatorScope` wraps `ServiceContainer`, allowing the container to be swapped
  during tests without changing the public API.
- `ServiceLocator` static is a convenience shortcut — not required.
- `IService` is an optional marker — services that implement it are auto-disposed on Remove/Clear.
- `ServiceDescriptor` is internal — never exposed outside the assembly.

---

## Directory Structure

```
com.tmv.service/                              ← UPM package root
├── package.json
├── CHANGELOG.md
├── README.md
│
├── Runtime/
│   ├── TMV.Service.asmdef
│   ├── Core/
│   │   ├── IService.cs                       ← optional marker interface 
│   │   ├── IServiceContainer.cs              ← main contract
│   │   ├── ServiceLifetime.cs                ← enum: Singleton | Transient
│   │   ├── ServiceDescriptor.cs              ← internal registration record
│   │   └── ServiceContainer.cs              ← concrete implementation
│   └── Global/
│       ├── ServiceLocatorScope.cs            ← internal scope wrapper
│       └── ServiceLocator.cs                 ← static facade (opt-in)
│
└── Tests/
    ├── EditMode/
    │   ├── TMV.Service.Tests.EditMode.asmdef
    │   ├── ServiceContainerTests.cs           ← unit tests for core logic
    │   └── ServiceLocatorTests.cs             ← unit tests for static facade
    └── PlayMode/
        ├── TMV.Service.Tests.PlayMode.asmdef
        └── ServiceContainerPlayModeTests.cs   ← MonoBehaviour tests (if needed)
```

---

## File Details

---

### `package.json`

```json
{
  "name": "com.tmv.service",
  "version": "1.0.0",
  "displayName": "TMV Service",
  "description": "Thread-safe service container for Unity. Supports Singleton and Transient lifetimes, auto-dispose via IService, and an optional static ServiceLocator facade.",
  "unity": "2022.3",
  "author": {
    "name": "truongmv"
  },
  "license": "MIT",
  "repository": {
    "type": "git",
    "url": "https://github.com/truongmv/service-based-architecture.git"
  }
}
```

---

### `Runtime/TMV.Service.asmdef`

```json
{
  "name": "TMV.Service",
  "rootNamespace": "TMV.Service",
  "references": [],
  "autoReferenced": true
}
```

`autoReferenced: true` so game assemblies do not need to declare an explicit reference.

---

### `Runtime/Core/ServiceLifetime.cs`

```csharp
namespace TMV.Service
{
    public enum ServiceLifetime
    {
        /// One instance per container — created on first Resolve, reused after.
        Singleton,

        /// New instance on every Resolve call.
        Transient
    }
}
```

---

### `Runtime/Core/IService.cs`

```csharp
namespace TMV.Service
{
    /// <summary>
    /// Opt-in marker. Services that implement IService are automatically
    /// disposed when removed or when the container is cleared.
    /// </summary>
    public interface IService : System.IDisposable { }
}
```

---

### `Runtime/Core/IServiceContainer.cs`

```csharp
using System;

namespace TMV.Service
{
    public interface IServiceContainer : IDisposable
    {
        void Add<T>(T service) where T : class;

        void Add<T>(
            Func<IServiceContainer, T> factory,
            ServiceLifetime lifetime = ServiceLifetime.Singleton) where T : class;

        void Replace<T>(T service) where T : class;

        void Replace<T>(
            Func<IServiceContainer, T> factory,
            ServiceLifetime lifetime = ServiceLifetime.Singleton) where T : class;

        void Remove<T>() where T : class;

        void Clear();

        T Get<T>() where T : class;

        bool TryGet<T>(out T service) where T : class;

        bool IsExists<T>() where T : class;
    }
}
```

---

### `Runtime/Core/ServiceDescriptor.cs`

```csharp
using System;

namespace TMV.Service
{
    // Internal record — not exposed outside the assembly.
    internal sealed class ServiceDescriptor
    {
        internal ServiceLifetime Lifetime { get; }
        internal object CachedInstance { get; set; }
        internal Func<IServiceContainer, object> Factory { get; }

        internal ServiceDescriptor(
            Func<IServiceContainer, object> factory,
            ServiceLifetime lifetime,
            object seed = null)
        {
            Factory = factory;
            Lifetime = lifetime;
            CachedInstance = seed;
        }
    }
}
```

---

### `Runtime/Core/ServiceContainer.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading;

namespace TMV.Service
{
    /// <summary>
    /// Concrete service container. Thread-safe. No static state.
    /// Create one per scope (app session, level, test case).
    /// </summary>
    public sealed class ServiceContainer : IServiceContainer
    {
        #region Fields

        private readonly Dictionary<Type, ServiceDescriptor> _descriptors = new();

        // ReaderWriterLockSlim allows concurrent reads without blocking each other.
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);

        private bool _disposed;

        #endregion

        #region Public Methods

        public void Add<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            WriteDescriptor(
                typeof(T),
                new ServiceDescriptor(_ => service, ServiceLifetime.Singleton, service),
                allowOverwrite: false);
        }

        public void Add<T>(
            Func<IServiceContainer, T> factory,
            ServiceLifetime lifetime = ServiceLifetime.Singleton) where T : class
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            WriteDescriptor(
                typeof(T),
                new ServiceDescriptor(c => factory(c), lifetime),
                allowOverwrite: false);
        }

        public void Replace<T>(T service) where T : class
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            WriteDescriptor(
                typeof(T),
                new ServiceDescriptor(_ => service, ServiceLifetime.Singleton, service),
                allowOverwrite: true);
        }

        public void Replace<T>(
            Func<IServiceContainer, T> factory,
            ServiceLifetime lifetime = ServiceLifetime.Singleton) where T : class
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            WriteDescriptor(
                typeof(T),
                new ServiceDescriptor(c => factory(c), lifetime),
                allowOverwrite: true);
        }

        public void Remove<T>() where T : class
        {
            _lock.EnterWriteLock();
            try
            {
                var type = typeof(T);
                if (!_descriptors.TryGetValue(type, out var descriptor))
                    throw new InvalidOperationException($"{type.Name} is not added.");
                DisposeDescriptor(descriptor);
                _descriptors.Remove(type);
            }
            finally { _lock.ExitWriteLock(); }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                foreach (var descriptor in _descriptors.Values)
                    DisposeDescriptor(descriptor);
                _descriptors.Clear();
            }
            finally { _lock.ExitWriteLock(); }
        }

        public T Get<T>() where T : class
        {
            if (!TryGet<T>(out var service))
                throw new InvalidOperationException($"{typeof(T).Name} is not added.");
            return service;
        }

        public bool TryGet<T>(out T service) where T : class
        {
            var type = typeof(T);

            // Fast path: singleton already cached — read lock only, does not block concurrent reads.
            _lock.EnterReadLock();
            try
            {
                if (_descriptors.TryGetValue(type, out var d) &&
                    d.Lifetime == ServiceLifetime.Singleton && d.CachedInstance != null)
                {
                    service = (T)d.CachedInstance;
                    return true;
                }
            }
            finally { _lock.ExitReadLock(); }

            return TryGetSlowPath<T>(type, out service);
        }

        public bool IsExists<T>() where T : class
        {
            _lock.EnterReadLock();
            try { return _descriptors.ContainsKey(typeof(T)); }
            finally { _lock.ExitReadLock(); }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Clear();
            _lock.Dispose();
        }

        #endregion

        #region Private/Protected Methods

        private void WriteDescriptor(Type type, ServiceDescriptor descriptor, bool allowOverwrite)
        {
            _lock.EnterWriteLock();
            try
            {
                if (_descriptors.TryGetValue(type, out var existing))
                {
                    if (!allowOverwrite)
                        throw new InvalidOperationException($"{type.Name} is already added. Use Replace.");
                    DisposeDescriptor(existing);
                }
                else if (allowOverwrite)
                {
                    throw new InvalidOperationException($"{type.Name} is not added. Use Add.");
                }
                _descriptors[type] = descriptor;
            }
            finally { _lock.ExitWriteLock(); }
        }

        private bool TryGetSlowPath<T>(Type type, out T service) where T : class
        {
            ServiceDescriptor descriptor;

            _lock.EnterWriteLock();
            try
            {
                if (!_descriptors.TryGetValue(type, out descriptor))
                {
                    service = null;
                    return false;
                }

                // Double-check after acquiring write lock.
                if (descriptor.Lifetime == ServiceLifetime.Singleton && descriptor.CachedInstance != null)
                {
                    service = (T)descriptor.CachedInstance;
                    return true;
                }
            }
            finally { _lock.ExitWriteLock(); }

            // Factory is invoked outside the lock — prevents LockRecursionException when the
            // factory resolves other services from this same container.
            var instance = (T)descriptor.Factory(this);

            if (descriptor.Lifetime == ServiceLifetime.Singleton)
            {
                _lock.EnterWriteLock();
                try
                {
                    // A concurrent get may have already cached an instance; discard ours.
                    if (descriptor.CachedInstance == null)
                        descriptor.CachedInstance = instance;
                    else
                    {
                        if (instance is IService s) s.Dispose();
                        instance = (T)descriptor.CachedInstance;
                    }
                }
                finally { _lock.ExitWriteLock(); }
            }

            service = instance;
            return true;
        }

        private static void DisposeDescriptor(ServiceDescriptor descriptor)
        {
            if (descriptor.CachedInstance is IService s)
                s.Dispose();
        }

        #endregion
    }
}
```

---

### `Runtime/Global/ServiceLocatorScope.cs`

```csharp
using System;

namespace TMV.Service
{
    // Wraps ServiceContainer so ServiceLocator static can swap the container for testing
    // without changing the public API surface.
    internal sealed class ServiceLocatorScope : IServiceContainer
    {
        #region Fields

        private IServiceContainer _container = new ServiceContainer();

        #endregion

        #region Public Methods

        internal void Reset()
        {
            _container.Dispose();
            _container = new ServiceContainer();
        }

        public void Add<T>(T service) where T : class
            => _container.Add(service);

        public void Add<T>(Func<IServiceContainer, T> factory, ServiceLifetime lifetime) where T : class
            => _container.Add(factory, lifetime);

        public void Replace<T>(T service) where T : class
            => _container.Replace(service);

        public void Replace<T>(Func<IServiceContainer, T> factory, ServiceLifetime lifetime) where T : class
            => _container.Replace(factory, lifetime);

        public void Remove<T>() where T : class => _container.Remove<T>();

        public void Clear() => _container.Clear();

        public T Get<T>() where T : class => _container.Get<T>();

        public bool TryGet<T>(out T service) where T : class
            => _container.TryGet(out service);

        public bool IsExists<T>() where T : class => _container.IsExists<T>();

        public void Dispose() => _container.Dispose();

        #endregion
    }
}
```

---

### `Runtime/Global/ServiceLocator.cs`

```csharp
using System;

namespace TMV.Service
{
    /// <summary>
    /// Optional static entry point. Games that prefer explicit DI can skip this entirely.
    /// </summary>
    public static class ServiceLocator
    {
        #region Fields

        private static readonly ServiceLocatorScope _scope = new();

        #endregion

        #region Properties

        /// <summary>Direct access to the underlying container — for scope injection or testing.</summary>
        public static IServiceContainer Container => _scope;

        #endregion

        #region Public Methods

        public static void Add<T>(T service) where T : class
            => _scope.Add(service);

        public static void Add<T>(
            Func<IServiceContainer, T> factory,
            ServiceLifetime lifetime = ServiceLifetime.Singleton) where T : class
            => _scope.Add(factory, lifetime);

        public static void Replace<T>(T service) where T : class
            => _scope.Replace(service);

        public static void Replace<T>(
            Func<IServiceContainer, T> factory,
            ServiceLifetime lifetime = ServiceLifetime.Singleton) where T : class
            => _scope.Replace(factory, lifetime);

        public static void Remove<T>() where T : class => _scope.Remove<T>();

        public static void Clear() => _scope.Clear();

        public static T Get<T>() where T : class => _scope.Get<T>();

        public static bool TryGet<T>(out T service) where T : class
            => _scope.TryGet(out service);

        public static bool IsExists<T>() where T : class => _scope.IsExists<T>();

        /// <summary>Reset to a fresh container. Call in [TearDown] when using the static facade in tests.</summary>
        public static void ResetForTesting() => _scope.Reset();

        #endregion
    }
}
```

---

### `Tests/EditMode/TMV.Service.Tests.EditMode.asmdef`

```json
{
  "name": "TMV.Service.Tests.EditMode",
  "rootNamespace": "TMV.Service.Tests",
  "references": ["TMV.Service", "UnityEngine.TestRunner", "UnityEditor.TestRunner"],
  "includePlatforms": [
    "Editor"
  ],
  "overrideReferences": true,
  "allowUnsafeCode": false,
  "autoReferenced": false,
  "precompiledReferences": ["nunit.framework.dll"],
  "noEngineReferences": false
}
```

---

### `Tests/PlayMode/TMV.Service.Tests.PlayMode.asmdef`

```json
{
  "name": "TMV.Service.Tests.PlayMode",
  "rootNamespace": "TMV.Service.Tests",
  "references": ["TMV.Service", "UnityEngine.TestRunner"],
  "overrideReferences": true,
  "allowUnsafeCode": false,
  "autoReferenced": false,
  "precompiledReferences": ["nunit.framework.dll"],
  "noEngineReferences": false
}
```

---

### `Tests/EditMode/ServiceContainerTests.cs` — outline

```csharp
[TestFixture]
public class ServiceContainerTests
{
    ServiceContainer _container;

    [SetUp]    void SetUp()    => _container = new ServiceContainer();
    [TearDown] void TearDown() => _container.Dispose();

    // Registration
    [Test] void Add_Instance_GetsCorrectly()

    [Test] void Add_Factory_Singleton_ReturnsSameInstance()

    [Test] void Add_Factory_Transient_ReturnsDifferentInstances()

    [Test] void Add_Duplicate_ThrowsInvalidOperation()

    // Replace
    [Test] void Replace_ExistingService_UpdatesInstance()

    [Test] void Replace_NotAdded_ThrowsInvalidOperation()

    [Test] void Replace_Singleton_DisposesOldInstance()

    // Remove
    [Test] void Remove_CallsDisposeOnIService()

    [Test] void Remove_NotAdded_ThrowsInvalidOperation()

    [Test] void Remove_AfterRemoval_IsExistsReturnsFalse()

    // Get
    [Test] void Get_NotAdded_ThrowsInvalidOperation()

    [Test] void TryGet_NotAdded_ReturnsFalse()

    [Test] void TryGet_Added_ReturnsTrueAndInstance()

    // Clear
    [Test] void Clear_DisposesAllIServiceInstances()

    [Test] void Clear_AllowsReAdditionAfterwards()

    // Thread safety
    [Test] void Get_Singleton_32ParallelTasks_ReturnsSameInstance()
}
```

---

### `Tests/EditMode/ServiceLocatorTests.cs` — outline

```csharp
[TestFixture]
public class ServiceLocatorTests
{
    [TearDown] void TearDown() => ServiceLocator.ResetForTesting();

    [Test] void Add_And_Get_ViaStaticFacade()

    [Test] void ResetForTesting_ClearsAllAdditions()

    [Test] void Container_Property_ReturnsActiveScope()
}
```

---

## Usage Guide

### 1. Install via UPM (git URL)

In `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.tmv.service": "https://github.com/truongmv/service-based-architecture.git"
  }
}
```

Or: **Package Manager → Add package from git URL**.

---

### 2. Bootstrap — static ServiceLocator

```csharp
// GameBootstrap.cs — runs before all scenes
void Awake()
{
    ServiceLocator.Add<IAudioService>(new AudioService());
    ServiceLocator.Add<IStorageService>(new PlayerPrefsStorage());

    // Factory: AnalyticsService needs IAudioService — container injects it
    ServiceLocator.Add<IAnalyticsService>(
        c => new AnalyticsService(c.Get<IAudioService>()),
        ServiceLifetime.Singleton
    );
}
```

---

### 3. Get a service

```csharp
// Throws if not added
var audio = ServiceLocator.Get<IAudioService>();

// Safe — no throw
if (ServiceLocator.TryGet<IAudioService>(out var audio))
    audio.Play("sfx_coin");
```

---

### 4. Explicit DI (no statics)

```csharp
public class LevelController
{
    private readonly IAudioService _audio;

    public LevelController(IServiceContainer container)
    {
        _audio = container.Get<IAudioService>();
    }
}

// Create an isolated container per level
var levelContainer = new ServiceContainer();
levelContainer.Add<IAudioService>(ServiceLocator.Get<IAudioService>());
var controller = new LevelController(levelContainer);
```

---

### 5. Auto-dispose with IService

```csharp
public class AudioService : IAudioService, IService
{
    public void Play(string id) { ... }

    // Called automatically on Remove or Clear
    public void Dispose() { /* release audio sources */ }
}
```

---

### 6. Unit test — no statics needed

```csharp
[Test]
public void WhenLevelStarts_AudioIsPlayed()
{
    var container = new ServiceContainer();
    var fake = new FakeAudioService();
    container.Add<IAudioService>(fake);

    var sut = new LevelController(container);
    sut.Start();

    Assert.That(fake.PlayCallCount, Is.EqualTo(1));
    container.Dispose();
}
```

---

### 7. Unit test — with static ServiceLocator

```csharp
[TestFixture]
public class BootstrapTests
{
    [TearDown]
    public void TearDown() => ServiceLocator.ResetForTesting(); // must reset after each test

    [Test]
    public void AfterBootstrap_AudioServiceIsResolvable()
    {
        GameBootstrap.Initialize();
        Assert.That(ServiceLocator.IsExists<IAudioService>(), Is.True);
    }
}
```

---

## Design Decisions

**Transient disposal** — the container does not track or dispose Transient instances. The caller
owns what it gets; if the resolved type implements `IService`, the caller is responsible for
calling `Dispose`. Tracking Transient references (even weakly) adds locking overhead for no benefit
in typical Unity usage.

**No named scopes** — level-scoped services are handled by creating a `new ServiceContainer()`
at level start and calling `Dispose()` on level exit. This is explicit, zero-overhead, and requires
no additional API surface.