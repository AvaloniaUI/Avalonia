using ControlCatalog.Models;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    internal class SectionViewModel(HomeSection homeSection) : ViewModelBase
    {
        public HomeSection HomeSection { get; } = homeSection;
    }
}
