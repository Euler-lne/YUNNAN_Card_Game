using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
    private TableManager tableManager;

    private UIManager uiManager;
    private Player player;
    private DealManager dealManager;

    private GameCore gameCore = null;

    public override void _Ready()
    {
        uiManager = GetNode<UIManager>("../CanvasLayer/UIManager");
        dealManager = GetNode<DealManager>("../DealManager");
        player = GetNode<Player>("../Player");
        // 所有客户端都初始化 DealManager
        gameCore = Multiplayer.IsServer() ? new GameCore() : null;
        dealManager.Init(gameCore);

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

        // TODO:游戏开始之前为每一个客户端设置对应的头像和名字
        gameCore.StartGame();
        // 开始发牌
        dealManager.StartDeal();
        // 开始回合
    }

    public override void _ExitTree()
    {
        if (Multiplayer.IsServer())
        {
            NetworkManager.Instance.OnTotalPlayersChanged -= uiManager.UpdatePlayerCount;
        }
    }
}