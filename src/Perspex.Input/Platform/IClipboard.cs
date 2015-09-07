namespace Perspex.Input.Platform
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public interface IClipboard
    {
        Task<string> GetTextAsync();

        Task SetTextAsync(string text);

        Task ClearAsync();
    }
}
