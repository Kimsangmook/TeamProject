using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopSelector : MonoBehaviour
{
    private User curUser;

    private Text moneyText;
    private Text userText;

    private void Awake()
    {
        moneyText = transform.Find("Money").GetComponent<Text>();
        userText = transform.Find("User").GetComponent<Text>();
    }

    private void Update()
    {
        if (curUser == null) return;

        moneyText.text = string.Format("현재 금액 : {0}", curUser.money);
        userText.text = curUser.gameObject.name;
    }

    public void SetUser(User user)
    {
        curUser = user;
        if (curUser == null)
        {
           gameObject.SetActive(false);
           return;
        }
        gameObject.SetActive(true);
    }

    public void BuyUnit(Unit unit)
    {
        if (curUser)
        {
            if (!curUser.BuyUnit(unit))
                Debug.Log("돈이 부족하여 유닛을 구매할 수 없습니다.");
        }
    }

    public void Close()
    {
        curUser = null;
        gameObject.SetActive(false);
    }
}
