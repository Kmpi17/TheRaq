using UnityEngine;

namespace GTAChiraq
{
    /// <summary>Cálculos puros del movimiento a pie (sin estado, testeables en EditMode).</summary>
    public static class MovementMath
    {
        /// <summary>Convierte el input 2D en una dirección horizontal de mundo relativa al yaw de la cámara (grados).</summary>
        public static Vector3 CameraRelative(Vector2 input, float cameraYaw)
        {
            var direction = Quaternion.Euler(0f, cameraYaw, 0f) * new Vector3(input.x, 0f, input.y);
            return Vector3.ClampMagnitude(direction, 1f);
        }

        /// <summary>Velocidad vertical inicial para alcanzar <paramref name="height"/> con la gravedad dada (negativa).</summary>
        public static float JumpVelocity(float height, float gravity) => Mathf.Sqrt(2f * height * -gravity);
    }
}
