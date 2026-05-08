using MoongladePure.SaaS.Domains;

namespace MoongladePure.Tests.SaaS;

[TestClass]
public class CustomDomainVerificationTests
{
    [TestMethod]
    public void BuildTxtRecordNameNormalizesHost()
    {
        var name = CustomDomainVerification.BuildTxtRecordName(" Blog.Customer.COM. ");

        Assert.AreEqual("_moonglade.blog.customer.com", name);
    }

    [TestMethod]
    public void BuildTxtRecordValueUsesMoongladePrefix()
    {
        var value = CustomDomainVerification.BuildTxtRecordValue("token");

        Assert.AreEqual("moonglade-site-verification=token", value);
    }

    [TestMethod]
    public void CreateTokenReturnsHexToken()
    {
        var token = CustomDomainVerification.CreateToken();

        Assert.AreEqual(64, token.Length);
        Assert.IsTrue(token.All(Uri.IsHexDigit));
        Assert.AreEqual(token, token.ToLowerInvariant());
    }
}
