﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;

namespace HB.FullStack.Mobile.Hack
{
    public class ImageSourceExConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                if (!Uri.TryCreate(strValue, UriKind.Absolute, out Uri result) || !(result.Scheme != "file"))
                {
                    return ImageSource.FromFile(strValue);
                }

                return new UriImageSourceEx { Uri = result };
            }

            throw new InvalidOperationException($"Cannot convert \"{value}\" into {typeof(ImageSource)}");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is FileImageSource fileImageSource)
            {
                return fileImageSource.File;
            }

            if (value is UriImageSource uriImageSource)
            {
                return uriImageSource.Uri.ToString();
            }

            throw new NotSupportedException();
        }
    }
}
