﻿#nullable enable
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;


using HB.FullStack.XamarinForms.Platforms;


using Xamarin.Essentials;
using Xamarin.Forms;

[assembly: Dependency(typeof(HB.FullStack.Droid.FileHelper))]
namespace HB.FullStack.Droid
{
    /// <summary>
    /// 不能放在HB.Framework.Client.Droid中，因为要用到项目中的Resources类
    /// </summary>
    public class FileHelper : IFileHelper
    {
        //TODO: 图片存储到公共相册中
        public static readonly string AvatarDirectory = System.IO.Path.Combine(FileSystem.AppDataDirectory, "time_avatars");
        public static readonly string OthersDirectory = System.IO.Path.Combine(FileSystem.AppDataDirectory, "others");
        public static readonly string CacheDirectory = System.IO.Path.Combine(FileSystem.AppDataDirectory, "cache");

        private readonly object _locker = new object();

        public string GetDirectoryPath(UserFileType fileType)
        {
            return fileType switch
            {
                UserFileType.Avatar => AvatarDirectory,
                UserFileType.Cache => CacheDirectory,
                _ => OthersDirectory
            };
        }

        public string GetFileSuffix(UserFileType fileType)
        {
            return fileType switch
            {
                UserFileType.Avatar => ".png",
                _ => "",
            };
        }

        public bool IsFileExisted(string fileName, UserFileType userFileType)
        {
            fileName = AddFileExtensionIfAbsent(fileName, userFileType);

            string filePath = System.IO.Path.Combine(GetDirectoryPath(userFileType), fileName);

            return File.Exists(filePath);
        }

        private string AddFileExtensionIfAbsent(string fileName, UserFileType userFileType)
        {
            if (!fileName.Contains('.'))
            {
                fileName += GetFileSuffix(userFileType);
            }

            return fileName;
        }

        public async Task SaveFileAsync(byte[] data, string fullPath)
        {
            string directory = System.IO.Path.GetDirectoryName(fullPath);

            CreateDirectoryIfNotExist(directory);

            using FileStream fileStream = File.Open(fullPath, FileMode.Create);

            await fileStream.WriteAsync(data).ConfigureAwait(false);

            await fileStream.FlushAsync().ConfigureAwait(false);
        }

        public async Task SaveFileAsync(byte[] data, string fileName, UserFileType userFileType)
        {
            string fullPath = GetFileFullPath(fileName, userFileType);

            await SaveFileAsync(data, fullPath).ConfigureAwait(false);

            //Make sure it shows up in the Photos gallery promptly.
            if (userFileType == UserFileType.Avatar)
            {
                Android.Media.MediaScannerConnection.ScanFile(Platform.CurrentActivity, new string[] { fullPath }, new string[] { "image/png", "image/jpeg" }, null);
            }
        }

        private string GetFileFullPath(string fileName, UserFileType userFileType)
        {
            fileName = AddFileExtensionIfAbsent(fileName, userFileType);

            string directory = GetDirectoryPath(userFileType);

            CreateDirectoryIfNotExist(directory);

            return System.IO.Path.Combine(directory, fileName);
        }

        public async Task SaveFileAsync(Stream stream, string fileName, UserFileType userFileType)
        {
            string fullPath = GetFileFullPath(fileName, userFileType);

            using FileStream fileStream = File.Open(fullPath, FileMode.Create);

            await stream.CopyToAsync(fileStream).ConfigureAwait(false);

            await fileStream.FlushAsync().ConfigureAwait(false);
        }

        public async Task<byte[]?> GetFileAsync(string fullPath)
        {
            if(!File.Exists(fullPath))
            {
                return null;
            }

            using FileStream fileStream = new FileStream(fullPath, FileMode.Open);

            using MemoryStream memoryStream = new MemoryStream();

            await fileStream.CopyToAsync(memoryStream).ConfigureAwait(false);

            return memoryStream.ToArray();
        }

        public async Task<Stream> GetResourceStreamAsync(string resourceName, ResourceType resourceType, string? packageName = null, CancellationToken? cancellationToken = null)
        {
            if (string.IsNullOrEmpty(packageName))
            {
                packageName = Platform.AppContext.PackageName;
            }
            resourceName = System.IO.Path.GetFileNameWithoutExtension(resourceName);

            int resId = Platform.AppContext.Resources!.GetIdentifier(resourceName, GetResourceTypeName(resourceType), packageName);

            using Stream stream = Platform.CurrentActivity.Resources!.OpenRawResource(resId);

            MemoryStream memoryStream = new MemoryStream();

            if (cancellationToken.HasValue)
            {
                await stream.CopyToAsync(memoryStream, cancellationToken.Value).ConfigureAwait(false);
            }
            else
            {
                await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
            }

            memoryStream.Position = 0;

            return memoryStream;

            static string GetResourceTypeName(ResourceType resourceType)
            {
                return resourceType switch
                {
                    ResourceType.Drawable => "drawable",
                    _ => "",
                };
            }
        }

        public static int GetResourceId2(string resourceName)
        {
            string withoutExtensionFileName = System.IO.Path.GetFileNameWithoutExtension(resourceName);

            //int resId = Platform.CurrentActivity.Resources.GetIdentifier(withoutExtensionFileName, "drawable", "com.brlite.mycolorfultime");
            int resId = (int)typeof(Resource.Drawable).GetField(withoutExtensionFileName).GetValue(null);
            return resId;
        }

        public string GetAssetsHtml(string name)
        {
            AssetManager assetsManager = Platform.CurrentActivity.Assets!;

            using Stream stream = assetsManager.Open(name);

            using StreamReader reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }

        private void CreateDirectoryIfNotExist(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
            {
                lock (_locker)
                {
                    if (!Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }
                }
            }
        }

        #region Avatar

        public async Task SaveAvatarAsync(ImageSource imageSource, long userId)
        {
            string directoryPath = GetDirectoryPath(UserFileType.Avatar);

            CreateDirectoryIfNotExist(directoryPath);

            string? path = GetAvatarFullPath(userId);

            using Bitmap? bitmap = await imageSource.GetBitMapAsync().ConfigureAwait(false);

            //using Bitmap scaledBitmap = bitmap.ScaleTo(Avatar_Max_Height, Avatar_Max_Width);

            using FileStream fileStream = new FileStream(path, FileMode.Create);

            bool result = await bitmap!.CompressAsync(Bitmap.CompressFormat.Png, 100, fileStream).ConfigureAwait(false);

            await fileStream.FlushAsync().ConfigureAwait(false);
        }

        public Task SaveAvatarAsync(byte[] avatarData, long userId)
        {
            return SaveFileAsync(avatarData, userId.ToString(), UserFileType.Avatar);
        }

        public Task SaveAvatarAsync(Stream stream, long userId)
        {
            return SaveFileAsync(stream, userId.ToString(), UserFileType.Avatar);
        }

        public string? GetAvatarFullPath(long userId)
        {
            string path = System.IO.Path.Combine(AvatarDirectory, $"{userId}.png");

            return System.IO.File.Exists(path) ? path : null;
        }

        public Task<byte[]?> GetAvatarAsync(long userId)
        {
            string? fullPath = GetAvatarFullPath(userId);

            if (fullPath == null)
            {
                return Task.FromResult<byte[]?>(null);
            }

            return GetFileAsync(fullPath);
        }

        #endregion
    }
}
#nullable restore