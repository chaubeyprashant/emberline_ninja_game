using UnityEngine;

namespace Emberline.Story
{
    /// <summary>
    /// Translates the shot vocabulary into camera moves. Kept out of CameraRig on
    /// purpose: the gameplay camera should not carry story types, and every shot
    /// here resolves to the one generic scripted-move call the rig exposes.
    /// </summary>
    public static class CinematicCamera
    {
        public static void Apply(ShotCamera kind, Transform subject, float duration, CameraRig rig)
        {
            if (rig == null || subject == null) return;

            var p = subject.position;
            var fwd = subject.forward;
            var right = subject.right;
            var eye = p + Vector3.up * 1.5f;

            Vector3 from, to, look = eye;
            switch (kind)
            {
                case ShotCamera.PushIn:
                    // Creep in. The move should be barely perceptible — a push-in
                    // the audience notices is a push-in that is too fast.
                    from = p + fwd * 4.2f + Vector3.up * 1.7f;
                    to = p + fwd * 2.4f + Vector3.up * 1.55f;
                    break;
                case ShotCamera.PullOut:
                    from = p + fwd * 2.6f + Vector3.up * 1.5f;
                    to = p + fwd * 6.5f + Vector3.up * 2.6f;
                    break;
                case ShotCamera.SlowDolly:
                    // Wide enough to hold the subject and the place they are in;
                    // at 3.6m the subject filled the frame and the village vanished.
                    from = p + fwd * 6.2f - right * 3.2f + Vector3.up * 2.1f;
                    to = p + fwd * 6.2f + right * 3.2f + Vector3.up * 2.1f;
                    break;
                case ShotCamera.OverShoulder:
                    from = p - fwd * 1.7f + right * 0.85f + Vector3.up * 1.75f;
                    to = p - fwd * 1.5f + right * 0.75f + Vector3.up * 1.72f;
                    look = p + fwd * 3f + Vector3.up * 1.3f;
                    break;
                case ShotCamera.Wide:
                    // Destruction reads at distance; a wide held still lets the
                    // player's eye travel the frame instead of being led round it.
                    from = p + fwd * 11f + Vector3.up * 7f;
                    to = p + fwd * 9.5f + Vector3.up * 6.4f;
                    look = p + Vector3.up * 0.8f;
                    break;
                case ShotCamera.Orbit:
                    rig.PlayCinematic(subject, duration);
                    return;
                case ShotCamera.Handheld:
                    from = p + fwd * 2.8f + Vector3.up * 1.6f;
                    to = p + fwd * 2.7f + right * 0.12f + Vector3.up * 1.63f;
                    break;
                default: // Hold — locked off. Stillness is the shot.
                    from = to = p + fwd * 3f + Vector3.up * 1.6f;
                    break;
            }

            rig.PlayScriptedShot(from, to, look, duration, kind == ShotCamera.Handheld);
        }
    }
}
