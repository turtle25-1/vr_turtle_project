using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;   // 캔버스에서 TextMeshPro 입력 필드를 쓸 경우

public class TurtleManager : MonoBehaviour
{
    /*────────────────────────────────────────────────────
     * 1. 인스펙터 바인딩용 필드
     *───────────────────────────────────────────────────*/
    [Header("Prefabs & UI")]
    [Tooltip("거북이 프리팹")]
    public GameObject turtlePrefab;                  // 거북이 프리팹
    [Tooltip("동일 씬 캔버스에 있는 TextMeshPro InputField")]
    public TMP_InputField commandInput;              // 사용자 명령어 입력창

    [Header("Spawn Settings")]
    [Tooltip("최대 스폰 가능한 거북이 수")]
    public int maxTurtles = 5;                       // 풀 사이즈
    public static Vector3 spawnPosition = Vector3.zero;
    public static readonly Quaternion spawnRotation = Quaternion.identity;

    /*────────────────────────────────────────────────────
     * 2. 내부 풀 · 전역 테이블
     *───────────────────────────────────────────────────*/
    private readonly List<GameObject>             turtlePool   = new List<GameObject>();
    private readonly Dictionary<string, Turtle3D> namedTurtles = new();      // “Space” → Turtle3D
    private readonly Dictionary<string, Vector3>  variables    = new();      // “a”     → (x,y,z)

    /*────────────────────────────────────────────────────
     * 3. 명령어 처리
     *───────────────────────────────────────────────────*/
    private readonly Queue<string> commandQueue = new();  // 대기 중인 명령
    private bool isProcessing = false;                    // 코루틴 동시 실행 방지

    /*────────────────────────────────────────────────────
     * 4. 싱글턴
     *───────────────────────────────────────────────────*/
    public static TurtleManager instance;

    /********************************************************
     *  Awake : 싱글턴 & 풀 생성 & UI 바인딩
     ********************************************************/
    void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) { Destroy(gameObject); return; }

        DontDestroyOnLoad(gameObject);
        CreateTurtlePool();

        if (commandInput != null)
            commandInput.onSubmit.AddListener(OnSubmitCommand);
        else
            Debug.LogWarning("[TurtleManager] commandInput 필드가 할당되지 않았습니다.");
    }

    /********************************************************
     *  1) 거북이 풀 선행 생성
     ********************************************************/
    void CreateTurtlePool()
    {
        for (int i = 0; i < maxTurtles; i++)
        {
            GameObject go = Instantiate(turtlePrefab);
            go.SetActive(false);
            turtlePool.Add(go);
        }
    }

    /********************************************************
     *  2) UI 입력 → 큐
     *     ▸ 모든 공백·탭·개행 제거하여 동일한 문장으로 통일
     ********************************************************/
    private void OnSubmitCommand(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return;

        string compact = Regex.Replace(raw, @"\s+", "");  // “ \t\n” 전부 제거
        EnqueueCommand(compact);

        commandInput.text = string.Empty;
        commandInput.ActivateInputField();                // 다음 입력 대기 상태
    }

    public void EnqueueCommand(string cmd)
    {
        if (string.IsNullOrEmpty(cmd)) return;
        commandQueue.Enqueue(cmd);
    }

    /********************************************************
     *  3) 매 프레임 : 처리 중 아니면 다음 명령 시작
     ********************************************************/
    void Update()
    {
        if (!isProcessing && commandQueue.Count > 0)
            StartCoroutine(ProcessCommand(commandQueue.Dequeue()));
    }

    /********************************************************
     *  4) 명령어 해석 & 실행
     *     ▸ 이미 공백이 모두 제거된 compact 문자열 전제
     ********************************************************/
    private IEnumerator ProcessCommand(string cmd)
    {
        isProcessing = true;

        /* ─────────── 4‑1. 스폰 :  Name=Turtle() ─────────── */
        if (cmd.EndsWith("Turtle()") && cmd.Contains('='))
        {
            string[] pair = cmd.Split('=');
            if (pair.Length == 2)
            {
                string name = pair[0];

                GameObject go = GetTurtleFromPool();
                if (go != null)
                {
                    go.SetActive(true);
                    Turtle3D turtle = go.GetComponent<Turtle3D>();
                    turtle.Initialize(name, spawnPosition, spawnRotation);
                    namedTurtles[name] = turtle;
                }
                else Debug.LogError("[TurtleManager] 풀에 남은 거북이가 없습니다.");
            }

            isProcessing = false;
            yield break;
        }

        /* ─────────── 4‑2. 위치 변수 할당 : a=Space.position() ─────────── */
        if (cmd.EndsWith(".position()") && cmd.Contains('='))
        {
            string[] pair = cmd.Split('=');
            if (pair.Length == 2)
            {
                string varName   = pair[0];                // “a”
                string rightExpr = pair[1];                // “Space.position()”

                string turtleKey = rightExpr[..rightExpr.IndexOf('.')];

                if (namedTurtles.TryGetValue(turtleKey, out Turtle3D turtle))
                {
                    Vector3 pos = turtle.Position;
                    variables[varName] = pos;
                    Debug.Log($"{varName}=({pos.x:F2},{pos.y:F2},{pos.z:F2})");
                }
                else Debug.LogError($"[TurtleManager] 존재하지 않는 거북이: {turtleKey}");
            }

            isProcessing = false;
            yield break;
        }

        /* ─────────── 4‑3. 이동 : Name.forward(x) / Name.fd(x) ─────────── */
        string[] verbs = { ".forward(", ".fd(" };
        foreach (string v in verbs)
        {
            if (cmd.Contains(v) && cmd.EndsWith(")"))
            {
                string name   = cmd[..cmd.IndexOf(v)];
                string numStr = cmd[(cmd.IndexOf(v) + v.Length)..^1];  // 마지막 ‘)’ 제외

                if (namedTurtles.TryGetValue(name, out Turtle3D turtle))
                {
                    if (float.TryParse(numStr, out float dist))
                        yield return turtle.TurtleForward(dist);
                    else
                        Debug.LogError("[TurtleManager] 거리 숫자 파싱 실패");
                }
                else Debug.LogError($"[TurtleManager] 존재하지 않는 거북이: {name}");

                isProcessing = false;
                yield break;
            }
        }

        /* ─────────── 4‑4. 알 수 없는 명령 ─────────── */
        Debug.LogError($"[TurtleManager] 명령 해석 실패: {cmd}");
        isProcessing = false;
    }

    /********************************************************
     *  5) 풀에서 비활성 개체 반환
     ********************************************************/
    private GameObject GetTurtleFromPool()
    {
        foreach (GameObject t in turtlePool)
            if (!t.activeSelf) return t;
        return null;
    }
}
