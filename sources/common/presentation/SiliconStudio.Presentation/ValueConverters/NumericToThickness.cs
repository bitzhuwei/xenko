﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SiliconStudio.Presentation.ValueConverters
{
    /// <summary>
    /// This converter will convert a double value to a <see cref="Thickness"/> structure that can be used for Margin, Padding, etc.
    /// A <see cref="Thickness"/> must be passed as a parameter of this converter. You can use the <see cref="MarkupExtensions.ThicknessExtension"/>
    /// markup extension to easily pass one, with the following syntax: {sskk:Thickness (arguments)}. The resulting thickness will
    /// be the parameter thickness multiplied bu the scalar double value.
    /// </summary>
    [ValueConversion(typeof(double), typeof(Thickness))]
    public class NumericToThickness : ValueConverterBase<NumericToThickness>
    {
        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double scalar;
            try
            {
                scalar = ConverterHelper.ConvertToDouble(value, culture);
            }
            catch (Exception exception)
            {
                throw new ArgumentException("The value of this converter must be convertible to a double.", exception);
            }

            if (!(parameter is Thickness))
            {
                throw new ArgumentException("The parameter of this converter must be an instance of the Thickness structure. Use {sskk:Thickness (arguments)} to construct one.");
            }

            var thickness = (Thickness)parameter;
            var result = new Thickness(thickness.Left * scalar, thickness.Top * scalar, thickness.Right * scalar, thickness.Bottom * scalar);
            return result;
        }

        /// <inheritdoc/>
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is Thickness))
            {
                throw new ArgumentException("The value of the ConvertBack method of this converter must be a an instance of the Thickness structure.");
            }
            if (!(parameter is Thickness))
            {
                throw new ArgumentException("The parameter of the ConvertBack method of this converter must be a an instance of the Thickness structure.");
            }
            var thicknessValue = (Thickness)value;
            var thicknessParameter = (Thickness)parameter;

            var scalar = 0.0;
            if (Math.Abs(thicknessParameter.Left) > double.Epsilon)
            {
                scalar = thicknessValue.Left / thicknessParameter.Left;
            }
            else if (Math.Abs(thicknessParameter.Right) > double.Epsilon)
            {
                scalar = thicknessValue.Right / thicknessParameter.Right;
            }
            else if (Math.Abs(thicknessParameter.Top) > double.Epsilon)
            {
                scalar = thicknessValue.Top / thicknessParameter.Top;
            }
            else if (Math.Abs(thicknessParameter.Bottom) > double.Epsilon)
            {
                scalar = thicknessValue.Bottom / thicknessParameter.Bottom;
            }

            return System.Convert.ChangeType(scalar, targetType);
        }
    }
}
