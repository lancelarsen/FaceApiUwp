using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace FaceApiUwp
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        //----------------------------------------------------------------------
        //--- Add your own Face API key -- see http://lancelarsen.com/setting-up-azure-cognitive-services/
        //----------------------------------------------------------------------
        private readonly IFaceServiceClient _faceServiceClient
            = new FaceServiceClient("fbd28a5beaf045e4a144edb0cc6183db");

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            //----------------------------------------------------------------------
            //--- Open File Dialog
            //----------------------------------------------------------------------
            var filePicker = new FileOpenPicker();
            filePicker.ViewMode = PickerViewMode.Thumbnail;
            filePicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            filePicker.FileTypeFilter.Add(".jpg");
            filePicker.FileTypeFilter.Add(".jpeg");
            filePicker.FileTypeFilter.Add(".png");
            var file = await filePicker.PickSingleFileAsync();
            if (file == null || !file.IsAvailable) return;

            AppendMessage("Processing...");
            buttonBrowse.Content = "Processing...";
            buttonBrowse.IsEnabled = false;

            //----------------------------------------------------------------------
            //--- Create a bitmap from the image file 
            //---   Using Nuget WriteableBitmapEx, as that does not exist in UWP
            //---   https://writeablebitmapex.codeplex.com/
            //----------------------------------------------------------------------
            var property = await file.Properties.GetImagePropertiesAsync();
            var bitmap = BitmapFactory.New((int)property.Width, (int)property.Height);
            using (bitmap.GetBitmapContext())
            {
                using (var fileStream = await file.OpenAsync(FileAccessMode.Read))
                {
                    bitmap = await BitmapFactory.New(1, 1).FromStream(fileStream, BitmapPixelFormat.Bgra8);
                }
            }

            //----------------------------------------------------------------------
            //--- Use the Azure Cognitive Services Face API to Locate and Analyse Faces
            //----------------------------------------------------------------------
            using (var imageStream = await file.OpenStreamForReadAsync())
            {
                var faces = await DetectFaces(imageStream);

                buttonBrowse.Content = "Browse";
                buttonBrowse.IsEnabled = true;

                if (faces == null) return;
                var faceCount = 1;
                AppendMessage("------------------------");
                AppendMessage($"{faces.Length} Face(s) Detected!");
                foreach (var face in faces)
                {
                    string colorName;
                    var color = SetColor(faceCount, out colorName);

                    DrawRectangle(bitmap, face, color, 10);

                    AppendMessage("------------------------");
                    AppendMessage($"Sideburns: {face.FaceAttributes.FacialHair.Sideburns}");
                    AppendMessage($"Moustache: {face.FaceAttributes.FacialHair.Moustache}");
                    AppendMessage($"Beard: {face.FaceAttributes.FacialHair.Beard}");
                    AppendMessage($"Glasses: {face.FaceAttributes.Glasses}");
                    AppendMessage($"Gender: {face.FaceAttributes.Gender}");
                    AppendMessage($"Smile: {face.FaceAttributes.Smile}");
                    AppendMessage($"Age: {face.FaceAttributes.Age}");
                    AppendMessage($"Face {faceCount++} ({colorName})");
                }
            }

            imagePhoto.Source = bitmap;
            AppendMessage("------------------------");
        }

        private async Task<Face[]> DetectFaces(Stream imageStream)
        {
            var attributes = new List<FaceAttributeType>();
            attributes.Add(FaceAttributeType.Age);
            attributes.Add(FaceAttributeType.Gender);
            attributes.Add(FaceAttributeType.Smile);
            attributes.Add(FaceAttributeType.Glasses);
            attributes.Add(FaceAttributeType.FacialHair);
            Face[] faces = null;
            try
            {
                faces = await _faceServiceClient.DetectAsync(imageStream, true, true, attributes);
            }
            catch (FaceAPIException exception)
            {
                AppendMessage("------------------------");
                AppendMessage($"Face API Error = {exception.ErrorMessage}");
            }
            catch (Exception exception)
            {
                AppendMessage("------------------------");
                AppendMessage($"Face API Error = {exception.Message}");
            }
            return faces;
        }

        private static Color SetColor(int faceCount, out string colorName)
        {
            Color color;
            switch (faceCount)
            {
                case 1: color = Colors.Red; colorName = "Red"; break;
                case 2: color = Colors.Blue; colorName = "Blue"; break;
                case 3: color = Colors.Green; colorName = "Green"; break;
                case 4: color = Colors.Yellow; colorName = "Yellow"; break;
                case 5: color = Colors.Purple; colorName = "Purple"; break;
                default: color = Colors.Orange; colorName = "Orange"; break;
            }
            return color;
        }

        private void AppendMessage(string message)
        {

            textResults.Text = $"{message}\r\n{textResults.Text}";
        }

        private static void DrawRectangle(WriteableBitmap bitmap, Face face, Color color, int thinkness)
        {
            var left = face.FaceRectangle.Left;
            var top = face.FaceRectangle.Top;
            var width = face.FaceRectangle.Width;
            var height = face.FaceRectangle.Height;

            DrawRectangle(bitmap, left, top, width, height, color, thinkness);
        }

        private static void DrawRectangle(WriteableBitmap bitmap, int left, int top, int width, int height, Color color, int thinkness)
        {
            var x1 = left;
            var y1 = top;
            var x2 = left + width;
            var y2 = top + height;

            bitmap.DrawRectangle(x1, y1, x2, y2, color);

            for (var i = 0; i < thinkness; i++)
            {
                bitmap.DrawRectangle(x1--, y1--, x2++, y2++, color);
            }
        }
    }
}
