using UnityEngine;
using UnityEngine.InputSystem;

public class PacStudentController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f; // 초당 약 5타일 이동
    public float tileSize = 1f;  // 타일 1칸의 월드 크기
    public Vector2 gridOrigin = Vector2.zero; // levelMap[0, 0]의 월드 좌표

    [Header("Components")]
    public Animator animator;
    public ParticleSystem dustParticles;
    public AudioSource moveAudioSource;
    public AudioClip moveEmptyClip;   // 빈 칸 이동 시 오디오
    public AudioClip movePelletClip;  // 펠릿을 먹을 때 오디오

    // 그리드 좌표 및 입력 버퍼
    private Vector2Int currentGridPos;
    private KeyCode lastInput = KeyCode.None;
    private KeyCode currentInput = KeyCode.None;

    // 선형 보간(Lerp) 상태 변수
    private bool isLerping = false;
    private Vector3 startWorldPos;
    private Vector3 targetWorldPos;
    private float lerpProgress = 0f;
    private float lerpDuration = 0f;

    // 맵 데이터 참조 (LevelGenerator의 levelMap 배열 구조에 맞게 연결)
    private int[,] levelMap;

    private void Start()
    {
        // 시작 시 "Right" 애니메이션 상태로 설정
        if (animator != null)
        {
            animator.Play("Right");
        }

        // 시작 월드 위치를 기준으로 초기 그리드 좌표 계산
        currentGridPos = WorldToGrid(transform.position);
        transform.position = GridToWorld(currentGridPos);

        StopMovementEffects();
    }

    // LevelGenerator에서 전체 맵 배열을 전달받을 때 호출
    public void InitializeMap(int[,] map, Vector2 origin, float size)
    {
        levelMap = map;
        gridOrigin = origin;
        tileSize = size;
        currentGridPos = WorldToGrid(transform.position);
        transform.position = GridToWorld(currentGridPos);
    }

    private void Update()
    {
        GatherInput();

        if (!isLerping)
        {
            TryStartNextMove();
        }
        else
        {
            PerformLerp();
        }
    }

    private void GatherInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.wKey.wasPressedThisFrame) lastInput = KeyCode.W;
        else if (Keyboard.current.aKey.wasPressedThisFrame) lastInput = KeyCode.A;
        else if (Keyboard.current.sKey.wasPressedThisFrame) lastInput = KeyCode.S;
        else if (Keyboard.current.dKey.wasPressedThisFrame) lastInput = KeyCode.D;
    }

    private void TryStartNextMove()
    {
        // 1순위: lastInput 방향 확인
        if (lastInput != KeyCode.None)
        {
            Vector2Int nextPosFromLast = currentGridPos + KeyToDirection(lastInput);
            if (IsWalkable(nextPosFromLast))
            {
                currentInput = lastInput;
                BeginLerp(nextPosFromLast, currentInput);
                return;
            }
        }

        // 2순위: lastInput이 막혔을 경우 기존 currentInput 방향 유지 확인
        if (currentInput != KeyCode.None)
        {
            Vector2Int nextPosFromCurrent = currentGridPos + KeyToDirection(currentInput);
            if (IsWalkable(nextPosFromCurrent))
            {
                BeginLerp(nextPosFromCurrent, currentInput);
                return;
            }
        }

        // 두 방향 모두 이동 불가 시 정지
        StopMovementEffects();
    }

    private void BeginLerp(Vector2Int targetGrid, KeyCode dirKey)
    {
        isLerping = true;
        startWorldPos = transform.position;
        targetWorldPos = GridToWorld(targetGrid);
        currentGridPos = targetGrid;

        float distance = Vector3.Distance(startWorldPos, targetWorldPos);
        lerpDuration = distance / moveSpeed;
        lerpProgress = 0f;

        UpdateFacingDirection(dirKey);
        PlayMovementEffects(HasPelletAt(targetGrid));
    }

    private void PerformLerp()
    {
        lerpProgress += Time.deltaTime / lerpDuration;
        transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, lerpProgress);

        if (lerpProgress >= 1f)
        {
            transform.position = targetWorldPos;
            isLerping = false;
        }
    }

    private Vector2Int KeyToDirection(KeyCode key)
    {
        switch (key)
        {
            case KeyCode.W: return new Vector2Int(0, -1); // 배열 행(row)이 위에서 아래로 증가하는 경우 -1
            case KeyCode.S: return new Vector2Int(0, 1);
            case KeyCode.A: return new Vector2Int(-1, 0);
            case KeyCode.D: return new Vector2Int(1, 0);
            default: return Vector2Int.zero;
        }
    }

    private bool IsWalkable(Vector2Int gridPos)
    {
        if (levelMap == null) return true; // 테스트용 안전장치

        int rows = levelMap.GetLength(0);
        int cols = levelMap.GetLength(1);

        if (gridPos.y < 0 || gridPos.y >= rows || gridPos.x < 0 || gridPos.x >= cols)
            return false;

        int tileType = levelMap[gridPos.y, gridPos.x];
        // Assessment 3 기준: 0(빈 공간), 5(일반 펠릿), 6(파워 펠릿)만 이동 가능
        // 벽(1,2,3,4) 및 Ghost Wall(7)은 이동 불가
        return tileType == 0 || tileType == 5 || tileType == 6;
    }

    private bool HasPelletAt(Vector2Int gridPos)
    {
        if (levelMap == null) return false;
        int tileType = levelMap[gridPos.y, gridPos.x];
        return tileType == 5 || tileType == 6;
    }

    private void UpdateFacingDirection(KeyCode dirKey)
    {
        if (animator == null) return;

        if (dirKey == KeyCode.W) animator.Play("Up");
        else if (dirKey == KeyCode.S) animator.Play("Down");
        else if (dirKey == KeyCode.A) animator.Play("Left");
        else if (dirKey == KeyCode.D) animator.Play("Right");
    }

    private void PlayMovementEffects(bool aboutToEatPellet)
    {
        if (animator != null) animator.speed = 1f;

        if (dustParticles != null && !dustParticles.isPlaying)
            dustParticles.Play();

        if (moveAudioSource != null)
        {
            AudioClip targetClip = aboutToEatPellet ? movePelletClip : moveEmptyClip;
            if (moveAudioSource.clip != targetClip || !moveAudioSource.isPlaying)
            {
                moveAudioSource.clip = targetClip;
                moveAudioSource.loop = true;
                moveAudioSource.Play();
            }
        }
    }

    private void StopMovementEffects()
    {
        if (animator != null) animator.speed = 0f;

        if (dustParticles != null && dustParticles.isPlaying)
            dustParticles.Stop();

        if (moveAudioSource != null && moveAudioSource.isPlaying)
            moveAudioSource.Stop();
    }

    private Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(
            gridOrigin.x + gridPos.x * tileSize,
            gridOrigin.y - gridPos.y * tileSize,
            0f
        );
    }

    private Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt((worldPos.x - gridOrigin.x) / tileSize);
        int y = Mathf.RoundToInt((gridOrigin.y - worldPos.y) / tileSize);
        return new Vector2Int(x, y);
    }
}