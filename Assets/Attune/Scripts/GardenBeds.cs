using System;
using System.Collections.Generic;

namespace Attune
{
    /// <summary>
    /// The Keyboard Garden's rules (design, Demos, 2): one bed per gesture. A gesture plants a
    /// seedling in its bed, in the next empty place, or in place of the bed's oldest plant when the
    /// bed is full. The plant planted last is the one a hold grows. Heights run from 0 (empty) through
    /// Seedling to 1 (full grown). Plain C#, so the tests can play it.
    /// </summary>
    public sealed class GardenBeds
    {
        /// <summary>A new plant's height.</summary>
        public const float Seedling = 0.15f;

        private readonly List<string> _beds = new();
        private readonly float[,] _heights;
        private readonly int[,] _plantedAt; // when each place was planted, by count, for finding the oldest
        private int _plantings;

        /// <summary>A garden with a bed for each gesture, each with room for plantsPerBed plants.</summary>
        public GardenBeds(IEnumerable<string> beds, int plantsPerBed)
        {
            _beds.AddRange(beds);
            PlantsPerBed = Math.Max(1, plantsPerBed);
            _heights = new float[_beds.Count, PlantsPerBed];
            _plantedAt = new int[_beds.Count, PlantsPerBed];
        }

        /// <summary>The beds, by the gesture that plants in each.</summary>
        public IReadOnlyList<string> Beds => _beds;

        /// <summary>Room in each bed.</summary>
        public int PlantsPerBed { get; }

        /// <summary>The bed of the plant a hold grows, or −1 before the first planting.</summary>
        public int CurrentBed { get; private set; } = -1;

        /// <summary>The place in its bed of the plant a hold grows, or −1.</summary>
        public int CurrentPlace { get; private set; } = -1;

        /// <summary>How tall the plant a hold grows is (0 with none). Setting it puts it back to a height.</summary>
        public float CurrentHeight
        {
            get => CurrentBed < 0 ? 0f : _heights[CurrentBed, CurrentPlace];
            set
            {
                if (CurrentBed >= 0)
                {
                    _heights[CurrentBed, CurrentPlace] = Math.Min(1f, Math.Max(Seedling, value));
                }
            }
        }

        /// <summary>How many plants are in the garden.</summary>
        public int Plants
        {
            get
            {
                int count = 0;
                foreach (float height in _heights)
                {
                    count += height > 0f ? 1 : 0;
                }

                return count;
            }
        }

        /// <summary>How many plants in a bed.</summary>
        public int PlantsIn(int bed)
        {
            int count = 0;
            for (int place = 0; place < PlantsPerBed; place++)
            {
                count += _heights[bed, place] > 0f ? 1 : 0;
            }

            return count;
        }

        /// <summary>How many plants are full grown.</summary>
        public int FullGrown
        {
            get
            {
                int count = 0;
                foreach (float height in _heights)
                {
                    count += height >= 1f ? 1 : 0;
                }

                return count;
            }
        }

        /// <summary>The height of the plant in a bed's place: 0 if there is none.</summary>
        public float Height(int bed, int place) => _heights[bed, place];

        /// <summary>Puts the plant in a bed's place back to a height (at least a seedling's), if there is one.</summary>
        public void SetHeight(int bed, int place, float height)
        {
            if (bed >= 0 && bed < _beds.Count && place >= 0 && place < PlantsPerBed && _heights[bed, place] > 0f)
            {
                _heights[bed, place] = Math.Min(1f, Math.Max(Seedling, height));
            }
        }

        /// <summary>The bed a gesture plants in, or −1 if it has none.</summary>
        public int BedOf(string gesture) => _beds.IndexOf(gesture);

        /// <summary>Plants a seedling in the gesture's bed; it becomes the plant a hold grows. Returns its bed, or −1 for a gesture with no bed.</summary>
        public int Plant(string gesture)
        {
            int bed = BedOf(gesture);
            if (bed < 0)
            {
                return -1;
            }

            int place = -1;
            int oldest = 0;
            for (int i = 0; i < PlantsPerBed; i++)
            {
                if (_heights[bed, i] <= 0f)
                {
                    place = i;
                    break;
                }

                if (_plantedAt[bed, i] < _plantedAt[bed, oldest])
                {
                    oldest = i;
                }
            }

            if (place < 0)
            {
                place = oldest;
            }

            _heights[bed, place] = Seedling;
            _plantedAt[bed, place] = ++_plantings;
            CurrentBed = bed;
            CurrentPlace = place;
            return bed;
        }

        /// <summary>Grows the plant a hold grows by amount of full height, up to full grown.</summary>
        public void Grow(float amount)
        {
            if (CurrentBed >= 0 && amount > 0f)
            {
                CurrentHeight += amount;
            }
        }
    }
}
