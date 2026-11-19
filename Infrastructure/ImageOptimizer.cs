using SkiaSharp;

namespace MreHunter.Api.Infrastructure
{
    /// <summary>
    /// Convierte las imágenes subidas desde el CMS a WebP y las reduce si son más grandes de lo necesario.
    /// Usa SkiaSharp (licencia MIT, sin costo). Una foto de 400 KB queda normalmente entre 40 y 80 KB.
    /// </summary>
    public static class ImageOptimizer
    {
        /// <summary>Ancho máximo: suficiente para un banner a pantalla completa en monitores Full HD.</summary>
        public const int MaxWidth = 1920;

        /// <summary>Calidad WebP (0-100). 80 no muestra diferencias visibles y reduce mucho el peso.</summary>
        public const int Quality = 80;

        public const string Extension = ".webp";

        /// <summary>
        /// Valida la imagen, la reduce a <see cref="MaxWidth"/> si es más ancha (manteniendo la proporción),
        /// corrige la rotación de fotos de celular (EXIF) y la guarda como
        /// <c>{folder}/{fileNameWithoutExtension}.webp</c>. Devuelve el nombre final del archivo.
        /// Borra versiones anteriores con el mismo nombre y otra extensión.
        /// </summary>
        public static async Task<string> SaveAsWebpAsync(IFormFile file, string folder, string fileNameWithoutExtension)
        {
            UploadValidation.EnsureImage(file);

            await using var input = new MemoryStream();
            await file.CopyToAsync(input);
            input.Position = 0;

            using var codec = SKCodec.Create(input)
                ?? throw new UploadValidationException("El archivo no es una imagen válida o está dañado.");

            using var decoded = SKBitmap.Decode(codec)
                ?? throw new UploadValidationException("No se pudo leer la imagen.");

            using var oriented = ApplyOrientation(decoded, codec.EncodedOrigin);
            using var resized = ResizeIfNeeded(oriented);
            using var image = SKImage.FromBitmap(resized ?? oriented);
            using var webp = image.Encode(SKEncodedImageFormat.Webp, Quality)
                ?? throw new InvalidOperationException("No se pudo convertir la imagen a WebP.");

            Directory.CreateDirectory(folder);

            foreach (var ext in UploadValidation.AllowedImageExtensions)
            {
                var old = Path.Combine(folder, fileNameWithoutExtension + ext);
                if (File.Exists(old))
                    File.Delete(old);
            }

            var finalName = fileNameWithoutExtension + Extension;
            await using (var output = File.Create(Path.Combine(folder, finalName)))
            {
                webp.SaveTo(output);
            }

            return finalName;
        }

        private static SKBitmap? ResizeIfNeeded(SKBitmap source)
        {
            if (source.Width <= MaxWidth)
                return null;

            var height = (int)Math.Round(source.Height * (MaxWidth / (double)source.Width));
            var info = new SKImageInfo(MaxWidth, height, source.ColorType, source.AlphaType);
            return source.Resize(info, new SKSamplingOptions(SKCubicResampler.Mitchell))
                ?? throw new InvalidOperationException("No se pudo redimensionar la imagen.");
        }

        /// <summary>Las fotos de celular guardan la rotación en EXIF; WebP no la conserva, así que se aplica aquí.</summary>
        private static SKBitmap ApplyOrientation(SKBitmap source, SKEncodedOrigin origin)
        {
            if (origin == SKEncodedOrigin.TopLeft || origin == SKEncodedOrigin.Default)
                return source.Copy();

            var swap = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop
                              or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
            var width = swap ? source.Height : source.Width;
            var height = swap ? source.Width : source.Height;

            var rotated = new SKBitmap(new SKImageInfo(width, height, source.ColorType, source.AlphaType));
            using var canvas = new SKCanvas(rotated);

            switch (origin)
            {
                case SKEncodedOrigin.TopRight:    canvas.Scale(-1, 1, width / 2f, 0); break;
                case SKEncodedOrigin.BottomRight: canvas.RotateDegrees(180, width / 2f, height / 2f); break;
                case SKEncodedOrigin.BottomLeft:  canvas.Scale(1, -1, 0, height / 2f); break;
                case SKEncodedOrigin.LeftTop:     canvas.Translate(width, 0); canvas.RotateDegrees(90); canvas.Scale(1, -1, 0, source.Height / 2f); break;
                case SKEncodedOrigin.RightTop:    canvas.Translate(width, 0); canvas.RotateDegrees(90); break;
                case SKEncodedOrigin.RightBottom: canvas.Translate(0, height); canvas.RotateDegrees(-90); canvas.Scale(1, -1, 0, source.Height / 2f); break;
                case SKEncodedOrigin.LeftBottom:  canvas.Translate(0, height); canvas.RotateDegrees(-90); break;
            }

            using var sourceImage = SKImage.FromBitmap(source);
            canvas.DrawImage(sourceImage, 0, 0, new SKSamplingOptions(SKFilterMode.Linear));
            return rotated;
        }
    }
}
