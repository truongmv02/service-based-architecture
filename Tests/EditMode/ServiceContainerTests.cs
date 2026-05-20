using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace TMV.Service.Tests
{
    /// <summary>
    /// Edit-mode unit tests for <see cref="ServiceContainer"/>.
    /// Each test uses a fresh container created in SetUp and disposed in TearDown.
    /// </summary>
    [TestFixture]
    public class ServiceContainerTests
    {
        #region Fields

        private ServiceContainer _container;

        #endregion

        #region Unity Callbacks

        [SetUp]
        public void SetUp() => _container = new ServiceContainer();

        [TearDown]
        public void TearDown() => _container.Dispose();

        #endregion

        #region Registration Tests

        /// <summary>
        /// A pre-built instance registered via Add is returned by Get
        /// as the exact same object reference (not a copy).
        /// </summary>
        [Test]
        public void Add_Instance_GetsCorrectly()
        {
            var instance = new FakeService();
            _container.Add<IFakeService>(instance);

            var resolved = _container.Get<IFakeService>();

            Assert.That(resolved, Is.SameAs(instance));
        }

        /// <summary>
        /// A factory registered as Singleton returns the same instance
        /// on every subsequent Get call.
        /// </summary>
        [Test]
        public void Add_Factory_Singleton_ReturnsSameInstance()
        {
            _container.Add<IFakeService>(_ => new FakeService(), ServiceLifetime.Singleton);

            var a = _container.Get<IFakeService>();
            var b = _container.Get<IFakeService>();

            Assert.That(a, Is.SameAs(b));
        }

        /// <summary>
        /// A factory registered as Transient creates a new instance
        /// on every Get call — references must differ.
        /// </summary>
        [Test]
        public void Add_Factory_Transient_ReturnsDifferentInstances()
        {
            _container.Add<IFakeService>(_ => new FakeService(), ServiceLifetime.Transient);

            var a = _container.Get<IFakeService>();
            var b = _container.Get<IFakeService>();

            Assert.That(a, Is.Not.SameAs(b));
        }

        /// <summary>
        /// Registering the same type twice with Add throws
        /// <see cref="InvalidOperationException"/> — use Replace instead.
        /// </summary>
        [Test]
        public void Add_Duplicate_ThrowsInvalidOperation()
        {
            _container.Add<IFakeService>(new FakeService());

            Assert.Throws<InvalidOperationException>(() =>
                _container.Add<IFakeService>(new FakeService()));
        }

        #endregion

        #region Replace Tests

        /// <summary>
        /// Replace swaps the registered instance so Get returns the new one.
        /// </summary>
        [Test]
        public void Replace_ExistingService_UpdatesInstance()
        {
            var original = new FakeService();
            var replacement = new FakeService();
            _container.Add<IFakeService>(original);

            _container.Replace<IFakeService>(replacement);

            Assert.That(_container.Get<IFakeService>(), Is.SameAs(replacement));
        }

        /// <summary>
        /// Calling Replace on a type that was never registered throws
        /// <see cref="InvalidOperationException"/> — use Add first.
        /// </summary>
        [Test]
        public void Replace_NotAdded_ThrowsInvalidOperation()
        {
            Assert.Throws<InvalidOperationException>(() =>
                _container.Replace<IFakeService>(new FakeService()));
        }

        /// <summary>
        /// When the outgoing singleton implements <see cref="IService"/>,
        /// Replace must call Dispose on it before installing the replacement.
        /// </summary>
        [Test]
        public void Replace_Singleton_DisposesOldInstance()
        {
            var original = new DisposableFakeService();
            _container.Add<IFakeService>(original);

            _container.Replace<IFakeService>(new DisposableFakeService());

            Assert.That(original.IsDisposed, Is.True);
        }

        #endregion

        #region Remove Tests

        /// <summary>
        /// Remove calls Dispose on the cached instance when the service
        /// implements <see cref="IService"/>.
        /// </summary>
        [Test]
        public void Remove_CallsDisposeOnIService()
        {
            var service = new DisposableFakeService();
            _container.Add<IFakeService>(service);

            _container.Remove<IFakeService>();

            Assert.That(service.IsDisposed, Is.True);
        }

        /// <summary>
        /// Removing a type that was never registered throws
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        [Test]
        public void Remove_NotAdded_ThrowsInvalidOperation()
        {
            Assert.Throws<InvalidOperationException>(() =>
                _container.Remove<IFakeService>());
        }

        /// <summary>
        /// After Remove, IsExists returns false — the slot is fully freed.
        /// </summary>
        [Test]
        public void Remove_AfterRemoval_IsExistsReturnsFalse()
        {
            _container.Add<IFakeService>(new FakeService());

            _container.Remove<IFakeService>();

            Assert.That(_container.IsExists<IFakeService>(), Is.False);
        }

        #endregion

        #region Get Tests

        /// <summary>
        /// Get on an unregistered type throws <see cref="InvalidOperationException"/>
        /// rather than returning null.
        /// </summary>
        [Test]
        public void Get_NotAdded_ThrowsInvalidOperation()
        {
            Assert.Throws<InvalidOperationException>(() =>
                _container.Get<IFakeService>());
        }

        /// <summary>
        /// TryGet on an unregistered type returns false and sets
        /// the out parameter to null — no exception is thrown.
        /// </summary>
        [Test]
        public void TryGet_NotAdded_ReturnsFalse()
        {
            var result = _container.TryGet<IFakeService>(out var service);

            Assert.That(result, Is.False);
            Assert.That(service, Is.Null);
        }

        /// <summary>
        /// TryGet on a registered type returns true and populates
        /// the out parameter with the exact registered instance.
        /// </summary>
        [Test]
        public void TryGet_Added_ReturnsTrueAndInstance()
        {
            var instance = new FakeService();
            _container.Add<IFakeService>(instance);

            var result = _container.TryGet<IFakeService>(out var service);

            Assert.That(result, Is.True);
            Assert.That(service, Is.SameAs(instance));
        }

        #endregion

        #region Clear Tests

        /// <summary>
        /// Clear disposes every cached <see cref="IService"/> instance in the container.
        /// </summary>
        [Test]
        public void Clear_DisposesAllIServiceInstances()
        {
            var a = new DisposableFakeService();
            var b = new DisposableFakeService();
            _container.Add<IFakeService>(a);
            _container.Add<IFakeService2>(b);

            _container.Clear();

            Assert.That(a.IsDisposed, Is.True);
            Assert.That(b.IsDisposed, Is.True);
        }

        /// <summary>
        /// After Clear the container is empty, so Add can register the same
        /// type again without throwing.
        /// </summary>
        [Test]
        public void Clear_AllowsReAdditionAfterwards()
        {
            _container.Add<IFakeService>(new FakeService());
            _container.Clear();

            Assert.DoesNotThrow(() =>
                _container.Add<IFakeService>(new FakeService()));
        }

        #endregion

        #region Thread Safety Tests

        /// <summary>
        /// 32 tasks resolve a singleton factory concurrently.
        /// All must receive the exact same instance — the container must
        /// not create duplicates under race conditions.
        /// </summary>
        [Test]
        public void Get_Singleton_32ParallelTasks_ReturnsSameInstance()
        {
            _container.Add<IFakeService>(_ => new FakeService(), ServiceLifetime.Singleton);

            var results = new IFakeService[32];
            var tasks = new Task[32];

            for (int i = 0; i < 32; i++)
            {
                int index = i;
                tasks[index] = Task.Run(() => results[index] = _container.Get<IFakeService>());
            }

            Task.WaitAll(tasks);

            for (int i = 1; i < 32; i++)
                Assert.That(results[i], Is.SameAs(results[0]));
        }

        #endregion

        #region Test Doubles

        private interface IFakeService { }
        private interface IFakeService2 { }

        private class FakeService : IFakeService { }

        /// <summary>Tracks whether Dispose was called — used to assert IService auto-disposal.</summary>
        private class DisposableFakeService : IFakeService, IFakeService2, IService
        {
            public bool IsDisposed { get; private set; }
            public void Dispose() => IsDisposed = true;
        }

        #endregion
    }
}
