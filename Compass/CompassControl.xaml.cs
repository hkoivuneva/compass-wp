﻿/**
 * Copyright (c) 2012-2013 Nokia Corporation.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Diagnostics;
using System.Windows.Input;

namespace Compass.Ui
{
    /// <summary>
    /// 
    /// </summary>
    public partial class CompassControl : UserControl
    {
        // Constants
        private const double RadiansToDegreesCoefficient = 57.2957795; // 180 / PI
        private const double ScaleRelativeTopMargin = 0.32f;
        private const double PlateManipulationRelativeTopMargin = 0.15f;
        private const int PlateNativeWidth = 870;
        private const int PlateNativeHeight = 1800;
        private const int ScaleNativeWidth = 828;
        private const int ScaleNativeHeight = 828;
        private const int NeedleNativeWidth = 63;

        // Data types

        public enum CompassControlArea
        {
            None = 0,
            PlateTop = 1,
            PlateCenter = 2,
            Scale = 3,
            PlateBottom = 4
        };

        // Members
        private double _plateCenterX = 0;
        private double _plateCenterY = 0;
        private double _previousX = 0;
        private double _previousY = 0;
        private int _plateManipulationBottom = 0;
        private int _scaleManipulationTop = 0;
        private int _scaleManipulationBottom = 0;
        private Boolean _isHd = false;

        // Properties

        private double _compassReading = 0;
        public double CompassReading
        {
            get
            {
                return _compassReading;
            }
            set
            {
                _compassReading = value;
                UpdateNeedleAngle();
            }
        }

        /// <summary>
        /// The area of the compass control being manipulated.
        /// </summary>
        public CompassControlArea ManipulatedArea
        {
            get;
            private set;
        }

        public double PlateWidth
        {
            get
            {
                return Plate.ActualWidth;
            }
        }

        /// <summary>
        /// The height of the compass control (plate).
        /// </summary>
        public double PlateHeight
        {
            get
            {
                return Plate.ActualHeight;
            }
            set
            {
                if (value > 0 && value != Plate.ActualHeight)
                {
                    double relativeSize = value / PlateNativeHeight;
                    SetRelativeSize(relativeSize);
                }
            }
        }

        /// <summary>
        /// Compass control (plate) angle.
        /// </summary>
        public double PlateAngle
        {
            get
            {
                return PlateRotation.Angle;
            }
            set
            {
                PlateRotation.Angle = value;
                UpdateNeedleAngle();
            }
        }

        /// <summary>
        /// Enabling/disabling touch manipulation.
        /// </summary>
        public Boolean ManipulationEnabled
        {
            get;
            set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public CompassControl()
        {
            InitializeComponent();
            ManipulatedArea = CompassControlArea.None;
            ManipulationEnabled = true;

            if (Application.Current.Host.Content.ActualWidth >= 720)
            {
                _isHd = true;
                PlateHeight = 600;
            }
            else
            {
                PlateHeight = 400;
            }

            Debug.WriteLine("CompassControl::CompassControl(): Is HD: " + _isHd);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="container"></param>
        /// <returns></returns>
        private CompassControlArea AreaAt(double x, double y, UIElement container)
        {
            if (container != null)
            {
                if (container == Plate)
                {
                    if (y < _plateManipulationBottom)
                    {
                        return CompassControlArea.PlateTop;
                    }
                    else if (y >= _plateManipulationBottom && y < _scaleManipulationTop)
                    {
                        return CompassControlArea.PlateCenter;
                    }
                    else if (y > _scaleManipulationBottom)
                    {
                        return CompassControlArea.PlateBottom;
                    }
                }
                else if (container == Scale)
                {
                    return CompassControlArea.Scale;
                }
            }

            return CompassControlArea.None;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnManipulationStarted(ManipulationStartedEventArgs e)
        {
            if (ManipulationEnabled)
            {
                _previousX = e.ManipulationOrigin.X;
                _previousY = e.ManipulationOrigin.Y;
                ManipulatedArea = AreaAt(_previousX, _previousY, e.ManipulationContainer);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            ManipulatedArea = CompassControlArea.None;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="e"></param>
        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            if (!ManipulationEnabled)
            {
                return;
            }

            if (ManipulatedArea == CompassControlArea.PlateTop)
            {
                double deltaX = e.ManipulationOrigin.X - _plateCenterX;
                double deltaY = e.ManipulationOrigin.Y - _plateCenterY;
                double previousDeltaX = _previousX - _plateCenterX;
                double previousDeltaY = _previousY - _plateCenterY;

                int angleDelta = 
                    -(int)Math.Round(
                        (Math.Atan2(previousDeltaY, previousDeltaX)
                        - Math.Atan2(deltaY, deltaX))
                        * RadiansToDegreesCoefficient);

                if (angleDelta > 180)
                {
                    angleDelta -= 360;
                }
                else if (angleDelta < -180)
                {
                    angleDelta += 360;
                }

                PlateRotation.Angle = (PlateRotation.Angle + angleDelta) % 360;
                UpdateNeedleAngle();
            }
            else if (ManipulatedArea == CompassControlArea.Scale)
            {
                double x = e.ManipulationOrigin.X;
                double y = e.ManipulationOrigin.Y;

                double centerX = Scale.ActualWidth / 2;
                double centerY = Scale.ActualHeight / 2;
                double deltaX = x - centerX;
                double deltaY = y - centerY;
                double previousDeltaX = _previousX - centerX;
                double previousDeltaY = _previousY - centerY;

                int angleDelta =
                    -(int)Math.Round(
                        (Math.Atan2(previousDeltaY, previousDeltaX)
                        - Math.Atan2(deltaY, deltaX))
                        * RadiansToDegreesCoefficient);

                if (angleDelta > 180)
                {
                    angleDelta -= 360;
                }
                else if (angleDelta < -180)
                {
                    angleDelta += 360;
                }

                ScaleRotation.Angle = (ScaleRotation.Angle + angleDelta) % 360;
            }
        }

        /// <summary>
        /// Scales the components of the compass control based on the given
        /// relative size.
        /// </summary>
        /// <param name="relativeSize">
        ///     The size relative to the native size of
        ///     the components. 1.0 is the native size (100 %).
        /// </param>
        private void SetRelativeSize(double relativeSize)
        {
            if (relativeSize <= 0)
            {
                return;
            }

            Plate.Width = relativeSize * PlateNativeWidth;
            ScaleShadow.Width = relativeSize * ScaleNativeWidth;
            Scale.Width = relativeSize * ScaleNativeWidth;
            Needle.Width = relativeSize * NeedleNativeWidth;

            _plateCenterX = relativeSize * PlateNativeWidth / 2;
            _plateCenterY = relativeSize * PlateNativeHeight / 2;

            Thickness topMargin = new Thickness();
            topMargin.Top = relativeSize * PlateNativeHeight * ScaleRelativeTopMargin;
            ScaleShadow.Margin = topMargin;
            Scale.Margin = topMargin;
            Needle.Margin = topMargin;

            _plateManipulationBottom =
                (int)(PlateNativeHeight * relativeSize * PlateManipulationRelativeTopMargin);
            _scaleManipulationTop = (int)(topMargin.Top + 60 * relativeSize);
            _scaleManipulationBottom = (int)(_scaleManipulationTop + ScaleNativeHeight * relativeSize);

            Debug.WriteLine("Manipulation margins: " + _plateManipulationBottom
                + ", " + _scaleManipulationTop
                + ", " + _scaleManipulationBottom);
        }

        /// <summary>
        /// Updates the needle angle based on the current compass reading.
        /// The angle of the plate is compensated.
        /// </summary>
        private void UpdateNeedleAngle()
        {
            NeedleRotation.Angle = -(_compassReading - 360) - PlateRotation.Angle;
        }
    }
}
