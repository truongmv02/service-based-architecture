using System;
using NUnit.Framework;

namespace TMV.Service.Tests
{
    /// <summary>
    /// Edit-mode unit tests for the <see cref="ServiceLocator"/> static facade.
    /// TearDown calls ResetForTesting after every test to keep the global scope clean.
    /// </summary>
    [TestFixture]
    public class ServiceLocatorTests
    {
        #region Unity Callbacks

        [TearDown]
        public void TearDown() => ServiceLocator.ResetForTesting();

        #endregion

        #region Public Methods

        /// <summary>
        /// The static Add and Get methods route correctly through the internal
        /// scope and return the exact registered instance.
        /// </summary>
        [Test]
        public void Add_And_Get_ViaStaticFacade()
        {
            var instance = new FakeService();
            ServiceLocator.Add<IFakeService>(instance);

            var resolved = ServiceLocator.Get<IFakeService>();

            Assert.That(resolved, Is.SameAs(instance));
        }

        /// <summary>
        /// ResetForTesting disposes and recreates the internal container,
        /// so previously registered services are no longer visible.
        /// </summary>
        [Test]
        public void ResetForTesting_ClearsAllAdditions()
        {
            ServiceLocator.Add<IFakeService>(new FakeService());

            ServiceLocator.ResetForTesting();

            Assert.That(ServiceLocator.IsExists<IFakeService>(), Is.False);
        }

        /// <summary>
        /// The Container property exposes the same underlying scope as the static
        /// methods — resolving via Container returns the same instance as Get.
        /// </summary>
        [Test]
        public void Container_Property_ReturnsActiveScope()
        {
            var instance = new FakeService();
            ServiceLocator.Add<IFakeService>(instance);

            var resolved = ServiceLocator.Container.Get<IFakeService>();

            Assert.That(resolved, Is.SameAs(instance));
        }

        /// <summary>
        /// A Transient factory registered via the static facade produces a
        /// distinct instance on each Get call.
        /// </summary>
        [Test]
        public void Add_Factory_Transient_ViaStaticFacade_ReturnsDifferentInstances()
        {
            ServiceLocator.Add<IFakeService>(_ => new FakeService(), ServiceLifetime.Transient);

            var a = ServiceLocator.Get<IFakeService>();
            var b = ServiceLocator.Get<IFakeService>();

            Assert.That(a, Is.Not.SameAs(b));
        }

        /// <summary>
        /// Remove via the static facade unregisters the service so
        /// IsExists subsequently returns false.
        /// </summary>
        [Test]
        public void Remove_ViaStaticFacade_RemovesService()
        {
            ServiceLocator.Add<IFakeService>(new FakeService());

            ServiceLocator.Remove<IFakeService>();

            Assert.That(ServiceLocator.IsExists<IFakeService>(), Is.False);
        }

        /// <summary>
        /// TryGet on an unregistered type returns false and sets the out
        /// parameter to null — no exception is thrown.
        /// </summary>
        [Test]
        public void TryGet_NotAdded_ReturnsFalse()
        {
            var result = ServiceLocator.TryGet<IFakeService>(out var service);

            Assert.That(result, Is.False);
            Assert.That(service, Is.Null);
        }

        #endregion

        #region Test Doubles

        private interface IFakeService { }

        private class FakeService : IFakeService { }

        #endregion
    }
}
