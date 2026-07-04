using System.Collections.Generic;

namespace BrainlessLabs.Neon
{
    /// <summary>
    /// One stat: a base value plus a list of modifiers, folded lazily on query.
    /// Fold order: (base + ΣAdd) × (1 + ΣPctAdd) × ΠMult.
    /// </summary>
    internal sealed class Stat
    {
        private readonly List<StatModifier> _modifiers = new();
        private float _baseValue;
        private float _foldedValue;
        private bool _dirty = true;

        public float BaseValue
        {
            get => _baseValue;
            set
            {
                _baseValue = value;
                _dirty = true;
            }
        }

        public float Value
        {
            get
            {
                if (_dirty)
                {
                    Fold();
                }
                return _foldedValue;
            }
        }

        public void Add(StatModifier modifier)
        {
            _modifiers.Add(modifier);
            _dirty = true;
        }

        public int RemoveBySource(ModifierSource source)
        {
            int removed = _modifiers.RemoveAll(m => m.Source.Equals(source));
            if (removed > 0)
            {
                _dirty = true;
            }
            return removed;
        }

        private void Fold()
        {
            float add = 0f;
            float pctAdd = 0f;
            float mult = 1f;

            foreach (var modifier in _modifiers)
            {
                switch (modifier.Op)
                {
                    case StatOp.Add:
                        add += modifier.Value;
                        break;
                    case StatOp.PctAdd:
                        pctAdd += modifier.Value;
                        break;
                    case StatOp.Mult:
                        mult *= modifier.Value;
                        break;
                }
            }

            _foldedValue = (_baseValue + add) * (1f + pctAdd) * mult;
            _dirty = false;
        }
    }
}
