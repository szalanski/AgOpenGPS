using System.Drawing;
using System.Drawing.Imaging;

using OpenTK.Graphics.OpenGL;

namespace AgOpenGPS.Core.Drawing
{
    public class Texture2D
    {

        private readonly int _textureId;
        public Texture2D(Bitmap bitmap)
        {
            GL.GenTextures(1, out _textureId);
            if (bitmap != null) SetBitmap(bitmap);
        }

        public void Bind()
        {
            GL.BindTexture(TextureTarget.Texture2D, _textureId);
        }

        public void Draw(double x0, double x1, double y0, double y1)
        {
            GL.Enable(EnableCap.Texture2D);

            Bind();
            GL.Begin(PrimitiveType.Quads);
            GL.TexCoord2(0, 0); GL.Vertex2(x0, y0);
            GL.TexCoord2(1, 0); GL.Vertex2(x1, y0);
            GL.TexCoord2(1, 1); GL.Vertex2(x1, y1);
            GL.TexCoord2(0, 1); GL.Vertex2(x0, y1);
            GL.End();
            GL.Disable(EnableCap.Texture2D);
        }

        public void DrawCentered(
            double halfSizeX,  // Pass minus hafSizeX to flip positive X-axis to negative X-axis
            double halfSizeY)  // Pass minus hafSizeXYto flip positive Y-axis to negative Y-axis
        {
            Draw(-halfSizeX, halfSizeX, -halfSizeY, halfSizeY);
        }

        public void SetBitmap(Bitmap bitmap)
        {
            GL.Enable(EnableCap.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, _textureId);
            BitmapData bitmapData = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.TexImage2D(
                TextureTarget.Texture2D,
                0,
                PixelInternalFormat.Rgba,
                bitmapData.Width,
                bitmapData.Height,
                0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra,
                PixelType.UnsignedByte,
                bitmapData.Scan0);
            bitmap.UnlockBits(bitmapData);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, 9729);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, 9729);
            GL.Disable(EnableCap.Texture2D);
        }

    }
}
