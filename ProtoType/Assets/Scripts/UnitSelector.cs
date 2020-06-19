using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitSelector : MonoBehaviour
{
    private Unit curUnit;

    private Text stat;
    private Text station;
    private Button move;

    public void Start()
    {
        stat = transform.Find("Stat").GetComponent<Text>();
        station = transform.Find("Station/Text").GetComponent<Text>();
        move = transform.Find("Move").GetComponent<Button>();
        Close(false);
    }

    public Unit GetUnit()
    {
        return curUnit;
    }

    public void SetUnit(Unit unit)
    {
        curUnit = unit;
        if (curUnit == null)
        {
            Close(false);
            return;
        }

        transform.position = new Vector3(curUnit.transform.position.x, curUnit.transform.position.y, transform.position.z);    
        stat.text = string.Format("이동력 : {0}\n충격력 : {1}~{2}\n체력 : {3}\n공격력 : {4}\n주둔중 : {5}\n사기 : {6}",
            unit.status.moveForce, 
            unit.status.shockForceMin + (unit.status.beStationed ? 1 : 0), 
            unit.status.shockForceMax + (unit.status.beStationed ? 1 : 0), 
            unit.status.hp, 
            unit.status.attack + (unit.status.beStationed ? 5 : 0), 
            unit.status.beStationed, 
            unit.status.morale);
        station.text = unit.status.beStationed ? "주둔해제" : "주둔";
        move.enabled = !unit.status.beStationed;
        gameObject.SetActive(true);
    }

    public void Close(bool selCancel)
    {
        if (curUnit)
        {
            if (selCancel)
                curUnit.ownerUser.CancelSelUnit();
            curUnit = null;
        }
        gameObject.SetActive(false);
    }

    public void OnMove()
    {
        if (curUnit.status.morale <= 8)
        {
            Debug.Log("사기가 부족하여 이동할 수 없습니다.");
            Close(true);
        }
        else
        {
            curUnit.UpdateMovePath();
            Close(false);
        }
    }

    public void OnStation()
    {
        curUnit.OnBeStation();
        Close(true);
    }
}
