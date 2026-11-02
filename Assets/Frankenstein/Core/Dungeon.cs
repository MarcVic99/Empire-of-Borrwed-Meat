using System;
using System.Collections.Generic;
using System.Linq;
namespace Frankenstein
{
    public enum LandmarkKind { Chest, Lore, Table, Altar, Stairs, Thread }
    public enum RoamingState { Patrol, Pursue, Flee }
    public sealed class DungeonRoom
    {
        public readonly int X, Y;
        public bool Visited, Cleared;
        public string[] Enemies;
        public double EnemyX, EnemyY, PatrolTime;
        public RoamingState EnemyState;
        public bool HasEncounter => !Cleared && Enemies != null && Enemies.Length > 0;
        public double CenterX => X * Dungeon.RoomSpacing;
        public double CenterY => Y * Dungeon.RoomSpacing;
        public DungeonRoom(int x, int y) { X = x; Y = y; }
    }
    public sealed class Landmark
    {
        public readonly LandmarkKind Kind;
        public readonly double X, Y;
        public readonly string Title, Text;
        public bool Used;
        public Landmark(LandmarkKind kind, double x, double y, string title, string text = "")
        { Kind = kind; X = x; Y = y; Title = title; Text = text; }
    }
    // One continuous collision map: room rectangles joined by walkable corridors.
    public sealed class Dungeon
    {
        public const int LastFloor = 3;
        public const double RoomSpacing = 14, RoomHalfSize = 5, CorridorHalfWidth = 1.4;
        private readonly Random random;
        private readonly List<DungeonRoom> rooms = new List<DungeonRoom>();
        private readonly List<Landmark> landmarks = new List<Landmark>();
        private readonly List<FloorArea> floorAreas = new List<FloorArea>();
        private struct FloorArea
        {
            public double Left, Right, Top, Bottom;
            public FloorArea(double left, double top, double right, double bottom)
            { Left = left; Top = top; Right = right; Bottom = bottom; }
        }
        public IReadOnlyList<DungeonRoom> Rooms => rooms.AsReadOnly();
        public IReadOnlyList<Landmark> Landmarks => landmarks.AsReadOnly();
        public DungeonRoom Current { get; private set; }
        public DungeonRoom Exit { get; private set; }
        public DungeonRoom ContactEnemy => rooms.FirstOrDefault(r => r.HasEncounter && Distance(X, Y, r.EnemyX, r.EnemyY) < .95);
        public bool TouchingEnemy => ContactEnemy != null;
        public int Floor { get; private set; }
        public double X { get; private set; }
        public double Y { get; private set; }
        public bool PuzzleSolved { get; private set; }
        public int PuzzleProgress { get; private set; }
        public static readonly string[] SealNames = { "Hueso", "Ojo", "Hilo" };
        private readonly int[] sealOrder = new int[3];
        public string PuzzleClue => string.Join(" → ", sealOrder.Select(i => SealNames[i]));
        public bool NearTable => landmarks.Any(l => l.Kind == LandmarkKind.Table && Distance(X, Y, l.X, l.Y) < 2.2);
        public Landmark Nearby => landmarks.Where(l => !l.Used || l.Kind == LandmarkKind.Table || l.Kind == LandmarkKind.Lore || l.Kind == LandmarkKind.Stairs)
            .Where(l => Distance(X, Y, l.X, l.Y) < 1.9).OrderBy(l => Distance(X, Y, l.X, l.Y)).FirstOrDefault();
        public Dungeon(int seed) { random = new Random(seed); Generate(1); }
        public DungeonRoom At(int x, int y) => rooms.FirstOrDefault(r => r.X == x && r.Y == y);
        public static double Distance(double x, double y, double a, double b) => Math.Sqrt((x - a) * (x - a) + (y - b) * (y - b));
        private void Generate(int floor)
        {
            Floor = floor; rooms.Clear(); landmarks.Clear(); PuzzleSolved = false; PuzzleProgress = 0;
            var order = new[] { 0, 1, 2 }.OrderBy(_ => random.Next()).ToArray(); Array.Copy(order, sealOrder, 3);
            rooms.Add(new DungeonRoom(0, 0));
            // A visible first corridor and enemy are guaranteed on every floor.
            rooms.Add(new DungeonRoom(1, 0));
            int count = random.Next(6, 10);
            while (rooms.Count < count)
            {
                var parent = rooms[random.Next(rooms.Count)]; int dir = random.Next(4);
                int x = parent.X + (dir == 0 ? 1 : dir == 1 ? -1 : 0), y = parent.Y + (dir == 2 ? 1 : dir == 3 ? -1 : 0);
                if (At(x, y) == null) rooms.Add(new DungeonRoom(x, y));
            }
            Exit = rooms.OrderByDescending(r => Math.Abs(r.X) + Math.Abs(r.Y)).First();
            string[] pool = floor == 1 ? new[] { "bandido", "bandida", "lobo" } : new[] { "ghoul", "arana", "lobo", "bandido", "bandida" };
            foreach (var r in rooms.Skip(1))
            {
                r.Enemies = Enumerable.Range(0, random.Next(1, floor == 1 ? 3 : 4)).Select(_ => pool[random.Next(pool.Length)]).ToArray();
                r.EnemyX = r.CenterX; r.EnemyY = r.CenterY + 1.1; r.PatrolTime = random.NextDouble() * 6;
                landmarks.Add(new Landmark(LandmarkKind.Chest, r.CenterX - 3.2, r.CenterY - 2.8, "Cofre sellado"));
            }
            rooms[1].Enemies[0] = "bandido";
            rooms[1].EnemyX = 7; rooms[1].EnemyY = 0; // visible patroller in first corridor
            if (floor == LastFloor) Exit.Enemies = new[] { "jefe" };
            foreach (var r in new[] { rooms[0], Exit })
                landmarks.Add(new Landmark(LandmarkKind.Table, r.CenterX - 3, r.CenterY + 2.8, "Mesa de injertos", "Las agujas aún están calientes. Aquí puedes recomponer el grupo."));
            landmarks.Add(new Landmark(LandmarkKind.Chest, -2.7, -2.6, "Provisiones del vestíbulo"));
            landmarks.Add(new Landmark(LandmarkKind.Lore, 2.5, -2.6, "Nota del primer injertador", "Piso " + floor + ". La piedra recuerda el orden: " + PuzzleClue + ". Repítelo ante el altar para abrir el descenso."));
            landmarks.Add(new Landmark(LandmarkKind.Altar, Exit.CenterX + 2.8, Exit.CenterY - 2.6, "Altar de las tres costuras", "El orden de los sellos está escrito en el vestíbulo."));
            landmarks.Add(new Landmark(LandmarkKind.Stairs, Exit.CenterX, Exit.CenterY - 3, "Descenso"));
            foreach (var r in rooms.Where((r, i) => i % 2 == 0))
                landmarks.Add(new Landmark(LandmarkKind.Thread, r.CenterX + 2.5, r.CenterY + 2.6, "Hilos de sutura"));
            Current = rooms[0]; Current.Visited = true; X = Y = 0;
            floorAreas.Clear();
            foreach (var r in rooms)
            {
                floorAreas.Add(new FloorArea(r.CenterX - RoomHalfSize, r.CenterY - RoomHalfSize, r.CenterX + RoomHalfSize, r.CenterY + RoomHalfSize));
                if (At(r.X + 1, r.Y) != null)
                    floorAreas.Add(new FloorArea(r.CenterX, r.CenterY - CorridorHalfWidth, r.CenterX + RoomSpacing, r.CenterY + CorridorHalfWidth));
                if (At(r.X, r.Y + 1) != null)
                    floorAreas.Add(new FloorArea(r.CenterX - CorridorHalfWidth, r.CenterY, r.CenterX + CorridorHalfWidth, r.CenterY + RoomSpacing));
            }
        }
        public bool IsWalkable(double x, double y, double margin = .24)
        {
            foreach (var area in floorAreas)
            {
                if (x >= area.Left + margin && x <= area.Right - margin && y >= area.Top + margin && y <= area.Bottom - margin) return true;
            }
            return false;
        }
        private bool CanStand(double x, double y) => IsWalkable(x, y) && !landmarks.Any(l =>
            l.Kind != LandmarkKind.Stairs && l.Kind != LandmarkKind.Thread && Distance(x, y, l.X, l.Y) < (l.Kind == LandmarkKind.Table ? 1.05 : .65));
        public void Move(double dx, double dy)
        {
            // Substeps stop high frame times and diagonal motion from tunneling through walls.
            int steps = Math.Max(1, (int)Math.Ceiling(Math.Max(Math.Abs(dx), Math.Abs(dy)) / .16));
            for (int i = 0; i < steps; i++)
            {
                if (CanStand(X + dx / steps, Y)) X += dx / steps;
                if (CanStand(X, Y + dy / steps)) Y += dy / steps;
                var room = rooms.FirstOrDefault(r => Math.Abs(X - r.CenterX) < RoomHalfSize && Math.Abs(Y - r.CenterY) < RoomHalfSize);
                if (room != null) { Current = room; room.Visited = true; }
            }
        }
        public bool HasSight(double ax, double ay, double bx, double by)
        {
            int steps = Math.Max(1, (int)(Distance(ax, ay, bx, by) / .18));
            for (int i = 0; i <= steps; i++) if (!IsWalkable(ax + (bx - ax) * i / steps, ay + (by - ay) * i / steps, .05)) return false;
            return true;
        }
        public void Tick(double seconds)
        {
            seconds = Math.Min(.1, Math.Max(0, seconds));
            foreach (var r in rooms.Where(r => r.HasEncounter))
            {
                double distance = Distance(X, Y, r.EnemyX, r.EnemyY);
                bool sees = distance < 5.5 && HasSight(r.EnemyX, r.EnemyY, X, Y);
                bool timid = r.Enemies[0] == "bandida" || r.Enemies[0] == "arana";
                r.EnemyState = sees ? (timid ? RoamingState.Flee : RoamingState.Pursue) : RoamingState.Patrol;
                r.PatrolTime += seconds;
                double tx = r.CenterX + Math.Cos(r.PatrolTime * .55) * 2.3, ty = r.CenterY + Math.Sin(r.PatrolTime * .55) * 2.3;
                if (sees) { tx = timid ? r.EnemyX + (r.EnemyX - X) : X; ty = timid ? r.EnemyY + (r.EnemyY - Y) : Y; }
                double length = Math.Max(.001, Distance(r.EnemyX, r.EnemyY, tx, ty));
                double speed = sees ? (timid ? 2.5 : 2.0) : .8;
                double dx = (tx - r.EnemyX) / length * speed * seconds, dy = (ty - r.EnemyY) / length * speed * seconds;
                if (CanStand(r.EnemyX + dx, r.EnemyY)) r.EnemyX += dx;
                if (CanStand(r.EnemyX, r.EnemyY + dy)) r.EnemyY += dy;
            }
        }
        public void Retreat(DungeonRoom encounter)
        {
            encounter.EnemyX = encounter.CenterX; encounter.EnemyY = encounter.CenterY;
            X = Y = 0; Current = rooms[0];
        }
        public bool ChooseSeal(int seal)
        {
            if (PuzzleSolved || Nearby?.Kind != LandmarkKind.Altar || Exit.HasEncounter) return false;
            if (seal < 0 || seal > 2) return false;
            if (sealOrder[PuzzleProgress] != seal) { PuzzleProgress = 0; return false; }
            PuzzleProgress++;
            if (PuzzleProgress == 3) { PuzzleSolved = true; landmarks.First(l => l.Kind == LandmarkKind.Altar).Used = true; }
            return true;
        }
        public bool AtStairs => Nearby?.Kind == LandmarkKind.Stairs && !Exit.HasEncounter && PuzzleSolved;
        public bool Descend() { if (!AtStairs || Floor == LastFloor) return false; Generate(Floor + 1); return true; }
    }
}
