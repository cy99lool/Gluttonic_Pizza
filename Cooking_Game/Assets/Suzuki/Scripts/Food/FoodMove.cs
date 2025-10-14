using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodMove : MonoBehaviour
{
    const float BreakThreshold = 0.01f;
    const float OverlapSphereRadius = 0f;// Rayの始点が当たり判定の内部にあるかを調べるためのものなので、点にしている

    [SerializeField] Rigidbody myRb;
    public Rigidbody Rigidbody => myRb;
    [Header("モデルのアニメーター"), SerializeField] Animator animator;
    [Header("重力"), SerializeField] float gravity = 9.8f;
    [Header("一秒あたりの減速率"), SerializeField] float brakeRate = 1.8f;
    [Header("地面についている判定の距離"), SerializeField] float onGroundDistance = 0.15f;
    [Header("消えるまでの時間"), SerializeField] float eraseLimit = 5f;
    [Header("ブレーキをかけるレイヤー"), SerializeField] LayerMask brakeMask;
    [Header("落下しないレイヤー"), SerializeField] LayerMask groundMask;
    [Header("ぶつかるレイヤー"), SerializeField] LayerMask hitMask;
    [Header("ぶつかったときの反射率（%）"), Range(0f, 100f), SerializeField] float myReflectRate = 90f;
    public float ReflectRate => myReflectRate;

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
        FallUpdate();
        AnimatorUpdate();
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
        if (animator == null) return;// nullチェック

        if (animator.GetBool("Ready") && isGround) animator.SetBool("Ready", false);// 発射後ピザに着地したときは通常モードへ移行
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

        // 全く同じ食べ物がくっついていないか確かめる（これから木構造を探索する処理をいれる予定、現状は自身の持っているリスト内だけでの判定のため）
        foreach (FoodMove food in mergedFoods)
        {
            if (food.Root == target.Root) return;
        }

        // くっつける
        Merge(ref mergedFoods, target);
    }

    void Merge(ref List<FoodMove> mergedFoods, FoodMove target)
    {
        if (target == null || this == null) return;
        if (target == this) return;
        if (ContainsRecursive(Root, target)) return;

        // リストに追加
        if (!mergedFoods.Contains(target)) mergedFoods.Add(target);

        // つながってきた食べ物の勢いを取得
        Vector3 velocity = target.Rigidbody.velocity;

        // トランスフォームの親設定
        target.transform.SetParent(transform);
        target.SetFoodParent(this);
        target.Rigidbody.isKinematic = true;// rootのrigidbodyの影響を受けてもらうため

        // rootのキャッシュ更新
        FoodMove currentRoot = Root;
        target.UpdateRootRecursive(currentRoot);

        const float ForceAtten = 0.05f;
        // rootにトルクや速度をかける（結合した勢いで回転するイメージ）（勢いを親にある程度の割合で渡す予定、つながってる合計の個数で勢いを割るかも）
        Root.AddTorque(Vector3.up, velocity.magnitude * ForceAtten);
        Root.AddForce(velocity.normalized, velocity.magnitude * ForceAtten);

        // 子になるオブジェクトの勢いを消す
        target.myRb.velocity = Vector3.zero;
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

    public void OnEat(FoodMove target)
    {
        if (target == null) return;

        // 結合の解除
        target.UnMerge();

        // 消滅処理（エフェクトもいれるならここに）
        Destroy(target.gameObject);
    }

    public void UnMerge()
    {
        // 結合がなければなにもしない
        if (parent == null && mergedFoods.Count == GameConstants.Zero) return;

        // mergetFoodリストのコピーを作成する
        List<FoodMove> childrenToUnmerge = new List<FoodMove>(mergedFoods);

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
                    if (child == null) continue;

                    // 親子付けの解除
                    child.transform.SetParent(null);
                    child.SetFoodParent(null);
                    child.UpdateRootRecursive(null);

                    // 再度自身のrigidbodyで動かせるように
                    child.Rigidbody.isKinematic = false;
                }

                //for (int i = mergedFoods.Count - 1; i >= 0; i--)
                //{
                //    if (mergedFoods[i] != null)
                //    {
                //        // 親子付けの解除
                //        mergedFoods[i].transform.SetParent(null);
                //        mergedFoods[i].SetFoodParent(null);
                //        mergedFoods[i].UpdateRootRecursive(null);

                //        // 再度自身のrigidbodyで動かせるように
                //        mergedFoods[i].Rigidbody.isKinematic = false;
                //    }
                //}
            }
        //}

        mergedFoods.Clear();
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
                    // 衝突時の相性を取得
                    InteractionType type = FoodInteractionRules.GetInteractionType(team, opponentFood.team);

                    switch (type)
                    {
                        case InteractionType.Merge:
                            stageManager.AddMergeEventList(this, opponentFood);
                            break;

                        case InteractionType.Eat:
                            // 結合済みの食材は食べる機能を持たない
                            if (Root == this && mergedFoods.Count == GameConstants.Zero) stageManager.AddEatEventList(this, opponentFood);
                            break;

                        case InteractionType.None:
                            stageManager.AddReflectList(this, opponentFood);
                            break;

                        default:
                            break;
                    }
                }

                //Reflect(myRb, oppoentRb);
                //stageManager.AddReflectList(this, opponentFood);
            }
        }
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
        {(TeamColor.Red, TeamColor.Yellow), InteractionType.Merge },

        // ==== Blue ====
        {(TeamColor.Blue, TeamColor.Green), InteractionType.Merge },
        {(TeamColor.Blue, TeamColor.Red), InteractionType.Eat},

        // ==== Green ====
        {(TeamColor.Green, TeamColor.Yellow), InteractionType.Eat },
        {(TeamColor.Green, TeamColor.Blue), InteractionType.Merge },

        // ==== Yellow ====
        {(TeamColor.Yellow, TeamColor.Red), InteractionType.Merge },
        {(TeamColor.Yellow, TeamColor.Blue), InteractionType.Eat },
    };

    public static InteractionType GetInteractionType(this TeamColor self, TeamColor other)
    {
        // ルールに当てはまるものは対応したタイプを返す
        if (rules.TryGetValue((self, other), out InteractionType result)) return result;

        // 当てはまらなければ何もしない
        return InteractionType.None;
    }
}

