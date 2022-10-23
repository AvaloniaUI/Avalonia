using System.Collections.Generic;
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
    }
}
