using System;
using System.Threading.Tasks;
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

        private ImageSource _photo = null;
        private string _description = string.Empty;

        public ImageSource Photo
        {
            get
            {
                return _photo;
            }

            set
            {
                _photo = value;
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

        public bool IsAnalyzeEnabled => _photo != null;
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

                var photo = await TakePhotoAsync();
                Photo = ImageSource.FromFile(photo.Path);
                Description = string.Empty;
            });

        }

        private void DoOnAnalyzeClicked(object sender, EventArgs eventArgs)
        {
            RunBusyAction(() =>
            {
                Description = "This is just an example";
            });
        }

        private void DoOnReadClicked(object sender, EventArgs eventArgs)
        {
            RunBusyAction(() =>
            {
                CrossTextToSpeech.Current.Speak(Description);
            });
        }

        #endregion

        #region Private Helpers

        private void RunBusyAction(Action action)
        {
            if (IsBusy)
                return;

            IsBusy = true;

            try
            {
                action();
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
                    await DisplayAlert("Use camera", "Acces to camera needed", "OK");
                }

                var results = await CrossPermissions.Current.RequestPermissionsAsync(Permission.Camera);
                permissionStatus = results[Permission.Camera];
            }

            return permissionStatus == PermissionStatus.Granted;
        }

        private async Task<MediaFile> TakePhotoAsync()
        {
            await CrossMedia.Current.Initialize();

            var newPhotoOptions = new StoreCameraMediaOptions
            {
                Directory = "tmp",
                Name = Guid.NewGuid().ToString()
            };

            return await CrossMedia.Current.TakePhotoAsync(newPhotoOptions);
        }

        #endregion
    }
}