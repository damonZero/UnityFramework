using UnityEngine;

namespace General
{
    public partial class CameraMoveAdv
    {
        [Header("惯性")]
        [SerializeField] private bool enableInertia;
        [SerializeField] private float smoothTime = 0.1f;

        partial void OnUpdateInertia()
        {
            if (!enableInertia || !underInertia) return;

            if (inertiaTime <= smoothTime)
            {
                cam.transform.position += velocity;
                velocity = Vector3.Lerp(velocity, Vector3.zero, inertiaTime / smoothTime);
                inertiaTime += Time.smoothDeltaTime;
                TriggerMoveCb();
            }
            else
            {
                underInertia = false;
                inertiaTime = 0;
            }
        }
    }
}
