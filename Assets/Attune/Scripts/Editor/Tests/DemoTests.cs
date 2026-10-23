using System.Collections.Generic;
using Attune.Signals;
using NUnit.Framework;

namespace Attune.Tests
{
    /// <summary>
    /// M4's demos played as rules (design, Demos 1 to 3): row-column scanning (ruling A29), the
    /// Compass Sweep's map and needle (A31), the Keyboard Garden's beds, and the Dwell Builder's plate.
    /// </summary>
    public class DemoTests
    {
        //---------- Row-column scanning

        [Test]
        public void APressPicksARowThenAnItemInIt()
        {
            var scan = new RowColumnScan(new[] { 4, 8, 8 }, new Scanner(1f, 0, true));
            Assert.That(scan.Row, Is.EqualTo(0));
            Assert.That(scan.Item, Is.EqualTo(-1), "no item while the rows are scanned");
            scan.Advance(1f);
            Assert.That(scan.Row, Is.EqualTo(1));

            Assert.That(scan.Press(out _, out _), Is.False, "the first press picks the row");
            Assert.That(scan.InRow, Is.True);
            Assert.That(scan.Row, Is.EqualTo(1));
            Assert.That(scan.Item, Is.EqualTo(0), "its items start from the first");
            scan.Advance(1f);
            scan.Advance(1f);
            Assert.That(scan.Item, Is.EqualTo(2));

            Assert.That(scan.Press(out int row, out int item), Is.True);
            Assert.That((row, item), Is.EqualTo((1, 2)));
            Assert.That(scan.InRow, Is.False);
            Assert.That(scan.Row, Is.EqualTo(0), "the rows start from the top again");
        }

        [Test]
        public void ARowWithNoPressGivesWayToTheRowsAtThatRow()
        {
            var scan = new RowColumnScan(new[] { 3, 3, 3 }, new Scanner(1f, 0, true));
            scan.Advance(1f);
            scan.Press(out _, out _);
            Assert.That(scan.InRow, Is.True);
            scan.Advance(1f);
            scan.Advance(1f);
            Assert.That(scan.InRow, Is.True, "every item is shown once");
            scan.Advance(1f);
            Assert.That(scan.InRow, Is.False);
            Assert.That(scan.Row, Is.EqualTo(1));
            Assert.That(scan.IsPaused, Is.False);
        }

        [Test]
        public void NotFromTheTopTheRowsGoOnFromThePickedRow()
        {
            var scan = new RowColumnScan(new[] { 2, 2, 2 }, new Scanner(1f, 0, false));
            scan.Advance(1f);
            scan.Advance(1f);
            scan.Press(out _, out _);
            scan.Press(out int row, out int item);
            Assert.That((row, item), Is.EqualTo((2, 0)));
            Assert.That(scan.Row, Is.EqualTo(2));
        }

        [Test]
        public void PausedRowsWakeOnAPressWithoutPicking()
        {
            var scan = new RowColumnScan(new[] { 2, 2 }, new Scanner(1f, 1, true));
            scan.Advance(1f);
            scan.Advance(1f);
            Assert.That(scan.IsPaused, Is.True);
            Assert.That(scan.Press(out _, out _), Is.False);
            Assert.That(scan.InRow, Is.False, "waking picks nothing");
            Assert.That(scan.IsPaused, Is.False);
        }

        //---------- Compass Sweep

        [Test]
        public void TheIslandHasItsBoatAndFiveFlags()
        {
            var map = new CompassMap(CompassMap.Island);
            Assert.That((map.Width, map.Height), Is.EqualTo((13, 8)));
            Assert.That(map.Boat, Is.EqualTo((0, 7)));
            Assert.That(map.Flags.Count, Is.EqualTo(5));
            Assert.That(map.IsOpen(0) && map.IsOpen(1) && map.IsOpen(2), Is.True, "north, north-east and east are sea");
            Assert.That(map.IsOpen(4) || map.IsOpen(5) || map.IsOpen(6), Is.False, "south and west are off the map");
        }

        [Test]
        public void TheBoatSailsOnlyOntoSea()
        {
            var map = new CompassMap(new[] { "B #", ". 1" });
            Assert.That(map.Step(2, out _), Is.False, "east is land");
            Assert.That(map.Boat, Is.EqualTo((0, 0)));
            Assert.That(map.Step(3, out int flag), Is.True, "south-east is sea");
            Assert.That(flag, Is.EqualTo(0));
            Assert.That(map.AllReached, Is.True);
            map.ResetFlags();
            Assert.That(map.FlagsReached, Is.EqualTo(0));
        }

        [Test]
        public void EveryFlagCanBeReachedBySea()
        {
            var map = new CompassMap(CompassMap.Island);
            for (int i = 0; i < map.Flags.Count; i++)
            {
                List<int> path = map.PathTo(map.Flags[i].X, map.Flags[i].Y);
                Assert.That(path, Is.Not.Null, "flag " + (i + 1));
                foreach (int heading in path)
                {
                    Assert.That(map.Step(heading, out _), Is.True);
                }

                Assert.That(map.IsReached(i), Is.True, "flag " + (i + 1));
            }

            Assert.That(map.AllReached, Is.True);
        }

        [Test]
        public void TheNeedleSweepsTheOpenHeadingsThenWaits()
        {
            var map = new CompassMap(CompassMap.Island);
            var needle = new CompassNeedle(new Scanner(1f, 3, true));
            needle.Refresh(map, 0);
            Assert.That(needle.Open, Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(needle.Heading, Is.EqualTo(0));
            needle.Advance(1f, 1f);
            needle.Advance(1f, 1f);
            Assert.That(needle.Heading, Is.EqualTo(2));
            Assert.That(needle.IsWaiting, Is.False);
            needle.Advance(1f, 1f);
            Assert.That(needle.IsWaiting, Is.True, "it waits after one full sweep");
            Assert.That(needle.Press(), Is.EqualTo(-1), "the press wakes it and doesn't sail");
            Assert.That(needle.IsWaiting, Is.False);
            Assert.That(needle.Heading, Is.EqualTo(2), "it wakes where it waited");
        }

        [Test]
        public void TheNeedleStaysOnTheHeadingItSailed()
        {
            // Ruling A31: pressing again repeats the step.
            var map = new CompassMap(CompassMap.Island);
            var needle = new CompassNeedle(new Scanner(1f, 3, true));
            needle.Refresh(map, 0);
            needle.Advance(2f, 1f);
            int heading = needle.Press();
            Assert.That(heading, Is.EqualTo(2));
            map.Step(heading, out _);
            needle.Refresh(map, heading);
            Assert.That(needle.Heading, Is.EqualTo(2));
            Assert.That(needle.Press(), Is.EqualTo(2), "a second press sails east again");
        }

        [Test]
        public void ABlockedHeadingPassesToTheNextOpenOneClockwise()
        {
            var map = new CompassMap(new[] { ". . .", ". B #", ". . ." });
            var needle = new CompassNeedle(new Scanner(1f, 3, true));
            needle.Refresh(map, 2);
            Assert.That(needle.Open, Has.No.Member(2), "east is land");
            Assert.That(needle.Heading, Is.EqualTo(3), "south-east, the next clockwise");
        }

        //---------- Keyboard Garden

        [Test]
        public void EachGesturePlantsInItsOwnBed()
        {
            var garden = new GardenBeds(KeyboardZones.Quadrants, 5);
            for (int bed = 0; bed < KeyboardZones.Quadrants.Length; bed++)
            {
                Assert.That(garden.Plant(KeyboardZones.Quadrants[bed]), Is.EqualTo(bed));
                Assert.That(garden.PlantsIn(bed), Is.EqualTo(1));
                Assert.That(garden.CurrentBed, Is.EqualTo(bed), "the newest plant is the one a hold grows");
            }

            Assert.That(garden.Plant("Smile"), Is.EqualTo(-1), "a gesture with no bed plants nothing");
            Assert.That(garden.Plants, Is.EqualTo(4));
        }

        [Test]
        public void AFullBedReplacesItsOldestPlant()
        {
            var garden = new GardenBeds(new[] { "A" }, 3);
            garden.Plant("A");
            garden.Grow(1f);
            garden.Plant("A");
            garden.Plant("A");
            Assert.That(garden.FullGrown, Is.EqualTo(1));
            garden.Plant("A");
            Assert.That(garden.PlantsIn(0), Is.EqualTo(3));
            Assert.That(garden.CurrentPlace, Is.EqualTo(0), "the first place, planted first, is replanted");
            Assert.That(garden.FullGrown, Is.EqualTo(0));
        }

        [Test]
        public void AHoldGrowsTheNewestPlantToFullAndNoFurther()
        {
            var garden = new GardenBeds(new[] { "A", "B" }, 2);
            garden.Grow(0.5f);
            Assert.That(garden.Plants, Is.EqualTo(0), "nothing grows before a planting");
            garden.Plant("A");
            garden.Plant("B");
            garden.Grow(0.4f);
            Assert.That(garden.Height(1, 0), Is.EqualTo(GardenBeds.Seedling + 0.4f).Within(1e-5f));
            Assert.That(garden.Height(0, 0), Is.EqualTo(GardenBeds.Seedling), "the older plant stays as it was");
            garden.Grow(5f);
            Assert.That(garden.CurrentHeight, Is.EqualTo(1f));
            garden.CurrentHeight = 0.3f;
            Assert.That(garden.Height(1, 0), Is.EqualTo(0.3f).Within(1e-5f), "a height can be put back");
        }

        //---------- Dwell Builder

        [Test]
        public void ThePickedBlockGoesOnACellAndComesOffTheSameWay()
        {
            var plate = new BuildPlate();
            Assert.That(plate.Picked, Is.EqualTo(0));
            Assert.That(plate.Place(3, 4), Is.EqualTo(0));
            plate.Pick(2);
            Assert.That(plate.Place(3, 4), Is.EqualTo(2), "another block replaces it");
            Assert.That(plate.Place(3, 4), Is.EqualTo(BuildPlate.Empty), "the same block takes it off");
            plate.Pick(9);
            Assert.That(plate.Picked, Is.EqualTo(2), "an unknown block is ignored");
            Assert.That(plate.Place(8, 0), Is.EqualTo(BuildPlate.Empty), "off the plate places nothing");
            Assert.That(plate.Placed, Is.EqualTo(0));
        }
    }
}
