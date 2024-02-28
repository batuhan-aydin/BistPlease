using AngleSharp;
using AngleSharp.Dom;

namespace ValueVest.Source.Bist.Core;

public class WebParser : IWebParser
{
	private readonly IBrowsingContext _context;
	public WebParser()
	{
		var config = Configuration.Default.WithDefaultLoader();
		_context = BrowsingContext.New(config);
	}

	public Task<IDocument> GetDocumentAsync(string url)
	{
		return _context.OpenAsync(url);
	}
}

public interface IWebParser
{
	Task<IDocument> GetDocumentAsync(string url);
}
