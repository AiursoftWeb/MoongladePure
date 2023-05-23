using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MoongladePure.Tests;

[TestClass]
public class StartUpTest
{
    [TestMethod]
    public void StartTest()
    {
        Assert.AreEqual(1, 2 - 1);
    }
}
