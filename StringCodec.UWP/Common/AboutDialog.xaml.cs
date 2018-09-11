using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“内容对话框”项模板

namespace StringCodec.UWP.Common
{
    public sealed partial class AboutDialog : ContentDialog
    {
        private CanvasBitmap logoImage = null;
        private CanvasImageBrush logoBrush = null;

        public AboutDialog()
        {
            this.InitializeComponent();
            this.RequestedTheme = (ElementTheme)Settings.Get("AppTheme", ElementTheme.Default);

            Package package = Package.Current;
            PackageId packageId = package.Id;
            PackageVersion version = packageId.Version;

            AboutTitle.Text = "AppName".T();
            AboutAuthorValue.Text = package.PublisherDisplayName;
            AboutVersionValue.Text = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
            AboutDescriptionValue.Text = packageId.Name;

        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void Logo_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            args.TrackAsyncAction(Task.Run(async () =>
            {
                // Load the background image and create an image brush from it
                this.logoImage = await CanvasBitmap.LoadAsync(sender, new Uri("ms-appx:///Assets/Square71x71Logo.scale-150.png"));
                //this.logoImage = await CanvasBitmap.LoadAsync(sender, new Uri("ms-appx:///Assets/CheckboardPattern_3264.png"));
                this.logoBrush = new CanvasImageBrush(sender, this.logoImage) { Opacity = 1.0f };

                // Set the brush's edge behaviour to wrap, so the image repeats if the drawn region is too big
                //this.logoBrush.ExtendX = this.logoBrush.ExtendY = CanvasEdgeBehavior.Wrap;

            }).AsAsyncAction());
        }

        private void Logo_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            var session = args.DrawingSession;
            //session.FillRectangle(new Rect(new Point(), sender.RenderSize), this.logoBrush);
            if(this.RequestedTheme == ElementTheme.Dark)
            {
                session.DrawImage(logoImage);
            }
            else
            {
                var invert = new InvertEffect() { Source = logoImage };
                session.DrawImage(invert);
            }
        }

        private async void Contact_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri(@"mailto:netcharm@163.com");
            var options = new Windows.System.LauncherOptions();
            options.TreatAsUntrusted = true;
            // Launch the URI
            var success = await Windows.System.Launcher.LaunchUriAsync(uri, options);

            if (success)
            {
                // URI launched
            }
            else
            {
                // URI launch failed
            }
        }

        private async void Site_Click(object sender, RoutedEventArgs e)
        {
            var uri = new Uri(@"https://github.com/netcharm");
            // Set the desired remaining view.
            var options = new Windows.System.LauncherOptions();
            options.TreatAsUntrusted = true;
            // Launch the URI
            var success = await Windows.System.Launcher.LaunchUriAsync(uri, options);

            if (success)
            {
                // URI launched
            }
            else
            {
                // URI launch failed
            }
        }

    }
}
