using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[System.Serializable]
public class UnitStatus
{
    public int moveForce;
    public int shockForceMin;
    public int shockForceMax;
    public int hp;
    public int attack;
    public int price;
    public int morale = 20;
    public bool orderUnit = false;
    [NonSerialized] public bool beStationed = false;
}

public class Unit : GameManagerFlowHandler
{
    public User ownerUser;
    public UnitStatus status;
    [NonSerialized] public SpriteRenderer sprRenderer;
    [NonSerialized] public Node moveNode;
    [NonSerialized] public Vector3Int curPos;
    [NonSerialized] public TileInst curTile;

    private bool cancelMove = false;
    private Coroutine moveCoroutine;
    private float moveDeltaTime = 0.0f;
    private float moveTime = 0.2f;
    private static Vector3[] dirPositions = { new Vector2(0.5f, 0.75f), new Vector2(1.0f, 0.0f), new Vector2(0.5f, -0.75f), new Vector2(-0.5f, -0.75f), new Vector2(-1.0f, 0.0f), new Vector2(-0.5f, 0.75f) };

    public class Node
    {
        public TileInst tile = null;
        public Vector3Int pos;
        public int moveCount = 0;
        public Node parent = null;
    }
    public Dictionary<Vector3Int, Node> moveNodes;

    /*
        이동 경로를 찾기 위한 재귀함수.
    */
    private void FindUnitMoveNodes(Node node)
    {
        if (node.moveCount > status.moveForce)
            return;

        Node existNode;
        if (moveNodes.TryGetValue(node.pos, out existNode))
        {
            if (existNode.moveCount < node.moveCount)
                return;
            existNode.parent = node.parent;
            existNode.moveCount = node.moveCount;
        }
        else
            moveNodes.Add(node.pos, node);

        for (int i = 0; i < dirPositions.Length; ++i)
        {
            Vector3 cellSize = GameManager.mInstance.grid.cellSize * 2;
            Vector3 cellPos = new Vector3(dirPositions[i].x * cellSize.x, dirPositions[i].y * cellSize.y, 0.0f);
            Vector3 worldPos = cellPos + GameManager.mInstance.grid.CellToWorld(node.pos);
            Vector3Int worldPos2Cell = GameManager.mInstance.grid.WorldToCell(worldPos);
            TileInst tileInst = GameManager.mInstance.GetTileInst(worldPos2Cell);
            if (tileInst != null && tileInst.tile != null)
            {
                if (tileInst.unit == null || tileInst.unit.ownerUser != ownerUser)
                   FindUnitMoveNodes(new Node { moveCount = node.moveCount + 1, tile = tileInst, pos = worldPos2Cell, parent = node });
            }
        }
    }

    /*
        유닛의 시야를 업데이트 합니다.
    */
    public void SightUpdate()
    {
        for (int i = 0; i < dirPositions.Length; ++i)
        {
            Vector3 cellSize = GameManager.mInstance.grid.cellSize * 2;
            Vector3 cellPos = new Vector3(dirPositions[i].x * cellSize.x, dirPositions[i].y * cellSize.y, 0.0f);
            Vector3 worldPos = cellPos + GameManager.mInstance.grid.CellToWorld(curPos);
            Vector3Int worldPos2Cell = GameManager.mInstance.grid.WorldToCell(worldPos);
            TileInst tileInst = GameManager.mInstance.GetTileInst(worldPos2Cell);
            if (tileInst.unit)
                tileInst.unit.sprRenderer.enabled = true;
        }
    }

    /*
        특정 위치가 유닛의 시야의 범위인 지 확인합니다.
    */
    public bool ContainSightRange(Vector3Int pos)
    {
        for (int i = 0; i < dirPositions.Length; ++i)
        {
            Vector3 cellSize = GameManager.mInstance.grid.cellSize * 2;
            Vector3 cellPos = new Vector3(dirPositions[i].x * cellSize.x, dirPositions[i].y * cellSize.y, 0.0f);
            Vector3 worldPos = cellPos + GameManager.mInstance.grid.CellToWorld(curPos);
            Vector3Int worldPos2Cell = GameManager.mInstance.grid.WorldToCell(worldPos);
            if (worldPos2Cell == pos)
                return true;
        }
        return false;
    }

    /*
        유닛의 주둔을 활성화/비활성화 합니다.
    */
    public void OnBeStation()
    {
        if (status.beStationed)
            status.beStationed = false;
        else
        {
           status.beStationed = true;
           CancelMove();
        }
    }

    /*
        유닛을 선택했을 때
    */
    public void OnTargetClicked()
    {
        CancelMove();
    }

    /*
        해당 유닛의 차례입니다.
    */
    public override void BeginStep()
    {
        base.BeginStep();
        ownerUser.SightUpdate();
    }

    /*
        유닛 선택이 해제됐을 때
    */
    public void OnTargetTerminate()
    {
        if (moveNodes == null)
            return;

        foreach (KeyValuePair<Vector3Int, Node> node in moveNodes)
        {
            GameManager.mInstance.tileMap.SetTileFlags(node.Value.pos, TileFlags.None);
            GameManager.mInstance.tileMap.SetColor(node.Value.pos, Color.white);
        }
    }

    /*
        유닛 선택이 되었을 때 셀을 클릭하면 ..
    */
    public void OnCellClicked(Vector3Int pos)
    {
        if (moveNodes == null)
            return;

        if (moveNodes.TryGetValue(pos, out moveNode))
            ownerUser.CancelSelUnit();
    }

    private void Awake()
    {
        sprRenderer = GetComponent<SpriteRenderer>();
    }

    protected override void Start()
    {
        base.Start();
        OnRefreshUnit(GameManager.mInstance.grid.WorldToCell(transform.position));
    }

    /*
        유닛을 dirPosition 으로 밀어냅니다.
    */
    private bool Knockback(Vector3 dirPosition)
    {
        var cellPos = GameManager.mInstance.tileMap.WorldToCell(dirPosition);
        var tile = GameManager.mInstance.GetTileInst(curPos + cellPos);
        if (tile == null || tile.tile == null || tile.unit != null)
            return false;

        moveNode = null;
        moveNodes = null;
        OnRefreshUnit(curPos + cellPos);
        return true;
    }

    /*
        특정 유닛과 충돌검사를 하고 해당 셀을 뺏을 수 있는 지 확인합니다.
    */
    private bool OnTakeAwayCell(Unit existUnit)
    {
        bool sameForce = status.shockForceMax == existUnit.status.shockForceMax;
        sameForce &= status.shockForceMax == status.shockForceMin;
        sameForce &= status.shockForceMax == existUnit.status.shockForceMin;
        if (sameForce)
        {
            Debug.LogError(String.Format("{0} {1} 유닛의 충격력이 완벽하게 동일하여 승자를 구분할 수 없습니다.", name, existUnit.name));
            return false;
        }

        int sf1 = 0, sf2 = 0;
        while (sf1 == sf2)
        {
            sf1 = UnityEngine.Random.Range(status.shockForceMin, status.shockForceMax);
            sf2 = UnityEngine.Random.Range(existUnit.status.shockForceMin, existUnit.status.shockForceMax);
        }
        int attack1 = status.attack, attack2 = existUnit.status.attack;
        if (status.beStationed)
        {
            sf1 += 1;
            attack1 += 5;
        }
        if (status.morale <= 8)
        {
            sf1 -= 1;
            attack1 -= 3;
        }

        if (existUnit.status.beStationed)
        {
            sf2 += 1;
            attack2 += 5;
        }
        if (existUnit.status.morale <= 8)
        {
            sf2 -= 1;
            attack2 -= 3;
        }

        bool canTakeAwayCell = false;
        if (sf1 > sf2)
        {
            status.morale -= 2;
            existUnit.status.morale -= 5;

            Vector3 dirPosition = GameManager.mInstance.grid.CellToWorld(moveNode.pos) - GameManager.mInstance.grid.CellToWorld(curPos);
            canTakeAwayCell = existUnit.Knockback(dirPosition);
        }
        else
        {
            existUnit.status.morale -= 2;
            status.morale -= 5;
        }

        status.hp -= attack2 * sf2;
        existUnit.status.hp -= attack1 * sf1;
        return canTakeAwayCell;
    }

    /*
        이동, 타일 등의 정보를 업데이트합니다.
    */
    private void OnRefreshUnit(Vector3Int pos)
    {
        TileInst moveTile = GameManager.mInstance.GetTileInst(pos);
        if (moveTile != null && moveTile.unit != null) 
        {
            // 동일한 유닛이 해당 타일 위에 있을 경우 이동을 멈춥니다.
            if (moveTile.unit.ownerUser == ownerUser)
            {
                CancelMove();
                return;
            }
            else
            {
                if (OnTakeAwayCell(moveTile.unit) == false)
                {
                    CancelMove();
                    return;
                }
            }
        }
        if (curTile != null)
        {
            curTile.unit = null;
        }
        curTile = moveTile;
        curTile.unit = this;
        curPos = pos;
        transform.position = GameManager.mInstance.tileMap.CellToWorld(curPos);
        SightUpdate();
    }

    /*
        이동 경로를 찾습니다.
    */
    public void UpdateMovePath()
    {
        moveNodes = new Dictionary<Vector3Int, Node>();
        FindUnitMoveNodes(new Node { moveCount = 0, pos = curPos, tile = curTile, parent = null });
        foreach (KeyValuePair<Vector3Int, Node> node in moveNodes)
        {
            GameManager.mInstance.tileMap.SetTileFlags(node.Value.pos, TileFlags.None);
            GameManager.mInstance.tileMap.SetColor(node.Value.pos, Color.green);
        }
    }

    /*
        이동중이던 상태를 취소합니다.
    */
    private void CancelMove()
    {
        cancelMove = true;
        moveNode = null;
        moveNodes = null;
        if (moveCoroutine != null)
            StopCoroutine(moveCoroutine);
        transform.position = GameManager.mInstance.tileMap.CellToWorld(curPos);
    }

    /*
        한 칸씩 이동 명령을 내리기 위한 코루틴 함수
    */
    IEnumerator Move()
    {
        Stack<Node> movePath = new Stack<Node>();
        while (moveNode != null)
        {
            movePath.Push(moveNode);
            moveNode = moveNode.parent;
        }
        if (movePath.Count > 0)
        {
            movePath.Pop(); // 시작 경로는 제거합니다.
        }
        while (movePath.Count > 0)
        {
            if (cancelMove)
                break;
            moveNode = movePath.Pop();
            moveDeltaTime = 0.0f;
            yield return new WaitForSeconds(moveTime);
            OnRefreshUnit(moveNode.pos);
        }
        moveNode = null;
    }

    private void Update()
    {
        // this is my step!
        if (IsMyStep())
        {
            if (moveCoroutine == null)
            {
                cancelMove = false;
                moveCoroutine = StartCoroutine("Move");
            }

            if (moveNode != null)
            {
                moveDeltaTime += Time.deltaTime;
                transform.position = Vector3.Lerp(transform.position, GameManager.mInstance.grid.CellToWorld(moveNode.pos), moveDeltaTime / moveTime);
            }
            else
            {
                moveCoroutine = null;
                CancelMove();
                CommitStep();
                return;
            }
        }

        /*
            유닛이 사망했는 지 확인합니다.
        */
        if (status.hp <= 0 || status.morale <= 0)
        {
            if (curTile != null) curTile.unit = null;
            reservationRemove = true;
            gameObject.SetActive(false);
            ownerUser.DelUnit(this);
            if (IsMyStep())
                CommitStep();
            return;
        }
    }
}
