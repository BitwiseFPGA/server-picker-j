using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace ServerPickerX.Helpers
{
    public static class ResourceHelper
    {
        // Every card shows a flag and the cards are rebuilt on each search keystroke and sort
        // change, so each image is decoded once and shared from then on. A Bitmap never changes
        // after it is created, so one instance can back any number of Image controls. A path
        // that fails to load is remembered as null rather than retried on every rebuild
        private static readonly ConcurrentDictionary<string, Bitmap?> BitmapCache = new(StringComparer.Ordinal);

        public static Bitmap? LoadImageFromResource(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if (BitmapCache.TryGetValue(path, out Bitmap? cachedBitmap))
            {
                return cachedBitmap;
            }

            Bitmap? bitmap;

            try
            {
                // The image is fully decoded by the constructor, so the stream can close after it
                using Stream stream = AssetLoader.Open(CreateResourceUriFromPath(path));

                bitmap = new Bitmap(stream);
            }
            catch
            {
                bitmap = null;
            }

            Bitmap? storedBitmap = BitmapCache.GetOrAdd(path, bitmap);

            // Another caller got there first, keep theirs and release this copy
            if (!ReferenceEquals(storedBitmap, bitmap))
            {
                bitmap?.Dispose();
            }

            return storedBitmap;
        }

        // Resources live in the assembly that contains this class, so its name is used rather than
        // the entry assembly's. That stays correct in the designer, in tests and after a rename
        public static Uri CreateResourceUriFromPath(string path)
        {
            string assemblyName = typeof(ResourceHelper).Assembly.GetName().Name!;

            return new Uri($"avares://{assemblyName}/{path.TrimStart('/')}");
        }
    }
}
