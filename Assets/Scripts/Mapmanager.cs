using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// シンプルな 30x30 マップ管理クラス（部屋管理、通路生成、起動時に自動生成されます）。
/// PlayerController からは IsWalkable / GetTileType / GetRandomFloorPosition などを利用してください。
/// 将来的に外部ファイル読み込みや手動配置に差し替えやすいよう、public API を用意しています。
/// </summary>
public class MapManager : MonoBehaviour
{
    public static MapManager Instance { get; private set; }

    [Header("Map Settings")]
    public int width = 30;
    public int height = 30;

    [Tooltip("生成する最大部屋数（配置成功した分だけ採用されます）")]
    public int maxRooms = 8;

    [Tooltip("部屋サイズの最小/最大（幅）")]
    public int roomMinWidth = 4;
    public int roomMaxWidth = 8;

    [Tooltip("部屋サイズの最小/最大（高さ）")]
    public int roomMinHeight = 4;
    public int roomMaxHeight = 8;

    [Header("Generation")]
    [Tooltip("マップ生成のシード。-1 にするとランダムシードを使用します")]
    public int seed = -1;

    // 内部タイル配列
    private TileType[,] tiles;

    // 部屋リスト
    public List<Room> rooms = new List<Room>();

    // 生成完了イベント
    public event System.Action OnMapGenerated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    private void Start()
    {
        GenerateMap();
    }

    /// <summary>
    /// マップを生成します（既存データをリセットして再生成）。
    /// </summary>
    public void GenerateMap()
    {
        if (seed >= 0)
            Random.InitState(seed);
        else
            Random.InitState(System.Environment.TickCount);

        tiles = new TileType[width, height];
        rooms.Clear();

        // 初期化：すべて壁
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
            tiles[x, y] = TileType.Wall;

        // 部屋生成
        int attempts = 0;
        int created = 0;
        while (created < maxRooms && attempts < maxRooms * 6)
        {
            attempts++;
            int rw = Random.Range(roomMinWidth, roomMaxWidth + 1);
            int rh = Random.Range(roomMinHeight, roomMaxHeight + 1);
            int rx = Random.Range(1, width - rw - 1);
            int ry = Random.Range(1, height - rh - 1);

            Room newRoom = new Room(rx, ry, rw, rh);

            bool overlaps = false;
            foreach (var r in rooms)
            {
                if (r.Overlaps(newRoom, 1)) // 余白1でチェック
                {
                    overlaps = true;
                    break;
                }
            }

            if (!overlaps)
            {
                CreateRoom(newRoom);
                rooms.Add(newRoom);
                created++;
            }
        }

        // 部屋を接続（単純な L 字コネクト）
        for (int i = 1; i < rooms.Count; i++)
        {
            Vector2Int prevCenter = rooms[i - 1].Center;
            Vector2Int curCenter = rooms[i].Center;

            if (Random.value < 0.5f)
            {
                CreateHorizontalTunnel(prevCenter.x, curCenter.x, prevCenter.y);
                CreateVerticalTunnel(prevCenter.y, curCenter.y, curCenter.x);
            }
            else
            {
                CreateVerticalTunnel(prevCenter.y, curCenter.y, prevCenter.x);
                CreateHorizontalTunnel(prevCenter.x, curCenter.x, curCenter.y);
            }
        }

        // 階段（スタートと別に1箇所）
        if (rooms.Count > 0)
        {
            var stairRoom = rooms[Random.Range(0, rooms.Count)];
            Vector2Int s = stairRoom.Center;
            tiles[s.x, s.y] = TileType.Stairs;
        }

        OnMapGenerated?.Invoke();
    }

    private void CreateRoom(Room r)
    {
        for (int x = r.x; x < r.x + r.width; x++)
        for (int y = r.y; y < r.y + r.height; y++)
            tiles[x, y] = TileType.Room;
    }

    private void CreateHorizontalTunnel(int x1, int x2, int y)
    {
        int start = Mathf.Min(x1, x2);
        int end = Mathf.Max(x1, x2);
        for (int x = start; x <= end; x++)
        {
            if (InBounds(x, y) && tiles[x, y] == TileType.Wall)
                tiles[x, y] = TileType.Corridor;
        }
    }

    private void CreateVerticalTunnel(int y1, int y2, int x)
    {
        int start = Mathf.Min(y1, y2);
        int end = Mathf.Max(y1, y2);
        for (int y = start; y <= end; y++)
        {
            if (InBounds(x, y) && tiles[x, y] == TileType.Wall)
                tiles[x, y] = TileType.Corridor;
        }
    }

    private bool InBounds(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    private bool InBounds(Vector2Int p) => InBounds(p.x, p.y);

    /// <summary>
    /// 指定グリッドが通行可能か（壁以外なら通行可能）。将来的に敵やオブジェクト判定を追加してください。
    /// </summary>
    public bool IsWalkable(Vector2Int grid)
    {
        if (!InBounds(grid)) return false;
        TileType t = tiles[grid.x, grid.y];
        return t != TileType.Wall;
    }

    /// <summary>
    /// タイルタイプを返す（範囲外は Wall と扱う）
    /// </summary>
    public TileType GetTileType(Vector2Int grid)
    {
        if (!InBounds(grid)) return TileType.Wall;
        return tiles[grid.x, grid.y];
    }

    /// <summary>
    /// 指定座標がどの部屋に含まれるかを返す。含まれなければ null。
    /// </summary>
    public Room GetRoomContaining(Vector2Int grid)
    {
        foreach (var r in rooms)
            if (r.Contains(grid))
                return r;
        return null;
    }

    /// <summary>
    /// 部屋の床（Room か Corridor も可）からランダムな位置を返す。
    /// stairsExclude=true で階段位置を除外できます。
    /// </summary>
    public Vector2Int GetRandomFloorPosition(bool stairsExclude = true)
    {
        List<Vector2Int> candidates = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            var t = tiles[x, y];
            if (t == TileType.Room || t == TileType.Corridor || t == TileType.Floor)
            {
                if (stairsExclude && t == TileType.Stairs) continue;
                candidates.Add(new Vector2Int(x, y));
            }
        }

        if (candidates.Count == 0)
            return Vector2Int.zero;
        return candidates[Random.Range(0, candidates.Count)];
    }

    /// <summary>
    /// デバッグ用：タイルの種類を文字列で取得（Console 出力などで利用）
    /// </summary>
    public string DumpMapToString()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                switch (tiles[x, y])
                {
                    case TileType.Wall: sb.Append('#'); break;
                    case TileType.Room: sb.Append('.'); break;
                    case TileType.Corridor: sb.Append(','); break;
                    case TileType.Stairs: sb.Append('>'); break;
                    default: sb.Append('?'); break;
                }
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    // --- Room クラス ---
    [System.Serializable]
    public class Room
    {
        public int x, y, width, height;
        public Vector2Int Center => new Vector2Int(x + width / 2, y + height / 2);

        public Room(int x, int y, int w, int h)
        {
            this.x = x; this.y = y; this.width = w; this.height = h;
        }

        public bool Contains(Vector2Int p)
        {
            return p.x >= x && p.x < x + width && p.y >= y && p.y < y + height;
        }

        /// <summary>
        /// 余白 padding を持って重複判定
        /// </summary>
        public bool Overlaps(Room other, int padding = 0)
        {
            return !(other.x - padding >= x + width ||
                     other.x + other.width + padding <= x ||
                     other.y - padding >= y + height ||
                     other.y + other.height + padding <= y);
        }
    }
}