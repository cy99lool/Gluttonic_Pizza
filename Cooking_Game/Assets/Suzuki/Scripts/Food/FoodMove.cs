using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodMove : MonoBehaviour
{
    const float BreakThreshold = 0.01f;
    const float OverlapSphereRadius = 0f;// Rayの始点が当たり判定の内部にあるかを調べるためのものなので、点にしている

    [SerializeField] Rigidbody myRb;
    public Rigidbody Rigidbody => myRb;

    [SerializeField] Collider myCollider;
    public Collider Collider => myCollider;

    [Header("モデルのアニメーター"), SerializeField] Animator animator;

    [Header("--- 移動関係の設定 ---")]
    [Header("重力"), SerializeField] float gravity = 9.8f;
    [Header("一秒あたりの減速率"), SerializeField] float brakeRate = 1.8f;
    [Header("地面についている判定の距離"), SerializeField] float onGroundDistance = 0.15f;
    [Header("消えるまでの時間"), SerializeField] float eraseLimit = 5f;
    [Header("ブレーキをかけるレイヤー"), SerializeField] LayerMask brakeMask;
    [Header("落下しないレイヤー"), SerializeField] LayerMask groundMask;
    [Header("ぶつかるレイヤー"), SerializeField] LayerMask hitMask;
    [Header("ぶつかったときの反射率（%）"), Range(0f, 100f), SerializeField] float myReflectRate = 90f;
    public float ReflectRate => myReflectRate;

    [Header("--- 爆弾関係の設定 ---")]
    [Header("爆弾になるまでに必要なつながる数"), SerializeField] int bombNum = 3;
    [Header("起爆時間"), SerializeField] float explodeTimer = 5f;
    [Header("爆発時に生成するプレハブ"), SerializeField] GameObject explodePrefab;

    [Header("--- エフェクトの設定 ---")]
    [Header("爆発カウントエフェクト"), SerializeField] GameObject bombCountDownEffect;

    [Header("--- その他設定 ---")]
    // ステータスとして他のクラスにまとめるかも（ポイントの倍率等を設定する場合もあるかも）
    [Header("チーム"), SerializeField] TeamColor team;
    public TeamColor Team => team;

    [Header("入手されるときのポイント"), SerializeField] int point = 10;
    public int ScorePoint => point;

    StageManager stageManager;
    float eraseTimer = GameConstants.FirstTimerValue;

    float BrakePower => 1f - brakeRate * Time.deltaTime;

    bool isGround = false;
    protected bool IsGround => isGround;

    bool isFalling = false;
    bool unEatable = false;// 捕食を行ったかどうか

    List<FoodMove> mergedFoods = new List<FoodMove>();
    public List<FoodMove> MergedFoods => mergedFoods;

    FoodMove? parent = null;
    FoodMove root;
    public FoodMove Root
    {
        get
        {
            if (root == null) root = parent == null ? this : parent.Root;// 初回取得だけ再帰

            return root;
        }
    }

    public TeamColor GetMostColor()
    {
        // 何もつながってなければ自分の色を返す
        if (Root.mergedFoods.Count == GameConstants.Zero) return team;

        // 色の探索（最大2色なので、そこまで数えられるようにしている）
        TeamColor otherColor = Root.team;
        (int targetCount, int otherCount) = CountColorRecursively(Root.team, ref otherColor);

        // 根の色の数 > 他の色の数
        if (targetCount > otherCount) return team;

        // 他の色の数 <= 根の色の数
        else return otherColor;
    }

    // 再帰的に色の数を探索し、合計の数を返す
    (int, int) CountColorRecursively(TeamColor targetColor, ref TeamColor otherColor)
    {
        int targetCount = (this.team == targetColor) ? 1 : 0;
        int otherCount = (this.team == otherColor) ? 1 : 0;

        // 異なる色を初めて見つけた場合記録
        if (this.team != targetColor && otherColor != targetColor) otherColor = this.team;

        // 再帰的に探索
        foreach(FoodMove child in this.mergedFoods)
        {
            (int childTargetCount, int childOtherCount) = child.CountColorRecursively(targetColor, ref otherColor);
            targetCount += childTargetCount;
            otherCount += childOtherCount;
        }

        return (targetCount, otherCount);
    }

    public void SetFoodParent(FoodMove parent)
    {
        if (this.parent == parent) return;

        this.parent = parent;
    }

    // Start is called before the first frame update
    protected void Start()
    {
        stageManager = FindAnyObjectByType<StageManager>();

        if (animator != null) animator.SetBool("Ready", true);// 最初は発射時のアニメーション

        isGround = false;
    }

    protected void FixedUpdate()
    {
        FallUpdate();       // 落下の更新処理
        AnimatorUpdate();   // アニメーションの更新処理
        BombUpdate();       // 爆弾の更新処理
    }

    float bombTimer = GameConstants.FirstTimerValue;

    void BombUpdate()
    {
        // 爆弾フラグが有効化の間だけカウントダウンや起爆処理を行う
        if (!bomb)
        {
            // タイマーのリセット
            if (bombTimer != GameConstants.FirstTimerValue) bombTimer = GameConstants.FirstTimerValue;

            // 起爆カウントエフェクトの非表示化
            if (bombCountEffectObject != null && bombCountEffectObject.activeSelf) bombCountEffectObject.SetActive(false);

            return;
        }

        // タイマーを更新して、起爆時間になったら起爆
        bombTimer += Time.deltaTime;

        if (bombTimer >= explodeTimer) Explode();
    }

    /// <summary>
    /// 起爆時の処理
    /// </summary>
    void Explode()
    {

        Debug.Log("[BOMB]");

        // Rigidbodyの無効化
        Rigidbody.isKinematic = false;

        // 起爆時のプレハブ生成
        if (explodePrefab != null) Instantiate(explodePrefab, transform.position, Quaternion.identity);

        // 起爆カウントエフェクトの非表示化
        if (bombCountEffectObject != null && bombCountEffectObject.activeSelf) bombCountEffectObject.SetActive(false);

        // 再帰的に起爆
        foreach (FoodMove child in this.mergedFoods)
        {
            child.Explode();
        }

        // オブジェクト破壊
        Destroy(gameObject);
    }

    /// <summary>
    /// アニメーターのフラグを個別に設定
    /// </summary>
    /// <param name="name"></param>
    /// <param name="flag"></param>
    public void SetAnimatorBool(string name, bool flag)
    {
        if (animator != null) animator.SetBool(name, flag);
    }

    void AnimatorUpdate()
    {
        // nullチェック
        if (animator == null) return;

        // 発射後ピザに着地したときは通常モードへ移行
        if (animator.GetBool("Ready") && isGround) animator.SetBool("Ready", false);

        // 爆弾アニメーション
        //animator.SetBool("Bomb", bomb);
    }

    void FallUpdate()
    {
        Ray groundRay = new Ray(transform.position + Vector3.up * transform.lossyScale.y * GameConstants.HalfMultiplyer, Vector3.down);

        // ========================================================================================================
        // 接地点の取得
        Collider groundHit = null;

        // 自身に重なっている床を調べる
        Collider[] overlapColliders = Physics.OverlapSphere(groundRay.origin, OverlapSphereRadius, groundMask);

        // 床が重なっていた場合
        if (overlapColliders.Length > 0)
        {
            for (int i = 0; i < overlapColliders.Length; i++)
            {
                // 減速させるレイヤー（例：ピザのレイヤー）があった場合、そちらを優先して適用する
                if (CompareLayer(brakeMask, overlapColliders[i].gameObject.layer))
                {
                    groundHit = overlapColliders[i];
                    break;
                }

                // 減速させるレイヤーがなかった場合、落下を停止するだけのレイヤーのものを適用する
                groundHit = overlapColliders[i];
            }
        }

        // 床が重なっていなかった場合
        else
        {
            RaycastHit[] hits = Physics.SphereCastAll(groundRay, transform.lossyScale.x * GameConstants.HalfMultiplyer, onGroundDistance + (transform.lossyScale.y * GameConstants.HalfMultiplyer), groundMask);

            if (hits.Length > 0)
            {
                for (int i = 0; i < hits.Length; i++)
                {
                    // 減速させるレイヤー（例：ピザのレイヤー）があった場合、そちらを優先して適用する
                    if (CompareLayer(brakeMask, hits[i].collider.gameObject.layer))
                    {
                        groundHit = hits[i].collider;
                        break;
                    }
                    // 減速させるレイヤーがなかった場合、落下を停止するだけのレイヤーのものを適用する
                    groundHit = hits[i].collider;
                }
            }
        }


        //if (Physics.Raycast(groundRay, out RaycastHit groundHit, onGroundDistance + (transform.lossyScale.y * GameConstants.HalfMultiplyer), groundMask))// 地面についているとき
        // 地面についているとき    
        if (groundHit != null)
        {
            if (isFalling)
            {
                // rootのみに設定
                if (Root == this)
                {
                    // 着地点の設定
                    Vector3 hitPos = groundHit.ClosestPoint(transform.position);
                    hitPos.y += transform.localScale.y * GameConstants.HalfMultiplyer;// 貫通対策

                    // 着地点に位置を設定
                    transform.position = hitPos;
                }

                StopFalling();

                EraseCheck();// 一定時間以上存在できない床の上にいた場合は消す
            }
        }

        //if (Physics.Raycast(groundRay, onGroundDistance + (transform.lossyScale.y * GameConstants.HalfMultiplyer), brakeMask))// ブレーキをかけるレイヤーのとき
        //if (Physics.SphereCast(groundRay, transform.lossyScale.x * GameConstants.HalfMultiplyer, out groundHit, onGroundDistance + (transform.lossyScale.y * GameConstants.HalfMultiplyer), brakeMask))// ブレーキをかけるレイヤーのとき

        // ブレーキをかける床の上のとき
        if (groundHit != null && CompareLayer(brakeMask, groundHit.gameObject.layer))
        {
            // 結合されていなければ、ピザの子にする
            if (transform.parent == null || (parent == null && transform.parent != groundHit.transform))
            {
                transform.parent = groundHit.transform;
            }

            Brake();// ブレーキ
            if (!isGround) isGround = true;// 接地開始


            if (eraseTimer > GameConstants.FirstTimerValue) eraseTimer = GameConstants.FirstTimerValue;// 消えないようにする
        }

        // 浮いているとき
        if (groundHit == null)
        {
            // 親がピザなら親子づけを外す
            if (transform.parent != null && CompareLayer(brakeMask, transform.parent.gameObject.layer))
            {
                transform.parent = null;
            }

            Fall();
            if (isGround) isGround = false;// 接地中断

            EraseCheck();// 一定時間以上浮いていたら消す
        }
    }

    void EraseCheck()
    {
        eraseTimer += Time.deltaTime;
        if (eraseTimer >= eraseLimit) Destroy(gameObject);
    }

    public virtual void AddForce(Vector3 direction, float power)
    {
        myRb.velocity += direction * power;
        Debug.Log($"direction:{direction}, power:{power}, velocity:{myRb.velocity}");
    }
    public virtual void AddTorque(Vector3 axis, float power)
    {
        myRb.AddTorque(axis * power, ForceMode.Impulse);
    }

    public void SetVelocity(Vector3 velocity)
    {
        myRb.velocity = velocity;
    }

    void Fall()
    {
        if (myRb.velocity.y == -gravity) return;
        Vector3 velocity = myRb.velocity;
        velocity.y = velocity.y > 0f ? velocity.y - gravity : -gravity;
        myRb.velocity = velocity;

        if (!isFalling) isFalling = true;
    }

    /// <summary>
    /// 落下を停止
    /// </summary>
    void StopFalling()
    {
        Vector3 velocity = myRb.velocity;
        velocity.y = 0f;
        myRb.velocity = velocity;

        if (isFalling) isFalling = false;
    }

    const float UnEatableThreshold = 0.01f;
    /// <summary>
    /// 時間による減速を行う
    /// </summary>
    void Brake()
    {
        Vector3 velocity = myRb.velocity;
        Vector3 angulerVelocity = myRb.angularVelocity;

        velocity.x *= BrakePower;
        velocity.z *= BrakePower;
        angulerVelocity *= BrakePower * BrakePower;

        // 速度が一定以下になったら停止
        if (velocity.x * velocity.x <= BreakThreshold && velocity.z * velocity.z <= BreakThreshold) velocity = Vector3.zero;

        // ある程度減速していた場合食べる機能を無効化
        if (velocity.x * velocity.x <= UnEatableThreshold && velocity.z * velocity.z <= UnEatableThreshold) unEatable = true;

        if (angulerVelocity.y * angulerVelocity.y <= BreakThreshold) angulerVelocity = Vector3.zero;

        myRb.velocity = velocity;


    }

    public void OnMerge(FoodMove target)
    {
        if (target == null) return;

        // 何もくっついていなければ探索を行わずにくっつける
        if (mergedFoods.Count == GameConstants.Zero)
        {
            Merge(ref mergedFoods, target);
            return;
        }

        // 全く同じ食べ物がくっついていないか確かめる
        foreach (FoodMove food in mergedFoods)
        {
            if (food.Root == target.Root) return;
        }

        // くっつける
        Merge(ref mergedFoods, target);
    }

    //  爆発カウントダウンの有効化フラグ
    bool bomb = false;
    GameObject bombCountEffectObject = null;

    void Merge(ref List<FoodMove> mergedFoods, FoodMove target)
    {
        if (target == null || this == null) return;
        if (target == this) return;
        if (ContainsRecursive(Root, target)) return;

        // targetのつながっている全メンバーを取得
        List<FoodMove> targetGroup = new List<FoodMove>();
        CollectAllConnected(target.Root, targetGroup);

        // つながるときの位置調整
        Vector3 offset = myCollider.ClosestPoint(target.transform.position) - transform.position;
        foreach (FoodMove member in targetGroup)
        {
            member.transform.position += offset;
            member.Rigidbody.isKinematic = true;// rootのrigidbodyの影響を受けてもらうため
        }

        // Rootの傾き調整
        Vector3 rootEulerAngles = Root.transform.eulerAngles;
        rootEulerAngles.x = 0f;
        rootEulerAngles.z = 0f;
        Root.transform.eulerAngles = rootEulerAngles;

        // グループ全員を新しいRoot配下に変更
        foreach (FoodMove member in targetGroup)
        {
            // トランスフォームの親設定
            member.transform.SetParent(transform);
            member.SetFoodParent(this);

            // めり込みの対策
            Vector3 localPosition = member.transform.localPosition;
            localPosition.y = 0f;
            member.transform.localPosition = localPosition;

            // 傾きの対策
            Vector3 eulerAngles = member.transform.eulerAngles;
            eulerAngles.x = 0f;
            eulerAngles.z = 0f;
            member.transform.eulerAngles = eulerAngles;

            // Rootのキャッシュ更新
            member.UpdateRootRecursive(Root);

            // リストに追加
            if (!mergedFoods.Contains(member)) mergedFoods.Add(member);
        }

        // つながってきた食べ物の勢いを取得
        Vector3 velocity = target.Rigidbody.velocity;

        const float MaxReflectRate = 1f;
        // rootにトルクや速度をかける（結合した勢いで回転するイメージ）（勢いを親にある程度の割合で渡す予定、つながってる合計の個数で勢いを割るかも）
        Root.AddTorque(Vector3.up, velocity.magnitude * (MaxReflectRate / Root.GetConnectedCount()));
        Root.AddForce(velocity.normalized, velocity.magnitude * (MaxReflectRate / Root.GetConnectedCount()));

        // 子になるオブジェクトの勢いを消す
        target.myRb.velocity = Vector3.zero;

        // ある程度つながったら爆弾化
        if (GetConnectedCount() >= bombNum)
        {
            // 爆弾フラグを有効化
            Root.bomb = true;

            // エフェクトを生成
            if (bombCountDownEffect != null && bombCountEffectObject == null) bombCountEffectObject = Instantiate(bombCountDownEffect);

            // エフェクトのオブジェクトが存在しているかnullチェック
            if (bombCountEffectObject == null) return;
                
            // エフェクトの位置を調整
            bombCountEffectObject.transform.position = transform.position;

            // エフェクトを有効化
            if(!bombCountEffectObject.activeSelf) bombCountEffectObject.SetActive(true);
        }
    }

    bool ContainsRecursive(FoodMove root, FoodMove target)
    {
        // 自分自身なら真
        if (root == target) return true;

        // 探索済みノードを記録するHashSet
        HashSet<FoodMove> visited = new HashSet<FoodMove>();
        Stack<FoodMove> stack = new Stack<FoodMove>();

        stack.Push(root);
        // 再帰的に子を探索していき、そこに対象が含まれていたなら真
        //foreach(FoodMove child in root.mergedFoods)
        //{
        //    if(ContainsRecursive(child, target)) return true;
        //}
        while (stack.Count > 0)
        {
            FoodMove current = stack.Pop();
            if (current == target) return true;

            // 訪問済みのノードはスキップ
            if (visited.Contains(current)) continue;

            // 訪問済みのノードに追加
            visited.Add(current);

            // 子ノードをスタックに追加
            foreach (FoodMove child in current.mergedFoods)
            {
                stack.Push(child);
            }
        }

        // 見つからなければ偽
        return false;
    }

    /// <summary>
    /// Rootからすべてのつながっている食べ物を取得
    /// </summary>
    void CollectAllConnected(FoodMove root, List<FoodMove> result)
    {
        // nullやすでに追加済みなら追加しない
        if (root == null || result.Contains(root)) return;

        result.Add(root);
        // つながっている子全てで再帰的に探索
        foreach (FoodMove child in root.mergedFoods)
        {
            CollectAllConnected(child, result);
        }
    }

    const float EatenFactor = 1.5f;
    public void OnEat(FoodMove target)
    {
        if (target == null) return;

        // 自身や他のmergedFoodsリストから削除
        if (mergedFoods.Contains(target)) mergedFoods.Remove(target);

        // 結合の解除
        target.UnMerge();

        // 消滅処理（エフェクトもいれるならここに）
        Destroy(target.gameObject);

        // 捕食を行った後処理
        transform.localScale *= EatenFactor;
        //animator.SetBool()
    }

    bool unmerging = false;
    protected void UnMerge()
    {
        // 結合がない、もしくは結合解除中はなにもしない
        if (parent == null && mergedFoods.Count == GameConstants.Zero) return;
        if (unmerging) return;

        // 結合解除中
        unmerging = true;

        // mergetFoodリストのコピーを作成する
        List<FoodMove> childrenToUnmerge = new List<FoodMove>(mergedFoods);
        mergedFoods.Clear();

        // 親子付けの引き継ぎ
        //// 親がある場合
        //if (parent != null)
        //{
        //    foreach (FoodMove child in childrenToUnmerge)
        //    {
        //        if (child == null) continue;

        //        // 親子付けの解除
        //        child.transform.SetParent(parent.transform);
        //        child.SetFoodParent(parent);
        //        child.UpdateRootRecursive(parent.root);
        //    }

        //    //if (mergedFoods.Count > 0)
        //    //{
        //    //    for (int i = mergedFoods.Count - 1; i >= 0; i--)
        //    //    {
        //    //        if (mergedFoods[i] != null)
        //    //        {
        //    //            // 親子付けの解除
        //    //            mergedFoods[i].transform.SetParent(parent.transform);
        //    //            mergedFoods[i].SetFoodParent(parent);
        //    //            mergedFoods[i].UpdateRootRecursive(parent.root);
        //    //        }
        //    //    }
        //    //}
        //}
        //// 親がない場合
        //else
        //{
        if (mergedFoods.Count > 0)
        {
            foreach (FoodMove child in mergedFoods)
            {
                if (child == null || child.Equals(null)) continue;

                // 親子付けの解除
                child.transform.SetParent(null);
                child.SetFoodParent(null);
                child.UpdateRootRecursive(null);

                // 再度自身のrigidbodyで動かせるように
                if (child.Rigidbody != null) child.Rigidbody.isKinematic = false;
            }
        }
        //}

        // 爆弾の解除判定
        if (GetConnectedCount() < bombNum) Root.bomb = false;

        unmerging = false;
    }

    void UpdateRootRecursive(FoodMove newRoot)
    {
        if (root == newRoot) return;

        root = newRoot == null ? this : newRoot;

        // つながっている食べ物にも根を反映
        foreach (FoodMove child in mergedFoods)
        {
            if (child != null && child != this) child.UpdateRootRecursive(root);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (CompareLayer(hitMask, other.gameObject.layer))
        {
            // 衝突時の処理（エフェクトの再生等、マネージャーに衝突を知らせるだけにする予定（お互いで衝突処理が呼び出されて異常な速度でふっとばし合うため））
            if (other.gameObject.TryGetComponent<FoodMove>(out FoodMove opponentFood))// 相手が食べ物なら
            {
                // 同じ根をもつ結合関係にある食べ物同士は反応させない
                if (opponentFood.Root != this.Root)
                {
                    // stageManagerが設定されていなければreturn
                    if (stageManager == null) return;

                    // 衝突時の相性を取得
                    InteractionType type = FoodInteractionRules.GetInteractionType(team, opponentFood.team);

                    switch (type)
                    {
                        case InteractionType.Merge:
                            // 爆弾化していない状態のときのみ結合
                            if(!Root.bomb && !opponentFood.Root.bomb) stageManager.AddMergeEventList(this, opponentFood);
                            break;

                        case InteractionType.Eat:
                            // 結合済みの食材や、すでに一度捕食を行った食材は捕食機能を持たない
                            if (Root == this && mergedFoods.Count == GameConstants.Zero && !unEatable)
                            {
                                stageManager.AddEatEventList(this, opponentFood);
                                unEatable = true;
                            }
                            else if (Root == this) stageManager.AddReflectList(this, opponentFood);
                            break;

                        case InteractionType.None:
                            {
                                // 捕食する側の食材が吹き飛ばないように
                                if (FoodInteractionRules.GetInteractionType(opponentFood.team, team) != InteractionType.Eat)
                                    stageManager.AddReflectList(this, opponentFood);
                                break;
                            }

                        default:
                            break;
                    }
                }

                //Reflect(myRb, oppoentRb);
                //stageManager.AddReflectList(this, opponentFood);
            }
        }
    }

    const int DefaultCount = 1;
    /// <summary>
    /// 自身につながっている食材の総数を求める
    /// </summary>
    /// <param name="visited">探索した食材の記録</param>
    /// <returns>つながっている食材の総数</returns>
    public int GetConnectedCount(HashSet<FoodMove> visited = null)
    {
        // 最初のみ初期化
        if (visited == null) visited = new HashSet<FoodMove>();

        // 無限再帰を防止
        if (visited.Contains(this)) return 0;

        // 探索した対象に自身を記録
        visited.Add(this);

        // カウントを初期化（自身をあらかじめ数える）
        int count = DefaultCount;

        // 子を再帰的に探索
        foreach (FoodMove child in mergedFoods)
        {
            if (child != null) count += child.GetConnectedCount(visited);
        }

        // 親方向も確認
        if (parent != null) parent.GetConnectedCount(visited);

        return count;
    }

    /// <summary>
    /// レイヤーマスクにレイヤーが含まれているかどうか確認する
    /// </summary>
    bool CompareLayer(LayerMask layerMask, int layer)
    {
        return ((1 << layer) & layerMask) != 0;
    }

    protected void SetReflectRate(float rate)
    {
        myReflectRate = rate;
    }


}

[System.Serializable]
public enum TeamColor
{
    Red = 0,
    Blue,
    Green,
    Yellow,
    AllSize
}

public enum InteractionType
{
    None = 0,
    Merge = 1,
    Eat = 2
}

/// <summary>
/// 食べ物同士の接触時の相性
/// </summary>
public static class FoodInteractionRules
{
    // ========================================================
    //
    // ★捕食の場合、{(捕食,被捕食),InteractionType.Eat}の順番
    //
    // ========================================================
    private static readonly Dictionary<(TeamColor, TeamColor), InteractionType> rules = new Dictionary<(TeamColor, TeamColor), InteractionType>
    {
        // ==== Red ====
        {(TeamColor.Red, TeamColor.Green), InteractionType.Eat },
        {(TeamColor.Red, TeamColor.Blue), InteractionType.Eat },
        //{(TeamColor.Red, TeamColor.Yellow), InteractionType.Merge },

        // ==== Blue ====
        //{(TeamColor.Blue, TeamColor.Green), InteractionType.Merge },
        {(TeamColor.Blue, TeamColor.Red), InteractionType.Eat},
        {(TeamColor.Blue, TeamColor.Yellow), InteractionType.Eat},

        // ==== Green ====
        //{(TeamColor.Green, TeamColor.Yellow), InteractionType.Eat },
        {(TeamColor.Green, TeamColor.Blue), InteractionType.Merge },

        // ==== Yellow ====
        {(TeamColor.Yellow, TeamColor.Red), InteractionType.Merge },
        //{(TeamColor.Yellow, TeamColor.Blue), InteractionType.Eat },
    };

    public static InteractionType GetInteractionType(this TeamColor self, TeamColor other)
    {
        // ルールに当てはまるものは対応したタイプを返す
        if (rules.TryGetValue((self, other), out InteractionType result)) return result;

        // 当てはまらなければ何もしない
        return InteractionType.None;
    }
}

