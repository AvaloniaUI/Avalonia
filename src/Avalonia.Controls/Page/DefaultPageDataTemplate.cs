using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    internal class DefaultPageDataTemplate : IRecyclingDataTemplate
    {
        public Control? Build(object? param)
        {
            if (param is Page page)
            {
                // Detach from any previous ContentPresenter so the new one can adopt it.
                var visualParent = page.GetVisualParent() as ContentPresenter;
                if (visualParent != null)
                    visualParent.Content = null;

                return page;
            }

            if (param is Control control)
            {
                var visualParent = control.GetVisualParent() as ContentPresenter;
                if (visualParent != null)
                    visualParent.Content = null;
            }

            return new ContentPage { Content = param };
        }

        public Control? Build(object? data, Control? existing)
        {
            if (existing is ContentPage existingPage
                && data is not null
                && data is not Page
                && data is not Control)
            {
                existingPage.Content = data;
                return existingPage;
            }

            return Build(data);
        }

        public bool Match(object? data) => data is not null;
    }
}
