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

    [Header("モデルのアニメーター"), SerializeField] protected Animator animator;

    [Header("--- 移動関係の設定 ---")]
    [Header("重力"), SerializeField] float gravity = 9.8f;
    [Header("角速度の最大値"), SerializeField] float maxAngularVelocity = 7f;
    [Header("一秒あたりの速度の減速率"), SerializeField] float brakeRate = 1.8f;
    [Header("一秒あたりの回転の減速率"), SerializeField] float rotateBrakeRate = 20f;
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

    [Header("牙のアニメーター"), SerializeField] Animator fangAnimator;
    [Header("牙の縮むアニメーション時間"), SerializeField] float fangDissaperAnimationTime = 0.5f;
    //[Header("〃の縮む速度(1で縮まない)"), Range(1f, 10f), SerializeField] float fangShrinkSpeed = 2f;
    [Header("捕食後のサイズ倍率"), SerializeField] float eatenFactor = 1.1f;

    StageManager stageManager;
    public StageManager StageManager => stageManager;

    float eraseTimer = GameConstants.FirstTimerValue;

    float BrakePower => (GameConstants.MaxPercentage - brakeRate) / GameConstants.MaxPercentage;
    float RotateBreakPower => (GameConstants.MaxPercentage - rotateBrakeRate) / GameConstants.MaxPercentage;

    bool isGround = false;
    protected bool IsGround => isGround;

    bool isFalling = false;
    bool eatMode = false;// 捕食モードかどうか
    public bool EatMode => eatMode; 

    bool fangDisappered = false;

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
        foreach (FoodMove child in this.mergedFoods)
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

    /// <summary>
    /// 捕食モードを有効化する
    /// </summary>
    public void EnableEatMode()
    {
        eatMode = true;
        fangDisappered = false;

        // 牙を表示
        if (fangAnimator != null) fangAnimator.gameObject.SetActive(true);
    }

    public void DisableEatMode()
    {
        eatMode = false;

        // 牙を非表示
        if (fangAnimator != null) fangAnimator.gameObject.SetActive(false);
    }

    protected void FixedUpdate()
    {
        FallUpdate();       // 落下の更新処理
        RotateLimitter(maxAngularVelocity);// 回転の制御
        AnimatorUpdate();   // アニメーションの更新処理
        BombUpdate();       // 爆弾の更新処理

        // 捕食能力がなくなった食べ物は牙を非表示に
        if (!fangDisappered)
        {
            if (!eatMode && fangAnimator != null && fangAnimator.gameObject.activeSelf)
            {
                StartCoroutine(FangDisapper(fangDissaperAnimationTime));
                fangDisappered = true;
            }
        }
    }

    IEnumerator FangDisapper(float shrinkTime)
    {
        float shrinkRate = GameConstants.One / shrinkTime;
        float timer = GameConstants.FirstTimerValue;

        Vector3 localScale = fangAnimator.gameObject.transform.localScale;

        while (timer < shrinkTime)
        {
            // 大きさを縮小
            if (localScale.x > GameConstants.Zero) localScale.x -= shrinkRate * Time.deltaTime;
            if (localScale.y > GameConstants.Zero) localScale.y -= shrinkRate * Time.deltaTime;
            if (localScale.z > GameConstants.Zero) localScale.z -= shrinkRate * Time.deltaTime;

            // 適用
            fangAnimator.gameObject.transform.localScale = localScale;

            // タイマー加算
            timer += Time.deltaTime;

            yield return null;
        }

        // 非表示に変更
        fangAnimator.gameObject.SetActive(false);
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
        // Rigidbodyがnullのとき（すでに削除されていたら）return
        if (Rigidbody == null) return;
        Debug.Log("[BOMB]");

        // Rigidbodyの無効化
        Rigidbody.isKinematic = false;

        // 起爆時のプレハブ生成
        if (this == Root && explodePrefab != null) Instantiate(explodePrefab, transform.position, Quaternion.identity, null);

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
    /// 角度と回転の速度を制御
    /// </summary>
    /// <param name="maxAngularVelocity">角速度の最高速制限</param>
    void RotateLimitter(float maxAngularVelocity)
    {
        // y軸以外の回転を0にする（床に平行にするため）
        Vector3 localEulerAngles = transform.localEulerAngles;
        localEulerAngles.x = GameConstants.Zero;
        localEulerAngles.z = GameConstants.Zero;

        transform.localEulerAngles = localEulerAngles;// 適用

        // 角速度を制限する
        myRb.maxAngularVelocity = maxAngularVelocity;
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

        // 牙のアニメーション
        if (fangAnimator == null || !fangAnimator.isActiveAndEnabled) return;

        if (!fangAnimator.GetBool("Shoot") && animator.GetBool("Ready")) fangAnimator.SetBool("Shoot", true);
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
                    hitPos.y += transform.localScale.y * GameConstants.HalfMultiplyer + 1f;// 貫通対策

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

        // y方向の勢いをなくす
        velocity.y = GameConstants.Zero;

        Vector3 angulerVelocity = myRb.angularVelocity;

        // ブレーキ
        velocity.x *= BrakePower;
        velocity.z *= BrakePower;
        angulerVelocity *= RotateBreakPower;

        // 速度が一定以下になったら停止
        if (velocity.x * velocity.x <= BreakThreshold && velocity.z * velocity.z <= BreakThreshold) velocity = Vector3.zero;

        // ある程度減速していた場合食べる機能を無効化
        if (velocity.x * velocity.x <= UnEatableThreshold && velocity.z * velocity.z <= UnEatableThreshold) eatMode = false;

        if (angulerVelocity.y * angulerVelocity.y <= BreakThreshold) angulerVelocity = Vector3.zero;

        // 速度の適用
        myRb.velocity = velocity;
        myRb.angularVelocity = angulerVelocity;

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
            if(food.Root == target.Root) Debug.LogWarning($"[Merge Failed] {food.name}と{target.name}を結合しようとしましたが、すでに同じRootです！");// デバッグ文 後で消す
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

        //// つながるときの位置調整
        //Vector3 offset = myCollider.ClosestPoint(target.transform.position) - transform.position;
        //foreach (FoodMove member in targetGroup)
        //{
        //    member.transform.position += offset;
        //    //member.Rigidbody.isKinematic = true;// rootのrigidbodyの影響を受けてもらうため
        //}

        // Rootの傾き調整
        Vector3 rootEulerAngles = Root.transform.eulerAngles;
        rootEulerAngles.x = 0f;
        rootEulerAngles.z = 0f;
        Root.transform.eulerAngles = rootEulerAngles;

        // グループ全員を新しいRoot配下に変更
        foreach (FoodMove member in targetGroup)
        {
            // トランスフォームの親設定
            //member.transform.SetParent(transform);
            member.SetFoodParent(this);

            // FixedJointを追加して接続
            FixedJoint joint = member.gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = this.Rigidbody;

            // めり込みの対策
            //Vector3 localPosition = member.transform.localPosition;
            //localPosition.y = 0f;
            //member.transform.localPosition = localPosition;

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
        const float TorqueFactor = 0.1f;
        // rootにトルクや速度をかける（結合した勢いで回転するイメージ）（勢いを親にある程度の割合で渡す予定、つながってる合計の個数で勢いを割るかも）
        Root.AddTorque(Vector3.up, velocity.normalized.magnitude * TorqueFactor * (MaxReflectRate / Root.GetConnectedCount()));
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
            if (!bombCountEffectObject.activeSelf) bombCountEffectObject.SetActive(true);
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

    public void OnEat(FoodMove target, Vector3 velocity)
    {
        if (target == null) return;

        // 自身や他のmergedFoodsリストから削除
        if (mergedFoods.Contains(target)) mergedFoods.Remove(target);

        // 結合の解除
        target.UnMerge();

        // 消滅処理（エフェクトもいれるならここに）
        Destroy(target.gameObject);

        // 捕食を行った後処理
        // 大きくする
        transform.localScale *= eatenFactor;

        // 速度を戻す（コライダーでぶつかって勢いがなくなったことの対策）
        myRb.velocity = velocity;

        // 捕食モードの再有効化
        if (!eatMode) EnableEatMode();

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
                //child.transform.SetParent(null);
                child.SetFoodParent(null);
                child.UpdateRootRecursive(null);

                // Fixed Jointの削除
                child.RemoveFixedJoint(transform.gameObject);

                // 再度自身のrigidbodyで動かせるように
                //if (child.Rigidbody != null) child.Rigidbody.isKinematic = false;
            }
        }
        //}

        // 爆弾の解除判定
        if (GetConnectedCount() < bombNum) Root.bomb = false;

        unmerging = false;
    }

    void RemoveFixedJoint(GameObject target)
    {
        // 自身のFixedJointを全て取得
        FixedJoint[] joints = GetComponents<FixedJoint>();

        for (int i = joints.Length - 1; i >= 0; i--)
        {
            // 目標とつながっているFixedJointを削除
            if (joints[i] != null && joints[i].connectedBody.gameObject == target) Destroy(joints[i]);
        }
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

    // めり込み防止のため、コライダーで衝突は検知することにした
    void OnCollisionEnter(Collision collision)
    {
        if (CompareLayer(hitMask, collision.gameObject.layer))
        {
            // 衝突時の処理（エフェクトの再生等、マネージャーに衝突を知らせるだけにする予定（お互いで衝突処理が呼び出されて異常な速度でふっとばし合うため））
            if (collision.gameObject.TryGetComponent<FoodMove>(out FoodMove opponentFood))// 相手が食べ物なら
            {
                // 同じ根をもつ結合関係にある食べ物同士は反応させない
                if (opponentFood.Root == this.Root) return;
                // stageManagerが設定されていなければreturn
                if (stageManager == null) return;

                // 衝突時の相性を取得
                InteractionType type = FoodInteractionRules.GetInteractionType(team, opponentFood.team);

                switch (type)
                {
                    case InteractionType.Merge:
                        // 爆弾化していない状態のときのみ結合
                        if (!Root.bomb && !opponentFood.Root.bomb) stageManager.AddMergeEventList(this, opponentFood);
                        break;

                    case InteractionType.Eat:
                        // 結合済みの食材や、すでに一度捕食を行った食材は捕食機能を持たない
                        if (Root == this && mergedFoods.Count == GameConstants.Zero && eatMode)
                        {
                            stageManager.AddEatEventList(this, opponentFood);
                            //unEatable = true;
                        }
                        else if (Root == this) stageManager.AddReflectList(this, opponentFood);
                        break;

                    case InteractionType.None:
                        {
                            // 捕食する側の食材が吹き飛ばないように
                            if (FoodInteractionRules.GetInteractionType(opponentFood.team, team, opponentFood.eatMode) != InteractionType.Eat)
                                stageManager.AddReflectList(this, opponentFood);
                            break;
                        }

                    default:
                        break;
                }
            }
        }
    }

    // 削除予定、トリガーを使用していたときのもの
    //void OnTriggerEnter(Collider other)
    //{
    //    if (CompareLayer(hitMask, other.gameObject.layer))
    //    {
    //        // 衝突時の処理（エフェクトの再生等、マネージャーに衝突を知らせるだけにする予定（お互いで衝突処理が呼び出されて異常な速度でふっとばし合うため））
    //        if (other.gameObject.TryGetComponent<FoodMove>(out FoodMove opponentFood))// 相手が食べ物なら
    //        {
    //            // 同じ根をもつ結合関係にある食べ物同士は反応させない
    //            if (opponentFood.Root == this.Root) return;
    //            // stageManagerが設定されていなければreturn
    //            if (stageManager == null) return;

    //            // 衝突時の相性を取得
    //            InteractionType type = FoodInteractionRules.GetInteractionType(team, opponentFood.team, eatMode);

    //            switch (type)
    //            {
    //                case InteractionType.Merge:
    //                    // 爆弾化していない状態のときのみ結合
    //                    if (!Root.bomb && !opponentFood.Root.bomb) stageManager.AddMergeEventList(this, opponentFood);
    //                    break;

    //                case InteractionType.Eat:
    //                    // 結合済みの食材や、すでに一度捕食を行った食材は捕食機能を持たない
    //                    if (Root == this && mergedFoods.Count == GameConstants.Zero && eatMode)
    //                    {
    //                        stageManager.AddEatEventList(this, opponentFood);
    //                        eatMode = false;
    //                    }
    //                    else if (Root == this) stageManager.AddReflectList(this, opponentFood);
    //                    break;

    //                case InteractionType.None:
    //                    {
    //                        // 捕食する側の食材が吹き飛ばないように
    //                        if (FoodInteractionRules.GetInteractionType(opponentFood.team, team, opponentFood.eatMode) != InteractionType.Eat)
    //                            stageManager.AddReflectList(this, opponentFood);
    //                        break;
    //                    }

    //                default:
    //                    break;
    //            }

    //            //Reflect(myRb, oppoentRb);
    //            //stageManager.AddReflectList(this, opponentFood);
    //        }
    //    }
    //}

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
    /// <summary>
    /// 捕食の関係
    /// </summary>
    static readonly Dictionary<(TeamColor, TeamColor), InteractionType> eatRule = new Dictionary<(TeamColor, TeamColor), InteractionType>
    {
        // ==== Red ====
        //{(TeamColor.Red, TeamColor.Red), InteractionType.Eat },
        {(TeamColor.Red, TeamColor.Green), InteractionType.Eat },
        {(TeamColor.Red, TeamColor.Blue), InteractionType.Eat },
        //{(TeamColor.Red, TeamColor.Yellow), InteractionType.Merge },

        // ==== Blue ====
        //{(TeamColor.Blue, TeamColor.Green), InteractionType.Merge },
        {(TeamColor.Blue, TeamColor.Red), InteractionType.Eat},
        {(TeamColor.Blue, TeamColor.Yellow), InteractionType.Eat},

        // ==== Green ====
        {(TeamColor.Green, TeamColor.Red), InteractionType.Eat },
        {(TeamColor.Green, TeamColor.Yellow), InteractionType.Eat },
        //{(TeamColor.Green, TeamColor.Blue), InteractionType.Merge },

        // ==== Yellow ====
        //{(TeamColor.Yellow, TeamColor.Red), InteractionType.Merge },
        {(TeamColor.Yellow, TeamColor.Blue), InteractionType.Eat },
        {(TeamColor.Yellow, TeamColor.Green), InteractionType.Eat}
    };

    /// <summary>
    /// くっつきの関係
    /// </summary>
    static readonly Dictionary<(TeamColor, TeamColor), InteractionType> mergeRule = new Dictionary<(TeamColor, TeamColor), InteractionType>
    {
        // ==== Red ====
        //{(TeamColor.Red, TeamColor.Green), InteractionType.Eat },
        //{(TeamColor.Red, TeamColor.Blue), InteractionType.Eat },
        //{(TeamColor.Red, TeamColor.Red), InteractionType.Merge },
        {(TeamColor.Red, TeamColor.Yellow), InteractionType.Merge },

        // ==== Blue ====
        {(TeamColor.Blue, TeamColor.Green), InteractionType.Merge },
        //{(TeamColor.Blue, TeamColor.Red), InteractionType.Eat},
        //{(TeamColor.Blue, TeamColor.Yellow), InteractionType.Eat},

        // ==== Green ====
        //{(TeamColor.Green, TeamColor.Red), InteractionType.Eat },
        //{(TeamColor.Green, TeamColor.Yellow), InteractionType.Eat },
        {(TeamColor.Green, TeamColor.Blue), InteractionType.Merge },

        // ==== Yellow ====
        {(TeamColor.Yellow, TeamColor.Red), InteractionType.Merge },
        //{(TeamColor.Yellow, TeamColor.Blue), InteractionType.Eat },
        //{(TeamColor.Yellow, TeamColor.Green), InteractionType.Eat}
    };

    /// <summary>
    /// ぶつかったときの反応
    /// </summary>
    /// <param name="self">自身</param>
    /// <param name="other">相手</param>
    /// <returns></returns>
    public static InteractionType GetInteractionType(this TeamColor self, TeamColor other)
    {
        // ルールに当てはまるものは対応したタイプを返す
        if (mergeRule.TryGetValue((self, other), out InteractionType result)) return result;

        // 当てはまらなければ何もしない
        return InteractionType.None;
    }

    /// <summary>
    /// ぶつかったときの反応（捕食モードかどうかで分ける場合）
    /// </summary>
    /// <param name="self">自身</param>
    /// <param name="other">相手</param>
    /// <param name="eatMode">捕食モードかどうか</param>
    /// <returns></returns>
    public static InteractionType GetInteractionType(this TeamColor self, TeamColor other, bool eatMode)
    {
        // 捕食モードのとき
        if (eatMode)
        {
            // ルールに当てはまるものは対応したタイプを返す
            if (eatRule.TryGetValue((self, other), out InteractionType result)) return result;
        }
        else
        {
            // ルールに当てはまるものは対応したタイプを返す
            if (mergeRule.TryGetValue((self, other), out InteractionType result)) return result;
        }

        // 当てはまらなければ何もしない
        return InteractionType.None;
    }
}

