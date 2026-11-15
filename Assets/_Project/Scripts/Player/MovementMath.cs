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

        /// <summary>
        /// Lleva la velocidad horizontal hacia <paramref name="target"/>: con <paramref name="acceleration"/> al arrancar o
        /// acelerar, y con <paramref name="deceleration"/> (más fuerte) al frenar o cambiar de sentido. Da inercia sin
        /// que el control se vuelva pesado.
        /// </summary>
        public static Vector3 Accelerate(Vector3 current, Vector3 target, float acceleration, float deceleration, float dt)
        {
            bool braking = target.sqrMagnitude < current.sqrMagnitude || Vector3.Dot(current, target) < 0f;
            return Vector3.MoveTowards(current, target, (braking ? deceleration : acceleration) * dt);
        }

        /// <summary>Velocidad de giro (grados/s): rápida parado y más lenta cuanto más deprisa se va.</summary>
        public static float TurnSpeed(float speed, float maxSpeed, float slowTurn, float fastTurn)
            => Mathf.Lerp(fastTurn, slowTurn, Mathf.Clamp01(speed / Mathf.Max(0.01f, maxSpeed)));

        /// <summary>Daño por caída según la velocidad al tocar suelo: nada hasta <paramref name="safeSpeed"/> (unos 4 m de caída).</summary>
        public static float FallDamage(float impactSpeed, float safeSpeed = 13f, float perMetrePerSecond = 9f)
            => impactSpeed <= safeSpeed ? 0f : (impactSpeed - safeSpeed) * perMetrePerSecond;
    }

    public enum ClimbKind { None, Vault, ClimbUp, ClimbOver, SlideOver }

    /// <summary>Reglas de escalada (puras): qué se hace según la altura del obstáculo y si es grueso (se puede estar encima).</summary>
    public static class ClimbMath
    {
        public const float MinHeight = 0.45f;  // por debajo lo sube el stepOffset o es suelo
        public const float VaultMax = 1.25f;   // saltar por encima de una valla o muro bajo
        public const float MaxHeight = 2.4f;   // lo más alto que se agarra
        public const float MaxDrop = 3f;       // al pasar al otro lado, el suelo no puede estar más abajo del borde
        public const float SlideMax = 1.6f;    // coches: se pasa por encima (capó, maletero o techo bajo) hasta esta altura

        /// <summary>Coches: se cruzan por encima si lo más alto del camino no pasa de <see cref="SlideMax"/>; si no, nada (se sube con <see cref="Classify"/>).</summary>
        public static ClimbKind ClassifyVehicle(float maxHeight)
            => maxHeight < MinHeight || maxHeight > SlideMax ? ClimbKind.None : ClimbKind.SlideOver;

        public static ClimbKind Classify(float height, bool thick)
        {
            if (height < MinHeight || height > MaxHeight) return ClimbKind.None;
            if (thick) return ClimbKind.ClimbUp;
            return height <= VaultMax ? ClimbKind.Vault : ClimbKind.ClimbOver;
        }

        public static float Duration(ClimbKind kind, float height) => kind switch
        {
            ClimbKind.Vault => 0.45f,
            ClimbKind.SlideOver => 0.4f + 0.2f * height,
            ClimbKind.ClimbUp => 0.35f + 0.3f * height,
            ClimbKind.ClimbOver => 0.55f + 0.3f * height,
            _ => 0f,
        };

        public static float StaminaCost(ClimbKind kind, float height) => kind switch
        {
            ClimbKind.Vault => 6f,
            ClimbKind.SlideOver => 7f,
            ClimbKind.ClimbUp => 8f + 5f * height,
            ClimbKind.ClimbOver => 10f + 6f * height,
            _ => 0f,
        };
    }
}
