using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;

namespace Avalonia.Platform
{
    public interface IShareProvider
    {
        /// <summary>
        /// Shares text using platform's sharing feature
        /// </summary>
        /// <param name="text">The text string to be shared</param>
        Task Share(string text);

        /// <summary>
        /// Shares a file using platform's sharing feature
        /// </summary>
        /// <param name="file">The <see cref="IStorageFile"/> to be shared</param>
        Task Share(IStorageFile file);

        /// <summary>
        /// Shares a list of files using platform's sharing feature
        /// </summary>
        /// <param name="files">The list of <see cref="IStorageFile"/> to be shared</param>
        Task Share(IList<IStorageFile> files);

        /// <summary>
        /// Shares a stream using platform's sharing feature
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to be shared</param>
        /// <param name="tempName">The name to save the stream as. if empty, a random name will be used</param>
        Task Share(Stream stream, string tempName = "");
    }
}
