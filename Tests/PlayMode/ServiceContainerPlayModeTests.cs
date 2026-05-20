using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace TMV.Service.Tests
{
    /// <summary>
    /// PlayMode tests for ServiceContainer — covers MonoBehaviour-hosted service scenarios.
    /// Only add tests here that genuinely require a running Unity runtime.
    /// Pure logic tests belong in EditMode.
    /// </summary>
    public class ServiceContainerPlayModeTests
    {
        #region Public Methods

        /// <summary>
        /// A service registered before a frame yield is still accessible after
        /// the frame boundary — confirms the container is not reset between frames.
        /// </summary>
        [UnityTest]
        public IEnumerator Add_And_Get_SurvivesFrameBoundary()
        {
            var container = new ServiceContainer();
            var instance = new FakeService();
            container.Add<IFakeService>(instance);

            yield return null;

            Assert.That(container.Get<IFakeService>(), Is.SameAs(instance));
            container.Dispose();
        }

        #endregion

        #region Test Doubles

        private interface IFakeService { }

        private class FakeService : IFakeService { }

        #endregion
    }
}
