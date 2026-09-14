using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    private int[,] levelMap =
    {
        {1,2,2,2,2,2,2,2,2,2,2,2,2,7},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,4},
        {2,6,4,0,0,4,5,4,0,0,0,4,5,4},
        {2,5,3,4,4,3,5,3,4,4,4,3,5,3},
        {2,5,5,5,5,5,5,5,5,5,5,5,5,5},
        {2,5,3,4,4,3,5,3,3,5,3,4,4,4},
        {2,5,3,4,4,3,5,4,4,5,3,4,4,3},
        {2,5,5,5,5,5,5,4,4,5,5,5,5,4},
        {1,2,2,2,2,1,5,4,3,4,4,3,0,4},
        {0,0,0,0,0,2,5,4,3,4,4,3,0,3},
        {0,0,0,0,0,2,5,4,4,0,0,0,0,0},
        {0,0,0,0,0,2,5,4,4,0,3,4,4,8},
        {2,2,2,2,2,1,5,3,3,0,4,0,0,0},
        {0,0,0,0,0,0,5,0,0,0,4,0,0,0},
    };

    [Header("Tile Sprites")]
    [SerializeField] private Sprite wallEmpty;          // 0
    [SerializeField] private Sprite wallOutsideCorner;  // 1
    [SerializeField] private Sprite wallOutside;        // 2
    [SerializeField] private Sprite wallInsideCorner;   // 3
    [SerializeField] private Sprite wallInside;         // 4
    [SerializeField] private Sprite junctionWall;       // 7
    [SerializeField] private Sprite wallExit;           // 8


    [Header("Pellet Prefabs / Sprites")]
    
    [SerializeField] private GameObject normalPelletPrefab; // 5
    [SerializeField] private GameObject powerPelletPrefab;  // 6

    private int[,] fullMap;
    private int fullRows;
    private int fullCols;

    void Start()
    {
        // 1. 씬에 이미 존재하는 Level 01 삭제
        GameObject[] existingLevels = GameObject.FindGameObjectsWithTag("Background");
        foreach (GameObject level in existingLevels)
        {
            Destroy(level);
        }

        // 2. 4분면을 합쳐서 29 x 28 전체 맵 데이터 구성
        BuildFullMap();

        // 3. 전체 맵 인스턴스화 및 회전 적용
        GenerateLevel();

        // 4. 카메라 뷰포트 조절
        AdjustCamera();
    }

    private void BuildFullMap()
    {
        // 원본 사분면의 크기를 동적으로 측정
        int qRows = levelMap.GetLength(0); // 15
        int qCols = levelMap.GetLength(1); // 14

        // 세로 반전 시 마지막 행을 1번만 포함하므로: (qRows * 2) - 1
        fullRows = (qRows * 2) - 1;
        // 가로 반전은 양쪽 모두 대칭이므로: qCols * 2
        fullCols = qCols * 2;

        fullMap = new int[fullRows, fullCols];

        // 1. 좌상단 (원본)
        for (int r = 0; r < qRows; r++)
        {
            for (int c = 0; c < qCols; c++)
            {
                fullMap[r, c] = levelMap[r, c];
            }
        }

        // 2. 우상단 (좌우 반전)
        for (int r = 0; r < qRows; r++)
        {
            for (int c = 0; c < qCols; c++)
            {
                fullMap[r, fullCols - 1 - c] = levelMap[r, c];
            }
        }

        // 3. 좌하단 (상하 반전, 마지막 원본 행 제외: qRows - 1 까지만)
        for (int r = 0; r < qRows - 1; r++)
        {
            for (int c = 0; c < qCols; c++)
            {
                fullMap[fullRows - 1 - r, c] = levelMap[r, c];
            }
        }

        // 4. 우하단 (상하 & 좌우 반전, 마지막 원본 행 제외)
        for (int r = 0; r < qRows - 1; r++)
        {
            for (int c = 0; c < qCols; c++)
            {
                fullMap[fullRows - 1 - r, fullCols - 1 - c] = levelMap[r, c];
            }
        }
    }

    private void GenerateLevel()
    {
        Transform mapParent = new GameObject("ProceduralLevel").transform;


        for (int r = -100; r < 100; r++)
        {
            for (int c = -100; c < 100; c++)
            {
                 // 중심이 (0, 0)이 되도록 좌표 오프셋 계산
                float posX = c - (fullCols - 1) / 2.0f;
                float posY = (fullRows - 1) / 2.0f - r;
                Vector3 pos = new Vector3(posX, posY, 0f);

                // 1. 모든 셀에 기본 배경(Empty) 생성
                CreateBackgroundTile(r, c, pos, mapParent);
            }
        }


        for (int r = 0; r < fullRows; r++)
        {
            for (int c = 0; c < fullCols; c++)
            {
                // 중심이 (0, 0)이 되도록 좌표 오프셋 계산
                float posX = c - (fullCols - 1) / 2.0f;
                float posY = (fullRows - 1) / 2.0f - r;
                Vector3 pos = new Vector3(posX, posY, 0f);



                // 2. 타일 ID에 맞는 요소(벽, 펠릿 등) 위에 생성
                int tileId = fullMap[r, c];
                if (tileId != 0) // 0(Empty)은 배경만 깔면 되므로 생략
                {
                    CreateTile(tileId, r, c, pos, mapParent);
                }
            }
        }
    }

    // 기본 바닥(배경) 타일 생성
    private void CreateBackgroundTile(int r, int c, Vector3 pos, Transform parent)
    {
        if (wallEmpty == null) return;

        GameObject bgObj = new GameObject($"BG_{r}_{c}");
        bgObj.transform.position = pos;
        bgObj.transform.rotation = Quaternion.identity;
        bgObj.transform.SetParent(parent);

        SpriteRenderer sr = bgObj.AddComponent<SpriteRenderer>();
        sr.sprite = wallEmpty;

        sr.color = new Color(1f, 1f, 1f, 90f / 255f);
        
        sr.sortingOrder = -2; // 바닥 레이어
    }

    // 오른쪽 사분면(열 인덱스가 절반 이상인 경우)이면 좌우 반전
    private bool ShouldFlipJunctionX(int c)
    {
        int halfCol = fullCols / 2;
        return c >= halfCol;
    }

    private void CreateTile(int tileId, int r, int c, Vector3 pos, Transform parent)
    {
        Sprite spriteToUse = null;
        float rotationZ = 0f;

        switch (tileId)
        {
            case 1: // Outside Corner
                spriteToUse = wallOutsideCorner;
                rotationZ = GetCornerRotation(r, c);
                break;

            case 2: // Outside Wall (기본 세로)
                spriteToUse = wallOutside;
                rotationZ = GetOutsideWallRotation(r, c);
                break;

            case 3: // Inside Corner
                spriteToUse = wallInsideCorner;
                rotationZ = GetCornerRotation(r, c);
                break;

            case 4: // Inside Wall (기본 가로)
                spriteToUse = wallInside;
                rotationZ = GetInsideWallRotation(r, c);
                break;

            case 5: // Normal Pellet
                if (normalPelletPrefab != null)
                {
                    GameObject pellet = Instantiate(normalPelletPrefab, pos, Quaternion.identity, parent);
                    SetSortingOrder(pellet, 1);
                }
                break;

            case 6: // Power Pellet
                if (powerPelletPrefab != null)
                {
                    GameObject powerPellet = Instantiate(powerPelletPrefab, pos, Quaternion.identity, parent);
                    SetSortingOrder(powerPellet, -1);
                }
                break;

            case 7: // Junction Wall
                spriteToUse = junctionWall;
                rotationZ = GetJunctionRotation(r, c);
                bool flipX = ShouldFlipJunctionX(c);
                break;

            case 8: // Ghost Exit Wall
                spriteToUse = wallExit;
                rotationZ = 0f;
                break;
            
        }

        // 벽 타일 스프라이트 렌더러 생성
        if (spriteToUse != null)
        {
            GameObject tileObj = new GameObject($"Tile_{r}_{c}");
            tileObj.transform.position = pos;
            tileObj.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
            tileObj.transform.SetParent(parent);

            SpriteRenderer sr = tileObj.AddComponent<SpriteRenderer>();
            sr.sprite = spriteToUse;
            sr.sortingOrder = -1; // 배경보다 위에 표시

            if (tileId == 7)
            {
                bool isRight = (c >= fullCols / 2);
                bool isBottom = (r >= fullRows / 2);

                // 상단(Top)에서는 오른쪽만 반전
                // 하단(Bottom)에서는 180도 회전이 걸려 있으므로 왼쪽만 반전
                sr.flipX = isRight ^ isBottom; // XOR 연산: 둘 중 하나만 참일 때 true
                sr.flipY = false;
            }
        }
    }

    private void SetSortingOrder(GameObject target, int order)
    {
        SpriteRenderer sr = target.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = order;
        }
    }

    // 1 & 2번: Inside Wall (기본: 가로 = 0도)
    private float GetInsideWallRotation(int r, int c)
    {
        // 상하 연속 길이 카운트
        int verticalSpan = 1;
        int curR = r - 1;
        while (curR >= 0 && IsInsideElement(curR, c)) { verticalSpan++; curR--; }
        curR = r + 1;
        while (curR < fullRows && IsInsideElement(curR, c)) { verticalSpan++; curR++; }

        // 좌우 연속 길이 카운트
        int horizontalSpan = 1;
        int curC = c - 1;
        while (curC >= 0 && IsInsideElement(r, curC)) { horizontalSpan++; curC--; }
        curC = c + 1;
        while (curC < fullCols && IsInsideElement(r, curC)) { horizontalSpan++; curC++; }

        // 1. 상하 길이가 좌우 길이보다 길면 확실한 세로 벽
        if (verticalSpan > horizontalSpan)
        {
            return 90f;
        }

        // 2. 좌우 길이가 상하 길이보다 길면 확실한 가로 벽
        if (horizontalSpan > verticalSpan)
        {
            return 0f;
        }

        // 3. 길이가 같을 때 (코너 인접 검사)
        // 바로 위나 아래에 코너(3)가 닿아있으면 세로 연결선
        if (GetTileId(r - 1, c) == 3 || GetTileId(r + 1, c) == 3)
        {
            return 90f;
        }

        // 기본값: 가로 (0도)
        return 0f;
    }

    // 3번: Outside Wall (기본: 세로 = 0도 유지)
    private float GetOutsideWallRotation(int r, int c)
    {
        bool left = IsWall(r, c - 1);
        bool right = IsWall(r, c + 1);

        // 좌우로 이어져 있으면 가로로 눕힘
        if (left || right)
        {
            return 90f;
        }
        return 0f;
    }

    // 3번: Junction Wall (기본: [0, 13] 좌상단 베이스 = 0도)
    // [0, 13]은 위쪽만 벽이 없고 좌/우/아래가 연결된 'ㅜ' 모양
    private float GetJunctionRotation(int r, int c)
    {
        bool up = IsWall(r - 1, c);
        bool down = IsWall(r + 1, c);
        bool left = IsWall(r, c - 1);
        bool right = IsWall(r, c + 1);

        // 위가 뚫려있음 (좌상단 베이스 [0, 13] 형태) -> 0도
        if (!up) return 0f;

        // 오른쪽이 뚫려있음 -> 반시계 90도
        if (!right) return 90f;

        // 아래가 뚫려있음 -> 180도
        if (!down) return 180f;

        // 왼쪽이 뚫려있음 -> 반시계 270도 (시계 90도)
        if (!left) return 270f;

        return 0f;
    }

    private float GetCornerRotation(int r, int c)
    {
        bool up = IsWall(r - 1, c);
        bool down = IsWall(r + 1, c);
        bool left = IsWall(r, c - 1);
        bool right = IsWall(r, c + 1);

        // 1. 사방이 모두 벽인 특수 케이스 (T자 교차부 내부 모서리 등)
        if (up && down && left && right)
        {
            // 대각선 타일 중 통로/빈공간(Wall이 아닌 것)이 있는 방향을 모서리로 채택
            bool diagUpRight = IsWall(r - 1, c + 1);
            bool diagDownRight = IsWall(r + 1, c + 1);
            bool diagDownLeft = IsWall(r + 1, c - 1);
            bool diagUpLeft = IsWall(r - 1, c - 1);

            if (!diagDownRight) return 0f;    // 우하단이 파여있음 -> 0도
            if (!diagDownLeft) return 270f;   // 좌하단이 파여있음 -> 270도
            if (!diagUpLeft) return 180f;     // 좌상단이 파여있음 -> 180도
            if (!diagUpRight) return 90f;     // 우상단이 파여있음 -> 90도

            // 대각선도 벽이라면 인접한 4번 벽의 흐름 파악
            // [10, 8]처럼 위쪽과 오른쪽으로 선이 빠져나가는 구조인 경우
            if (GetTileId(r - 1, c) == 3 && GetTileId(r, c + 1) == 4) return 90f;
            if (GetTileId(r + 1, c) == 3 && GetTileId(r, c + 1) == 4) return 0f;

            return 0f;
        }

        // 2. 일반적인 외곽/단순 코너 (2방향만 벽인 경우)
        if (down && right && !up && !left) return 0f;    // 아래 + 오른쪽
        if (down && left && !up && !right) return 270f;  // 아래 + 왼쪽
        if (up && left && !down && !right) return 180f;  // 위 + 왼쪽
        if (up && right && !down && !left) return 90f;   // 위 + 오른쪽

        // 3. 3방향이 벽인 T자형 인접 코너 판정
        // 위가 막히고 아래/오른쪽으로 빠져야 할 때
        if (down && right && !left) return 0f;
        if (down && left && !right) return 270f;
        if (up && left && !right) return 180f;
        if (up && right && !left) return 90f;

        return 0f;
    }

    // 헬퍼 함수들
    private bool IsInsideElement(int r, int c)
    {
        int id = GetTileId(r, c);
        return id == 3 || id == 4;
    }

    private int GetTileId(int r, int c)
    {
        if (r < 0 || r >= fullRows || c < 0 || c >= fullCols) return -1;
        return fullMap[r, c];
    }

    private bool IsWall(int r, int c)
    {
        if (r < 0 || r >= fullRows || c < 0 || c >= fullCols) return false;
        int val = fullMap[r, c];
        // 1, 2, 3, 4, 7, 8번을 벽/연결부로 취급
        return (val == 1 || val == 2 || val == 3 || val == 4 || val == 7 || val == 8);
    }

    private void AdjustCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        cam.transform.position = new Vector3(0f, 0f, -10f);

        // 맵 높이: 29, 맵 너비: 28
        // 여백(Padding)을 1.5 유닛 정도 줌
        float verticalHalfSize = (fullRows / 2f) + 1.5f;
        float horizontalHalfSize = ((fullCols / 2f) + 1.5f) / cam.aspect;

        cam.orthographicSize = Mathf.Max(verticalHalfSize, horizontalHalfSize);
    }
}