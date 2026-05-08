using MoongladePure.SaaS.Identity;

namespace MoongladePure.Tests.SaaS;

[TestClass]
public class UsernamePolicyTests
{
    [TestMethod]
    public void ValidateAcceptsSimpleUsername()
    {
        var policy = new UsernamePolicy();

        var result = policy.Validate(" Alice-01 ");

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("alice-01", result.NormalizedUsername);
    }

    [TestMethod]
    [DataRow("ab")]
    [DataRow("-alice")]
    [DataRow("alice-")]
    [DataRow("alice_blog")]
    [DataRow("alice.blog")]
    [DataRow("admin")]
    [DataRow("www")]
    [DataRow("app")]
    public void ValidateRejectsInvalidOrReservedUsernames(string username)
    {
        var policy = new UsernamePolicy();

        var result = policy.Validate(username);

        Assert.IsFalse(result.Succeeded);
    }
}
