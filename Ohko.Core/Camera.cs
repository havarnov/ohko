using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ohko.Core;

public class Camera(GraphicsDevice graphicsDevice)
{
    public Vector2 Position { get; set; }

    public float Zoom { get; set; }

    public Matrix Transform { get; private set; }

    public void Update()
    {
        Transform = Matrix.CreateTranslation(
            new Vector3(-Position.X, -Position.Y, 0)) * Matrix.CreateScale(Zoom, Zoom, 1) * Matrix.CreateTranslation(
            graphicsDevice.Viewport.Width / 2f,
            graphicsDevice.Viewport.Height / 2f,
            0);
    }

    public Matrix GetViewMatrix(Vector2 parallax)
    {
        return Matrix.CreateTranslation(
            new Vector3(-Position * parallax, 0)) * Matrix.CreateScale(Zoom, Zoom, 1) * Matrix.CreateTranslation(
            graphicsDevice.Viewport.Width / 2f,
            graphicsDevice.Viewport.Height / 2f,
            0);
        // // To add parallax, simply multiply it by the position
        // return Matrix.CreateTranslation(new Vector3(-Position * parallax, 0.0f)) *
        //        // The next line has a catch. See note below.
        //        Matrix.CreateTranslation(new Vector3(-origin, 0.0f)) *
        //        Matrix.CreateRotationZ(Rotation) *
        //        Matrix.CreateScale(Zoom, Zoom, 1) *
        //        Matrix.CreateTranslation(new Vector3(origin, 0.0f));
    }
}