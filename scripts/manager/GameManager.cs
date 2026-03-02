using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
    private TableManager tableManager;

    private UIManager uiManager;

    public override void _Ready()
    {
        uiManager = GetNode<UIManager>("../CanvasLayer/UIManager");
        TableManager tableManager = GetNode<TableManager>("../TableRoot/TableManager");

        // 所有客户端都初始化 DealManager
        DealManager.Instance.Init(Multiplayer.IsServer() ? new GameCore() : null, tableManager);

        if (Multiplayer.IsServer())
        {
            NetworkManager.Instance.AssignServerSeat();

            uiManager.UpdatePlayerCount(NetworkManager.Instance.TotalPlayers);
            uiManager.ConnectStartButtonPressed(StartGame);
            NetworkManager.Instance.OnTotalPlayersChanged += uiManager.UpdatePlayerCount;
        }
    }

    public void StartGame()
    {
        if (!Multiplayer.IsServer())
            return;

        // 服务器初始化游戏数据
        GameCore core = DealManager.Instance.GameCore; // 或直接传给 DealManager
        core.StartGame();
        // 开始发牌
        DealManager.Instance.StartDeal();
    }

    public override void _ExitTree()
    {
        if (Multiplayer.IsServer())
        {
            NetworkManager.Instance.OnTotalPlayersChanged -= uiManager.UpdatePlayerCount;
        }
    }
}