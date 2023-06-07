using System;
using System.IO;
using System.Text;

namespace Avalonia.Markup.Xaml;

public class RuntimeXamlLoaderDocument
{
    public RuntimeXamlLoaderDocument(string xaml)
    {
        XamlStream = new MemoryStream(Encoding.UTF8.GetBytes(xaml));
    }

    public RuntimeXamlLoaderDocument(Uri? baseUri, string xaml)
        : this(xaml)
    {
        BaseUri = baseUri;
    }

    public RuntimeXamlLoaderDocument(object? rootInstance, string xaml)
        : this(xaml)
    {
        RootInstance = rootInstance;
    }
    
    public RuntimeXamlLoaderDocument(Uri? baseUri, object? rootInstance, string xaml)
        : this(baseUri, xaml)
    {
        RootInstance = rootInstance;
    }
    
    public RuntimeXamlLoaderDocument(Stream stream)
    {
        XamlStream = stream;
    }

    public RuntimeXamlLoaderDocument(Uri? baseUri, Stream stream)
        : this(stream)
    {
        BaseUri = baseUri;
    }

    public RuntimeXamlLoaderDocument(object? rootInstance, Stream stream)
        : this(stream)
    {
        RootInstance = rootInstance;
    }

    public RuntimeXamlLoaderDocument(Uri? baseUri, object? rootInstance, Stream stream)
        : this(baseUri, stream)
    {
        RootInstance = rootInstance;
    }
    
    /// <summary>
    /// The URI of the XAML being loaded.
    /// </summary>
    public Uri? BaseUri { get; set; }

    /// <summary>
    /// The optional instance into which the XAML should be loaded.
    /// </summary>
    public object? RootInstance { get; set; }
    
    /// <summary>
    /// The stream containing the XAML.
    /// </summary>
    public Stream XamlStream { get; }
    
    /// <summary>
    /// Parent's service provider to pass to the Build method or type ctor, if available.
    /// </summary>
    public IServiceProvider? ServiceProvider { get; set; }
}
