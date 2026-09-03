namespace Emberline.Enemies
{
    /// <summary>
    /// The last few attacks an enemy used, and the penalties that keep it from
    /// using the same one again. Five entries; no allocation after construction.
    /// </summary>
    public class EnemyAttackHistory
    {
        private const int Size = 5;
        private readonly string[] _ids = new string[Size];
        private readonly AttackCategory[] _cats = new AttackCategory[Size];
        private readonly float[] _times = new float[Size];
        private int _head, _count;

        public void Clear() { _head = 0; _count = 0; }

        public void Record(AttackDefinition a, float now)
        {
            _ids[_head] = a.id;
            _cats[_head] = a.category;
            _times[_head] = now;
            _head = (_head + 1) % Size;
            if (_count < Size) _count++;
        }

        public string LastId => _count == 0 ? null : _ids[(_head - 1 + Size) % Size];

        /// <summary>How many attacks in a row have been thrown recently (combo depth).</summary>
        public int ComboStep
        {
            get
            {
                // Count back while attacks are close in time — a chain, not a lull.
                var n = 0;
                for (var i = 0; i < _count && i < Size; i++)
                {
                    var idx = (_head - 1 - i + Size) % Size;
                    if (i > 0)
                    {
                        var prev = (_head - i + Size) % Size;
                        if (_times[prev] - _times[idx] > 2f) break;
                    }
                    n++;
                }
                return n;
            }
        }
        public AttackCategory? LastCategory => _count == 0 ? null : _cats[(_head - 1 + Size) % Size];

        private int Back(int n) => (_head - 1 - n + Size) % Size;

        /// <summary>
        /// Multiplier on a candidate's score. Immediately repeating the same
        /// attack is nearly forbidden; the same category twice running is
        /// discouraged; an attack used within the last three is dampened.
        /// Strategic repeats (the same thrust against a player still
        /// retreating) survive because the state score outweighs the penalty.
        /// </summary>
        public float Penalty(AttackDefinition a)
        {
            if (_count == 0) return 1f;
            var m = 1f;
            if (_ids[Back(0)] == a.id) m *= 0.18f;
            if (_cats[Back(0)] == a.category) m *= 0.6f;
            for (var n = 1; n < System.Math.Min(_count, 3); n++)
                if (_ids[Back(n)] == a.id) m *= 0.7f;
            // Three of the same category in the last four: force variety.
            var same = 0;
            for (var n = 0; n < System.Math.Min(_count, 4); n++) if (_cats[Back(n)] == a.category) same++;
            if (same >= 3) m *= 0.35f;
            return m;
        }

        /// <summary>How many of the last four were this id — for telemetry and the overlay.</summary>
        public int RecentCount(string id)
        {
            var c = 0;
            for (var n = 0; n < System.Math.Min(_count, 4); n++) if (_ids[Back(n)] == id) c++;
            return c;
        }
    }
}
