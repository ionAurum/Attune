namespace Attune
{
    /// <summary>
    /// The Dwell Builder's rules (design, Demos, 3): an 8×8 plate and a palette of four blocks. The
    /// picked block is placed on a cell; placing it on a cell that already holds it takes it off, so
    /// a mistake can be undone the same way it was made. The first block is picked at the start. Row
    /// 0 is the top. Plain C#, so the tests can play it.
    /// </summary>
    public sealed class BuildPlate
    {
        /// <summary>Cells across and down.</summary>
        public const int Size = 8;

        /// <summary>No block on a cell.</summary>
        public const int Empty = -1;

        /// <summary>The blocks' names, in the palette's order.</summary>
        public static readonly string[] BlockNames = { "Stone", "Grass", "Water", "Wood" };

        private readonly int[,] _cells = new int[Size, Size];

        /// <summary>An empty plate with the first block picked.</summary>
        public BuildPlate()
        {
            Clear();
        }

        /// <summary>How many blocks the palette holds.</summary>
        public static int Blocks => BlockNames.Length;

        /// <summary>The picked block.</summary>
        public int Picked { get; private set; }

        /// <summary>How many cells hold a block.</summary>
        public int Placed
        {
            get
            {
                int count = 0;
                foreach (int cell in _cells)
                {
                    count += cell != Empty ? 1 : 0;
                }

                return count;
            }
        }

        /// <summary>The block on a cell, or Empty.</summary>
        public int At(int x, int y) => _cells[x, y];

        /// <summary>Picks a block from the palette; an unknown one is ignored.</summary>
        public void Pick(int block)
        {
            if (block >= 0 && block < Blocks)
            {
                Picked = block;
            }
        }

        /// <summary>Places the picked block on a cell, or takes it off if the cell holds it already. Returns what the cell holds now.</summary>
        public int Place(int x, int y)
        {
            if (x < 0 || y < 0 || x >= Size || y >= Size)
            {
                return Empty;
            }

            _cells[x, y] = _cells[x, y] == Picked ? Empty : Picked;
            return _cells[x, y];
        }

        /// <summary>Empties the plate and picks the first block.</summary>
        public void Clear()
        {
            for (int x = 0; x < Size; x++)
            {
                for (int y = 0; y < Size; y++)
                {
                    _cells[x, y] = Empty;
                }
            }

            Picked = 0;
        }
    }
}
