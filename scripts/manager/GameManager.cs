using Euler.Event;
using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
    private UIManager uiManager;
    private Player player;
    private DealManager dealManager;
    private TurnManager turnManager;
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
            turnManager = new();
            turnManager.Init(GetNode<TurnRequest>("../TurnRequest"), gameCore.GetDealerSeat());
            NetworkManager.Instance.OnTotalPlayersChanged += uiManager.UpdatePlayerCount;
            dealManager.DealEndEvent += OnDealEndEvent;
        }
    }
    public override void _ExitTree()
    {
        if (Multiplayer.IsServer())
        {
            dealManager.DealEndEvent -= OnDealEndEvent;
            NetworkManager.Instance.OnTotalPlayersChanged -= uiManager.UpdatePlayerCount;
        }
    }


    private void StartGame()
    {
        if (!Multiplayer.IsServer())
            return;

        gameCore.StartGame();
        // 开始发牌
        dealManager.StartDeal();
        // 开始回合
    }

    private void OnDealEndEvent()
    {
        if (!Multiplayer.IsServer())
            return;
        GD.Print("回合开始");
        turnManager.StartTurn(gameCore.GetDealerSeat());
    }
}