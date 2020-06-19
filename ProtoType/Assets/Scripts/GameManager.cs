using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public abstract class GameManagerFlowHandler : MonoBehaviour
{
    [System.NonSerialized] public bool stepHandleByButton = false;
    [System.NonSerialized] public bool reservationRemove = false;

    protected virtual void Start()
    {
        GameManager.mInstance.AddHandler(this);
    }

    public bool IsMyStep()
    {
        return this == GameManager.mInstance.GetCurHandler();
    }

    public virtual void BeginStep()
    {
    }

    public virtual void EndStep()
    {
    }

    protected void CommitStep()
    {
        GameManager.mInstance.GotoNextStep();
    }
}

public class TileInst
{
    public GameTile tile = null;
    public Unit unit = null;
}

public class GameManager : MonoBehaviour
{
    public static GameManager mInstance;

    public Grid grid;
    public Tilemap tileMap;
    [NonSerialized] public Dictionary<Vector3Int, TileInst> tileInsts = new Dictionary<Vector3Int, TileInst>();

    public GameObject uiRoot = null;
    public UnitSelector unitSelector = null;
    public ShopSelector shopSelector = null;

    [NonSerialized] private int handlerStep = 0;
    private List<GameManagerFlowHandler> handlers = new List<GameManagerFlowHandler>();

    private void Awake()
    {
        if (mInstance)
            QuitGame("::gameManager duplicate.");

        if (!grid)
            QuitGame("::gameManager need child grid.");

        if (!tileMap)
            QuitGame("::gameManager need child grid/tileMap.");

        if (unitSelector == null)
            QuitGame("::gameManager need unitSelector.");

        if (shopSelector == null)
            QuitGame("::gameManager need shopSelector.");

        for (int y = tileMap.cellBounds.yMin; y < tileMap.cellBounds.yMax; ++y)
        {
            for (int x = tileMap.cellBounds.xMin; x < tileMap.cellBounds.xMax; ++x)
            {
                var tileInst = new TileInst();
                tileInst.tile = tileMap.GetTile(new Vector3Int(x, y, 0)) as GameTile;
                tileInsts.Add(new Vector3Int(x, y, 0), tileInst);
            }
        }

        mInstance = this;
    }

    private void Update()
    {
    }

    public void AddHandler(GameManagerFlowHandler handler)
    {
        handlers.Add(handler);
        if (handlers.Count == 1) handlers[0].BeginStep();
    }

    public GameManagerFlowHandler GetCurHandler()
    {
        return handlers[handlerStep];
    }

    public TileInst GetTileInst(Vector3Int pos)
    {
        TileInst ret;
        if (tileInsts.TryGetValue(pos, out ret))
            return ret;
        return null;
    }

    public void GotoNextStep()
    {
        if (handlers.Count == 0)
        {
            Debug.Log("no exist step handler.");
            return;
        }

        handlers[handlerStep].EndStep();

        if (++handlerStep >= handlers.Count)
            handlerStep = 0;

        while (handlers[handlerStep].reservationRemove)
        {
            if (handlers.Count == 0) break;
            if (handlerStep >= handlers.Count) handlerStep = 0;

            GameObject.Destroy(handlers[handlerStep].gameObject);
            handlers.Remove(handlers[handlerStep]);
        }

        handlers[handlerStep].BeginStep();
    }

    public void QuitGame(string errorLog = "")
    {
        if (errorLog != string.Empty)
            Debug.LogError(errorLog);

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
