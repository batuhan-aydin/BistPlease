using AngleSharp.Dom;
using AngleSharp;

namespace BistPlease.Worker.Core;

public class AngleSharpWrapper : IAngleSharpWrapper
{
    private readonly IBrowsingContext _context;
    public AngleSharpWrapper()
    {
        var config = Configuration.Default.WithDefaultLoader();
        _context = BrowsingContext.New(config);
    }

    public Task<IDocument> GetDocumentAsync(string url)
    {
        return _context.OpenAsync(url);
    }
}

public interface IAngleSharpWrapper
{
    Task<IDocument> GetDocumentAsync(string url);
}