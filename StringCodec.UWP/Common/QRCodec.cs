using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using ZXing;
using ZXing.Rendering;
using ZXing.QrCode.Internal;
using ZXing.Common;
using Windows.UI;
using Windows.Graphics.Capture;
using Microsoft.Graphics.Canvas;
using Windows.UI.Composition;
using Windows.Graphics;
using Microsoft.Graphics.Canvas.UI.Composition;
using Windows.UI.Xaml;
using Windows.Graphics.DirectX;
using System.Numerics;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Controls;

namespace StringCodec.UWP.Common
{
    public class ScreenCapture
    {
        // Capture API objects.
        private SizeInt32 _lastSize;
        private GraphicsCaptureItem _item;
        private Direct3D11CaptureFramePool _framePool;
        private GraphicsCaptureSession _session;

        // Non-API related members.
        private CanvasDevice _canvasDevice;
        private CompositionGraphicsDevice _compositionGraphicsDevice;
        private Compositor _compositor;
        private CompositionDrawingSurface _surface;

        public Image Preview
        {
            set
            {
                Setup(value);
            }
        }

        private void Setup(Image image)
        {
            _canvasDevice = new CanvasDevice();
            _compositionGraphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(Window.Current.Compositor, _canvasDevice);
            _compositor = Window.Current.Compositor;

            _surface = _compositionGraphicsDevice.CreateDrawingSurface(
                new Size(400, 400),
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                DirectXAlphaMode.Premultiplied);    // This is the only value that currently works with the composition APIs.

            var visual = _compositor.CreateSpriteVisual();
            visual.RelativeSizeAdjustment = Vector2.One;
            var brush = _compositor.CreateSurfaceBrush(_surface);
            brush.HorizontalAlignmentRatio = 0.5f;
            brush.VerticalAlignmentRatio = 0.5f;
            brush.Stretch = CompositionStretch.Uniform;
            visual.Brush = brush;
            ElementCompositionPreview.SetElementChildVisual(image, visual);
        }

        public async Task StartCaptureAsync()
        {
            // The GraphicsCapturePicker follows the same pattern the 
            // file pickers do. 
            var picker = new GraphicsCapturePicker();
            GraphicsCaptureItem item = await picker.PickSingleItemAsync();

            // The item may be null if the user dismissed the 
            // control without making a selection or hit Cancel. 
            if (item != null)
            {
                StartCaptureInternal(item);
            }
        }


        private void StartCaptureInternal(GraphicsCaptureItem item)
        {
            // Stop the previous capture if we had one.
            StopCapture();

            _item = item;
            _lastSize = _item.Size;

            _framePool = Direct3D11CaptureFramePool.Create(
               _canvasDevice, // D3D device 
               DirectXPixelFormat.B8G8R8A8UIntNormalized, // Pixel format 
               2, // Number of frames 
               _item.Size); // Size of the buffers 

            _framePool.FrameArrived += (s, a) =>
            {
                // The FrameArrived event is raised for every frame on the thread
                // that created the Direct3D11CaptureFramePool. This means we 
                // don't have to do a null-check here, as we know we're the only 
                // one dequeueing frames in our application.  

                // NOTE: Disposing the frame retires it and returns  
                // the buffer to the pool.

                using (var frame = _framePool.TryGetNextFrame())
                {
                    ProcessFrame(frame);
                }
            };

            _item.Closed += (s, a) =>
            {
                StopCapture();
            };

            _session = _framePool.CreateCaptureSession(_item);
            _session.StartCapture();
        }

        public void StopCapture()
        {
            _session?.Dispose();
            _framePool?.Dispose();
            _item = null;
            _session = null;
            _framePool = null;
        }

        private void ProcessFrame(Direct3D11CaptureFrame frame)
        {
            // Resize and device-lost leverage the same function on the
            // Direct3D11CaptureFramePool. Refactoring it this way avoids 
            // throwing in the catch block below (device creation could always 
            // fail) along with ensuring that resize completes successfully and 
            // isn’t vulnerable to device-lost.   
            bool needsReset = false;
            bool recreateDevice = false;

            if ((frame.ContentSize.Width != _lastSize.Width) ||
                (frame.ContentSize.Height != _lastSize.Height))
            {
                needsReset = true;
                _lastSize = frame.ContentSize;
            }

            try
            {
                // Take the D3D11 surface and draw it into a  
                // Composition surface.

                // Convert our D3D11 surface into a Win2D object.
                var canvasBitmap = CanvasBitmap.CreateFromDirect3D11Surface(
                    _canvasDevice,
                    frame.Surface);

                // Helper that handles the drawing for us.
                FillSurfaceWithBitmap(canvasBitmap);
            }

            // This is the device-lost convention for Win2D.
            catch (Exception e) when (_canvasDevice.IsDeviceLost(e.HResult))
            {
                // We lost our graphics device. Recreate it and reset 
                // our Direct3D11CaptureFramePool.  
                needsReset = true;
                recreateDevice = true;
            }

            if (needsReset)
            {
                ResetFramePool(frame.ContentSize, recreateDevice);
            }
        }

        private void FillSurfaceWithBitmap(CanvasBitmap canvasBitmap)
        {
            CanvasComposition.Resize(_surface, canvasBitmap.Size);

            using (var session = CanvasComposition.CreateDrawingSession(_surface))
            {
                session.Clear(Colors.Transparent);
                session.DrawImage(canvasBitmap);
            }
        }

        private void ResetFramePool(SizeInt32 size, bool recreateDevice)
        {
            do
            {
                try
                {
                    if (recreateDevice)
                    {
                        _canvasDevice = new CanvasDevice();
                    }

                    _framePool.Recreate(
                        _canvasDevice,
                        DirectXPixelFormat.B8G8R8A8UIntNormalized,
                        2,
                        size);
                }
                // This is the device-lost convention for Win2D.
                catch (Exception e) when (_canvasDevice.IsDeviceLost(e.HResult))
                {
                    _canvasDevice = null;
                    recreateDevice = true;
                }
            } while (_canvasDevice == null);
        }

    }

    static public class QRCodec
    {
        public enum ERRORLEVEL { L, M, Q, H };

        #region Older codes
        //        static private float calcBorderWidth(ImageSource QRImage, Point mark, Size size)
        //        {
        //            if (mark.X <= 0 || mark.Y <= 0) return 0;

        //            float pixelWidth = 1.0f;

        //            int X = (int)Math.Max(0, mark.X - 100);
        //            int Y = (int)Math.Max(0, mark.Y - 100);
        //            int W = (int)Math.Min(QRImage.Width - X, size.Width + 200);
        //            int H = (int)Math.Min(QRImage.Height - Y, size.Height + 200);
        //            ImageSource bwQR = QRImage.Clone(new Rectangle(X, Y, W, H), PixelFormat.Format1bppIndexed);
        //#if DEBUG
        //            bwQR.Save("test-bw.png");
        //#endif

        //            int origX = 100;
        //            int origY = 100;
        //            if (mark.X < 100) origX = mark.X;
        //            if (mark.Y < 100) origY = mark.Y;

        //            Color colorMark = bwQR.GetPixel(origX, origY);
        //            for (var i = 2; i < 100; i++)
        //            {
        //                Color color = bwQR.GetPixel(origX - i, origY);
        //                //if(color.ToArgb() == Color.White.ToArgb())
        //                if (color.ToArgb() != colorMark.ToArgb())
        //                {
        //                    pixelWidth = i;
        //                    break;
        //                }
        //            }
        //            return (pixelWidth);
        //        }
        #endregion

        static private void setDecodeOptions(BarcodeReader br)
        {
            br.AutoRotate = true;
            br.TryInverted = true;
            br.Options.CharacterSet = "UTF-8";
            br.Options.TryHarder = true;
            br.Options.PureBarcode = false;
            br.Options.ReturnCodabarStartEnd = true;
            br.Options.UseCode39ExtendedMode = true;
            br.Options.UseCode39RelaxedExtendedMode = true;
            br.Options.PossibleFormats = new List<BarcodeFormat>
            {
                BarcodeFormat.All_1D,
                BarcodeFormat.DATA_MATRIX,
                BarcodeFormat.AZTEC,
                BarcodeFormat.PDF_417,
                BarcodeFormat.QR_CODE
            };
            //br.Options.PossibleFormats.Add(BarcodeFormat.DATA_MATRIX);
            //br.Options.PossibleFormats.Add(BarcodeFormat.QR_CODE);
        }

        static public async Task<WriteableBitmap> Encode(string content, Color fgcolor, Color bgcolor, ERRORLEVEL ECL)
        {
            WriteableBitmap result = null;

            if (content.Length <= 0) return(result);

            ErrorCorrectionLevel ecl = ErrorCorrectionLevel.L;
            switch (ECL)
            {
                case ERRORLEVEL.L:
                    ecl = ErrorCorrectionLevel.L;
                    break;
                case ERRORLEVEL.M:
                    ecl = ErrorCorrectionLevel.M;
                    break;
                case ERRORLEVEL.Q:
                    ecl = ErrorCorrectionLevel.Q;
                    break;
                case ERRORLEVEL.H:
                    ecl = ErrorCorrectionLevel.H;
                    break;
                default:
                    ecl = ErrorCorrectionLevel.L;
                    break;
            }

            var bw = new BarcodeWriter();
            bw.Options.Width = 1024;
            bw.Options.Height = 1024;
            bw.Options.PureBarcode = false;
            bw.Options.Hints.Add(EncodeHintType.ERROR_CORRECTION, ecl);
            bw.Options.Hints.Add(EncodeHintType.MARGIN, 2);
            bw.Options.Hints.Add(EncodeHintType.DISABLE_ECI, true);
            bw.Options.Hints.Add(EncodeHintType.CHARACTER_SET, "UTF-8");

            bw.Format = BarcodeFormat.QR_CODE;
            bw.Renderer = new WriteableBitmapRenderer() { Foreground = fgcolor, Background = bgcolor };

            //var renderer = new SvgRenderer();
            //var img = renderer.Render(new BitMatrix(512), BarcodeFormat.QR_CODE, content);
            var text = content.Length>984 ? content.Substring(0, 984) : content;
            result = bw.Write(text);
            return (result);
        }

        static public async Task<string> Decode(WriteableBitmap image)
        {
            string result = string.Empty;
            if (image == null) return (result);
            if (image.PixelWidth < 32 || image.PixelHeight < 32) return (result);

            var br = new BarcodeReader();
            setDecodeOptions(br);
            try
            {
                //var qrResult = br.Decode(image);
                //if (qrResult != null)
                //{
                //    result = qrResult.Text;
                //}
                var qrResults = br.DecodeMultiple(image);
                var textList = new List<string>();
                foreach (var line in qrResults)
                {
                    textList.Add(line.Text);
                }
                if (textList.Count >= 0)
                {
                    result = string.Join("\n\r", textList);
                }
            }
            catch (Exception) { }

            return (result);
        }
    }
}
