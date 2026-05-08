using MoongladePure.SaaS.Hosting;
using MoongladePure.SaaS.Identity;

namespace MoongladePure.Tests.SaaS;

[TestClass]
public class SaaSHostClassifierTests
{
    private static readonly SaaSOptions Options = new()
    {
        PortalHosts = ["example.com", "www.example.com"],
        SiteSubdomainRoot = "app.example.com"
    };

    [TestMethod]
    public void ClassifyReturnsPortalForConfiguredPortalHost()
    {
        var classifier = CreateClassifier();

        var result = classifier.Classify("WWW.Example.com:443", Options);

        Assert.AreEqual(SaaSHostKind.Portal, result.Kind);
        Assert.AreEqual("www.example.com", result.Host);
    }

    [TestMethod]
    public void ClassifyReturnsUserSubdomainForValidUsername()
    {
        var classifier = CreateClassifier();

        var result = classifier.Classify("alice.app.example.com", Options);

        Assert.AreEqual(SaaSHostKind.UserSubdomain, result.Kind);
        Assert.AreEqual("alice", result.Username);
    }

    [TestMethod]
    public void ClassifyRejectsNestedUserSubdomain()
    {
        var classifier = CreateClassifier();

        var result = classifier.Classify("www.alice.app.example.com", Options);

        Assert.AreEqual(SaaSHostKind.Unknown, result.Kind);
    }

    [TestMethod]
    public void ClassifyRejectsReservedUserSubdomain()
    {
        var classifier = CreateClassifier();

        var result = classifier.Classify("admin.app.example.com", Options);

        Assert.AreEqual(SaaSHostKind.Unknown, result.Kind);
    }

    [TestMethod]
    public void ClassifyReturnsCustomDomainCandidateForNonPlatformHost()
    {
        var classifier = CreateClassifier();

        var result = classifier.Classify("blog.customer.com", Options);

        Assert.AreEqual(SaaSHostKind.CustomDomainCandidate, result.Kind);
        Assert.AreEqual("blog.customer.com", result.Host);
    }

    private static SaaSHostClassifier CreateClassifier() => new(new UsernamePolicy());
}
