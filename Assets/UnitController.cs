using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class UnitController : MonoBehaviour
{
    // このユニットのプレイヤー番号
    public int Player;

    //　ユニットの種類
    public TYPE Type;

    // 置いてからの経過ターン
    public int ProgressTurnCount;

    // 置いてある場所
    public Vector2Int Pos, OldPos;

    // 移動状態
    public List<STATUS> Status;

    // 1＝ポーン、2＝ルーク、3＝ナイト、4＝ビショップ、5＝クイーン、6＝キング
    public enum TYPE
    {
        NONE = -1,
        PAWN = 1,
        ROOK,
        KNIGHT,
        BISHOP,
        QUEEN,
        KING,
    }

    // 移動状態
    public enum STATUS
    {
        NONE = -1,
        QSIDE_CASTLING = 1,
        KSIDE_CASTLING,
        EN_PASSANT,
        CHECK,
    }

    // Start is called before the first frame update
    void Start()
    {
        Status = new List<STATUS>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    // 初期設定
    public void SetUnit(int player, TYPE type, GameObject tile)
    {
        Player = player;
        Type = type;
        MoveUnit(tile);
        ProgressTurnCount = -1; // 初動に戻す
    }

    // 行動可能範囲取得
    public List<Vector2Int> GetMovableTiles(UnitController[,] units, bool checkking = true)
    {
        List<Vector2Int> ret = new List<Vector2Int>();

        // クイーン
        if (TYPE.QUEEN == Type)
        {
            // ルークとビショップの動きを合成
            ret = getMovableTiles(units, TYPE.ROOK);

            foreach (var v in getMovableTiles(units, TYPE.BISHOP))
            {
                if (!ret.Contains(v)) ret.Add(v);
            }
        }
        // キング
        else if (TYPE.KING == Type)
        {
            ret = getMovableTiles(units, Type);

            // 敵の行動範囲を考慮しない
            if (!checkking) return ret;

            // 削除対象のタイル
            List<Vector2Int> removetiles = new List<Vector2Int>();

            // 敵の移動可能範囲と被っているかチェック
            foreach(var v in ret)
            {
                // 移動した状態を作る
                UnitController[,] copyunits2 = new UnitController[units.GetLength(0), units.GetLength(1)];
                Array.Copy(units, copyunits2, units.Length);
                // 移動してみる
                copyunits2[Pos.x, Pos.y]  = null;
                copyunits2[v.x, v.y]      = this;

                int checkcount = GameSceneDirector.GetCheckUnits(copyunits2, Player, false).Count;

                // 被っていたら削除対象
                if (0 < checkcount) removetiles.Add(v);
            }

            // ↑で取得した的の移動範囲と被っているタイルを削除
            foreach(var v in removetiles)
            {
                ret.Remove(v);

                // キャスリングできる時だけ真横のタイルも削除
                if (-1 != ProgressTurnCount || Pos.y != v.y) continue;

                // 方向
                int add = -1;
                int cnt = units.GetLength(0);
                if (0 > Pos.x - v.x) add = 1;

                for (int i = v.x; 0 < cnt; i += add)
                {
                    Vector2Int del = new Vector2Int(i, v.y);

                    ret.Remove(del);
                    cnt--; 
                }
            }
        }
        else 
        {
            ret = getMovableTiles(units, Type);
        }

        return ret;
    }

    // 指定されたタイプの移動可能範囲を返す
    List<Vector2Int> getMovableTiles(UnitController[,] units, TYPE type)
    {
        List<Vector2Int> ret = new List<Vector2Int>();

        // ポーン
        if (TYPE.PAWN == type)
        {
            int dir = 1;
            if (1 == Player) dir = -1;

            // 前方2マス
            List<Vector2Int> vec = new List<Vector2Int>()
            {
                // dirで反対側になおす
                new Vector2Int(0, 1 * dir),
                new Vector2Int(0, 2 * dir),
            };

            // 2回目以降は1マスしか進めない
            if (-1 < ProgressTurnCount) vec.RemoveAt(vec.Count - 1);

            foreach (var v in vec)
            {
                Vector2Int checkpos = Pos + v;
                if (!isCheckable(units, checkpos)) continue;
                // 他のコマがあったら進めない
                if (null != units[checkpos.x, checkpos.y]) break;

                ret.Add(checkpos);
                
            }

            // 取れる時は斜めに進める
            vec = new List<Vector2Int>()
            {
                // dirで反対側になおす
                new Vector2Int(-1, 1 * dir),
                new Vector2Int(+1, 1 * dir),
            };

            foreach (var v in vec)
            {
                Vector2Int checkpos = Pos + v;
                if (!isCheckable(units, checkpos)) continue;

                // アンパッサン
                if (null != getEnPassantUnit(units, checkpos))
                {
                    ret.Add(checkpos);
                    continue;
                }

                // 何もない
                if (null == units[checkpos.x, checkpos.y]) continue;

                // 自軍のユニットは無視
                if (Player == units[checkpos.x, checkpos.y].Player) continue;

                // ここまできたら追加
                ret.Add(checkpos);
            }
        }
        // ルーク
        else if (TYPE.ROOK == type)
        {
            // 上下左右ユニットとぶつかるまでどこまでも進める
            List<Vector2Int> vec = new List<Vector2Int>()
            {
                new Vector2Int( 0,  1),
                new Vector2Int( 0, -1),
                new Vector2Int( 1,  0),
                new Vector2Int(-1,  0),
            };

            foreach (var v in vec)
            {
                Vector2Int checkpos = Pos + v;
                while (isCheckable(units, checkpos))
                {
                    // 誰かいたら終了 (敵のコマだったら追加して終了)
                    if (null != units[checkpos.x, checkpos.y])
                    {
                        if (Player != units[checkpos.x, checkpos.y].Player)
                        {
                            ret.Add(checkpos);
                        }

                        break;
                    }

                    ret.Add(checkpos);
                    checkpos += v;
                }
            }
        }
        // ナイト
        else if (TYPE.KNIGHT == type)
        {
            List<Vector2Int> vec = new List<Vector2Int>()
            {
                new Vector2Int(-1, 2),
                new Vector2Int(-2, 1),
                new Vector2Int( 1, 2),
                new Vector2Int( 2, 1),
                new Vector2Int(-1,-2),
                new Vector2Int(-2,-1),
                new Vector2Int( 1,-2),
                new Vector2Int( 2,-1),
            };

            foreach (var v in vec)
            {
                Vector2Int checkpos = Pos + v;
                if (!isCheckable(units, checkpos)) continue;

                // 同じプレイヤーのところは移動できない
                if (null != units[checkpos.x, checkpos.y]
                    && Player == units[checkpos.x, checkpos.y].Player)
                {
                    continue;
                }

                ret.Add(checkpos);
            }
        }
        // ビショップ
        else if (TYPE.BISHOP == type)
        {
            // 上左右斜めにユニットとぶつかるまでどこまでも進める
            List<Vector2Int> vec = new List<Vector2Int>()
            {
                new Vector2Int( 1, 1),
                new Vector2Int(-1, 1),
                new Vector2Int( 1,-1),
                new Vector2Int(-1,-1),
            };

            foreach (var v in vec)
            {
                Vector2Int checkpos = Pos + v;
                while (isCheckable(units, checkpos))
                {
                    // 誰かいたら終了 (敵のコマだったら追加して終了)
                    if (null != units[checkpos.x, checkpos.y])
                    {
                        if (Player != units[checkpos.x, checkpos.y].Player)
                        {
                            ret.Add(checkpos);
                        }

                        break;
                    }

                    ret.Add(checkpos);
                    checkpos += v;
                }
            }
        }
        // キング
        else if (TYPE.KING == type)
        {
            List<Vector2Int> vec = new List<Vector2Int>()
            {
                new Vector2Int(-1, 1),
                new Vector2Int( 0, 1),
                new Vector2Int( 1, 1),
                new Vector2Int(-1, 0),
                new Vector2Int( 1, 0),
                new Vector2Int(-1,-1),
                new Vector2Int( 0,-1),
                new Vector2Int( 1,-1),
            };

            foreach (var v in vec)
            {
                Vector2Int checkpos = Pos + v;
                if (!isCheckable(units, checkpos)) continue;

                // 同じプレイヤーのところへは移動できない
                if (null != units[checkpos.x, checkpos.y]
                    && Player == units[checkpos.x, checkpos.y].Player)
                {
                    continue;
                }

                ret.Add(checkpos);
            }

            // ここから下はキャスリングの処理
            
            // 初動以外ならキャスリングの判定をしない
            if (-1 != ProgressTurnCount) return ret;

            // チェックされていたらキャスリングできない
            if ( Status.Contains(STATUS.CHECK)) return ret;

            // キャスリングの場所
            vec = new List<Vector2Int>()
            {
                new Vector2Int(-2, 0),
                new Vector2Int( 2, 0),
            };

            foreach (var v in vec)
            {
                // 左ルーク
                int posx = 0;
                int add = -1;
                // 右ルーク
                if (0 < v.x)
                {
                    add = 1;
                    posx = units.GetLength(0) - 1;
                }

                // 端にルークがいるかどうか
                if (null == units[posx, Pos.y]) continue;

                // ルークじゃない
                if (TYPE.ROOK != units[posx, Pos.y].Type) continue;

                // 初動じゃない
                if (-1 != units[posx, Pos.y].ProgressTurnCount) continue;

                // 移動する途中に誰かいる
                bool lineok = true;

                int cnt = Mathf.Abs(Pos.x - posx) - 1;
                if (0 > Pos.x - v.x) add = 1;

                for (int i = Pos.x + add; 0 < cnt; i += add)
                {
                    //print("調査x " + i);
                    if (null != units[i, Pos.y])
                    {
                        lineok = false;
                    }
                    cnt--; 
                }

                if (!lineok) continue;

                // ここまで来れたら追加
                Vector2Int checkpos = Pos + v;
                ret.Add(checkpos);
            }

        }
        return ret;
    }

    bool isCheckable(UnitController[,] ary, Vector2Int idx)
    {
        if (    idx.x < 0 || ary.GetLength(0) <= idx.x
             || idx.y < 0 || ary.GetLength(1) <= idx.y )
        {
            return false;
        }

        return true;
    }

    // 選択された処理
    public void SelectUnit(bool select = true)
    {
        Vector3 pos = transform.position;
        pos.y += 2;
        GetComponent<Rigidbody>().isKinematic = true;

        // 選択解除
        if (!select)
        {
            pos.y = 1.35f;
            GetComponent<Rigidbody>().isKinematic = false;
        }
    }

    // 移動処理
    public void MoveUnit(GameObject tile)
    {
        // 移動時は非選択状態にする
        SelectUnit(false);

        // タイルのポジションから配列の番号に戻す
        Vector2Int idx = new Vector2Int(
            (int)tile.transform.position.x + GameSceneDirector.TILE_X / 2,
            (int)tile.transform.position.z + GameSceneDirector.TILE_Y / 2);

        // 新しい場所へ
        Vector3 pos = tile.transform.position;
        pos.y = 1.35f;
        transform.position = pos;

        // 移動状態をリセット
        Status.Clear();

        // アンパッサン等の処理
        if (TYPE.PAWN == Type)
        {
            // 縦に2タイル進んだ時
            if (1 < Mathf.Abs(idx.y - Pos.y))
            {
                Status.Add(STATUS.EN_PASSANT);
            }

            // 移動した一歩間に残像が残る
            int dir = -1;
            if (1 == Player) dir = 1;

            Pos.y = idx.y + dir;
        }
        // キャスリング
        else if (TYPE.KING == Type)
        {
            // 横に2タイル進んだら
            if (1 < idx.x - Pos.x)
            {
                Status.Add(STATUS.KSIDE_CASTLING);
            }
            else if (-1 > idx.x - Pos.x)
            {
                Status.Add(STATUS.QSIDE_CASTLING);
            }
        }

        // インデックスの更新
        OldPos = Pos;
        Pos = idx;

        // 置いてからの経過ターンをリセット
        ProgressTurnCount = 0;
    }

    // 前回移動してからのターンをカウントする
    public void ProgressTurn()
    {
        // 初動は無視
        if (0 > ProgressTurnCount) return;

        ProgressTurnCount++;

        // アンパッサンフラグチェック
        if (Type == TYPE.PAWN)
        {
            if (1 < ProgressTurnCount)
            {
                Status.Remove(STATUS.EN_PASSANT);
            }
        }
    }

    // 相手のアンパッサン状態のユニットを返す
    UnitController getEnPassantUnit(UnitController[,] units, Vector2Int pos)
    {
        foreach(var v in units)
        {
            if (null == v) continue;
            if (Player == v.Player) continue;
            if (!v.Status.Contains(STATUS.EN_PASSANT)) continue;
            
            if (v.OldPos == pos) return v;
        }

        return null;
    }

    // 今回のターンのチェック状態をリセット
    public void SetCheckStatus(bool flag=true)
    {
        Status.Remove(STATUS.CHECK);
        if (flag) Status.Add(STATUS.CHECK);
    }
}
