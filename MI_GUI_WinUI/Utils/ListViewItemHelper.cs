using Microsoft.UI.Xaml;

namespace MI_GUI_WinUI.Utils
{
    public class ListViewItemHelper
    {
        public static readonly DependencyProperty IndexProperty =
            DependencyProperty.RegisterAttached(
                "Index",
                typeof(int),
                typeof(ListViewItemHelper),
                new PropertyMetadata(-1)
            );

        public static void SetIndex(UIElement element, int value)
        {
            element.SetValue(IndexProperty, value);
        }

        public static int GetIndex(UIElement element)
        {
            return (int)element.GetValue(IndexProperty);
        }
    }
}
