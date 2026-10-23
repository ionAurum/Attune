namespace Attune.Signals
{
    /// <summary>
    /// Point → Select by dwell (design, Adapters): resting on one target for DwellSeconds selects it,
    /// once. Moving to another target, or to none, cancels and starts again. Nothing (None) never
    /// selects, and that is what a rest zone reports, so looking around is always safe. After a
    /// select the point must leave the target before it can select it again.
    /// </summary>
    public sealed class DwellTimer
    {
        /// <summary>No target: empty space or a rest zone.</summary>
        public const int None = -1;

        private float _held;
        private bool _fired;

        /// <summary>A timer that selects after dwellSeconds.</summary>
        public DwellTimer(float dwellSeconds)
        {
            DwellSeconds = dwellSeconds;
        }

        /// <summary>How long resting on a target takes to select it.</summary>
        public float DwellSeconds { get; set; }

        /// <summary>The target under the point at the last update, or None.</summary>
        public int Target { get; private set; } = None;

        /// <summary>How far the dwell on the current target has gone, 0 to 1; 0 on None, and after it has selected.</summary>
        public float Progress => Target == None || _fired ? 0f : DwellSeconds <= 0f ? 1f : System.Math.Min(1f, _held / DwellSeconds);

        /// <summary>
        /// The point has been over target for dt more seconds (None for nothing or a rest zone).
        /// Returns the target it selected on this update, or None.
        /// </summary>
        public int Update(int target, float dt)
        {
            if (target != Target)
            {
                Target = target;
                _held = 0f;
                _fired = false;
                return None;
            }

            if (target == None || _fired)
            {
                return None;
            }

            _held += dt;
            if (_held < DwellSeconds)
            {
                return None;
            }

            _fired = true;
            return target;
        }

        /// <summary>
        /// Starts with the point resting on target as though it had just selected it: it must leave
        /// before that target can be selected. For where the point already is as the targets appear,
        /// or as a menu closes over them, so arriving never selects.
        /// </summary>
        public void Settle(int target)
        {
            Target = target;
            _held = 0f;
            _fired = target != None;
        }

        /// <summary>Cancels any dwell under way, as a menu opening over the targets does.</summary>
        public void Cancel()
        {
            Target = None;
            _held = 0f;
            _fired = false;
        }
    }
}
