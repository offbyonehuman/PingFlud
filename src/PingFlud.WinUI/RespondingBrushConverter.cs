using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace PingFlud.WinUI;

public sealed class RespondingBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var resourceKey = value is true ? "SuccessBrush" : "DangerBrush";
        return (Brush)Microsoft.UI.Xaml.Application.Current.Resources[resourceKey];
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language) =>
        throw new NotSupportedException();
}
