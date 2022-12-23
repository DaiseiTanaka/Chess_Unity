using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleSceneDirector : MonoBehaviour
{
    // ゲーム全体で使うプレイヤー数
    static public int PlayerCount = 2;

    // タイルのプレハブ
    public GameObject[] prefabTile;
    //　カーソルのプレハブ
    public GameObject prefabCursor;

    // 内部データ
    GameObject[,] tiles;
    UnitController[,] units;

    // ユニットのプレハブ（色ごと）
    public List<GameObject> prefabWhiteUnits;
    public List<GameObject> prefabBlackUnits;

    // 1＝ポーン、2＝ルーク、3＝ナイト、4＝ビショップ、5＝クイーン、6＝キング
    public int[,] unitType = 
    {
        {2, 1, 0, 0, 0, 0, 11, 12},
        {3, 1, 0, 0, 0, 0, 11, 13},
        {4, 1, 0, 0, 0, 0, 11, 14},
        {5, 1, 0, 0, 0, 0, 11, 15},
        {6, 1, 0, 0, 0, 0, 11, 16},
        {4, 1, 0, 0, 0, 0, 11, 14},
        {3, 1, 0, 0, 0, 0, 11, 13},
        {2, 1, 0, 0, 0, 0, 11, 12},
    };

    // Start is called before the first frame update
    void Start()
    {
        // 内部データ
        tiles = new GameObject[GameSceneDirector.TILE_X, GameSceneDirector.TILE_Y];
        units = new UnitController[GameSceneDirector.TILE_X, GameSceneDirector.TILE_Y]; 

        for (int i = 0; i < GameSceneDirector.TILE_X; i++)
        {
            for (int j = 0; j < GameSceneDirector.TILE_Y; j++)
            {
                //　タイルノウニットのポジション
                float x = i - GameSceneDirector.TILE_X / 2;
                float y = j - GameSceneDirector.TILE_Y / 2;

                Vector3 pos = new Vector3(x, 0, y);

                //　作成
                int idx = (i + j) % 2;
                GameObject tile = Instantiate(prefabTile[idx], pos, Quaternion.identity);

                tiles[i, j] = tile;

                // ユニットの作成
                int type   = unitType[i, j] % 10;
                int player = unitType[i, j] / 10;

                GameObject prefab = getPrefabUnit(player, type);
                GameObject unit = null;
                UnitController ctrl = null;

                if (null == prefab) continue;

                pos.y += 1.5f;
                unit = Instantiate(prefab);

                // 初期設定
                ctrl = unit.GetComponent<UnitController>();
                ctrl.SetUnit(player, (UnitController.TYPE)type, tile);

                // 内部データセット
                units[i, j] = ctrl;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // ユニットのプレハブを返す
    GameObject getPrefabUnit(int player, int type)
    {
        int idx = type - 1;

        if (0 > idx) return null;

        GameObject prefab = prefabWhiteUnits[idx];
        if (1 == player) prefab = prefabBlackUnits[idx];

        return prefab;
    }

    public void Retry()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void PvP()
    {
        PlayerCount = 2;
        SceneManager.LoadScene("SampleScene");
    }
    public void PvE()
    {
        PlayerCount = 1;
        SceneManager.LoadScene("SampleScene");
    }
    public void EvE()
    {
        PlayerCount = 0;
        SceneManager.LoadScene("SampleScene");
    }
}
