namespace BrainlessLabs.Neon
{
    /// <summary>
    /// How a modifier folds into a stat: (base + ΣAdd) × (1 + ΣPctAdd) × ΠMult.
    /// </summary>
    public enum StatOp
    {
        Add,
        PctAdd,
        Mult
    }
}
