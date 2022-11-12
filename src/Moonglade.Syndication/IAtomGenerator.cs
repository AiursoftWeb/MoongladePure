namespace MoongladePure.Syndication;

public interface IAtomGenerator
{
    Task<string> WriteAtomAsync();
}