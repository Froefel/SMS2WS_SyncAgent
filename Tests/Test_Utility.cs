using NUnit.Framework;

namespace SMS2WS_SyncAgent.Tests
{
#if DEBUG
    [TestFixture]
    class Test_Utility
    {

        [Test]
        public void IsConnectedToInternet_with_valid_destination_returns_true()
        {
            Assert.IsTrue(Utility.IsConnectedToInternet() == true,
                          "No internet connection");
        }
    }
#endif
}
