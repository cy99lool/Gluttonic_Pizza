using UnityEngine;

public class BowControler : MonoBehaviour
{
    public Transform leftPoint;      // 弦の左端
    public Transform rightPoint;     // 弦の右端
    public Transform shootPoint;     // 矢の初期位置
    public GameObject arrowPrefab;
    [SerializeField] Transform pivot;
    public LineRenderer stringRenderer;
    public float maxPullDistance = 2f;
    public float maxForce = 30f;

    private GameObject currentArrow;
    private GameObject arrowEffect;
    private bool isAiming = false;
    private Vector3 startMousePos;
    Vector3 startStringPos;


    void Start()
    {
        startStringPos = shootPoint.position;
        ResetStringRendererPos();// 起動時に弦が張られるように
    }

    void Update()
    {

        // エイム開始
        if (Input.GetMouseButtonDown(0))
        {
            //startMousePos = GetMouseWorldPosition();
            //currentArrow = Instantiate(arrowPrefab, shootPoint.position, shootPoint.rotation);
            //currentArrow.GetComponent<Rigidbody>().isKinematic = true;
            //isAiming = true;

            //arrowEffect = currentArrow.transform.Find("ArrowEffect")?.gameObject;
            //if (arrowEffect != null) arrowEffect.SetActive(true); // 表示開始

        }

        // エイム中
        if (Input.GetMouseButton(0) && isAiming)
        {
            //Vector3 currentMousePos = GetMouseWorldPosition();
            //float pullDistance = Mathf.Clamp(Vector3.Distance(startMousePos, currentMousePos), 0, maxPullDistance);
            //Vector3 pullDirection = (currentMousePos - startMousePos).normalized;
            //Vector3 arrowPos = shootPoint.position + pullDirection * pullDistance;

            //// ▼ X軸の制限を追加（必要に応じて値を調整）
            //float minX = shootPoint.position.x - 2.5f;
            //float maxX = shootPoint.position.x + 2.5f;
            //arrowPos.x = Mathf.Clamp(arrowPos.x, minX, maxX);

            //// ▼ Y軸は固定（2D風やZ方向の暴れ防止）
            //arrowPos.y = shootPoint.position.y;

            //// Z軸の移動
            //float minZ = shootPoint.position.z - 5f;
            //float maxZ = shootPoint.position.z + 0f;
            //arrowPos.z = Mathf.Clamp(arrowPos.z, minZ, maxZ);

            //// 矢を動かす
            //currentArrow.transform.position = arrowPos;

            //// 弦の中央点を矢の位置に
            //UpdateStringRenderer(arrowPos);
        }
        
        // エイム終わり
        if (Input.GetMouseButtonUp(0) && isAiming)
        {
            //Vector3 currentMousePos = GetMouseWorldPosition();
            //Vector3 direction = (startMousePos - currentMousePos).normalized;
            //float pullDistance = Mathf.Clamp(Vector3.Distance(startMousePos, currentMousePos), 0, maxPullDistance);
            //float force = (pullDistance / maxPullDistance) * maxForce;

            //Rigidbody rb = currentArrow.GetComponent<Rigidbody>();
            //rb.isKinematic = false;
            //rb.AddForce(direction * force, ForceMode.Impulse);


            //Vector3 rawDirection = (startMousePos - currentMousePos).normalized;

            //Vector3 fixedDirection = Vector3.up;
            //rb.isKinematic = false;
            //rb.AddForce(direction * force, ForceMode.Impulse);



            //isAiming = false;
            //UpdateStringRenderer(shootPoint.position); // 弦を戻す

            //if (arrowEffect != null) arrowEffect.SetActive(false);


        }

        // 通常時は初期状態に
        if (!isAiming && stringRenderer != null)
        {
            //UpdateStringRenderer(shootPoint.position);
        }
    }

    /// <summary>
    /// エイム開始（ドラッグの最初）
    /// </summary>
    public void StartAim()
    {
        startMousePos = shootPoint.position;// 最初の位置を記録している

        // 矢の生成
        currentArrow = Instantiate(arrowPrefab, shootPoint.position, Quaternion.identity);
        currentArrow.GetComponent<Rigidbody>().isKinematic = true;

        // 矢の向きを設定
        SetArrowDirection();

        arrowEffect = currentArrow.transform.Find("ArrowEffect")?.gameObject;
        if (arrowEffect != null) arrowEffect.SetActive(true); // 表示開始
    }
    /// <summary>
    /// エイム中の弦と矢を動かす
    /// </summary>
    public void Aim(Vector3 mousePos)
    {
        //Vector3 currentMousePos = mousePos;
        // どれくらい離れているかや方向の計算
        float pullDistance = Mathf.Clamp(Vector3.Distance(startMousePos, mousePos), 0, maxPullDistance);
        Vector3 pullDirection = (mousePos - startMousePos).normalized;
        Vector3 arrowPos = shootPoint.position + pullDirection * pullDistance;

        //// ▼ X軸の制限を追加（必要に応じて値を調整）
        //float rangeX = 4f;
        //float minX = shootPoint.localPosition.x - rangeX;
        //float maxX = shootPoint.localPosition.x + rangeX;
        //arrowPos.x = Mathf.Clamp(arrowPos.x, minX, maxX);

        //// ▼ Y軸は固定（2D風やZ方向の暴れ防止）
        //arrowPos.y = shootPoint.localPosition.y;

        //// Z軸の移動
        //float minZ = shootPoint.localPosition.z - 5f;
        //float maxZ = shootPoint.localPosition.z + 0f;
        //arrowPos.z = Mathf.Clamp(arrowPos.z, minZ, maxZ);

        // 矢を動かす
        currentArrow.transform.position = arrowPos;

        // 矢の向きを設定
        SetArrowDirection();

        // 弦の中央点を矢の位置に
        UpdateStringRenderer(arrowPos);
    }

    /// <summary>
    /// エイム終了
    /// </summary>
    public void EndAim(Vector3 mousePos)
    {
        ////方向と発射速度の計算(消すかも？)
        //Vector3 direction = (startMousePos - mousePos).normalized;
        //float pullDistance = Mathf.Clamp(Vector3.Distance(startMousePos, mousePos), 0, maxPullDistance);
        //float force = (pullDistance / maxPullDistance) * maxForce;

        //// 弾（現在エフェクト再生オブジェクト）の発射
        //Rigidbody rb = currentArrow.GetComponent<Rigidbody>();
        //rb.isKinematic = false;
        //rb.AddForce(direction * force, ForceMode.Impulse);


        //Vector3 rawDirection = (startMousePos - mousePos).normalized;

        //Vector3 fixedDirection = Vector3.up;
        //rb.isKinematic = false;
        //rb.AddForce(direction * force, ForceMode.Impulse);

        // 矢の消去
        GameObject.Destroy(currentArrow);

        isAiming = false;
        ResetStringRendererPos(); // 弦を戻す

        if (arrowEffect != null) arrowEffect.SetActive(false);
    }

    /// <summary>
    /// エイムしていないときの弦の位置に戻す
    /// </summary>
    void ResetStringRendererPos()
    {
        UpdateStringRenderer(startStringPos);
    }

    /// <summary>
    /// 矢の向いている方向を設定する
    /// </summary>
    void SetArrowDirection()
    {
        currentArrow.transform.LookAt(pivot);// 飛ばす先を向かせる
        //// 反対を向かせる（このままでは顔が上下逆に向いているように見える）
        //Vector3 eulerAngles = currentArrow.transform.eulerAngles;
        //eulerAngles.y += 180f;
        //currentArrow.transform.eulerAngles = eulerAngles;// 角度の適用
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = 10f; // カメラからの距離に応じて調整
        return Camera.main.ScreenToWorldPoint(mouseScreenPos);

    }

    /// <summary>
    /// 弦の描画位置を更新する
    /// </summary>
    /// <param name="centerPoint">弦を引っ張る地点（掴んでいる地点）</param>
    void UpdateStringRenderer(Vector3 centerPoint)
    {
        if (stringRenderer == null) return;
        stringRenderer.positionCount = 3;
        stringRenderer.SetPosition(0, leftPoint.position);
        stringRenderer.SetPosition(1, centerPoint);
        stringRenderer.SetPosition(2, rightPoint.position);
    }



}