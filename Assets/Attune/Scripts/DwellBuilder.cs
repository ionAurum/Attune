using System.Collections.Generic;
using Aurumworks;
using Attune.Signals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Attune
{
    /// <summary>
    /// Demo 3, the Dwell Builder (design, Demos, 3): an 8×8 build plate and a palette of four
    /// blocks. With a source that points, resting on a block picks it and resting on a cell places
    /// it (Point → Select by dwell): a ring shows the dwell's progress, moving away cancels, and a
    /// border round the screen is a rest zone where nothing is ever picked. There are no clicks
    /// anywhere. With a source that only presses, the palette and the plate's rows are scanned, a
    /// press picks the lit row and then the lit block or cell in it (ruling A29: row-column
    /// scanning). Placing the picked block on a cell that holds it takes it off.
    /// </summary>
    public class DwellBuilder : DemoScreen
    {
        private const int PaletteId = 100; // a palette block's dwell target is PaletteId + block; a cell's is y * Size + x

        private static readonly Color _lit = new(1f, 0.84f, 0.2f);
        private static readonly Color[] _blockColours =
        {
            new(0.66f, 0.68f, 0.72f), new(0.38f, 0.74f, 0.33f), new(0.3f, 0.58f, 0.97f), new(0.7f, 0.47f, 0.25f),
        };

        private readonly DwellField _field = new();
        private readonly DwellTimer _dwell = new(1.5f);
        private readonly List<RectTransform> _palette = new();
        private readonly List<TextMeshProUGUI> _paletteNames = new();
        private readonly Vector3[] _corners = new Vector3[4];
        private (UiShape Fill, PolyShape Mark)[,] _cellViews;
        private RectTransform[,] _cells;
        private RectTransform _plate, _paletteColumn, _pickedFrame, _rowFrame, _itemFrame, _hoverFrame, _cursor;
        private RectTransform[] _rows;
        private ArcShape _progress;
        private PolyShape _cursorDot;
        private RowColumnScan _scan;
        private bool _settle; // the next dwell frame takes where the point rests as already picked

        /// <summary>The plate and the picked block.</summary>
        public BuildPlate Plate { get; private set; }

        /// <summary>True while a source that points plays it by dwelling; false while it is scanned.</summary>
        public bool IsDwelling { get; private set; }

        /// <summary>The row-column scan while it is scanned (row 0 the palette, then the plate's rows from the top); else null.</summary>
        public RowColumnScan Scan => IsDwelling ? null : _scan;

        /// <inheritdoc/>
        protected override string DemoId => "DwellBuilder";

        /// <inheritdoc/>
        protected override string Status => Plate == null ? "" : $"Picked: {BuildPlate.BlockNames[Plate.Picked]}  ·  Blocks {Plate.Placed}";

        /// <inheritdoc/>
        protected override string ActionHint => IsDwelling
            ? "Rest on a block, then on a cell  ·  The edge is a rest zone"
            : "Tap: pick the lit row, then the lit block or cell";

        /// <inheritdoc/>
        protected override float StageBottom => AttuneTuning.Current.RestBorder + 0.02f;

        /// <inheritdoc/>
        protected override float StageSide => AttuneTuning.Current.RestBorder + 0.02f;

        //----------

        /// <summary>A dwell target's middle on the screen, 0 to 1 on each axis, y up: a palette block's (PaletteTarget) or a cell's (CellTarget).</summary>
        public Vector2 CentreOf(int target)
        {
            RectTransform rect = target >= PaletteId ? _palette[target - PaletteId] : _cells[target % BuildPlate.Size, target / BuildPlate.Size];
            Rect area = ScreenArea(rect);
            return area.center;
        }

        /// <summary>A palette block's dwell target.</summary>
        public static int PaletteTarget(int block) => PaletteId + block;

        /// <summary>A cell's dwell target.</summary>
        public static int CellTarget(int x, int y) => y * BuildPlate.Size + x;

        /// <inheritdoc/>
        protected override void BuildStage()
        {
            Plate = new BuildPlate();
            UiTheme theme = UiKit.Theme;
            float border = AttuneTuning.Current.RestBorder;
            _field.RestBorder = border;

            // The rest zone, drawn as a margin round the screen, under everything else. The header's
            // band is drawn as part of it: nothing there can be picked either.
            RectTransform rest = UiKit.Anchor(UiKit.Rect("Rest zone", ScreenRect), 0f, 0f, 1f, 1f);
            rest.SetSiblingIndex(1);
            UiKit.Fill(rest, theme.Panel);
            RectTransform inside = UiKit.Anchor(UiKit.Rect("Inside", rest), border, border, 1f - border, Mathf.Min(1f - border, 0.895f));
            UiKit.Shape(inside, theme.Background, theme.SmallCorner);
            UiKit.Shape(UiKit.Anchor(UiKit.Rect("Edge", inside), 0f, 0f, 1f, 1f), theme.Line, theme.SmallCorner, 3f);

            // The palette: a column of the four blocks, each with its own mark as well as colour (ruling A10).
            RectTransform palette = UiKit.Anchor(UiKit.Rect("Palette", Stage), 0f, 0.02f, 0.17f, 0.98f);
            _paletteColumn = palette;
            for (int block = 0; block < BuildPlate.Blocks; block++)
            {
                float top = 1f - block * 0.25f;
                RectTransform tile = UiKit.Anchor(UiKit.Rect("Block: " + BuildPlate.BlockNames[block], palette), 0.08f, top - 0.24f, 0.92f, top - 0.01f);
                UiKit.Shape(tile, theme.Panel, theme.ButtonCorner);
                Fit(UiKit.Anchor(UiKit.Rect("Swatch area", tile), 0.1f, 0.3f, 0.9f, 0.95f), out RectTransform square);
                Block(square, block, out _, out _);
                TextMeshProUGUI name = UiKit.Text(tile, BuildPlate.BlockNames[block], 26f, theme.Text, FontStyles.Bold, TextAlignmentOptions.Center);
                UiKit.Anchor(name.rectTransform, 0f, 0.02f, 1f, 0.3f);
                _palette.Add(tile);
                _paletteNames.Add(name);
            }

            // The plate: as large a square as fits beside the palette.
            RectTransform plateArea = UiKit.Anchor(UiKit.Rect("Plate area", Stage), 0.2f, 0f, 1f, 1f);
            Fit(plateArea, out _plate);
            UiKit.Shape(_plate, theme.Panel, theme.PanelCorner);
            _cells = new RectTransform[BuildPlate.Size, BuildPlate.Size];
            _cellViews = new (UiShape, PolyShape)[BuildPlate.Size, BuildPlate.Size];
            _rows = new RectTransform[BuildPlate.Size];
            float step = 1f / BuildPlate.Size;
            for (int y = 0; y < BuildPlate.Size; y++)
            {
                RectTransform row = UiKit.Anchor(UiKit.Rect("Row " + (y + 1), _plate), 0f, 1f - (y + 1) * step, 1f, 1f - y * step);
                _rows[y] = row;
                for (int x = 0; x < BuildPlate.Size; x++)
                {
                    RectTransform cell = UiKit.Anchor(UiKit.Rect($"Cell {x},{y}", row), x * step + step * 0.06f, 0.06f, (x + 1) * step - step * 0.06f, 0.94f);
                    _cells[x, y] = cell;
                    Block(cell, -1, out UiShape fill, out PolyShape mark);
                    _cellViews[x, y] = (fill, mark);
                }
            }

            // On top: the picked block, the lit row and item while scanning, the target under the pointer, and the pointer with its ring.
            _pickedFrame = Frame("Picked", theme.Text, 5f);
            _rowFrame = Frame("Row lit", _lit, 7f);
            _itemFrame = Frame("Item lit", _lit, 7f);
            _hoverFrame = Frame("Under the pointer", theme.Text, 3f);
            float size = AttuneTuning.Current.DwellRingSize;
            _cursor = UiKit.Pin(UiKit.Rect("Pointer", ScreenRect), new Vector2(0.5f, 0.5f), Vector2.one * size);
            ArcShape.On(_cursor, "Ring", theme.Text, 4f);
            _progress = ArcShape.On(_cursor, "Dwell", _lit, 12f);
            _cursorDot = PolyShape.On(UiKit.Anchor(UiKit.Rect("Dot", _cursor), 0.42f, 0.42f, 0.58f, 0.58f), "Shape", PolyShape.Circle(), theme.Text);

            ChooseMode();
            Show();
        }

        /// <inheritdoc/>
        protected override void Begin() => Router.Dwell = _dwell;

        /// <inheritdoc/>
        protected override void End()
        {
            if (Router.Dwell == _dwell)
            {
                Router.Dwell = null;
            }
        }

        /// <inheritdoc/>
        protected override void SourceChanged()
        {
            ChooseMode();
            Show();
        }

        //----------

        /// <inheritdoc/>
        protected override void Play(float dt)
        {
            TimingValues timing = Router.Timing;
            if (IsDwelling)
            {
                Canvas.ForceUpdateCanvases(); // the targets' places on the screen, after layout
                _field.RestBorder = AttuneTuning.Current.RestBorder;
                _field.Clear();
                for (int block = 0; block < BuildPlate.Blocks; block++)
                {
                    AddTarget(PaletteTarget(block), _palette[block]);
                }

                for (int y = 0; y < BuildPlate.Size; y++)
                {
                    for (int x = 0; x < BuildPlate.Size; x++)
                    {
                        AddTarget(CellTarget(x, y), _cells[x, y]);
                    }
                }

                _dwell.DwellSeconds = timing.DwellSeconds;
                Vector2 point = Router.Point;
                int under = _field.TargetAt(point.x, point.y);
                if (_settle)
                {
                    _dwell.Settle(under); // arriving, or a menu closing, never picks what the point rests on
                    _settle = false;
                }
                else
                {
                    int picked = _dwell.Update(under, dt);
                    if (picked != DwellTimer.None)
                    {
                        Select(picked);
                    }
                }
            }
            else
            {
                _scan.Rows.StepSeconds = timing.ScanStepSeconds;
                _scan.Rows.Rounds = timing.ScanRounds;
                _scan.Rows.FromTop = timing.ScanFromTop;
                if (!Router.IsPressDown)
                {
                    _scan.Advance(dt); // it stands still while a press is held, so the press picks what was lit as it began
                }
            }

            Show();
        }

        /// <inheritdoc/>
        protected override void Paused()
        {
            _dwell.Cancel();
            _settle = true;
            _cursor.gameObject.SetActive(false);
        }

        /// <inheritdoc/>
        protected override void PressEnded(PressKind kind)
        {
            if (IsDwelling)
            {
                return; // no clicks anywhere
            }

            if (_scan.Press(out int row, out int item))
            {
                Select(row == 0 ? PaletteTarget(item) : CellTarget(item, row - 1));
            }

            Show();
        }

        private void Select(int target)
        {
            if (target >= PaletteId)
            {
                Plate.Pick(target - PaletteId);
            }
            else
            {
                Plate.Place(target % BuildPlate.Size, target / BuildPlate.Size);
            }

            Router.CountSelect();
        }

        private void ChooseMode()
        {
            IsDwelling = ChannelRoutes.Has(Router.Supplied, Channels.Point);
            _dwell.Cancel();
            _settle = true;
            var rows = new List<int> { BuildPlate.Blocks };
            for (int y = 0; y < BuildPlate.Size; y++)
            {
                rows.Add(BuildPlate.Size);
            }

            TimingValues timing = Router.Timing;
            _scan = new RowColumnScan(rows, new Scanner(timing.ScanStepSeconds, timing.ScanRounds, timing.ScanFromTop));
        }

        //----------

        private void Show()
        {
            UiTheme theme = UiKit.Theme;
            for (int y = 0; y < BuildPlate.Size; y++)
            {
                for (int x = 0; x < BuildPlate.Size; x++)
                {
                    int block = Plate.At(x, y);
                    (UiShape fill, PolyShape mark) = _cellViews[x, y];
                    fill.color = block == BuildPlate.Empty ? theme.Raised : _blockColours[block];
                    mark.gameObject.SetActive(block != BuildPlate.Empty);
                    if (block != BuildPlate.Empty)
                    {
                        mark.SetPoints(Mark(block));
                    }
                }
            }

            for (int block = 0; block < BuildPlate.Blocks; block++)
            {
                string name = block == Plate.Picked ? BuildPlate.BlockNames[block] + ": picked" : BuildPlate.BlockNames[block];
                if (_paletteNames[block].text != name)
                {
                    _paletteNames[block].text = name;
                }
            }

            Cover(_pickedFrame, _palette[Plate.Picked], 0f);

            // Scanning: the lit row, then the lit item in it.
            bool scanning = !IsDwelling && _scan != null;
            bool row = scanning && !_scan.IsPaused && !_scan.InRow && _scan.Row >= 0;
            _rowFrame.gameObject.SetActive(row);
            if (row)
            {
                Cover(_rowFrame, _scan.Row == 0 ? _paletteColumn : _rows[_scan.Row - 1], 6f);
            }

            bool item = scanning && _scan.InRow && _scan.Item >= 0;
            _itemFrame.gameObject.SetActive(item);
            if (item)
            {
                Cover(_itemFrame, _scan.Row == 0 ? _palette[_scan.Item] : _cells[_scan.Item, _scan.Row - 1], 8f);
            }

            // Dwelling: the pointer, its ring, and the target under it.
            _cursor.gameObject.SetActive(IsDwelling);
            int target = IsDwelling ? _dwell.Target : DwellTimer.None;
            _hoverFrame.gameObject.SetActive(target != DwellTimer.None);
            if (target != DwellTimer.None)
            {
                Cover(_hoverFrame, target >= PaletteId ? _palette[target - PaletteId] : _cells[target % BuildPlate.Size, target / BuildPlate.Size], 4f);
            }

            if (IsDwelling)
            {
                Vector2 point = Router.Point;
                _cursor.anchorMin = _cursor.anchorMax = point;
                _cursor.anchoredPosition = Vector2.zero;
                _progress.Fill = _dwell.Progress;
                _cursorDot.color = _field.IsRest(point.x, point.y) ? theme.Muted : theme.Text;
            }
        }

        // Draws a block (or an empty cell, for block −1) in rect: a rounded square of its colour with its mark.
        private void Block(RectTransform rect, int block, out UiShape fill, out PolyShape mark)
        {
            fill = UiKit.Shape(rect, block < 0 ? UiKit.Theme.Raised : _blockColours[block], 8f);
            RectTransform markArea = UiKit.Anchor(UiKit.Rect("Mark", rect), 0.3f, 0.3f, 0.7f, 0.7f);
            mark = PolyShape.On(markArea, "Shape", Mark(Mathf.Max(0, block)), UiKit.Theme.Background);
            mark.gameObject.SetActive(block >= 0);
        }

        private static Vector2[] Mark(int block) => block switch
        {
            0 => PolyShape.Square(),
            1 => PolyShape.Triangle(),
            2 => PolyShape.Circle(),
            _ => PolyShape.Diamond(),
        };

        // A frame on the stage, over the rest; Cover moves it over a target.
        private RectTransform Frame(string name, Color color, float width)
        {
            RectTransform frame = UiKit.Rect(name, Stage);
            UiKit.Shape(frame, color, UiKit.Theme.ButtonCorner, width);
            frame.gameObject.SetActive(false);
            return frame;
        }

        // Moves frame (a child of Stage) over target, pad wider all round, and shows it.
        private void Cover(RectTransform frame, RectTransform target, float pad)
        {
            frame.gameObject.SetActive(true);
            Rect area = LocalArea(target, Stage);
            frame.anchorMin = frame.anchorMax = Vector2.zero;
            frame.pivot = new Vector2(0.5f, 0.5f);
            Vector2 origin = -Stage.rect.size * Stage.pivot;
            frame.anchoredPosition = area.center - origin;
            frame.sizeDelta = area.size + Vector2.one * (pad * 2f);
        }

        private void AddTarget(int id, RectTransform rect)
        {
            Rect area = ScreenArea(rect);
            _field.Add(id, area.xMin, area.yMin, area.width, area.height);
        }

        // Where rect is on the screen, 0 to 1 on each axis, y up (the canvas is screen space overlay).
        private Rect ScreenArea(RectTransform rect)
        {
            rect.GetWorldCorners(_corners);
            float w = UnityEngine.Screen.width, h = UnityEngine.Screen.height;
            return Rect.MinMaxRect(_corners[0].x / w, _corners[0].y / h, _corners[2].x / w, _corners[2].y / h);
        }

        // Where rect is in space's local units.
        private Rect LocalArea(RectTransform rect, RectTransform space)
        {
            rect.GetWorldCorners(_corners);
            Vector3 min = space.InverseTransformPoint(_corners[0]);
            Vector3 max = space.InverseTransformPoint(_corners[2]);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        // A square child of area, as large as fits, centred.
        private static void Fit(RectTransform area, out RectTransform square)
        {
            square = UiKit.Anchor(UiKit.Rect("Square", area), 0f, 0f, 1f, 1f);
            var fitter = square.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
        }
    }
}
