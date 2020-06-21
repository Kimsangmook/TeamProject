using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class User : GameManagerFlowHandler
{
    public int money = 3500;
    public int turnMoney = 60;
    public Color sigColor;
    private List<Unit> units = new List<Unit>();
    private List<Unit> addUnits = new List<Unit>();
    private Unit selUnit = null;
    private Unit buyUnit = null;
    private Vector3 prevMousePosition;
    private Vector3 curMousePosition;
    private int turnOfNoOrderUnit;

    protected override void Start()
    {
        base.Start();
        stepHandleByButton = true;
    }

    public override void BeginStep()
    {
        base.BeginStep();
        money += turnMoney;
        GameManager.mInstance.uiRoot.SetActive(true);
        GameManager.mInstance.shopSelector.SetUser(this);
        SightUpdate();
        
        if (++turnOfNoOrderUnit > 2)
        {
            Debug.Log(gameObject.name + " 유저가 패배하였습니다.");
            GameManager.mInstance.QuitGame();
        }
        foreach (var unit in units)
        {
            if (unit.status.orderUnit)
                turnOfNoOrderUnit = 0;
        }
    }

    public void SightUpdate()
    {
        foreach (var tile in GameManager.mInstance.tileInsts)
        {
            if (tile.Value.unit)
            {
                tile.Value.unit.sprRenderer.enabled = false;
            }
        }
        foreach (var tile in GameManager.mInstance.tileInsts)
        {
            if (tile.Value.unit)
            {
                if (tile.Value.unit.ownerUser == this)
                {
                    tile.Value.unit.sprRenderer.enabled = true;
                    tile.Value.unit.SightUpdate();
                }
            }
        }
    }

    public override void EndStep()
    {
        base.EndStep();
        CancelSelUnit();
        GameManager.mInstance.shopSelector.SetUser(null);
        GameManager.mInstance.uiRoot.SetActive(false);
        units.AddRange(addUnits);
        addUnits.Clear();
    }

    public void DelUnit(Unit unit)
    {
        units.Remove(unit);
    }

    public void CancelSelUnit()
    {
        if (selUnit) selUnit.OnTargetTerminate();
        GameManager.mInstance.unitSelector.SetUnit(null);
        selUnit = null;
    }

    public bool BuyUnit(Unit unit)
    {
        if (money < unit.status.price) 
            return false;

        GameManager.mInstance.uiRoot.SetActive(false);
        CancelSelUnit();
        buyUnit = unit;
        return true;
    }

    public void CancelBuyUnit()
    {
        GameManager.mInstance.uiRoot.SetActive(true);
        GameManager.mInstance.tileMap.SetColor(GameManager.mInstance.grid.WorldToCell(prevMousePosition), Color.white);
        buyUnit = null;
    }

    private void Update()
    {
        if (IsMyStep())
        {
            prevMousePosition = curMousePosition;
            curMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            if (Input.GetMouseButtonDown(0))
            {
                var raycastHit2D = Physics2D.Raycast(curMousePosition, transform.forward, 100.0f);
                if (raycastHit2D)
                {
                    var unit = raycastHit2D.transform.GetComponent<Unit>();
                    if (selUnit != unit && unit.ownerUser == this && addUnits.Find(x => x == unit) == null)
                    {
                        if (selUnit) selUnit.OnTargetTerminate();
                        selUnit = unit;
                        selUnit.OnTargetClicked();
                        CancelBuyUnit();
                        GameManager.mInstance.unitSelector.SetUnit(selUnit);
                        return;
                    }
                }

                if (selUnit != null)
                {
                    selUnit.OnCellClicked(GameManager.mInstance.grid.WorldToCell(curMousePosition));
                    return;
                }
            }

            if (buyUnit != null)
            {
                /*
                    유닛을 구매 할 때 타일의 색상을 변경합니다.
                */
                GameManager.mInstance.tileMap.SetColor(GameManager.mInstance.grid.WorldToCell(prevMousePosition), Color.white);
                var cellPos = GameManager.mInstance.grid.WorldToCell(curMousePosition);
                GameManager.mInstance.tileMap.SetTileFlags(cellPos, UnityEngine.Tilemaps.TileFlags.None);
                GameManager.mInstance.tileMap.SetColor(cellPos, Color.green);

                if (Input.GetMouseButtonDown(1))
                {
                    CancelBuyUnit();
                    return;
                }

                var tileInst = GameManager.mInstance.GetTileInst(cellPos);
                if (tileInst != null && tileInst.tile != null && tileInst.unit == null)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        /*
                            지휘 유닛 일 경우 조건에 제약 받지 않고 소환할 수 있습니다.
                            이외 유닛일 경우 지휘 옆 타일인지 확인해야 합니다.
                        */
                        bool canSpawn = buyUnit.status.orderUnit;
                        if (!canSpawn)
                        {
                            foreach (var myUnit in units)
                            {
                                if (myUnit.status.orderUnit && myUnit.ContainSightRange(cellPos))
                                {
                                    canSpawn = true;
                                    break;
                                }
                            }
                        }
                        if (!canSpawn)
                        {
                            return;
                        }
                        money -= buyUnit.status.price;
                        var unitInst = GameObject.Instantiate<Unit>(buyUnit, curMousePosition, Quaternion.identity);
                        unitInst.ownerUser = this;
                        unitInst.sprRenderer.color = sigColor;
                        addUnits.Add(unitInst);
                        CancelBuyUnit();
                        SightUpdate();
                    }
                    return;
                }
            }
        }
    }
}
