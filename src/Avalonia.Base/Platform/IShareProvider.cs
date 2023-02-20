using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace Avalonia.Platform
{
    public interface IShareProvider
    {
        bool CanShareAsync(IDataObject dataObject);

        /// <summary>
        /// Shares text using platform's sharing feature
        /// </summary>
        /// <param name="dataObject">The object string to be shared</param>
        Task ShareAsync(IDataObject dataObject);
    }
}
