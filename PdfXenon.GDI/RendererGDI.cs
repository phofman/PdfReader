﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PdfXenon.Standard;

namespace PdfXenon.GDI
{
    public class RendererGDI : Renderer
    {
        private Graphics _graphics;
        private GraphicsPath _currentPath;
        private RenderPoint _currentPoint;        

        public Bitmap Bitmap { get; private set; }

        public override void Initialize(PdfRectangle mediaBox, PdfRectangle cropBox)
        {
            // Create a bitmap that is rounded up in size to the nearest pixel size
            Bitmap = new Bitmap((int)Math.Ceiling(mediaBox.UpperRightX), (int)Math.Ceiling(mediaBox.UpperRightY), PixelFormat.Format32bppArgb);

            // Create a GDI+ context for writing to the bitmap
            _graphics = Graphics.FromImage(Bitmap);
            _graphics.SmoothingMode = SmoothingMode.AntiAlias;
            _graphics.FillRectangle(Brushes.White, 0, 0, Bitmap.Width, Bitmap.Height);

            // Use a transform to convert from PDF (Y-axis points upwards) to GDI+ (Y-axis points downwards)
            _graphics.Transform = new Matrix(1, 0, 0, -1, 0, Bitmap.Height);

            // Set the initial clipping region to the cropbox
            GraphicsState.Clipping = new Region(new RectangleF(cropBox.LowerLeftX, cropBox.LowerLeftY, cropBox.Width, cropBox.Height));
        }

        public override void SubPathStart(RenderPoint pt)
        {
            if (_currentPath == null)
                _currentPath = new GraphicsPath();
            else
                _currentPath.StartFigure();

            // Convert point to user space
            _currentPoint = GraphicsState.CTM.Transform(pt);
        }

        public override void SubPathLineTo(RenderPoint pt)
        {
            // Convert point to user space
            GraphicsState.CTM.Transform(pt);

            _currentPath.AddLine(_currentPoint.X, _currentPoint.Y, pt.X, pt.Y);
            _currentPoint = pt;
        }

        public override void SubPathBezier(RenderPoint pt2, RenderPoint pt3, RenderPoint pt4)
        {
            // Convert points to user space
            GraphicsState.CTM.Transform(pt2);
            GraphicsState.CTM.Transform(pt3);
            GraphicsState.CTM.Transform(pt4);

            _currentPath.AddBezier(_currentPoint.X, _currentPoint.Y, pt2.X, pt2.Y, pt3.X, pt3.Y, pt4.X, pt4.Y);
            _currentPoint = pt4;
        }

        public override void SubPathClose()
        {
            _currentPath.CloseFigure();
        }

        public override void PathRectangle(RenderPoint pt, float width, float height)
        {
            if (_currentPath == null)
                _currentPath = new GraphicsPath();
            else
                _currentPath.StartFigure();

            // Convert points to user space
            GraphicsState.CTM.Transform(pt);
            RenderPoint pt2 = GraphicsState.CTM.Transform(pt.X + width, pt.Y + height);

            _currentPath.AddLine(pt.X, pt.Y, pt2.X, pt.Y);
            _currentPath.AddLine(pt2.X, pt.Y, pt2.X, pt2.Y);
            _currentPath.AddLine(pt2.X, pt2.Y, pt.X, pt2.Y);

            _currentPath.CloseFigure();
        }

        public override void PathStroke()
        {
            using (Pen pen = CreatePen())
            {
                _graphics.Clip = (Region)GraphicsState.Clipping;
                _graphics.DrawPath(pen, _currentPath);
            }
        }

        public override void PathFill(bool evenOdd)
        {
            if (evenOdd)
                _currentPath.FillMode = FillMode.Alternate;
            else
                _currentPath.FillMode = FillMode.Winding;

            using (Brush brush = CreateBrush())
            {
                _graphics.Clip = (Region)GraphicsState.Clipping;
                _graphics.FillPath(brush, _currentPath);
            }
        }

        public override void PathClip(bool evenOdd)
        {
            if (evenOdd)
                _currentPath.FillMode = FillMode.Alternate;
            else
                _currentPath.FillMode = FillMode.Winding;

            // Always copy the region, so each graphics state has its own instance
            Region currentClip = (Region)GraphicsState.Clipping;
            Region newClip = currentClip.Clone();
            newClip.Intersect(_currentPath);
            GraphicsState.Clipping = newClip;
        }

        public override void PathEnd()
        {
            if (_currentPath != null)
            {
                _currentPath.Dispose();
                _currentPath = null;
            }
        }

        public override void Finshed()
        {
            if (_graphics != null)
            {
                _graphics.Flush();
                _graphics.Dispose();
                _graphics = null;
            }
        }

        private Brush CreateBrush()
        {
            if (GraphicsState.ColorSpaceNonStroking is RenderColorSpaceRGB colorSpaceRGB)
            {
                RenderColorRGB rgb = colorSpaceRGB.GetColorRGB();
                Color color = Color.FromArgb(255, (int)(255 * rgb.R), (int)(255 * rgb.G), (int)(255 * rgb.B));
                SolidBrush brush = new SolidBrush(color);
                return brush;
            }
            else if (GraphicsState.ColorSpaceNonStroking is RenderColorSpacePattern colorSpacePatten)
            {
                RenderPatternType pattern = colorSpacePatten.GetPattern();
                if (pattern is RenderPatternShadingAxial axial)
                {
                    // Get the points that represents the start and end of the axial gradient line
                    PdfArray coord = axial.Coords;
                    PointF pointStart = new PointF(coord.Objects[0].AsNumber(), coord.Objects[1].AsNumber());
                    PointF pointEnd = new PointF(coord.Objects[2].AsNumber(), coord.Objects[3].AsNumber());

                    //Default the colors, they are overridden later with actual colors
                    LinearGradientBrush brush = new LinearGradientBrush(pointStart, pointEnd, Color.White, Color.Black);

                    // Use the specified color space for converting the function result to RGB color values
                    RenderColorSpaceRGB colorSpace = (RenderColorSpaceRGB)axial.ColorSpaceValue;

                    // The more positions we provide, the more accurate the gradient becomes
                    Color[] colors = new Color[512];
                    float[] positions = new float[512];
                    for(int i=0; i<positions.Length; i++)
                    {
                        float position = 0;
                        if (i == (positions.Length - 1))
                            position = 1;
                        else
                            position = 1f / positions.Length * i;

                        // Use the axial function to get values for the position and then use color space to conver that into actual RGB values
                        colorSpace.Parse(axial.FunctionValue.Call(new float[] { position }));
                        RenderColorRGB rgb = colorSpace.GetColorRGB();

                        colors[i] = Color.FromArgb(255, (int)(255 * rgb.R), (int)(255 * rgb.G), (int)(255 * rgb.B));
                        positions[i] = position;
                    }

                    brush.InterpolationColors = new ColorBlend()
                    {
                        Colors = colors,
                        Positions = positions
                    };

                    return brush;
                }

                Console.WriteLine(new RenderDebugBuilder(this));
            }

            throw new NotImplementedException($"Colorspace '{GraphicsState.ColorSpaceNonStroking.GetType().Name}' not recogonized.");
        }

        private Pen CreatePen()
        {
            if (GraphicsState.ColorSpaceStroking is RenderColorSpaceRGB colorSpaceRGB)
            {
                // Get the current stroke colour and convert to GDI color
                RenderColorRGB rgb = colorSpaceRGB.GetColorRGB();
                Color color = Color.FromArgb(255, (int)(255 * rgb.R), (int)(255 * rgb.G), (int)(255 * rgb.B));
                Pen pen = new Pen(color, GraphicsState.LineWidth);

                // Only if the dash pattern is more than a single value, do we need to apply it
                if ((GraphicsState.DashArray != null) && (GraphicsState.DashArray.Length > 0))
                {
                    pen.DashStyle = DashStyle.Custom;
                    pen.DashPattern = GraphicsState.DashArray;
                    pen.DashOffset = GraphicsState.DashPhase;
                }

                // Define how the start and end caps of the line appear
                switch (GraphicsState.LineCapStyle)
                {
                    case 0:
                    default:
                        pen.StartCap = LineCap.Flat;
                        pen.EndCap = LineCap.Flat;
                        break;
                    case 1:
                        pen.StartCap = LineCap.Round;
                        pen.EndCap = LineCap.Round;
                        break;
                    case 2:
                        pen.StartCap = LineCap.Square;
                        pen.EndCap = LineCap.Square;
                        break;
                }

                // Define how multiple lines are joined together
                switch (GraphicsState.LineJoinStyle)
                {
                    case 0:
                    default:
                        pen.LineJoin = LineJoin.Miter;
                        break;
                    case 1:
                        pen.LineJoin = LineJoin.Round;
                        break;
                    case 2:
                        pen.LineJoin = LineJoin.Bevel;
                        break;
                }

                return pen;
            }

            throw new NotImplementedException($"Colorspace '{GraphicsState.ColorSpaceStroking.GetType().Name}' not recogonized.");
        }
    }
}
