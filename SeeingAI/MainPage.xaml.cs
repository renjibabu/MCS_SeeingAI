using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Vision;
using Plugin.Media;
using Plugin.Media.Abstractions;
using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using Plugin.TextToSpeech;
using Xamarin.Forms;
// ReSharper disable ExplicitCallerInfoArgument

namespace SeeingAI
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        #region Properties

        private MediaFile _photo;
        private ImageSource _photoSource;
        private string _description = string.Empty;

        public ImageSource PhotoSource
        {
            get
            {
                return _photoSource;
            }

            set
            {
                _photoSource = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAnalyzeEnabled));
            }
        }

        public string Description
        {
            get
            {
                return _description;
            }

            set
            {
                _description = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsReadEnabled));
            }
        }

        public bool IsAnalyzeEnabled => PhotoSource != null;

        public bool IsReadEnabled => !string.IsNullOrEmpty(Description);

        #endregion

        #region Event Handlers

        private void DoOnCaptureClicked(object sender, EventArgs eventArgs)
        {
            RunBusyAction(async () =>
            {
                var canAccessCamera = await CanAccessCameraAsync();

                if (!canAccessCamera)
                    return;

                _photo = await TakePhotoAsync();

                if (_photo == null)
                    return;

                PhotoSource = ImageSource.FromStream(() => _photo.GetStream());
                Description = string.Empty;
            });
        }

        private void DoOnAnalyzeClicked(object sender, EventArgs eventArgs)
        {
            RunBusyAction(async () =>
            {
                var visionClient = new VisionServiceClient("8ecd694afa784378b4b9ad5bd1927cea");
                var features = new[] { VisualFeature.Description };
                var photoStream = _photo.GetStream();

                var analysisResult = await visionClient.AnalyzeImageAsync(photoStream, features.ToList());
                var bestAnalysisresult = analysisResult.Description.Captions.OrderByDescending(c => c.Confidence).First();
                var description = $"I think it is {bestAnalysisresult.Text}.";

                Description = description;
            });
        }

        private void DoOnReadClicked(object sender, EventArgs eventArgs)
        {
            RunBusyAction(async () =>
            {
                var text = Description;
                var textToSpeech = CrossTextToSpeech.Current;
                var crossLocale = textToSpeech.GetInstalledLanguages().First(cl => cl.Language == "en" || cl.Language == "en-US");

                await Task.Run(() => textToSpeech.Speak(text, crossLocale: crossLocale));
            });
        }

        #endregion

        #region Private Helpers

        private async void RunBusyAction(Func<Task> action)
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                await action();
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task<bool> CanAccessCameraAsync()
        {
            var permissionStatus = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Camera);

            if (permissionStatus != PermissionStatus.Granted)
            {
                if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Permission.Camera))
                {
                    await DisplayAlert("Use camera", "Access to camera needed", "OK");
                }

                var results = await CrossPermissions.Current.RequestPermissionsAsync(Permission.Camera);
                permissionStatus = results[Permission.Camera];
            }

            return permissionStatus == PermissionStatus.Granted;
        }

        private static async Task<MediaFile> TakePhotoAsync()
        {
            await CrossMedia.Current.Initialize();

            var newPhotoOptions = new StoreCameraMediaOptions
            {
                Directory = "tmp",
                Name = Guid.NewGuid().ToString(),
                CustomPhotoSize = 50
            };

            return await CrossMedia.Current.TakePhotoAsync(newPhotoOptions);
        }

        #endregion
    }
}