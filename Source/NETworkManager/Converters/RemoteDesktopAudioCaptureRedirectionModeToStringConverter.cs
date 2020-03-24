﻿using NETworkManager.Localization.Translators;
using NETworkManager.Models.RemoteDesktop;
using System;
using System.Globalization;
using System.Windows.Data;

namespace NETworkManager.Converters
{
    public sealed class RemoteDesktopAudioCaptureRedirectionModeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is AudioCaptureRedirectionMode s))
                return "-/-";

            return RemoteDesktopAudioCaptureRedirectionModeTranslator.GetInstance().Translate(s.ToString());
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
