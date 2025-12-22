using UnityEngine;
using System.Collections.Generic;
using System.Linq; 

public class SkinManager : MonoBehaviour
{
    public static SkinManager instance;
    private SkinDatabase _skinDatabase;    
    [Header("Current Status")]
    // 現在装備中のスキンのID
    public int currentSkinID; 
    bool anyNewUnlock = false;

    // --- 永続化データのキー ---
    private const string KEY_TOTAL_PLAY_COUNT = "Stats_PlayCount";
    private const string KEY_TOTAL_PLAY_TIME = "Stats_TotalTime";
    private const string KEY_DEATH_COUNT = "Stats_DeathCount";
    private const string KEY_BEST_SCORE = "Stats_BestScore";
    private const string KEY_SKIN_UNLOCKED_PREFIX = "Skin_Unlocked_";
    private const string KEY_EQUIPPED_SKIN = "Skin_Equipped";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // シーン遷移しても消えないようにする
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        _skinDatabase = SkinDatabase.instance;
        LoadStatus();
    }
    /// <summary>
    /// ゲーム開始時にデータをロード
    /// </summary>
    private void LoadStatus()
    {
        // 初期スキン(ID:0)は必ず解放扱いにする
        PlayerPrefs.SetInt(KEY_SKIN_UNLOCKED_PREFIX + "0", 1);
        
        // 装備中のスキンをロード（なければ0番）
        currentSkinID = PlayerPrefs.GetInt(KEY_EQUIPPED_SKIN, 0);
    }

    /// <summary>
    /// ゲームオーバー時に呼ばれる：統計を更新し、スキンの解放チェックを行う
    /// </summary>
    /// <param name="runScore">今回のスコア</param>
    /// <param name="runTime">今回の生存時間(秒)</param>
    public void ReportGameResult(float runScore, float runTime)
    {
        // 1. 統計データを更新・保存
        int playCount = PlayerPrefs.GetInt(KEY_TOTAL_PLAY_COUNT, 0) + 1;
        float totalTime = PlayerPrefs.GetFloat(KEY_TOTAL_PLAY_TIME, 0f) + runTime;
        int deathCount = PlayerPrefs.GetInt(KEY_DEATH_COUNT, 0) + 1;
        float bestScore = PlayerPrefs.GetFloat(KEY_BEST_SCORE, 0f);

        if (runScore > bestScore) bestScore = runScore;

        PlayerPrefs.SetInt(KEY_TOTAL_PLAY_COUNT, playCount);
        PlayerPrefs.SetFloat(KEY_TOTAL_PLAY_TIME, totalTime);
        PlayerPrefs.SetInt(KEY_DEATH_COUNT, deathCount);
        PlayerPrefs.SetFloat(KEY_BEST_SCORE, bestScore);
        PlayerPrefs.Save();

        // 2. 解放条件のチェック
        CheckUnlockConditions(runScore, runTime, playCount, totalTime, deathCount, bestScore);
    }

    /// <summary>
    /// 全スキンをチェックして解放処理を行う
    /// </summary>
    private void CheckUnlockConditions(float runScore, float runTime, int playCount, float totalTime, int deathCount, float bestScore)
    {
        int unlockedCount = 0; // 解放済みスキンの数（ゴールドクラゲ用）

        // 通常の条件チェック
        foreach (var skin in _skinDatabase.GetAllSkins())
        {
            // すでに解放済みならスキップ（ただしカウントはする）
            if (IsUnlocked(skin.id))
            {
                unlockedCount++;
                continue;
            }

            bool unlock = false;

            switch (skin.unlockType)
            {
                case SkinData.UnlockType.None:
                    unlock = true;
                    break;
                case SkinData.UnlockType.ScoreReach:
                    if (bestScore >= skin.conditionValue) unlock = true;
                    break;
                case SkinData.UnlockType.PlayCount:
                    if (playCount >= (int)skin.conditionValue) unlock = true;
                    break;
                case SkinData.UnlockType.SurvivalTime:
                    // 今回の生存時間が条件を超えたか
                    if (runTime >= skin.conditionValue) unlock = true;
                    break;
                case SkinData.UnlockType.TotalPlayTime:
                    if (totalTime >= skin.conditionValue) unlock = true;
                    break;
                case SkinData.UnlockType.DeathCount:
                    if (deathCount >= (int)skin.conditionValue) unlock = true;
                    break;
                // AdWatchやCompleteAllは特殊なのでここでの単純比較はしない
            }

            if (unlock)
            {
                UnlockSkin(skin.id);
                unlockedCount++;
                anyNewUnlock = true;
                Debug.Log($"Skin Unlocked! : {skin.skinName}");
            }
        }

        // 最後に「コンプリート条件（ゴールドクラゲ）」のチェック 
        // 自身のID以外の全スキン数と比較
        SkinData goldSkin = _skinDatabase.GetAllSkins().FirstOrDefault(s => s.unlockType == SkinData.UnlockType.CompleteAll);
        if (goldSkin != null && !IsUnlocked(goldSkin.id))
        {
            // 自分以外すべて解放されているか
            if (unlockedCount >= _skinDatabase.GetAllSkins().Count - 1)
            {
                UnlockSkin(goldSkin.id);
                Debug.Log("ALL COMPLETE! Gold Skin Unlocked!");
            }
        }
    }

    /// <summary>
    /// スキンを解放状態にする
    /// </summary>
    public void UnlockSkin(int id)
    {
        PlayerPrefs.SetInt(KEY_SKIN_UNLOCKED_PREFIX + id, 1);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// スキンが解放されているか確認
    /// </summary>
    public bool IsUnlocked(int id)
    {
        return PlayerPrefs.GetInt(KEY_SKIN_UNLOCKED_PREFIX + id, 0) == 1;
    }

    /// <summary>
    /// スキンを装備する
    /// </summary>
    public void EquipSkin(int id)
    {
        if (IsUnlocked(id))
        {
            currentSkinID = id;
            PlayerPrefs.SetInt(KEY_EQUIPPED_SKIN, id);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// 現在装備中のスキンのSpriteを取得（Playerが表示するときに使う）
    /// </summary>
    public Sprite GetCurrentSkinSprite()
    {
        var skin = _skinDatabase.GetAllSkins().FirstOrDefault(s => s.id == currentSkinID);
        return skin != null ? skin.skinSprite : null;
    }
}