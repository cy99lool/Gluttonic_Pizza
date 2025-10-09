using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable] // Json化したときに保存されるように
public class CursorInfo : MonoBehaviour
{
    const float FillAmountMin = 0f;
    const float FillAmountMax = 1f;

    [System.Serializable]
    public enum Mode
    {
        None = 0,
        Normal = 1,
        Big = 2,
        Bomb = 3
    }

    [System.Serializable]
    class FoodVarient
    {
        [Header("モード"), SerializeField] Mode mode = Mode.Normal;
        public Mode VarientMode => mode;
        [Header("食べ物"), SerializeField] FoodMove food;
        public FoodMove VarientFood => food;
    }

    const Mode DefaultMode = Mode.Normal;

    [FormerlySerializedAs("team"), SerializeField] TeamColor teamColor;
    SystemManager.Team team;
    public SystemManager.Team Team => team;

    SystemManager systemManager;

    [SerializeField] Mode foodMode;
    public Mode FoodMode => foodMode;

    [SerializeField] List<FoodVarient> foodVarients;
    public FoodMove Food
    {
        get
        {
            // モードに応じた食べ物を返す
            for (int i = 0; i < foodVarients.Count; i++)
            {
                if (foodVarients[i].VarientMode == foodMode) return foodVarients[i].VarientFood;
            }
            // 登録されていなかった場合
            return foodVarients[0].VarientFood;// 登録されているものの先頭を返す
        }
    }

    [SerializeField] UnityEngine.UI.Button bigButton;
    [SerializeField] UnityEngine.UI.Button bombButton;
    [SerializeField] TMPro.TextMeshProUGUI bulletCountText;

    List<Mode> canModes;
    public List<Mode> CanModes => canModes;
    public bool Shootable => team.Shootable;
    public bool CanBig => GetCanFlag(Mode.Big);// 移行可能モードに巨大化があるかを返す
    public bool CanBomb => GetCanFlag(Mode.Bomb);// 移行可能モードに爆弾化があるかを返す

    void Start()
    {
        foodMode = DefaultMode;
        canModes = new List<Mode>();

        // SystemManagerを取得（チームの取得用、JSONファイルにならないようにインスペクタ上ではアタッチしていない）
        systemManager = FindObjectOfType<SystemManager>();

        // 自身のチームを取得
        foreach(SystemManager.Team team in systemManager.Teams)
        {
            if(teamColor == team.Color)
            {
                this.team = team;
                break;
            }
        }
    }

    void Update()
    {
        UpdateButtonFillAmount(CanBig, bigButton);// 巨大化ボタンの更新
        UpdateButtonFillAmount(CanBomb, bombButton);// 爆弾化ボタンの更新
        UpdateBulletCountUI();// 残弾数の更新
    }

    void UpdateButtonFillAmount(bool flag, UnityEngine.UI.Button button)
    {
        // ボタンが見えるように
        if (flag && button.image.fillAmount != FillAmountMax) button.image.fillAmount = FillAmountMax;

        // ボタンが見えないように
        if (!flag && button.image.fillAmount != FillAmountMin) button.image.fillAmount = FillAmountMin;
    }

    void UpdateBulletCountUI()
    {
        if (bulletCountText == null) return;
        bulletCountText.text = $"残弾数:{team.BulletCount}";
    }

    // 現在の食材のモードを設定
    public void SetMode(Mode mode)
    {
        foodMode = mode;
    }

    // それぞれのモードの移行可能フラグを設定
    // 単体
    public void SetModeFlag(Mode canMode)
    {
        if(canModes.Count > 0)
        {
            foreach(Mode mode in canModes)
            {
                if (mode == canMode) return;// 既に移行可能なら追加しない
            }
        }
        canModes.Add(canMode);// 移行可能なモードのリストに追加
    }
    // リスト
    public void SetModeFlag(List<Mode> canModes)
    {
        this.canModes = canModes;
    }

    void RemoveFlag(Mode mode)
    {
        // モードがあるなら
        if(canModes.Contains(mode)) canModes.Remove(mode);// 移行可能なモードから削除
    }

    bool GetCanFlag(Mode targetMode)
    {
        if (canModes.Count == 0) return false;// 移行可能モードのリストがなければできないと返す

        foreach (Mode mode in canModes)
        {
            if (mode == targetMode) return true;// 移行可能モードのリストにあればできると返す
        }
        return false;
    }

    public void OnShoot()
    {
        switch(foodMode)
        {
            // 通常弾
            case Mode.Normal:
                break;
            // 巨大弾
            case Mode.Big:
                SetMode(Mode.Normal);// 通常弾に戻す
                RemoveFlag(Mode.Big);// 巨大化可能状態を解除
                break;
            // 爆発弾
            case Mode.Bomb:
                SetMode(Mode.Normal);// 通常弾に戻す
                RemoveFlag(Mode.Bomb);// 爆弾化可能状態を解除
                break;
            default:
                return;
        }

        // 残弾数を減らす
        if(foodMode != Mode.None)
        {
            team.SubtractBullet();
        }
    }

    // デバッグ用項目
    public void OnInfinityBig()
    {
        SetModeFlag(Mode.Big);// 無限巨大化
    }
    public void OnInfinityBomb()
    {
        SetModeFlag(Mode.Bomb);// 無限爆弾化
    }
}
