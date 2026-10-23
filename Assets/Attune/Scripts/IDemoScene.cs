using System.Collections.Generic;

namespace Attune
{
    /// <summary>What every demo scene reports of a visit, built or stub: the presses and gestures it took.</summary>
    public interface IDemoScene
    {
        /// <summary>Presses this visit has taken while no menu was open.</summary>
        int Presses { get; }

        /// <summary>Gestures this visit has taken while no menu was open.</summary>
        IReadOnlyList<string> Gestures { get; }
    }
}
