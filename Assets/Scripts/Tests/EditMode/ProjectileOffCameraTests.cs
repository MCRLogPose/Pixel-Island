using NUnit.Framework;
using PixelIsland.Combat;
using UnityEngine;

namespace PixelIsland.Tests.EditMode
{
    /// <summary>
    /// Comprueba la regla de "fuera de camara" que usa el proyectil para
    /// reciclarse, con una camara ortografica montada al efecto.
    /// </summary>
    public sealed class ProjectileOffCameraTests
    {
        GameObject _cameraObject;
        Camera _camera;

        [SetUp]
        public void SetUp()
        {
            _cameraObject = new GameObject("TestCamera");
            _camera = _cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = 5f;
            _camera.transform.position = new Vector3(0f, 0f, -10f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cameraObject);
        }

        [Test]
        public void PointAtCenter_IsOnCamera()
        {
            Assert.IsFalse(Projectile.IsOffCamera(_camera, Vector3.zero, 0f));
        }

        [Test]
        public void PointBeyondRightEdge_IsOffCamera()
        {
            Assert.IsTrue(Projectile.IsOffCamera(_camera, new Vector3(1000f, 0f, 0f), 0f));
        }

        [Test]
        public void PointBehindCamera_IsOffCamera()
        {
            Assert.IsTrue(Projectile.IsOffCamera(_camera, new Vector3(0f, 0f, -100f), 0f));
        }

        [Test]
        public void NullCamera_IsNeverOffCamera()
        {
            Assert.IsFalse(Projectile.IsOffCamera(null, new Vector3(1000f, 0f, 0f), 0f));
        }
    }
}
