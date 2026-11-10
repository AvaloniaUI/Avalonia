using System.Threading.Tasks;

namespace Avalonia.Controls
{
    internal interface ISceneManager
    {
        Task<bool> ShowScene(Scene scene);
    }
}
